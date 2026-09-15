using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace ExtractorOLE.Tests.Integration
{
    // Integration tests: DetectMimeType (ExtractionHelper, real DI-free
    // parameterless constructor, no mocking) against every sample recorded in
    // the corpus manifest (samples/manifest.json). One test case per manifest
    // entry, generated from the manifest itself at [MemberData] discovery time
    // -- expected values are read from the manifest, never hand-copied, so this
    // suite tracks the manifest as source of truth.
    //
    // One sample (oversized-declared-size-bomb.doc) is excluded from the
    // equality Theory: the manifest's own notes record that its
    // expectedDetectedFormat.enum is NOT an empirically observed value --
    // CfbMimeDetector throws OversizedNestedContentException from its
    // declared-size guard before any format is computed for that file. It gets
    // a dedicated throw-assertion Fact instead.
    public class DetectMimeTypeCorpusManifestTests
    {
        private const string OversizedBombSamplePath = "samples/adversarial/oversized-declared-size-bomb.doc";

        private sealed class ManifestRoot
        {
            [JsonPropertyName("samples")]
            public List<ManifestSample> Samples { get; set; } = new();
        }

        private sealed class ManifestSample
        {
            [JsonPropertyName("path")]
            public string Path { get; set; } = string.Empty;

            [JsonPropertyName("expectedDetectedFormat")]
            public ExpectedDetectedFormat ExpectedDetectedFormat { get; set; } = new();
        }

        private sealed class ExpectedDetectedFormat
        {
            [JsonPropertyName("enum")]
            public string Enum { get; set; } = string.Empty;

            [JsonPropertyName("mimeType")]
            public string MimeType { get; set; } = string.Empty;
        }

        // Walks up from the test assembly's location looking for the repo's
        // checked-in samples/manifest.json, mirroring the pattern used by
        // ExtractorOle/ExtractorOLE/Program.cs's private FindDefaultSampleFile().
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

        private static List<ManifestSample> LoadManifestSamples()
        {
            var manifestPath = FindRepoRootFile("samples/manifest.json");
            var json = File.ReadAllText(manifestPath);
            var root = JsonSerializer.Deserialize<ManifestRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return root?.Samples ?? new List<ManifestSample>();
        }

        public static IEnumerable<object[]> EqualityAssertionSamples()
        {
            foreach (var sample in LoadManifestSamples())
            {
                if (sample.Path == OversizedBombSamplePath) continue;
                yield return new object[] { sample.Path, sample.ExpectedDetectedFormat.Enum, sample.ExpectedDetectedFormat.MimeType };
            }
        }

        [Theory]
        [MemberData(nameof(EqualityAssertionSamples))]
        public void DetectMimeType_CorpusSample_MatchesManifestExpectedFormat(string relativePath, string expectedEnum, string expectedMimeType)
        {
            var fullPath = FindRepoRootFile(relativePath);
            var fileBytes = File.ReadAllBytes(fullPath);
            var request = new MimeDetectionRequest { FileBytes = fileBytes, FileName = Path.GetFileName(relativePath) };
            var expectedFormat = (DetectedFormatEnum)Enum.Parse(typeof(DetectedFormatEnum), expectedEnum);

            var result = new ExtractionHelper().DetectMimeType(request);

            Assert.Equal(expectedFormat, result.DetectedFormat);
            Assert.Equal(expectedMimeType, result.MimeType);
        }

        [Fact]
        public void DetectMimeType_OversizedDeclaredSizeBomb_ThrowsOversizedNestedContentException()
        {
            var fullPath = FindRepoRootFile(OversizedBombSamplePath);
            var fileBytes = File.ReadAllBytes(fullPath);
            var request = new MimeDetectionRequest { FileBytes = fileBytes, FileName = Path.GetFileName(OversizedBombSamplePath) };

            Assert.Throws<OversizedNestedContentException>(() => new ExtractionHelper().DetectMimeType(request));
        }
    }
}
