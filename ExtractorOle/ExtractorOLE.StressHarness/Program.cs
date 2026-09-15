using System.Diagnostics;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExtractorOLE;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE.StressHarness
{
    /// <summary>
    /// Hand-rolled concurrent-load console harness (ADR-002).
    ///
    /// Not a unit test: exercises <see cref="MainExtractor"/> and
    /// <see cref="IExtractionHelper"/> under N concurrent callers via
    /// <see cref="LoadGenerator"/> (SemaphoreSlim-bounded concurrency), timing
    /// each call and reporting throughput/latency/memory, and comparing
    /// extraction results against a single-threaded baseline to catch
    /// shared-mutable-state corruption under concurrency.
    ///
    /// Usage: dotnet run --project ExtractorOLE.StressHarness -- [concurrency] [iterations] [samplesDir]
    /// All arguments are optional.
    /// </summary>
    internal static class Program
    {
        private const int DefaultConcurrency = 50;
        private const int DefaultIterations = 200;

        private static async Task Main(string[] args)
        {
            int concurrency = args.Length > 0 && int.TryParse(args[0], out var c) ? c : DefaultConcurrency;
            int iterations = args.Length > 1 && int.TryParse(args[1], out var i) ? i : DefaultIterations;
            string? samplesDir = args.Length > 2 ? args[2] : null;

            var samples = LoadSamples(samplesDir);

            Console.WriteLine($"Samples: {samples.Count} | Concurrency: {concurrency} | Iterations: {iterations}");

            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var extractor = provider.GetRequiredService<MainExtractor>();
            var helper = provider.GetRequiredService<IExtractionHelper>();

            var options = new LoadGenerationOptions(concurrency, iterations);

            Console.WriteLine();
            Console.WriteLine("--- DetectMimeTypeFromBytes ---");
            var mimeSummary = await LoadGenerator.RunAsync(helper.DetectMimeTypeFromBytes, samples, options);
            PrintSummary(mimeSummary);

            // Single-threaded baseline per sample, used below to detect
            // shared-mutable-state corruption once Extract runs concurrently.
            var baselines = new Dictionary<byte[], DocumentExtractionResult?>(ReferenceEqualityComparer.Instance);
            foreach (var sample in samples)
            {
                try
                {
                    baselines[sample] = extractor.Extract(sample);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[baseline] sample failed: {ex.Message}");
                    baselines[sample] = null;
                }
            }

            long mismatches = 0;
            DocumentExtractionResult ExtractAndCompare(byte[] bytes)
            {
                var actual = extractor.Extract(bytes);
                if (baselines.TryGetValue(bytes, out var baseline) && baseline is not null &&
                    (actual.MimeType != baseline.MimeType || actual.ExtractedText != baseline.ExtractedText))
                {
                    Interlocked.Increment(ref mismatches);
                }
                return actual;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
            long heapBefore = GC.GetTotalMemory(false);
            int gen0Before = GC.CollectionCount(0), gen1Before = GC.CollectionCount(1), gen2Before = GC.CollectionCount(2);

            Console.WriteLine();
            Console.WriteLine("--- Extract ---");
            var extractSummary = await LoadGenerator.RunAsync(ExtractAndCompare, samples, options);

            long allocatedAfter = GC.GetTotalAllocatedBytes(precise: false);
            long heapAfter = GC.GetTotalMemory(false);
            int gen0After = GC.CollectionCount(0), gen1After = GC.CollectionCount(1), gen2After = GC.CollectionCount(2);
            long peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64;

            PrintSummary(extractSummary);
            PrintDiagnostics(
                extractSummary,
                allocatedBefore,
                allocatedAfter,
                heapBefore,
                heapAfter,
                gen0Before,
                gen0After,
                gen1Before,
                gen1After,
                gen2Before,
                gen2After,
                peakWorkingSetBytes,
                mismatches,
                samples.Count);

            if (mismatches > 0)
            {
                Environment.ExitCode = 1;
            }
        }

        private static List<byte[]> LoadSamples(string? samplesDir)
        {
            var samples = new List<byte[]>();
            var directory = !string.IsNullOrWhiteSpace(samplesDir) && Directory.Exists(samplesDir)
                ? samplesDir
                : FindDefaultSamplesDirectory();

            if (directory is not null)
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                {
                    if (string.Equals(Path.GetFileName(file), "manifest.json", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    samples.Add(File.ReadAllBytes(file));
                }
            }

            if (samples.Count == 0)
            {
                Console.WriteLine("No sample directory provided (or it was empty) - generating a synthetic in-memory .docx sample.");
                samples.Add(CreateSyntheticDocx());
            }

            return samples;
        }

        private static string? FindDefaultSamplesDirectory()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "samples");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            return null;
        }

        private static byte[] CreateSyntheticDocx()
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document(
                    new Body(
                        new Paragraph(
                            new Run(
                                new Text("Synthetic load-test document generated by ExtractorOLE.StressHarness.")))));
                mainPart.Document.Save();
            }

            return stream.ToArray();
        }

        private static void PrintSummary(LoadGenerationSummary summary)
        {
            var latencies = summary.LatenciesMs.OrderBy(l => l).ToList();
            var throughput = summary.Elapsed.TotalSeconds > 0 ? summary.Succeeded / summary.Elapsed.TotalSeconds : 0;

            Console.WriteLine();
            Console.WriteLine("=== Stress harness summary ===");
            Console.WriteLine($"Total iterations : {summary.TotalCalls}");
            Console.WriteLine($"Succeeded        : {summary.Succeeded}");
            Console.WriteLine($"Failed           : {summary.Failed}");
            Console.WriteLine($"Peak concurrency : {summary.PeakObservedConcurrency}");
            Console.WriteLine($"Wall-clock time  : {summary.Elapsed.TotalSeconds:F3} s");
            Console.WriteLine($"Throughput       : {throughput:F2} ops/sec");

            if (latencies.Count > 0)
            {
                Console.WriteLine($"Latency min      : {latencies[0]:F2} ms");
                Console.WriteLine($"Latency avg      : {latencies.Average():F2} ms");
                Console.WriteLine($"Latency p50      : {Percentile(latencies, 50):F2} ms");
                Console.WriteLine($"Latency p95      : {Percentile(latencies, 95):F2} ms");
                Console.WriteLine($"Latency p99      : {Percentile(latencies, 99):F2} ms");
                Console.WriteLine($"Latency max      : {latencies[^1]:F2} ms");
            }
        }

        private static void PrintDiagnostics(
            LoadGenerationSummary summary,
            long allocatedBefore,
            long allocatedAfter,
            long heapBefore,
            long heapAfter,
            int gen0Before,
            int gen0After,
            int gen1Before,
            int gen1After,
            int gen2Before,
            int gen2After,
            long peakWorkingSetBytes,
            long mismatches,
            int sampleCount)
        {
            var succeeded = summary.Succeeded;

            Console.WriteLine();
            Console.WriteLine("=== Memory under load ===");
            Console.WriteLine($"Allocated (total)  : {(allocatedAfter - allocatedBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Allocated / op     : {(succeeded > 0 ? (allocatedAfter - allocatedBefore) / 1024.0 / succeeded : 0):F2} KB");
            Console.WriteLine($"Heap size delta    : {(heapAfter - heapBefore) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"GC collections     : Gen0={gen0After - gen0Before} Gen1={gen1After - gen1Before} Gen2={gen2After - gen2Before}");
            Console.WriteLine($"Peak working set   : {peakWorkingSetBytes / 1024.0 / 1024.0:F2} MB");

            Console.WriteLine();
            Console.WriteLine("=== Shared-state / correctness check ===");
            Console.WriteLine($"Distinct samples   : {sampleCount} (same-file concurrency when sampleCount < concurrency)");
            Console.WriteLine($"Correctness checks : {succeeded - mismatches}/{succeeded} matched baseline");
            Console.WriteLine($"Mismatches         : {mismatches}{(mismatches == 0 ? "  [PASS: no cross-call corruption detected]" : "  [FAIL: shared-mutable-state corruption suspected]")}");
        }

        private static double Percentile(List<double> sortedLatencies, double percentile)
        {
            if (sortedLatencies.Count == 0) return 0;

            var rank = (percentile / 100.0) * (sortedLatencies.Count - 1);
            var lower = (int)Math.Floor(rank);
            var upper = (int)Math.Ceiling(rank);
            if (lower == upper) return sortedLatencies[lower];

            var frac = rank - lower;
            return sortedLatencies[lower] + frac * (sortedLatencies[upper] - sortedLatencies[lower]);
        }
    }
}
