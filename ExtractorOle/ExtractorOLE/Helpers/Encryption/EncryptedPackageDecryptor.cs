using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Agile;
using NPOI.POIFS.Crypt;
using NPOI.Util;
using OpenMcdf;

namespace ExtractorOLE.Helpers.Encryption;

// [ydk:req:extraction/password-decryption] The one entry point detection and extraction use to
// decrypt an MS-OFFCRYPTO encrypted OOXML package (a CFB holding EncryptionInfo + EncryptedPackage).
// Routes on our own EncryptionInfo version check (ADR-012): 4.4 -> ported Agile decryptor; 3.2/4.2
// with the Standard flags -> NPOI main's Standard EncryptionInfo/Decryptor; anything else
// (Extensible, IRM, CryptoAPI RC4, unknown) -> PASSWORD_PROTECTED. The declared decrypted size is
// checked against the per-entry cap and the stream length before any decrypted buffer is
// allocated. The password, keys and buffers are never logged or stored
// ([ydk:req:extraction/password-confidentiality]).
internal static class EncryptedPackageDecryptor
{
    internal const string EncryptionInfoStreamName = "EncryptionInfo";
    internal const string EncryptedPackageStreamName = "EncryptedPackage";

    // MS-OFFCRYPTO 2.3.4.4: EncryptedPackage starts with an 8-byte StreamSize (decrypted length).
    private const int StreamSizeFieldLength = 8;
    private const int ChunkLength = 4096;

    // MS-OFFCRYPTO 2.3.4.5/2.3.4.6/2.3.4.7: Standard EncryptionInfo layout.
    private const int VersionHeaderLength = 8;
    private const int FixedHeaderLength = 32;
    private const int SaltSize = 16;
    private const int VerifierHashSize = 20;
    private const int AesEncryptedVerifierHashLength = 32;
    private const int Sha1AlgIdHash = 0x8004;
    private const int FlagCryptoApi = 0x04;
    private const int FlagExternal = 0x10;
    private const int FlagAes = 0x20;

    // Decrypts the package under root and returns it in a new stream positioned at 0, owned by the
    // caller. allocateOutput creates the decrypted buffer for the declared size; it is injectable so
    // tests can prove guards fire before any allocation.
    public static Stream Decrypt(Storage root, string? password, long maxEntryBytes, Func<long, Stream>? allocateOutput = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        allocateOutput ??= static size => new MemoryStream((int)size);

        var info = ReadEncryptionInfo(root);
        var scheme = RouteScheme(info);
        if (string.IsNullOrEmpty(password))
        {
            throw new PasswordProtectedException();
        }

        using var package = OpenRequired(root, EncryptedPackageStreamName);
        return scheme == Scheme.Agile
            ? DecryptAgile(info, password, package, maxEntryBytes, allocateOutput)
            : DecryptStandard(info, password, package, maxEntryBytes, allocateOutput);
    }

    private enum Scheme
    {
        Agile,
        Standard,
    }

    private static Scheme RouteScheme(byte[] info)
    {
        var major = BinaryPrimitives.ReadUInt16LittleEndian(info);
        var minor = BinaryPrimitives.ReadUInt16LittleEndian(info.AsSpan(2));
        var flags = BinaryPrimitives.ReadInt32LittleEndian(info.AsSpan(4));

        if (major == 4 && minor == 4)
        {
            return Scheme.Agile;
        }

        // MS-OFFCRYPTO 2.3.4.5: Standard is version 3.2 or 4.2 with fCryptoAPI and fAES set and
        // fExternal clear. Extensible (x.3), IRM and CryptoAPI RC4 are not decrypted.
        const int standardMask = FlagCryptoApi | FlagAes | FlagExternal;
        if ((major == 3 || major == 4) && minor == 2 && (flags & standardMask) == (FlagCryptoApi | FlagAes))
        {
            return Scheme.Standard;
        }

        throw new PasswordProtectedException();
    }

    // Size guards (ADR-012 decision 6), all before password verification and before any decrypted
    // buffer exists. The ciphertext is padded to the cipher block size, so the stream must hold the
    // declared size rounded up to a whole block.
    private static long ReadDeclaredSize(Stream package, long maxEntryBytes, int blockSize)
    {
        Span<byte> field = stackalloc byte[StreamSizeFieldLength];
        var read = package.ReadAtLeast(field, field.Length, throwOnEndOfStream: false);
        if (read < field.Length)
        {
            throw new TruncatedContainerException(field.Length, read);
        }

        var declaredSize = BinaryPrimitives.ReadInt64LittleEndian(field);
        if (declaredSize < 0)
        {
            throw Corrupt("EncryptedPackage declares a negative size");
        }

        // A MemoryStream cannot hold more than Array.MaxLength bytes, whatever the configured cap.
        var limit = Math.Min(maxEntryBytes, Array.MaxLength);
        if (declaredSize > limit)
        {
            throw new OversizedNestedContentException(
                $"EncryptedPackage declares {declaredSize} bytes decrypted (per-entry limit {limit})");
        }

        var required = (declaredSize + blockSize - 1) / blockSize * blockSize;
        var available = package.Length - StreamSizeFieldLength;
        if (required > available)
        {
            throw new TruncatedContainerException(required + StreamSizeFieldLength, package.Length);
        }

        return declaredSize;
    }

    private static Stream DecryptAgile(byte[] info, string password, Stream package, long maxEntryBytes, Func<long, Stream> allocateOutput)
    {
        using var decryptor = AgileDecryptor.FromDescriptor(
            new MemoryStream(info, VersionHeaderLength, info.Length - VersionHeaderLength, writable: false));
        var declaredSize = ReadDeclaredSize(package, maxEntryBytes, decryptor.Header.BlockSize);
        if (!decryptor.VerifyPassword(password))
        {
            throw new InvalidPasswordException();
        }

        package.Position = 0;
        decryptor.VerifyIntegrity(package);
        package.Position = StreamSizeFieldLength;

        return Fill(allocateOutput(declaredSize), output => decryptor.Decrypt(package, declaredSize, output));
    }

    private static Stream DecryptStandard(byte[] info, string password, Stream package, long maxEntryBytes, Func<long, Stream> allocateOutput)
    {
        ValidateStandardEncryptionInfo(info);

        // NPOI's StandardEncryptionHeader casts its input to InputStream for Mark/Reset, so the bytes
        // must be wrapped this way (a LittleEndianByteArrayInputStream throws InvalidCastException).
        var encryptionInfo = new EncryptionInfo(
            new LittleEndianInputStream(new ByteArrayInputStream(info)), EncryptionMode.Standard);
        var cipherAlgorithm = encryptionInfo.Header.CipherAlgorithm;
        var blockSize = cipherAlgorithm.blockSize;
        var declaredSize = ReadDeclaredSize(package, maxEntryBytes, blockSize);

        var decryptor = encryptionInfo.Decryptor;
        if (!decryptor.VerifyPassword(password))
        {
            throw new InvalidPasswordException();
        }

        var cipher = CryptoFunctions.GetCipher(
            decryptor.GetSecretKey(), cipherAlgorithm, ChainingMode.ecb, null, Cipher.DECRYPT_MODE);

        return Fill(allocateOutput(declaredSize), output =>
        {
            var segment = new byte[ChunkLength];
            var plain = new byte[ChunkLength];
            try
            {
                long remaining = declaredSize;
                long consumed = 0;
                while (remaining > 0)
                {
                    var plainLength = (int)Math.Min(ChunkLength, remaining);
                    var cipherLength = (plainLength + blockSize - 1) / blockSize * blockSize;
                    var read = package.ReadAtLeast(segment.AsSpan(0, cipherLength), cipherLength, throwOnEndOfStream: false);
                    if (read < cipherLength)
                    {
                        throw new TruncatedContainerException(consumed + cipherLength, consumed + read);
                    }

                    cipher.Update(segment, 0, cipherLength, plain);
                    output.Write(plain, 0, plainLength);
                    consumed += cipherLength;
                    remaining -= plainLength;
                }
            }
            finally
            {
                Array.Clear(plain);
            }
        });
    }

    // NPOI ignores headerSize, reads the CSP name until a NUL and does not validate the key size or
    // algorithm, so a damaged header would surface as a wrong password or a generic exception. Every
    // field NPOI reads is checked here first: a corrupt header is CORRUPT_FILE, never INVALID_PASSWORD.
    private static void ValidateStandardEncryptionInfo(byte[] info)
    {
        var span = info.AsSpan();
        if (span.Length < VersionHeaderLength + 4)
        {
            throw Corrupt("Standard EncryptionInfo is shorter than its header size field");
        }

        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(span[VersionHeaderLength..]);
        var headerStart = VersionHeaderLength + 4;
        const int verifierLength = 4 + SaltSize + 16 + 4 + AesEncryptedVerifierHashLength;
        if (headerSize < FixedHeaderLength || (long)headerStart + headerSize + verifierLength > span.Length)
        {
            throw Corrupt("Standard EncryptionInfo headerSize does not fit the stream");
        }

        var header = span.Slice(headerStart, headerSize);
        var algId = BinaryPrimitives.ReadInt32LittleEndian(header[8..]);
        var algIdHash = BinaryPrimitives.ReadInt32LittleEndian(header[12..]);
        var keySize = BinaryPrimitives.ReadInt32LittleEndian(header[16..]);
        var expectedKeySize = algId switch
        {
            0x660E => 128,
            0x660F => 192,
            0x6610 => 256,
            _ => throw Corrupt("Standard EncryptionInfo AlgID is not AES"),
        };
        if (keySize != expectedKeySize)
        {
            throw Corrupt("Standard EncryptionInfo KeySize does not match its AES AlgID");
        }

        if (algIdHash != Sha1AlgIdHash)
        {
            throw Corrupt("Standard EncryptionInfo AlgIDHash is not SHA-1");
        }

        // CSPName: UTF-16LE, NUL-terminated, filling the rest of the header. NPOI treats a leading
        // int32 of 16 as "no CSP name" (it is the verifier's SaltSize), so that must not appear here.
        var cspName = header[FixedHeaderLength..];
        if (!cspName.IsEmpty)
        {
            if (cspName.Length % 2 != 0
                || (cspName.Length >= 4 && BinaryPrimitives.ReadInt32LittleEndian(cspName) == SaltSize)
                || FirstUtf16Nul(cspName) != cspName.Length - 2)
            {
                throw Corrupt("Standard EncryptionInfo CSPName is not terminated at the end of the header");
            }
        }

        var verifier = span[(headerStart + headerSize)..];
        if (BinaryPrimitives.ReadInt32LittleEndian(verifier) != SaltSize)
        {
            throw Corrupt("Standard EncryptionInfo SaltSize is not 16");
        }

        if (BinaryPrimitives.ReadInt32LittleEndian(verifier[(4 + SaltSize + 16)..]) != VerifierHashSize)
        {
            throw Corrupt("Standard EncryptionInfo VerifierHashSize is not 20");
        }
    }

    private static int FirstUtf16Nul(ReadOnlySpan<byte> utf16)
    {
        for (var i = 0; i + 1 < utf16.Length; i += 2)
        {
            if (utf16[i] == 0 && utf16[i + 1] == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static Stream Fill(Stream output, Action<Stream> write)
    {
        try
        {
            write(output);
            output.Position = 0;
            return output;
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private static byte[] ReadEncryptionInfo(Storage root)
    {
        using var stream = OpenRequired(root, EncryptionInfoStreamName);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        if (buffer.Length < VersionHeaderLength)
        {
            throw Corrupt("EncryptionInfo is shorter than its version header");
        }

        return buffer.ToArray();
    }

    private static CfbStream OpenRequired(Storage root, string name) =>
        root.TryOpenStream(name, out var stream)
            ? stream
            : throw Corrupt($"the {name} stream is missing");

    private static CorruptFileException Corrupt(string reason) => AgileDescriptorReader.Corrupt(reason);
}
