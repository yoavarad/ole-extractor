using System.Security.Cryptography;
using ExtractorOLE.Configuration;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

// The plain compound file rebuilt from an encrypted legacy file, plus the leaves that cannot be
// recovered from it; MainExtractor records each as a FieldError after the open.
internal sealed record LegacyPlaintext(byte[] Bytes, IReadOnlyList<(string FieldPath, CorruptFileException Failure)> DeferredFieldFailures);

// [ydk:req:extraction/legacy-decryption] [ydk:req:extraction/legacy-plaintext-rebuild]
// Decrypts an encrypted .doc that LegacyEncryptionGuard recognised and rebuilds it as a plain
// compound file, which the unchanged doc path then opens and extracts.
internal static class LegacyDecryption
{
    private const string WordDocumentStream = "WordDocument";
    private const string DataStream = "Data";
    private const string EncryptionStream = "encryption";

    // Test seam: runs on the calling flow just before DocDecryptor starts (cancellation tests).
    internal static readonly AsyncLocal<Action?> BeforeDocDecryptForTest = new();

    internal static LegacyPlaintext Decrypt(ExtractionInput input, LegacyEncryptedFile file, string password,
        MimeDetectionLimits limits, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(limits);

        if (file is not EncryptedDoc doc)
        {
            throw new PasswordProtectedException(); // interim until T-lg-ppt-wire
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
}
