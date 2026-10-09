using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using ExtractorOLE;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE.Benchmarks
{
    /// <summary>
    /// Benchmarks <see cref="MainExtractor.Extract(ExtractionRequest)"/> (the facade: guardrails and
    /// parse-time checks included) against the fixed samples in samples/benchmark/, one case per
    /// format. Profiling mechanism from ADR-002 / testing-strategy.md; NOT part of the test gate.
    /// Run: dotnet run -c Release --project ExtractorOle/ExtractorOLE.Benchmarks [-- --filter "*10mb*"]
    /// </summary>
    public abstract class ExtractionBenchmarks
    {
        private MainExtractor _extractor = null!;
        private ExtractionRequest _request = null!;

        // Id like "docx-10mb"; see BenchmarkSamples.
        [ParamsSource(nameof(SampleIds))]
        public string Sample { get; set; } = string.Empty;

        public IEnumerable<string> SampleIds => BenchmarkSamples.Ids(Tier);

        protected abstract string Tier { get; }

        [GlobalSetup]
        public void Setup()
        {
            string path = BenchmarkSamples.PathFor(Sample);
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"Benchmark sample '{path}' is missing. Regenerate with: {BenchmarkSamples.RegenerateCommand}", path);

            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();
            _extractor = provider.GetRequiredService<MainExtractor>();
            var helper = provider.GetRequiredService<IExtractionHelper>();

            _request = new ExtractionRequest
            {
                FileBytes = File.ReadAllBytes(path),
                FileName = Path.GetFileName(path),
                DetectedMimeType = helper.MimeFor(BenchmarkSamples.MimeFor(Sample)),
            };

            // Sanity check (#165): a sample whose extraction silently yields nothing makes its row
            // meaningless, so fail the setup loudly instead of timing an empty result.
            var result = _extractor.Extract(_request);
            if (string.IsNullOrWhiteSpace(result.ExtractedText))
                throw new InvalidOperationException(
                    $"Benchmark sample '{Sample}' ({path}) extracted no text; the benchmark would measure nothing. " +
                    $"Regenerate with: {BenchmarkSamples.RegenerateCommand}");
            // Scaling check (#185): a sample padded with unread bytes extracts a tiny, fixed text, so
            // its cost would not grow with size. Real samples carry text on the order of size/3.
            if (result.ExtractedText.Length < _request.FileBytes.Length / 100)
                throw new InvalidOperationException(
                    $"Benchmark sample '{Sample}' ({path}) extracted only {result.ExtractedText.Length} text chars from " +
                    $"{_request.FileBytes.Length} bytes; the size is padding, not content. " +
                    $"Regenerate with: {BenchmarkSamples.RegenerateCommand}");
            Console.WriteLine(
                $"// Sample {Sample}: {result.ExtractedText.Length} text chars, {result.EmbeddedFiles.Count} embedded files");
        }

        [Benchmark]
        public DocumentExtractionResult Extract() => _extractor.Extract(_request);
    }

    [Config(typeof(Tier10MbConfig))]
    public class Extract10MbBenchmarks : ExtractionBenchmarks
    {
        protected override string Tier => "10mb";
    }

    [Config(typeof(Tier100MbConfig))]
    public class Extract100MbBenchmarks : ExtractionBenchmarks
    {
        protected override string Tier => "100mb";
    }

    // One invocation per iteration (Monitoring): each Extract() is far above timer resolution and a
    // 100MB input must not be re-run thousands of times. P95 needs enough iterations to mean
    // something, hence 30 at 10MB; 100MB is capped at 15 to keep the whole run to a few minutes.
    internal sealed class Tier10MbConfig : TierConfig { public Tier10MbConfig() : base(warmups: 3, iterations: 30) { } }
    internal sealed class Tier100MbConfig : TierConfig { public Tier100MbConfig() : base(warmups: 1, iterations: 15) { } }

    internal abstract class TierConfig : ManualConfig
    {
        protected TierConfig(int warmups, int iterations)
        {
            AddJob(Job.Default.WithStrategy(RunStrategy.Monitoring).WithWarmupCount(warmups).WithIterationCount(iterations));
            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.Min, StatisticColumn.Median, StatisticColumn.P95, StatisticColumn.Max,
                new AllocatedPerInputByteColumn(), new P95TargetColumn());
        }
    }
}
