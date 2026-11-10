using System.Diagnostics;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE.StressHarness
{
    /// <summary>
    /// `batch` mode (ADR-007): runs <see cref="MainExtractor.ExtractManyAsync"/> over the samples
    /// (repeated <c>repeat</c> times) at MaxDOP=1 and MaxDOP=ProcessorCount, reports throughput,
    /// and checks every item against a serial <see cref="MainExtractor.Extract(ExtractionRequest)"/>.
    /// Then reports the peak managed heap of <see cref="MainExtractor.ExtractManyAsync"/> against
    /// <see cref="MainExtractor.ExtractManyStreamingAsync"/> at MaxDOP=ProcessorCount (ADR-011).
    /// Usage: batch [repeat] [samplesDir]
    /// </summary>
    internal static class BatchMode
    {
        private const int DefaultRepeat = 20;

        public static async Task RunAsync(string[] args)
        {
            int repeat = args.Length > 0 && int.TryParse(args[0], out var r) ? r : DefaultRepeat;
            string? samplesDir = args.Length > 1 ? args[1] : null;

            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var extractor = provider.GetRequiredService<MainExtractor>();
            var helper = provider.GetRequiredService<IExtractionHelper>();

            var named = Program.LoadNamedSamples(samplesDir, out var resolvedDirectory);
            var samples = named.Select(n => n.Bytes).ToList();
            var expectedFailures = ExpectedFailures.Load(resolvedDirectory);
            var failingSamples = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mimes = named.Select(n => DetectMime(helper, n.Bytes, n.Name, failingSamples)).ToList();
            var requests = Enumerable.Range(0, repeat)
                .SelectMany(_ => samples.Select((bytes, i) => new ExtractionRequest
                {
                    FileBytes = bytes,
                    FileName = $"sample_{i}",
                    DetectedMimeType = mimes[i],
                }))
                .ToList();

            Console.WriteLine($"Batch mode | Samples: {samples.Count} | Repeat: {repeat} | Items: {requests.Count} | ProcessorCount: {Environment.ProcessorCount}");

            // Serial baseline; also warms JIT/caches before the timed runs.
            var serial = requests.Select(req => Outcome(() => extractor.Extract(req))).ToList();
            int serialOk = serial.Count(o => o.StartsWith("ok|", StringComparison.Ordinal));
            for (int k = 0; k < serial.Count; k++)
            {
                if (!serial[k].StartsWith("ok|", StringComparison.Ordinal))
                {
                    failingSamples.Add(named[k % named.Count].Name);
                }
            }

            Console.WriteLine($"Serial baseline: {serialOk}/{serial.Count} succeeded");

            long mismatches = 0;
            foreach (var dop in new[] { 1, Environment.ProcessorCount }.Distinct())
            {
                var sw = Stopwatch.StartNew();
                var batch = await extractor.ExtractManyAsync(requests, dop);
                sw.Stop();

                int failed = batch.Count(b => !b.Succeeded);
                int itemMismatches = batch.Count(b => Outcome(b) != serial[b.Index]);
                mismatches += itemMismatches;
                Console.WriteLine(
                    $"MaxDOP={dop,-3} | {sw.Elapsed.TotalSeconds:F3} s | {requests.Count / sw.Elapsed.TotalSeconds:F2} items/sec | " +
                    $"per-item errors: {failed} | mismatches vs serial: {itemMismatches}");
            }

            // Peak managed heap (ADR-011): the list API keeps every result until the batch returns; the
            // streaming API keeps at most 2 x MaxDOP + 1. The streaming consumer drops each result after
            // comparing it; the list run holds the whole batch. Peaks are sampled, so they are approximate.
            int peakDop = Environment.ProcessorCount;
            using (var sampler = new HeapSampler())
            {
                IReadOnlyList<BatchExtractionResult>? batch = await extractor.ExtractManyAsync(requests, peakDop);
                long retained = GC.GetTotalMemory(forceFullCollection: true);
                GC.KeepAlive(batch);
                batch = null; // so the streaming run's baseline does not include these results
                Console.WriteLine($"Peak heap ExtractManyAsync          MaxDOP={peakDop} | baseline {Mb(sampler.Baseline)} MB | peak {Mb(sampler.Peak)} MB | retained at return {Mb(retained)} MB");
            }

            using (var sampler = new HeapSampler())
            {
                long streamMismatches = 0;
                int streamed = 0;
                await foreach (var item in extractor.ExtractManyStreamingAsync(requests, peakDop))
                {
                    streamed++;
                    if (Outcome(item) != serial[item.Index]) streamMismatches++;
                }
                mismatches += streamMismatches + (streamed == requests.Count ? 0 : 1);
                Console.WriteLine($"Peak heap ExtractManyStreamingAsync MaxDOP={peakDop} | baseline {Mb(sampler.Baseline)} MB | peak {Mb(sampler.Peak)} MB | items {streamed} | mismatches vs serial: {streamMismatches}");
            }

            Console.WriteLine(mismatches == 0 ? "[PASS] batch output identical to serial Extract()" : "[FAIL] batch output differs from serial Extract()");
            Environment.ExitCode = ExitCodeFor(expectedFailures, failingSamples, mismatches);
        }

        // Adversarial samples fail identically in serial and batch (caught as mismatches otherwise): the run
        // fails when the failing set (serial Extract or DetectMime failures) differs from the expected-failures
        // set in either direction, or batch diverged from serial.
        internal static int ExitCodeFor(IReadOnlySet<string> expected, IReadOnlySet<string> failing, long mismatches)
        {
            int setCode = ExpectedFailures.ExitCodeFor(expected, failing);
            if (mismatches > 0)
            {
                Console.WriteLine($"[FAIL] mismatches: {mismatches}");
                return 1;
            }

            return setCode;
        }

        // A detection failure counts as a failing sample; the request then carries a generic mime and fails per-item in Extract.
        private static string DetectMime(IExtractionHelper helper, byte[] bytes, string name, HashSet<string> failingSamples)
        {
            try
            {
                return helper.MimeFor(helper.DetectMimeTypeFromBytes(bytes)) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[detect] {name} failed: {ex.Message}");
                failingSamples.Add(name);
                return "application/octet-stream";
            }
        }

        // Comparable summary of one extraction: mime + text on success, exception type on failure.
        private static string Outcome(Func<DocumentExtractionResult> extract)
        {
            try
            {
                var result = extract();
                return $"ok|{result.MimeType}|{result.ExtractedText}";
            }
            catch (Exception ex)
            {
                return $"err|{ex.GetType().FullName}";
            }
        }

        private static string Mb(long bytes) => (bytes / (1024.0 * 1024.0)).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

        // Samples GC.GetTotalMemory every 10 ms from a full-GC baseline until disposed; Peak is the highest sample.
        private sealed class HeapSampler : IDisposable
        {
            private readonly Timer _timer;
            private long _peak;

            public HeapSampler()
            {
                Baseline = GC.GetTotalMemory(forceFullCollection: true);
                _peak = Baseline;
                _timer = new Timer(_ => Sample(), null, 0, 10);
            }

            public long Baseline { get; }

            public long Peak { get { Sample(); return Interlocked.Read(ref _peak); } }

            private void Sample()
            {
                long now = GC.GetTotalMemory(forceFullCollection: false);
                long seen;
                while (now > (seen = Interlocked.Read(ref _peak)) && Interlocked.CompareExchange(ref _peak, now, seen) != seen) { }
            }

            public void Dispose() => _timer.Dispose();
        }

        private static string Outcome(BatchExtractionResult item) =>
            item.Succeeded ? $"ok|{item.Result!.MimeType}|{item.Result.ExtractedText}" : $"err|{item.Error!.GetType().FullName}";
    }
}
