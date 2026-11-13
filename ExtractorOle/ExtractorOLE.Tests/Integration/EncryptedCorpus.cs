using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Integration
{
    // Reusable harness (docs/specs/testing-strategy.md, "tested three ways") over samples/encrypted/.
    // Samples are found by folder through EncryptedSamples; the manifest supplies the format, the
    // password, the twin and the detection expectations, so nothing is hard-coded by name.
    // A theory data source is one call per format group (Paths), so a later suite can plug in
    // another group, e.g. the encrypted doc/ppt entries, and reuse the assertions below.
    internal sealed record EncryptedCorpusEntry(
        string Path,
        string FullPath,
        string Format,
        string? Password,
        string? TwinFullPath,
        bool NegativeOnly,
        string? CorrectPasswordErrorCode,
        string ExpectedDetectedMimeType,
        string InnerMimeType);

    internal static class EncryptedCorpus
    {
        public const string WrongPassword = "not-the-password";
        public const string ArbitraryPassword = "an-arbitrary-password";

        private static readonly Lazy<MainExtractor> Extractor = new(() =>
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        });

        private static readonly ExtractionHelper Helper = new();

        private static readonly Lazy<IReadOnlyList<EncryptedCorpusEntry>> Cached = new(Load);

        public static IReadOnlyList<EncryptedCorpusEntry> Entries() => Cached.Value;

        private static List<EncryptedCorpusEntry> Load()
        {
            var manifestPath = EncryptedSamples.FindRepoRootFile("samples/manifest.json");
            var repoRoot = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(manifestPath))!;
            using var doc = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
            var manifest = doc.RootElement.GetProperty("samples").EnumerateArray()
                .ToDictionary(e => e.GetProperty("path").GetString()!, e => e.Clone(), StringComparer.OrdinalIgnoreCase);

            return EncryptedSamples.Enumerate().Select(sample =>
            {
                if (!manifest.TryGetValue(sample.Path, out var entry))
                {
                    throw new InvalidOperationException($"Encrypted sample '{sample.Path}' has no manifest entry");
                }

                var detected = entry.GetProperty("expectedDetectedFormat").GetProperty("mimeType").GetString()!;
                var code = entry.TryGetProperty("expectedBehavior", out var behavior) ? behavior.GetProperty("code").GetString() : null;
                return new EncryptedCorpusEntry(
                    sample.Path,
                    sample.FullPath,
                    entry.GetProperty("format").GetString()!,
                    sample.Password,
                    sample.UnencryptedTwin is null ? null : System.IO.Path.Combine(repoRoot, sample.UnencryptedTwin),
                    entry.TryGetProperty("negativeOnly", out var negative) && negative.GetBoolean(),
                    code,
                    detected,
                    InnerMimeType(entry, detected));
            }).ToList();
        }

        // The mime type Extract is given, and detection returns once the container opens: the first
        // target format when the manifest names one (an encrypted package detects as Unknown without a
        // password), otherwise the detected format.
        private static string InnerMimeType(JsonElement entry, string detected)
        {
            if (!entry.TryGetProperty("targetFormats", out var targets)) return detected;
            return Helper.MimeFor(targets[0].GetString() switch
            {
                "docx" => OfficeMimeType.Word,
                "xlsx" => OfficeMimeType.Excel,
                "pptx" => OfficeMimeType.PowerPoint,
                "doc" => OfficeMimeType.WordLegacy,
                "xls" => OfficeMimeType.ExcelLegacy,
                "ppt" => OfficeMimeType.PowerPointLegacy,
                var other => throw new InvalidOperationException($"Unmapped manifest target format '{other}'"),
            });
        }

        // Theory data source: encrypted samples of the given manifest formats that carry an open
        // password, are not negative-only and decrypt to a result (no expectedBehavior).
        public static IEnumerable<object[]> DecryptablePaths(params string[] formats) =>
            Paths(e => e.Password is not null && !e.NegativeOnly && e.CorrectPasswordErrorCode is null && formats.Contains(e.Format));

        // Password-bearing samples whose correct-password outcome is a recorded rejection (tampered).
        public static IEnumerable<object[]> RejectedWithCorrectPasswordPaths(params string[] formats) =>
            Paths(e => e.Password is not null && e.CorrectPasswordErrorCode is not null && formats.Contains(e.Format));

        public static IEnumerable<object[]> Paths(Func<EncryptedCorpusEntry, bool> filter) =>
            Entries().Where(filter).Select(e => new object[] { e.Path });

        public static EncryptedCorpusEntry Get(string path) =>
            Entries().Single(e => string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase));

        public static ExtractionRequest ExtractRequest(EncryptedCorpusEntry entry, string? password) =>
            ExtractRequest(File.ReadAllBytes(entry.FullPath), entry, password);

        public static ExtractionRequest ExtractRequest(byte[] bytes, EncryptedCorpusEntry entry, string? password) => new()
        {
            FileBytes = bytes,
            FileName = System.IO.Path.GetFileName(entry.Path),
            DetectedMimeType = entry.InnerMimeType,
            Password = password,
        };

        public static DocumentExtractionResult Extract(ExtractionRequest request) => Extractor.Value.Extract(request);

        public static MimeDetectionResult Detect(EncryptedCorpusEntry entry, string? password) =>
            Helper.DetectMimeType(new MimeDetectionRequest
            {
                FileBytes = File.ReadAllBytes(entry.FullPath),
                FileName = System.IO.Path.GetFileName(entry.Path),
                Password = password,
            });

        // Every public leaf, the embedded bytes included; FormatMetadata by its runtime type. The
        // result has no file-size leaf, so equality here is the "FileSizeBytes excepted" comparison.
        public static string Leaves(DocumentExtractionResult result) =>
            JsonSerializer.Serialize(result) + JsonSerializer.Serialize(result.FormatMetadata, result.FormatMetadata?.GetType() ?? typeof(object));

        // ---- Extract, no / wrong password: the fatal set returns no result ----

        public static void AssertExtractNoPasswordRejected(EncryptedCorpusEntry entry)
        {
            Assert.Throws<PasswordProtectedException>(() => Extract(ExtractRequest(entry, null)));
            Assert.Throws<PasswordProtectedException>(() => Extract(ExtractRequest(entry, string.Empty)));
        }

        public static void AssertExtractWrongPasswordRejected(EncryptedCorpusEntry entry)
        {
            var ex = Assert.Throws<InvalidPasswordException>(() => Extract(ExtractRequest(entry, WrongPassword)));
            Assert.DoesNotContain(WrongPassword, ex.ToString(), StringComparison.Ordinal);
        }

        // ---- Extract, correct password ----

        // Equal to the twin's extraction leaf for leaf. 'unreadableMetadata' is the single recorded
        // exception: a scheme whose encrypted document properties are not read yet, so Metadata is
        // asserted empty instead of equal to the twin's.
        public static void AssertExtractCorrectPasswordEqualsTwin(EncryptedCorpusEntry entry, bool unreadableMetadata = false)
        {
            Assert.NotNull(entry.TwinFullPath);
            var decrypted = Extract(ExtractRequest(entry, entry.Password));
            var twin = Extract(ExtractRequest(File.ReadAllBytes(entry.TwinFullPath), entry, null));

            Assert.Equal(entry.InnerMimeType, decrypted.MimeType);
            Assert.Empty(decrypted.FieldErrors);
            Assert.Empty(twin.FieldErrors);
            if (unreadableMetadata)
            {
                Assert.Equal(JsonSerializer.Serialize(new FileMetadata()), JsonSerializer.Serialize(decrypted.Metadata));
                twin.Metadata = decrypted.Metadata; // the twin's metadata is intentionally not compared
            }

            Assert.Equal(Leaves(twin), Leaves(decrypted));
        }

        public static void AssertExtractCorrectPasswordRejectedAsRecorded(EncryptedCorpusEntry entry)
        {
            var ex = Assert.Throws<CorruptFileException>(() => Extract(ExtractRequest(entry, entry.Password)));
            Assert.Equal(entry.CorrectPasswordErrorCode, ex.Code);
            Assert.DoesNotContain(entry.Password!, ex.ToString(), StringComparison.Ordinal);
        }

        // ---- DetectMimeType, the same three ways ----

        // Encrypted package (the target format differs from what the container shows): Unknown
        // without a password, INVALID_PASSWORD with a wrong one, the inner format with the right one.
        public static void AssertDetectEncryptedPackageThreeWays(EncryptedCorpusEntry entry)
        {
            foreach (var none in new[] { null, string.Empty })
            {
                var unknown = Detect(entry, none);
                Assert.Equal(DetectedFormat.Unknown, unknown.DetectedFormat);
                Assert.Equal(entry.ExpectedDetectedMimeType, unknown.MimeType);
            }

            var ex = Assert.Throws<InvalidPasswordException>(() => Detect(entry, WrongPassword));
            Assert.DoesNotContain(WrongPassword, ex.ToString(), StringComparison.Ordinal);

            AssertDetectsInnerFormat(entry);
        }

        public static void AssertDetectsInnerFormat(EncryptedCorpusEntry entry)
        {
            var inner = Detect(entry, entry.Password);
            Assert.Equal(entry.InnerMimeType, inner.MimeType);
            Assert.True(inner.IsSupported);
        }

        // Legacy container (xls/doc/ppt): the format is detected unchanged whatever the password.
        public static void AssertDetectLegacyUnchangedThreeWays(EncryptedCorpusEntry entry)
        {
            foreach (var password in new[] { null, WrongPassword, entry.Password })
            {
                var result = Detect(entry, password);
                Assert.Equal(entry.ExpectedDetectedMimeType, result.MimeType);
                Assert.True(result.IsSupported);
            }
        }
    }
}
