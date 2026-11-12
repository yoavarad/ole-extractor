using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using NPOI.POIFS.Crypt;
using NPOI.Util;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

internal enum LegacyContainer { Doc, Ppt }

internal enum LegacyCipherScheme { BinaryRc4, CryptoApiRc4 }

internal sealed record LegacyEncryptionHeader(
    LegacyCipherScheme Scheme,
    ushort VersionMajor,
    ushort VersionMinor,
    uint Flags,
    int KeySizeBits,
    int HeaderSize,
    byte[] Payload);

// [ydk:req:extraction/legacy-encryption-header-bounds] The one place that validates an MS-OFFCRYPTO
// EncryptionInfo blob of a legacy .doc / .ppt (RC4 or RC4 CryptoAPI) before NPOI sees it.
internal static class LegacyEncryptionHeaderReader
{
    internal const uint FlagCryptoApi = 0x04;
    internal const uint FlagDocProps = 0x08;
    internal const uint FlagExternal = 0x10;
    internal const uint FlagAes = 0x20;
    internal const int MaxPasswordLength = 255;

    // The header alone does not say which container it came from.
    private const string ContainerMimeType = "application/x-ole-storage";
    private const int BinaryRc4Length = 52;
    private const int HeaderStart = 12;
    private const int MinHeaderSize = 32;
    private const int VerifierLength = 60;
    private const int RequiredSaltSize = 16;
    private const int RequiredVerifierHashSize = 20;
    private const uint AlgIdRc4 = 0x6801;
    private const uint AlgIdHashSha1 = 0x8004;
    private const int DefaultKeySizeBits = 40;
    private const int MaxKeySizeBits = 128;

    internal static LegacyEncryptionHeader Parse(ReadOnlySpan<byte> payload, LegacyContainer container, string detectedMimeType)
    {
        if (payload.Length < 4)
        {
            throw Corrupt(detectedMimeType, "legacy encryption header: EncryptionVersionInfo is truncated");
        }

        var major = BinaryPrimitives.ReadUInt16LittleEndian(payload);
        var minor = BinaryPrimitives.ReadUInt16LittleEndian(payload[2..]);

        if (major == 1 && minor == 1)
        {
            return ParseBinaryRc4(payload, container, detectedMimeType);
        }

        if (major is < 2 or > 4 || minor != 2)
        {
            throw Unsupported(container, detectedMimeType, $"EncryptionVersionInfo {major}.{minor} is not supported");
        }

        return ParseCryptoApi(payload, container, detectedMimeType, major, minor);
    }

    internal static Decryptor CreateDecryptor(LegacyEncryptionHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);
        try
        {
            var info = new EncryptionInfo(new LittleEndianInputStream(new ByteArrayInputStream(header.Payload)), null);
            return info.Decryptor;
        }
        catch (Exception ex) when (ParseFailure.IsParseFailure(ex))
        {
            throw new CorruptFileException(
                ContainerMimeType,
                $"legacy encryption header rejected by NPOI ({ex.GetType().Name})",
                ex);
        }
    }

    internal static bool VerifyPassword(Decryptor decryptor, string password)
    {
        ArgumentNullException.ThrowIfNull(decryptor);
        ArgumentException.ThrowIfNullOrEmpty(password);
        return decryptor.VerifyPassword(password.Length > MaxPasswordLength ? password[..MaxPasswordLength] : password);
    }

    private static LegacyEncryptionHeader ParseBinaryRc4(ReadOnlySpan<byte> payload, LegacyContainer container, string mime)
    {
        if (container == LegacyContainer.Ppt)
        {
            throw Corrupt(mime, "legacy encryption header: version 1.1 is not valid in a PowerPoint document");
        }

        if (payload.Length < BinaryRc4Length)
        {
            throw Corrupt(mime, "legacy encryption header: BinaryRC4 header is truncated");
        }

        return new LegacyEncryptionHeader(
            LegacyCipherScheme.BinaryRc4, 1, 1, 0, DefaultKeySizeBits, 0, payload[..BinaryRc4Length].ToArray());
    }

    private static LegacyEncryptionHeader ParseCryptoApi(
        ReadOnlySpan<byte> payload, LegacyContainer container, string mime, ushort major, ushort minor)
    {
        if (payload.Length < HeaderStart)
        {
            throw Corrupt(mime, "legacy encryption header: header fields are truncated");
        }

        var flags = BinaryPrimitives.ReadUInt32LittleEndian(payload[4..]);
        var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(payload[8..]);

        if (headerSize < MinHeaderSize || HeaderStart + (ulong)headerSize + VerifierLength > (ulong)payload.Length)
        {
            throw Corrupt(mime, "legacy encryption header: headerSize is out of range");
        }

        if ((flags & FlagCryptoApi) == 0)
        {
            throw Corrupt(mime, "legacy encryption header: fCryptoAPI is not set");
        }

        var algId = BinaryPrimitives.ReadUInt32LittleEndian(payload[(HeaderStart + 8)..]);
        var algIdHash = BinaryPrimitives.ReadUInt32LittleEndian(payload[(HeaderStart + 12)..]);
        var unsupported =
            (flags & FlagExternal) != 0 ? "fExternal is set"
            : (flags & FlagAes) != 0 ? "fAES is set"
            : algId != AlgIdRc4 ? "AlgID is not RC4"
            : algIdHash is not (AlgIdHashSha1 or 0) ? "AlgIDHash is not SHA-1"
            : null;
        if (unsupported is not null)
        {
            throw Unsupported(container, mime, unsupported);
        }

        var keySize = BinaryPrimitives.ReadUInt32LittleEndian(payload[(HeaderStart + 16)..]);
        if (keySize == 0)
        {
            keySize = DefaultKeySizeBits;
        }

        if (keySize is < DefaultKeySizeBits or > MaxKeySizeBits || keySize % 8 != 0)
        {
            throw Corrupt(mime, $"legacy encryption header: keySize {keySize} is not 40..128 step 8");
        }

        var size = (int)headerSize;
        var verifier = HeaderStart + size;
        if (BinaryPrimitives.ReadUInt32LittleEndian(payload[verifier..]) != RequiredSaltSize)
        {
            throw Corrupt(mime, "legacy encryption header: saltSize is not 16");
        }

        if (BinaryPrimitives.ReadUInt32LittleEndian(payload[(verifier + 4 + 16 + 16)..]) != RequiredVerifierHashSize)
        {
            throw Corrupt(mime, "legacy encryption header: verifierHashSize is not 20");
        }

        return new LegacyEncryptionHeader(
            LegacyCipherScheme.CryptoApiRc4, major, minor, flags, (int)keySize, size,
            payload[..(verifier + VerifierLength)].ToArray());
    }

    private static Exception Unsupported(LegacyContainer container, string mime, string reason) =>
        container == LegacyContainer.Doc
            ? new PasswordProtectedException()
            : Corrupt(mime, $"legacy encryption header: {reason}");

    private static CorruptFileException Corrupt(string mime, string reason) => new(mime, reason);
}
