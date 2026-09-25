using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Integration
{
    // Integration tests (docs/specs/testing-strategy.md): MainExtractor.Extract, wired through the
    // production DI registration with no mocked format-handling component, against every entry of
    // the 'samples' array in samples/manifest.json. One Theory row per manifest entry, discovered
    // from the manifest itself, so expected values are read from the manifest and never
    // hand-copied. The manifest entry decides the assertion:
    //   - expectedBehavior present  -> Extract must throw the exact exception type recorded there.
    //   - otherwise                 -> exact text (bodyTextLines / generator-spec bodyText /
    //                                  confirmedSubstrings), exact metadata, exact subfile list.
    // Nothing here asserts only "text is non-empty".
    //
    // Manifest metadata keys with no field on FileMetadata (Subject, Comments, RevisionNumber,
    // LastPrinted, EditingDurationMinutes) cannot be asserted through Extract's result; any other
    // unknown key fails the test so a new manifest key never goes silently unasserted.
    public class ExtractCorpusManifestTests
    {
        private static readonly Lazy<MainExtractor> Extractor = new(() =>
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        });

        // Manifest metadata keys that map onto FileMetadata.
        private static readonly HashSet<string> MappedMetadataKeys = new()
        {
            "Title", "Author", "LastModifiedBy", "Created", "Modified",
        };

        // Manifest metadata keys the DocumentExtractionResult has no field for.
        private static readonly HashSet<string> UnmappedMetadataKeys = new()
        {
            "Subject", "Comments", "RevisionNumber", "LastPrinted", "EditingDurationMinutes",
        };

        // Extractor-generated subfile names are "<prefix>_<index><ext>" with prefix embedded_object
        // or slide_image (rule: docs/specs/extraction.md, Subfiles). Manifest entries recording that
        // scheme are compared to the extractor's names exactly; this only tells them apart from
        // curated third-party entries that record the raw part file name (image1.png).
        private static readonly Regex ExtractorFileName =
            new(@"^(?:embedded_object|slide_image)_\d+\.\w+$", RegexOptions.Compiled);

        private static string FindRepoRootFile(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
        }

        private static List<JsonElement> LoadManifestSamples()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(FindRepoRootFile("samples/manifest.json")));
            return doc.RootElement.GetProperty("samples").EnumerateArray().Select(e => e.Clone()).ToList();
        }

        public static IEnumerable<object[]> ManifestSamplePaths() =>
            LoadManifestSamples().Select(s => new object[] { s.GetProperty("path").GetString()! });

        [Fact]
        public void Manifest_HasSamples_SoTheTheoryCannotPassVacuously()
        {
            Assert.NotEmpty(LoadManifestSamples());
        }

        [Theory]
        [MemberData(nameof(ManifestSamplePaths))]
        public void Extract_CorpusSample_MatchesManifest(string path)
        {
            var entry = LoadManifestSamples().Single(s => s.GetProperty("path").GetString() == path);
            var request = new ExtractionRequest
            {
                FileBytes = File.ReadAllBytes(FindRepoRootFile(path)),
                FileName = Path.GetFileName(path),
                DetectedMimeType = MimeTypeFor(entry),
            };

            if (entry.TryGetProperty("expectedBehavior", out var behavior))
            {
                AssertRejected(behavior, request);
                return;
            }

            var result = Extractor.Value.Extract(request);

            Assert.Equal(request.DetectedMimeType, result.MimeType);
            AssertMetadata(entry, result.Metadata);
            AssertFormatMetadata(entry, result.FormatMetadata);
            AssertText(entry, result.ExtractedText);
            AssertSubfiles(path, entry, result.EmbeddedFiles);
        }

        // Extract trusts the caller-supplied mime type. Adversarial entries whose detection result
        // is Unknown/nominal are extracted as the format they target (targetFormats[0]); every
        // other entry uses its expectedDetectedFormat, exactly what DetectMimeType returns for it.
        private static string MimeTypeFor(JsonElement entry)
        {
            if (!entry.TryGetProperty("targetFormats", out var targets))
            {
                return entry.GetProperty("expectedDetectedFormat").GetProperty("mimeType").GetString()!;
            }

            var format = targets[0].GetString() switch
            {
                "docx" => OfficeMimeTypeEnum.Word,
                "xlsx" => OfficeMimeTypeEnum.Excel,
                "pptx" => OfficeMimeTypeEnum.PowerPoint,
                "doc" => OfficeMimeTypeEnum.WordLegacy,
                "xls" => OfficeMimeTypeEnum.ExcelLegacy,
                "ppt" => OfficeMimeTypeEnum.PowerPointLegacy,
                var other => throw new InvalidOperationException($"Unmapped manifest target format '{other}'"),
            };
            return new ExtractionHelper().MimeFor(format);
        }

        private static void AssertRejected(JsonElement behavior, ExtractionRequest request)
        {
            var expectedType = behavior.GetProperty("errorId").GetString() switch
            {
                "ydk:error:extraction/oversized-nested-content" => typeof(OversizedNestedContentException),
                "ydk:error:extraction/password-protected" => typeof(PasswordProtectedException),
                "ydk:error:extraction/corrupt-file" => typeof(CorruptFileException),
                "ydk:error:extraction/truncated-container" => typeof(TruncatedContainerException),
                var other => throw new InvalidOperationException($"Unmapped manifest errorId '{other}'"),
            };

            var thrown = Assert.Throws(expectedType, () => Extractor.Value.Extract(request));

            if (behavior.TryGetProperty("code", out var code))
            {
                Assert.Equal(code.GetString(), ((ExtractorOleException)thrown).Code);
            }
        }

        private static void AssertMetadata(JsonElement entry, FileMetadata actual)
        {
            var metadata = entry.GetProperty("metadata");
            foreach (var key in metadata.EnumerateObject().Select(p => p.Name))
            {
                Assert.True(MappedMetadataKeys.Contains(key) || UnmappedMetadataKeys.Contains(key),
                    $"Manifest metadata key '{key}' is neither asserted nor known to be unmappable");
            }

            string? Text(string key) =>
                metadata.TryGetProperty(key, out var v) ? v.GetString() : null;

            // Third-party entries record their title in bodyText rather than metadata.
            var expectedTitle = Text("Title");
            if (expectedTitle == null && entry.TryGetProperty("bodyText", out var body) && body.ValueKind == JsonValueKind.Object)
            {
                foreach (var titleKey in new[] { "titleFromCoreProperties", "titleFromSummaryInformation" })
                {
                    if (body.TryGetProperty(titleKey, out var t)) expectedTitle = t.GetString();
                }
            }

            Assert.Equal(expectedTitle, actual.Title);
            Assert.Equal(Text("Author"), actual.Creator);
            Assert.Equal(Text("LastModifiedBy"), actual.LastModifiedBy);
            AssertInstant(Text("Created"), actual.Created, "Created");
            AssertInstant(Text("Modified"), actual.Modified, "Modified");
        }

        private static void AssertInstant(string? expectedUtc, DateTime? actual, string field)
        {
            if (expectedUtc == null)
            {
                Assert.Null(actual);
                return;
            }

            var expected = DateTime.Parse(expectedUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            Assert.True(actual.HasValue, $"{field} expected {expectedUtc} but was null");
            Assert.Equal(expected, actual!.Value.ToUniversalTime());
        }

        private static void AssertFormatMetadata(JsonElement entry, FormatMetadata? actual)
        {
            if (!entry.TryGetProperty("expectedFormatMetadata", out var expected)) return;

            var hasMacros = actual switch
            {
                WordFormatMetadata w => w.HasMacros,
                ExcelFormatMetadata x => x.HasMacros,
                PowerPointFormatMetadata p => p.HasMacros,
                _ => throw new Xunit.Sdk.XunitException($"Unexpected FormatMetadata type {actual?.GetType().Name ?? "null"}"),
            };
            Assert.Equal(expected.GetProperty("hasMacros").GetBoolean(), hasMacros);
        }

        private static string CollapseWhitespace(string text) => Regex.Replace(text, @"\s+", " ");

        private static string[] Lines(string text) => text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        private static void AssertText(JsonElement entry, string actualText)
        {
            if (entry.TryGetProperty("bodyTextLines", out var lines))
            {
                Assert.Equal(lines.EnumerateArray().Select(l => l.GetString()!).ToArray(), Lines(actualText));
                return;
            }

            var body = entry.GetProperty("bodyText");

            // Generator-spec body: word[i] = wordPool[i % pool], wordsPerLine words per line.
            if (body.TryGetProperty("wordPool", out var poolElement))
            {
                var pool = poolElement.EnumerateArray().Select(w => w.GetString()!).ToArray();
                var wordCount = body.GetProperty("wordCount").GetInt32();
                var wordsPerLine = body.GetProperty("wordsPerLine").GetInt32();
                var expectedLines = Enumerable.Range(0, wordCount)
                    .Select(i => pool[i % pool.Length])
                    .Chunk(wordsPerLine)
                    .Select(chunk => string.Join(" ", chunk))
                    .ToArray();

                Assert.Equal(body.GetProperty("lineCount").GetInt32(), expectedLines.Length);
                Assert.Equal(expectedLines, Lines(actualText));
                return;
            }

            // Real third-party documents: the manifest records only verbatim-confirmed substrings,
            // read from the source XML text runs. A substring can span two runs/paragraphs (the
            // extractor emits one line per run/paragraph), so line breaks and runs of whitespace
            // compare as a single space; all other characters compare exactly.
            var substrings = body.GetProperty("confirmedSubstrings").EnumerateArray().Select(s => s.GetString()!).ToList();
            Assert.NotEmpty(substrings);
            var normalizedText = CollapseWhitespace(actualText);
            foreach (var substring in substrings)
            {
                Assert.Contains(CollapseWhitespace(substring), normalizedText, StringComparison.Ordinal);
            }
        }

        private static void AssertSubfiles(string path, JsonElement entry, List<EmbeddedFileItem> actual)
        {
            var expectedCount = entry.GetProperty("expectedSubfileCount").GetInt32();
            Assert.Equal(expectedCount, actual.Count);

            var expected = entry.GetProperty("expectedSubfiles").EnumerateArray().ToList();
            if (expected.Count == 0) return;

            Assert.Equal(expectedCount, expected.Count);

            // Package path + size identify the embedded content (order-insensitive).
            Assert.Equal(
                expected.Select(e => (e.GetProperty("packagePath").GetString()!, e.GetProperty("sizeInBytes").GetInt64())).OrderBy(t => t).ToList(),
                actual.Select(a => (a.PackagePath, a.SizeInBytes)).OrderBy(t => t).ToList());

            // File names are compared exactly, prefix included, and in order when the manifest records
            // extractor-scheme names; curated third-party entries record the raw part file name
            // (image1.png) instead.
            var expectedNames = expected.Select(e => e.GetProperty("fileName").GetString()!).ToList();
            if (expectedNames.All(n => ExtractorFileName.IsMatch(n)))
            {
                Assert.Equal(expectedNames, actual.Select(a => a.FileName).ToList());
            }
        }
    }
}
