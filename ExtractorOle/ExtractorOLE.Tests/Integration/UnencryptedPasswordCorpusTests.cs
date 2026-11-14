using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using Xunit;

namespace ExtractorOLE.Tests.Integration
{
    // [S-pw-boundaries#1] (docs/specs/extraction.md, "Unencrypted file with a password"): over every
    // unencrypted manifest sample of all six formats (curated, synthetic, adversarial), a non-empty
    // password changes nothing. DetectMimeType and Extract with a password return exactly the
    // no-password outcome: the same detection result, the same ExtractionResult (every leaf and the
    // FieldErrors), or, for a sample the product rejects, the same exception. Encrypted samples are
    // covered by EncryptedCorpusThreeWayTests, so samples/encrypted/ is excluded here.
    public class UnencryptedPasswordCorpusTests
    {
        private const string OversizedBombSamplePath = "samples/adversarial/oversized-declared-size-bomb.doc";

        private const string Password = "a-password-for-an-unencrypted-file";

        private static readonly ExtractionHelper Helper = new();

        public static IEnumerable<object[]> UnencryptedSamplePaths() =>
            LoadManifestSamples().Select(s => new object[] { s.GetProperty("path").GetString()! });

        [Fact]
        public void UnencryptedSamples_CoverAllSixFormats_SoTheTheoryCannotPassVacuously()
        {
            var formats = LoadManifestSamples().Select(MimeTypeFor).ToHashSet();

            foreach (var format in new[]
            {
                OfficeMimeType.Word, OfficeMimeType.Excel, OfficeMimeType.PowerPoint,
                OfficeMimeType.WordLegacy, OfficeMimeType.ExcelLegacy, OfficeMimeType.PowerPointLegacy,
            })
            {
                Assert.Contains(Helper.MimeFor(format), formats);
            }
        }

        [Theory]
        [MemberData(nameof(UnencryptedSamplePaths))]
        public void DetectMimeType_PasswordOnUnencryptedSample_EqualsNoPasswordResult(string path)
        {
            var bytes = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(path));
            var fileName = Path.GetFileName(path);

            string Detect(string? password) => Outcome(() =>
                JsonSerializer.Serialize(Helper.DetectMimeType(
                    new MimeDetectionRequest { FileBytes = bytes, FileName = fileName, Password = password })));

            var expected = Detect(null);
            // Detection returns a result for every sample except the declared-size bomb, which it rejects.
            Assert.StartsWith(path == OversizedBombSamplePath ? "throws:" : "result:", expected, StringComparison.Ordinal);

            Assert.Equal(expected, Detect(Password));
            Assert.Equal(expected, Detect(string.Empty));
        }

        [Theory]
        [MemberData(nameof(UnencryptedSamplePaths))]
        public void Extract_PasswordOnUnencryptedSample_EqualsNoPasswordResult(string path)
        {
            var entry = LoadManifestSamples().Single(s => s.GetProperty("path").GetString() == path);
            var bytes = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(path));
            var mime = MimeTypeFor(entry);

            string Extract(string? password) => Outcome(() =>
            {
                var result = EncryptedCorpus.Extract(new ExtractionRequest
                {
                    FileBytes = bytes,
                    FileName = Path.GetFileName(path),
                    DetectedMimeType = mime,
                    Password = password,
                });
                return EncryptedCorpus.Leaves(result) + "|" + JsonSerializer.Serialize(result.FieldErrors);
            });

            var expected = Extract(null);
            Assert.StartsWith(entry.TryGetProperty("expectedBehavior", out _) ? "throws:" : "result:", expected, StringComparison.Ordinal);
            Assert.Equal(expected, Extract(Password));
            Assert.Equal(expected, Extract(string.Empty));
        }

        // The outcome as a comparable string: the serialized result, or the exception's type, code
        // and message when the product rejects the sample (an unrelated password must not change that).
        private static string Outcome(Func<string> run)
        {
            try
            {
                var outcome = "result:" + run();
                Assert.DoesNotContain(Password, outcome, StringComparison.Ordinal);
                return outcome;
            }
            catch (Exception ex)
            {
                var code = (ex as ExtractorOleException)?.Code;
                return $"throws:{ex.GetType().FullName}:{code}:{ex.Message}";
            }
        }

        private static List<JsonElement> LoadManifestSamples()
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(EncryptedSamples.FindRepoRootFile("samples/manifest.json")));
            return doc.RootElement.GetProperty("samples").EnumerateArray()
                .Where(s => !s.GetProperty("path").GetString()!.StartsWith("samples/encrypted/", StringComparison.Ordinal))
                .Select(s => s.Clone())
                .ToList();
        }

        // Extract trusts the caller-supplied mime type: adversarial entries target a format
        // (targetFormats[0]); every other entry uses its expected detected format.
        private static string MimeTypeFor(JsonElement entry)
        {
            if (!entry.TryGetProperty("targetFormats", out var targets))
            {
                return entry.GetProperty("expectedDetectedFormat").GetProperty("mimeType").GetString()!;
            }

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
    }
}
