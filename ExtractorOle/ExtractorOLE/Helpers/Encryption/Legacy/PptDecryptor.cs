using System.Buffers.Binary;
using System.Security.Cryptography;
using ExtractorOLE.Exceptions;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.Crypt.CryptoAPI;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

internal sealed record DecryptedPptCore(byte[] CurrentUser, byte[] PowerPointDocument, CryptoAPIDecryptor Decryptor);

// [ydk:req:extraction/legacy-decryption] [ydk:req:extraction/password-decryption]
// Decrypts a RC4 CryptoAPI .ppt (MS-PPT 2.3.7): every persist object except the CryptSession10Container
// is one continuous RC4 run keyed with block = persist id over its 8-byte record header and recLen
// body (no 512-byte re-keying). UserEditAtoms, PersistDirectoryAtoms and the CryptSession10Container
// stay as they are, so offsets never move; only the Current User token changes to the plain value.
internal static class PptDecryptor
{
    private const int RecordHeaderSize = 8;
    private const int OffsetHeaderToken = 12;
    private const int CancellationStride = 64;

    // Throws InvalidPasswordException when the verifier rejects password, before anything is decrypted.
    // The NPOI decryptor holds the derived key in NPOI-internal objects (ISecretKey, Cipher) this code
    // cannot zero; see ADR-013 Consequences ("More crypto-adjacent code we own") and ADR-012.
    internal static DecryptedPptCore DecryptCore(
        PptEncryptionLayout layout, LegacyEncryptionHeader header, string password, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(header);

        if (LegacyEncryptionHeaderReader.CreateDecryptor(header) is not CryptoAPIDecryptor decryptor)
        {
            throw new CorruptFileException(detectedMimeType, "encrypted presentation: encryption header is not RC4 CryptoAPI");
        }

        if (!LegacyEncryptionHeaderReader.VerifyPassword(decryptor, password))
        {
            throw new InvalidPasswordException();
        }

        if (layout.CurrentUser.Length < OffsetHeaderToken + 4)
        {
            throw new CorruptFileException(detectedMimeType, "encrypted presentation: Current User stream is truncated");
        }

        var objects = ValidatePersistMap(layout, detectedMimeType);
        var document = (byte[])layout.PowerPointDocument.Clone();
        try
        {
            DecryptPersistObjects(document, objects, decryptor, detectedMimeType);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(document);
            throw;
        }

        var currentUser = (byte[])layout.CurrentUser.Clone();
        if (BinaryPrimitives.ReadUInt32LittleEndian(currentUser.AsSpan(OffsetHeaderToken)) == PptEncryptionProbe.TokenEncrypted)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(currentUser.AsSpan(OffsetHeaderToken), PptEncryptionProbe.TokenPlain);
        }

        return new DecryptedPptCore(currentUser, document, decryptor);
    }

    // [ydk:error:extraction/corrupt-file] Every persist object header must lie inside the stream and no
    // two ids may share an offset; returns the objects to decrypt, ordered by offset.
    private static List<KeyValuePair<uint, uint>> ValidatePersistMap(PptEncryptionLayout layout, string detectedMimeType)
    {
        var length = layout.PowerPointDocument.Length;
        var offsets = new HashSet<uint>();
        foreach (var (id, offset) in layout.PersistOffsets)
        {
            if (offset + (long)RecordHeaderSize > length)
            {
                throw new CorruptFileException(detectedMimeType, $"encrypted presentation: persist object {id} lies past the PowerPoint Document stream");
            }

            if (!offsets.Add(offset))
            {
                throw new CorruptFileException(detectedMimeType, $"encrypted presentation: persist object {id} shares its offset with another object");
            }
        }

        return layout.PersistOffsets
            .Where(entry => entry.Key != layout.CryptSessionPersistId)
            .OrderBy(entry => entry.Value)
            .ToList();
    }

    private static void DecryptPersistObjects(
        byte[] document, List<KeyValuePair<uint, uint>> objects, CryptoAPIDecryptor decryptor, string detectedMimeType)
    {
        Cipher? cipher = null;
        for (var i = 0; i < objects.Count; i++)
        {
            if (i % CancellationStride == 0)
            {
                ExtractionCancellation.ThrowIfCancellationRequested();
            }

            var (id, offset) = objects[i];
            var start = (int)offset;
            cipher = decryptor.InitCipherForBlock(cipher, (int)id);
            cipher.Update(document, start, RecordHeaderSize, document, start);

            var recLen = BinaryPrimitives.ReadUInt32LittleEndian(document.AsSpan(start + 4));
            if (start + (long)RecordHeaderSize + recLen > document.Length)
            {
                throw new CorruptFileException(detectedMimeType, $"encrypted presentation: persist object {id} runs past the PowerPoint Document stream");
            }

            cipher.Update(document, start + RecordHeaderSize, (int)recLen, document, start + RecordHeaderSize);
        }
    }
}
