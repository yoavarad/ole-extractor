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
    // Every entry also records expectedFieldErrors (fieldPath + code, empty for a clean sample), the
    // per-field errors Extract must return alongside the result; a missing key fails the entry.
    //
    // Every manifest metadata key is asserted against FileMetadata; an unknown key fails the test
    // so a new manifest key never goes silently unasserted. Manifest names map to FileMetadata as:
    // Author -> Creator, Created -> Created, Modified -> Modified (entity CreatedAt/ModifiedAt/Author
    // are the spec names), EditingDurationMinutes -> EditingDurationMinutes, LastPrinted -> LastPrinted.
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
            "Subject", "Comments", "RevisionNumber", "LastPrinted", "EditingDurationMinutes",
            "Keywords", "ApplicationName",
        };

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

            var expectedFieldErrors = ReadExpectedFieldErrors(entry);

            if (entry.TryGetProperty("expectedBehavior", out var behavior))
            {
                // A rejected sample throws, so there is no result to carry field errors.
                Assert.Empty(expectedFieldErrors);
                AssertRejected(behavior, request);
                return;
            }

            var result = Extractor.Value.Extract(request);

            Assert.Equal(request.DetectedMimeType, result.MimeType);
            AssertMetadata(entry, result.Metadata);
            AssertFormatMetadata(entry, result.FormatMetadata);
            AssertText(entry, result.ExtractedText);
            AssertSubfiles(path, entry, result.EmbeddedFiles);
            AssertFieldErrors(expectedFieldErrors, result.FieldErrors);
        }

        [Fact]
        public void Manifest_FieldErrorExpectations_CoverCleanSamplesInEverySupportedFormatAndAtLeastOneFaultedSample()
        {
            var extracted = LoadManifestSamples().Where(s => !s.TryGetProperty("expectedBehavior", out _)).ToList();
            var cleanMimeTypes = extracted
                .Where(s => ReadExpectedFieldErrors(s).Count == 0)
                .Select(MimeTypeFor)
                .ToHashSet();

            foreach (var mime in new[]
            {
                OfficeMimeType.Word, OfficeMimeType.Excel, OfficeMimeType.PowerPoint,
                OfficeMimeType.WordLegacy, OfficeMimeType.ExcelLegacy, OfficeMimeType.PowerPointLegacy,
            })
            {
                Assert.Contains(new ExtractionHelper().MimeFor(mime), cleanMimeTypes);
            }

            Assert.Contains(extracted, s => ReadExpectedFieldErrors(s).Count > 0);
        }

        [Fact]
        public void Manifest_ListsEveryCorpusSampleFile()
        {
            var samplesRoot = Path.GetDirectoryName(FindRepoRootFile("samples/manifest.json"))!;
            var listed = LoadManifestSamples()
                .Select(s => s.GetProperty("path").GetString()!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var group in new[] { "adversarial", "curated", "encrypted", "synthetic" })
            {
                foreach (var file in Directory.EnumerateFiles(Path.Combine(samplesRoot, group), "*", SearchOption.AllDirectories))
                {
                    var relative = "samples/" + Path.GetRelativePath(samplesRoot, file).Replace(Path.DirectorySeparatorChar, '/');
                    Assert.Contains(relative, listed);
                }
            }
        }

        private static List<(string FieldPath, string Code)> ReadExpectedFieldErrors(JsonElement entry)
        {
            Assert.True(entry.TryGetProperty("expectedFieldErrors", out var errors),
                $"Manifest entry '{entry.GetProperty("path").GetString()}' has no expectedFieldErrors");

            var parsed = errors.EnumerateArray()
                .Select(e => (e.GetProperty("fieldPath").GetString()!, e.GetProperty("code").GetString()!))
                .ToList();
            AssertNoDuplicatePaths(parsed.Select(e => e.Item1), "manifest expectedFieldErrors");
            return parsed;
        }

        private static void AssertNoDuplicatePaths(IEnumerable<string> fieldPaths, string source)
        {
            // Only the bare collection name may repeat (one error per failed subfile with an unknown path).
            var duplicate = fieldPaths.Where(p => p != nameof(DocumentExtractionResult.EmbeddedFiles))
                .GroupBy(p => p).FirstOrDefault(g => g.Count() > 1);
            Assert.True(duplicate is null, $"Duplicate FieldPath '{duplicate?.Key}' in {source}");
        }

        // FieldErrors is never null: empty when clean, otherwise exactly the recorded set (compared
        // order-insensitively; each error is path + code, the message is display-only).
        private static void AssertFieldErrors(List<(string FieldPath, string Code)> expected, List<FieldError>? actual)
        {
            Assert.NotNull(actual);
            AssertNoDuplicatePaths(actual.Select(e => e.FieldPath), "extracted FieldErrors");
            Assert.Equal(
                expected.OrderBy(e => e.FieldPath, StringComparer.Ordinal).ThenBy(e => e.Code, StringComparer.Ordinal).ToList(),
                actual.Select(e => (e.FieldPath, e.Code.ToString()))
                    .OrderBy(e => e.FieldPath, StringComparer.Ordinal).ThenBy(e => e.Item2, StringComparer.Ordinal).ToList());
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
                "docx" => OfficeMimeType.Word,
                "xlsx" => OfficeMimeType.Excel,
                "pptx" => OfficeMimeType.PowerPoint,
                "doc" => OfficeMimeType.WordLegacy,
                "xls" => OfficeMimeType.ExcelLegacy,
                "ppt" => OfficeMimeType.PowerPointLegacy,
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
                Assert.True(MappedMetadataKeys.Contains(key),
                    $"Manifest metadata key '{key}' is not asserted");
            }

            string? Text(string key) =>
                metadata.TryGetProperty(key, out var v) ? v.GetString() : null;

            int? Int(string key)
            {
                if (!metadata.TryGetProperty(key, out var v)) return null;
                return v.ValueKind == JsonValueKind.String ? int.Parse(v.GetString()!, CultureInfo.InvariantCulture) : v.GetInt32();
            }

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
            // The manifest records these only when the sample's native properties were captured;
            // a curated sample can carry more than it records, so they are asserted when recorded.
            if (metadata.TryGetProperty("Subject", out _)) Assert.Equal(Text("Subject"), actual.Subject);
            if (metadata.TryGetProperty("Keywords", out _)) Assert.Equal(Text("Keywords"), actual.Keywords);
            if (metadata.TryGetProperty("ApplicationName", out _)) Assert.Equal(Text("ApplicationName"), actual.ApplicationName);
            if (metadata.TryGetProperty("Comments", out _)) Assert.Equal(Text("Comments"), actual.Comments);
            if (metadata.TryGetProperty("LastPrinted", out _)) AssertInstant(Text("LastPrinted"), actual.LastPrinted, "LastPrinted");
            if (metadata.TryGetProperty("RevisionNumber", out _)) Assert.Equal(Int("RevisionNumber"), actual.RevisionNumber);
            if (metadata.TryGetProperty("EditingDurationMinutes", out _)) Assert.Equal(Int("EditingDurationMinutes"), actual.EditingDurationMinutes);
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

        private static readonly string[] LineSeparators = { "\r\n", "\n" };

        private static string[] Lines(string text) => text.Split(LineSeparators, StringSplitOptions.None);

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

            // Real third-party decks: expectedSlides lists every slide's text lines in deck (p:sldIdLst)
            // order, slide body then its notes, read from the source XML rather than extractor output.
            // Compared exactly (only platform line endings normalized), so a swapped slide fails.
            if (body.TryGetProperty("expectedSlides", out var slides))
            {
                var expectedLines = slides.EnumerateArray()
                    .SelectMany(slide => slide.EnumerateArray().Select(l => l.GetString()!))
                    .ToArray();
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

            // File names are compared exactly, prefix included, and in order.
            Assert.Equal(
                expected.Select(e => e.GetProperty("fileName").GetString()!).ToList(),
                actual.Select(a => a.FileName).ToList());
        }
    }
}
