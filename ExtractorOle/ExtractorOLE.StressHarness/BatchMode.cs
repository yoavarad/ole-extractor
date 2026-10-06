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

            var samples = Program.LoadSamples(samplesDir);
            var mimes = samples.Select(bytes => DetectMime(helper, bytes)).ToList();
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
            int serialOk = serial.Count(o => o.StartsWith("ok|"));
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

            Console.WriteLine(mismatches == 0 ? "[PASS] batch output identical to serial Extract()" : "[FAIL] batch output differs from serial Extract()");
            if (mismatches > 0) Environment.ExitCode = 1;
        }

        // Adversarial samples can make detection throw; such items then fail per-item in Extract.
        private static string DetectMime(IExtractionHelper helper, byte[] bytes)
        {
            try
            {
                return helper.MimeFor(helper.DetectMimeTypeFromBytes(bytes)) ?? string.Empty;
            }
            catch (Exception)
            {
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

        private static string Outcome(BatchExtractionResult item) =>
            item.Succeeded ? $"ok|{item.Result!.MimeType}|{item.Result.ExtractedText}" : $"err|{item.Error!.GetType().FullName}";
    }
}
