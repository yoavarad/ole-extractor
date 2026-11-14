using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ExtractorOLE.Exceptions;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.FileSystem;
using NPOI.Util;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

internal sealed record DecryptedDocStreams(byte[] WordDocument, string TableStreamName, byte[] Table, byte[]? Data);

// [ydk:req:extraction/legacy-decryption] [ydk:req:extraction/password-decryption]
// Decrypts the RC4 / RC4 CryptoAPI streams of a legacy .doc (MS-DOC 2.2.6.2/2.2.6.3): WordDocument
// after its 68-byte FibBase, the table stream after its lKey header, and the whole Data stream. Each
// stream is RC4 in 512-byte blocks keyed per block from stream offset 0, so the clear prefix still
// consumes keystream (NPOI's ChunkedCipherInputStream.ReadPlain). Decryption is length-preserving.
// Also de-obfuscates XOR Method 2 docs (MS-OFFCRYPTO 2.3.7.4-2.3.7.6, MS-DOC 2.2.6.1): WordDocument after
// its 68 bytes, the whole table and Data streams, keyed by stream offset; per 2.3.7.6 a byte stays unchanged
// when 0 or equal to the key byte, so the transform is an involution. The password is accepted if either
// conversion matches lKey: NPOI's per-UTF-16-unit low/high byte rule, or Windows-1252 bytes (ANSI stand-in).
internal static class DocDecryptor
{
    internal const int ChunkSize = 512;

    private const string DocMimeType = "application/msword";
    private const string WordDocumentStream = "WordDocument";
    private const string DataStream = "Data";
    private const int OffFlags = 0x0A;
    private const int OffLKey = 0x0E;
    private const ushort FlagEncrypted = 0x0100;
    private const ushort FlagObfuscated = 0x8000;

    // Throws InvalidPasswordException when the verifier rejects password, before any stream is read.
    // The NPOI decryptor holds the derived key in NPOI-internal objects (ISecretKey, Cipher) this code
    // cannot zero; see ADR-013 Consequences ("More crypto-adjacent code we own") and ADR-012.
    internal static DecryptedDocStreams Decrypt(
        DirectoryNode root, DocFibProbe fib, LegacyEncryptionHeader header, string password, long maxEntryBytes)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(fib);
        ArgumentNullException.ThrowIfNull(header);
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntryBytes);

        var decryptor = LegacyEncryptionHeaderReader.CreateDecryptor(header);
        if (!LegacyEncryptionHeaderReader.VerifyPassword(decryptor, password))
        {
            throw new InvalidPasswordException();
        }

        decryptor.SetChunkSize(ChunkSize);

        var wordDocument = DecryptStream(root, decryptor, WordDocumentStream, DocEncryptionProbe.FibBaseLength, maxEntryBytes)
            ?? throw new CorruptFileException(DocMimeType, "encrypted document: WordDocument stream missing");
        byte[]? table = null;
        try
        {
            table = DecryptStream(root, decryptor, fib.TableStreamName, fib.LKey, maxEntryBytes)
                ?? throw new CorruptFileException(DocMimeType, "encrypted document: table stream missing");
            var data = DecryptStream(root, decryptor, DataStream, 0, maxEntryBytes);
            ClearFibEncryptionMarkers(wordDocument);
            return new DecryptedDocStreams(wordDocument, fib.TableStreamName, table, data);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(wordDocument);
            if (table is not null)
            {
                CryptographicOperations.ZeroMemory(table);
            }

            throw;
        }
    }

    // Throws InvalidPasswordException when neither password conversion matches lKey, before any stream is read.
    internal static DecryptedDocStreams DecryptXor(DirectoryNode root, DocFibProbe fib, string password, long maxEntryBytes)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(fib);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntryBytes);
        if (fib.Kind != DocEncryptionKind.Xor)
        {
            throw new ArgumentException("DecryptXor requires an Xor probe.", nameof(fib));
        }

        var matched = password;
        if (!MatchesXorKey(matched, fib.LKey))
        {
            matched = AnsiPasswordCandidate(password);
            if (!MatchesXorKey(matched, fib.LKey))
            {
                throw new InvalidPasswordException();
            }
        }

        var xorArray = CryptoFunctions.CreateXorArray2(matched);
        byte[]? wordDocument = null;
        byte[]? table = null;
        try
        {
            wordDocument = ReadRawStream(root, WordDocumentStream, DocEncryptionProbe.FibBaseLength, maxEntryBytes)
                ?? throw new CorruptFileException(DocMimeType, "encrypted document: WordDocument stream missing");
            CryptoFunctions.XorTransform2(
                wordDocument, DocEncryptionProbe.FibBaseLength, wordDocument.Length - DocEncryptionProbe.FibBaseLength,
                xorArray, DocEncryptionProbe.FibBaseLength);
            table = ReadRawStream(root, fib.TableStreamName, 0, maxEntryBytes)
                ?? throw new CorruptFileException(DocMimeType, "encrypted document: table stream missing");
            CryptoFunctions.XorTransform2(table, 0, table.Length, xorArray, 0);
            var data = ReadRawStream(root, DataStream, 0, maxEntryBytes);
            if (data is not null)
            {
                CryptoFunctions.XorTransform2(data, 0, data.Length, xorArray, 0);
            }

            ClearFibEncryptionMarkers(wordDocument);
            return new DecryptedDocStreams(wordDocument, fib.TableStreamName, table, data);
        }
        catch
        {
            if (wordDocument is not null)
            {
                CryptographicOperations.ZeroMemory(wordDocument);
            }

            if (table is not null)
            {
                CryptographicOperations.ZeroMemory(table);
            }

            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(xorArray);
        }
    }

    // The uint32 LE at FibBase 0x0E equals CreateXorVerifier2 (high 16 bits XorKey, low 16 verifier), checked on real fixtures.
    private static bool MatchesXorKey(string candidate, uint lKey) =>
        candidate.Length > 0 && unchecked((uint)CryptoFunctions.CreateXorVerifier2(candidate)) == lKey;

    // Windows-1252 stand-in for the LCID/ANSI conversion; one char per byte so NPOI's low-byte rule yields the same bytes.
    private static string AnsiPasswordCandidate(string password)
    {
        var bytes = CodePagesEncodingProvider.Instance.GetEncoding(1252)!.GetBytes(password);
        try
        {
            var length = Math.Min(15, bytes.Length);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = (char)bytes[i];
            }

            return new string(chars);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    // Returns null when the stream does not exist.
    private static byte[]? DecryptStream(
        DirectoryNode root, Decryptor decryptor, string name, long clearPrefix, long maxEntryBytes)
    {
        var cipher = ReadRawStream(root, name, clearPrefix, maxEntryBytes);
        if (cipher is null)
        {
            return null;
        }

        var plain = new byte[cipher.Length];
        try
        {
            using var stream = (ChunkedCipherInputStream)decryptor.GetDataStream(
                new ByteArrayInputStream(cipher), cipher.Length, 0);
            stream.ReadPlain(plain, 0, (int)clearPrefix);
            var offset = (int)clearPrefix;
            while (offset < plain.Length)
            {
                var n = stream.Read(plain, offset, plain.Length - offset);
                if (n <= 0)
                {
                    throw Truncated(name);
                }

                offset += n;
            }
        }
        catch (EndOfStreamException)
        {
            CryptographicOperations.ZeroMemory(plain);
            throw Truncated(name);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plain);
            throw;
        }

        return plain;
    }

    // Returns null when the stream does not exist.
    private static byte[]? ReadRawStream(DirectoryNode root, string name, long clearPrefix, long maxEntryBytes)
    {
        ExtractionCancellation.ThrowIfCancellationRequested();

        if (!root.HasEntry(name))
        {
            return null;
        }

        if (root.GetEntry(name) is not DocumentEntry entry)
        {
            throw new CorruptFileException(DocMimeType, $"encrypted document: {name} is not a stream");
        }

        if (entry.Size > maxEntryBytes)
        {
            throw new OversizedNestedContentException(
                $"{name} stream declares {entry.Size} bytes (per-entry limit {maxEntryBytes})");
        }

        if (clearPrefix > entry.Size)
        {
            throw new CorruptFileException(DocMimeType, $"encrypted document: {name} is shorter than its clear prefix");
        }

        var cipher = new byte[entry.Size];
        using (var input = root.CreateDocumentInputStream(entry))
        {
            var read = 0;
            while (read < cipher.Length)
            {
                var n = input.Read(cipher, read, cipher.Length - read);
                if (n <= 0)
                {
                    throw Truncated(name);
                }

                read += n;
            }
        }

        return cipher;
    }

    // fEncrypted and fObfuscated off, lKey 0, so the rebuilt file reads as an unencrypted document.
    // The table stream's clear header bytes stay: no FIB structure overlaps them.
    private static void ClearFibEncryptionMarkers(byte[] wordDocument)
    {
        var flags = BinaryPrimitives.ReadUInt16LittleEndian(wordDocument.AsSpan(OffFlags));
        BinaryPrimitives.WriteUInt16LittleEndian(
            wordDocument.AsSpan(OffFlags), (ushort)(flags & ~(FlagEncrypted | FlagObfuscated)));
        BinaryPrimitives.WriteUInt32LittleEndian(wordDocument.AsSpan(OffLKey), 0);
    }

    private static CorruptFileException Truncated(string name) =>
        new(DocMimeType, $"encrypted document: {name} stream is shorter than its declared size");
}
