using System.Buffers.Binary;
using System.Security.Cryptography;
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

    // Returns null when the stream does not exist.
    private static byte[]? DecryptStream(
        DirectoryNode root, Decryptor decryptor, string name, long clearPrefix, long maxEntryBytes)
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
        catch
        {
            CryptographicOperations.ZeroMemory(plain);
            throw;
        }

        return plain;
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
