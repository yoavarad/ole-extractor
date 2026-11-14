using System.Globalization;
using System.Security.Cryptography;
using ExtractorOLE.Configuration;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using NPOI.POIFS.FileSystem;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

// The plain compound file rebuilt from an encrypted legacy file, plus the leaves that cannot be
// recovered from it; MainExtractor records each as a FieldError after the open.
internal sealed record LegacyPlaintext(byte[] Bytes, IReadOnlyList<(string FieldPath, CorruptFileException Failure)> DeferredFieldFailures);

// [ydk:req:extraction/legacy-decryption] [ydk:req:extraction/legacy-plaintext-rebuild]
// Decrypts an encrypted .doc or .ppt that LegacyEncryptionGuard recognised and rebuilds it as a plain
// compound file, which the unchanged doc/ppt path then opens and extracts.
internal static class LegacyDecryption
{
    private const string WordDocumentStream = "WordDocument";
    private const string DataStream = "Data";
    private const string EncryptionStream = "encryption";
    private const string CurrentUserStream = "Current User";
    private const string PowerPointDocumentStream = "PowerPoint Document";
    private const string PicturesStream = "Pictures";

    // Test seam: runs on the calling flow just before DocDecryptor starts (cancellation tests).
    internal static readonly AsyncLocal<Action?> BeforeDocDecryptForTest = new();

    internal static LegacyPlaintext Decrypt(ExtractionInput input, LegacyEncryptedFile file, string password,
        MimeDetectionLimits limits, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(limits);

        if (file is EncryptedPpt ppt)
        {
            return DecryptPpt(input, ppt, password, limits, detectedMimeType);
        }

        if (file is not EncryptedDoc doc)
        {
            throw new ArgumentException($"unsupported legacy encrypted file {file.GetType().Name}", nameof(file));
        }

        // Stream/path input of an encrypted doc is buffered whole; the guard returned null for a
        // plain file, so unencrypted input never reaches this path.
        var bytes = input.OriginalBytes ?? input.Bytes;
        using var fs = CfbFileSystems.Open(new MemoryStream(bytes, writable: false));

        BeforeDocDecryptForTest.Value?.Invoke();
        var streams = DocDecryptor.Decrypt(fs.Root, doc.Fib, doc.Header, password, limits.MaxDeclaredEntryBytes);
        byte[] plain;
        try
        {
            var rootStreams = new Dictionary<string, byte[]?>(StringComparer.Ordinal)
            {
                [WordDocumentStream] = streams.WordDocument,
                [streams.TableStreamName] = streams.Table,
            };
            if (streams.Data is not null)
            {
                rootStreams[DataStream] = streams.Data;
            }

            plain = PlainCompoundFileBuilder.Build(fs.Root, rootStreams, limits.MaxDeclaredEntryBytes, detectedMimeType);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(streams.WordDocument);
            CryptographicOperations.ZeroMemory(streams.Table);
            if (streams.Data is not null)
            {
                CryptographicOperations.ZeroMemory(streams.Data);
            }
        }

        // Gap G2: a CryptoAPI doc whose document properties are encrypted (fDocProps clear) keeps
        // them in the "encryption" stream, which this version does not read.
        var deferred = new List<(string, CorruptFileException)>();
        if (doc.Header.Scheme == LegacyCipherScheme.CryptoApiRc4
            && (doc.Header.Flags & LegacyEncryptionHeaderReader.FlagDocProps) == 0
            && fs.Root.HasEntry(EncryptionStream))
        {
            deferred.Add(("Metadata", new CorruptFileException(detectedMimeType,
                "document properties are stored encrypted (MS-OFFCRYPTO 2.3.5.4) and are not decrypted in this version")));
        }

        return new LegacyPlaintext(plain, deferred);
    }

    // ppt: persist objects (PptDecryptor), Pictures (PptPicturesDecryptor) and the EncryptedSummary
    // property streams (CryptoApiEncryptedSummaryReader) go into the rebuilt file. A Pictures stream
    // that cannot be decrypted is left out and recorded as a CORRUPT_FILE FieldError at
    // EmbeddedFiles[Pictures]; an unreadable EncryptedSummary is container-level and propagates.
    private static LegacyPlaintext DecryptPpt(ExtractionInput input, EncryptedPpt ppt, string password,
        MimeDetectionLimits limits, string detectedMimeType)
    {
        var bytes = input.OriginalBytes ?? input.Bytes;
        using var fs = CfbFileSystems.Open(new MemoryStream(bytes, writable: false));

        var core = PptDecryptor.DecryptCore(ppt.Layout, ppt.Header, password, detectedMimeType);
        var rootStreams = new Dictionary<string, byte[]?>(StringComparer.Ordinal)
        {
            [CurrentUserStream] = core.CurrentUser,
            [PowerPointDocumentStream] = core.PowerPointDocument,
        };
        var deferred = new List<(string, CorruptFileException)>();
        try
        {
            if (fs.Root.HasEntry(PicturesStream) && fs.Root.GetEntry(PicturesStream) is DocumentEntry pictures)
            {
                // Reading the stream is container-level (oversized or broken CFB chain); only a failure
                // to decrypt its records costs just the Pictures subfile.
                var cipher = ReadStream(fs.Root, pictures, limits.MaxDeclaredEntryBytes, detectedMimeType);
                try
                {
                    rootStreams[PicturesStream] = PptPicturesDecryptor.Decrypt(cipher, core.Decryptor, detectedMimeType);
                }
                catch (CorruptFileException ex)
                {
                    rootStreams[PicturesStream] = null;
                    deferred.Add((FieldIsolation.SubfilePath(PicturesStream), ex));
                }
            }

            // Decided by the stream's presence, not by the header flag. The decrypted property streams
            // replace the plain placeholders; a descriptor naming a stream already replaced above (or
            // EncryptedSummary itself) is ignored and zeroed.
            if (fs.Root.HasEntry(CryptoApiEncryptedSummaryReader.PptStreamName))
            {
                if (fs.Root.GetEntry(CryptoApiEncryptedSummaryReader.PptStreamName) is not DocumentEntry summary)
                {
                    throw new CorruptFileException(detectedMimeType, "encrypted presentation: EncryptedSummary is not a stream");
                }

                var encryptedSummary = ReadStream(fs.Root, summary, limits.MaxDeclaredEntryBytes, detectedMimeType);
                rootStreams[CryptoApiEncryptedSummaryReader.PptStreamName] = null;
                foreach (var stream in CryptoApiEncryptedSummaryReader.Read(encryptedSummary, core.Decryptor, detectedMimeType))
                {
                    if (!rootStreams.TryAdd(stream.Name, stream.Data))
                    {
                        CryptographicOperations.ZeroMemory(stream.Data);
                    }
                }
            }

            return new LegacyPlaintext(
                PlainCompoundFileBuilder.Build(fs.Root, rootStreams, limits.MaxDeclaredEntryBytes, detectedMimeType),
                deferred);
        }
        finally
        {
            foreach (var data in rootStreams.Values)
            {
                if (data is not null)
                {
                    CryptographicOperations.ZeroMemory(data);
                }
            }
        }
    }

    // [ydk:error:extraction/corrupt-file] The declared size is checked against the per-entry cap before
    // the buffer is allocated; a stream shorter than its declared size is corrupt.
    private static byte[] ReadStream(DirectoryNode root, DocumentEntry entry, long maxEntryBytes, string detectedMimeType)
    {
        if (entry.Size > maxEntryBytes)
        {
            throw new OversizedNestedContentException(string.Create(CultureInfo.InvariantCulture,
                $"{entry.Name} stream declares {entry.Size} bytes (per-entry limit {maxEntryBytes})"));
        }

        var buffer = new byte[entry.Size];
        using var stream = root.CreateDocumentInputStream(entry);
        var read = 0;
        while (read < buffer.Length)
        {
            var n = stream.Read(buffer, read, buffer.Length - read);
            if (n <= 0)
            {
                throw new CorruptFileException(detectedMimeType,
                    $"encrypted presentation: {entry.Name} stream is truncated");
            }

            read += n;
        }

        return buffer;
    }
}
