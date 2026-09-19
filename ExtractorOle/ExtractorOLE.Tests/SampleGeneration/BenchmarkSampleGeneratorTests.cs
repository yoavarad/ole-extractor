using System.Security.Cryptography;
using System.Text.Json;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.SampleGeneration
{
    /// <summary>
    /// T-f0963fc3. The small-size tests run everywhere: they prove the generator is deterministic,
    /// lands inside its size band and yields extractable text for all 6 formats. The manifest tests
    /// validate the real 12 files in the gitignored samples/benchmark/ and are skipped until those
    /// are generated (dotnet run --project SampleGenerator -- --benchmark).
    /// </summary>
    public class BenchmarkSampleGeneratorTests
    {
        private const long SmallTarget = 1_000_000;

        public static IEnumerable<object[]> Formats() =>
            Enum.GetValues<SampleFormat>().Select(f => new object[] { f });

        private static MainExtractor BuildExtractor()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        }

        private static DocumentExtractionResult ExtractThroughPublicContract(byte[] bytes, string fileName)
        {
            var mime = new ExtractionHelper().DetectMimeType(
                new MimeDetectionRequest { FileBytes = bytes, FileName = fileName }).MimeType;
            return BuildExtractor().Extract(
                new ExtractionRequest { FileBytes = bytes, FileName = fileName, DetectedMimeType = mime });
        }

        private static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

        [Theory]
        [MemberData(nameof(Formats))]
        public void Generate_SmallTarget_IsDeterministicAndInsideSizeBand(SampleFormat format)
        {
            var first = BenchmarkSampleGenerator.Generate(format, SmallTarget);
            var second = BenchmarkSampleGenerator.Generate(format, SmallTarget);

            Assert.Equal(Sha256(first), Sha256(second));
            Assert.InRange(first.Length, (long)(SmallTarget * BenchmarkSampleGenerator.MinFillRatio), SmallTarget);
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void Generate_SmallTarget_ExtractsNonEmptyText(SampleFormat format)
        {
            var bytes = BenchmarkSampleGenerator.Generate(format, SmallTarget);

            var result = ExtractThroughPublicContract(bytes, BenchmarkSampleGenerator.FileName(format, "small"));

            Assert.False(string.IsNullOrWhiteSpace(result.ExtractedText));
        }

        [Fact]
        public void Tiers_StayWithinExtractGuardrails()
        {
            var limits = new MimeDetectionLimits();

            Assert.Equal(10_000_000, BenchmarkSampleGenerator.Tier10MbBytes);
            Assert.InRange(BenchmarkSampleGenerator.Tier100MbBytes,
                95L * 1024 * 1024, 100L * 1024 * 1024);
            Assert.True(BenchmarkSampleGenerator.Tier100MbBytes <= limits.MaxFileSizeBytes);
        }

        // ---- the real 12 files, checked against samples/manifest.json ----

        private sealed class SkipUnlessGeneratedAttribute : TheoryAttribute
        {
            public SkipUnlessGeneratedAttribute()
            {
                if (!Directory.Exists(RepoPath("samples/benchmark")))
                {
                    Skip = "samples/benchmark/ not generated: dotnet run --project SampleGenerator -- --benchmark";
                }
            }
        }

        private static string RepoPath(string relativePath)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "samples", "manifest.json")))
                {
                    return Path.Combine(dir.FullName, relativePath);
                }
            }
            throw new FileNotFoundException("samples/manifest.json not found above " + AppContext.BaseDirectory);
        }

        public static IEnumerable<object[]> ManifestEntries()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(RepoPath("samples/manifest.json")));
            foreach (var entry in doc.RootElement.GetProperty("benchmarkSamples").EnumerateArray())
            {
                yield return new object[]
                {
                    entry.GetProperty("path").GetString()!,
                    entry.GetProperty("sizeBytes").GetInt64(),
                    entry.GetProperty("sha256").GetString()!,
                };
            }
        }

        [Fact]
        public void Manifest_ListsAll12BenchmarkSamples()
        {
            Assert.Equal(12, ManifestEntries().Count());
        }

        [SkipUnlessGenerated]
        [MemberData(nameof(ManifestEntries))]
        public void GeneratedBenchmarkSample_MatchesManifestAndExtractsNonEmptyText(string path, long sizeBytes, string sha256)
        {
            var bytes = File.ReadAllBytes(RepoPath(path));

            Assert.Equal(sizeBytes, bytes.Length);
            Assert.Equal(sha256, Sha256(bytes));
            Assert.True(bytes.Length <= new MimeDetectionLimits().MaxFileSizeBytes);

            var result = ExtractThroughPublicContract(bytes, Path.GetFileName(path));

            Assert.False(string.IsNullOrWhiteSpace(result.ExtractedText));
        }
    }
}
