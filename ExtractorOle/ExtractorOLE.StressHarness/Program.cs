using System.Collections.Concurrent;
using System.Diagnostics;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExtractorOLE;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE.StressHarness
{
    /// <summary>
    /// Hand-rolled concurrent-load console harness (ADR-002).
    ///
    /// Not a unit test: exercises <see cref="MainExtractor"/> under N concurrent
    /// callers via Parallel.ForEachAsync with SemaphoreSlim-bounded concurrency,
    /// timing each call with Stopwatch and reporting throughput/latency.
    ///
    /// Usage: dotnet run --project ExtractorOLE.StressHarness -- [concurrency] [iterations] [samplesDir]
    /// All arguments are optional.
    /// </summary>
    internal static class Program
    {
        private const int DefaultConcurrency = 8;
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

            var latencies = new ConcurrentBag<double>();
            long failures = 0;
            long mismatches = 0;

            var baselines = new List<DocumentExtractionResult?>(samples.Count);
            for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
            {
                try
                {
                    baselines.Add(extractor.Extract(samples[sampleIndex]));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[baseline] sample {sampleIndex} failed: {ex.Message}");
                    baselines.Add(null);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
            long heapBefore = GC.GetTotalMemory(false);
            int gen0Before = GC.CollectionCount(0), gen1Before = GC.CollectionCount(1), gen2Before = GC.CollectionCount(2);

            using var gate = new SemaphoreSlim(concurrency);
            var indices = Enumerable.Range(0, iterations);

            var overall = Stopwatch.StartNew();

            await Parallel.ForEachAsync(indices, async (index, ct) =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    var bytes = samples[index % samples.Count];
                    var baseline = baselines[index % samples.Count];
                    var sw = Stopwatch.StartNew();
                    var actual = await Task.Run(() => extractor.Extract(bytes), ct);
                    sw.Stop();
                    latencies.Add(sw.Elapsed.TotalMilliseconds);

                    if (baseline is not null && (actual.MimeType != baseline.MimeType || actual.ExtractedText != baseline.ExtractedText))
                    {
                        Interlocked.Increment(ref mismatches);
                        Console.WriteLine($"[iteration {index}] mismatch: result diverged from baseline (mime/text).");
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failures);
                    Console.WriteLine($"[iteration {index}] failed: {ex.Message}");
                }
                finally
                {
                    gate.Release();
                }
            });

            overall.Stop();

            long allocatedAfter = GC.GetTotalAllocatedBytes(precise: false);
            long heapAfter = GC.GetTotalMemory(false);
            int gen0After = GC.CollectionCount(0), gen1After = GC.CollectionCount(1), gen2After = GC.CollectionCount(2);
            long peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64;

            PrintSummary(
                overall.Elapsed,
                latencies.ToList(),
                failures,
                iterations,
                mismatches,
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
                samples.Count);

            if (mismatches > 0)
            {
                Environment.ExitCode = 1;
            }
        }

        private static List<byte[]> LoadSamples(string? samplesDir)
        {
            var samples = new List<byte[]>();

            if (!string.IsNullOrWhiteSpace(samplesDir) && Directory.Exists(samplesDir))
            {
                foreach (var file in Directory.EnumerateFiles(samplesDir))
                {
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

        private static void PrintSummary(
            TimeSpan elapsed,
            List<double> latencies,
            long failures,
            int totalIterations,
            long mismatches,
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
            int sampleCount)
        {
            latencies.Sort();
            var succeeded = latencies.Count;
            var throughput = elapsed.TotalSeconds > 0 ? succeeded / elapsed.TotalSeconds : 0;

            Console.WriteLine();
            Console.WriteLine("=== Stress harness summary ===");
            Console.WriteLine($"Total iterations : {totalIterations}");
            Console.WriteLine($"Succeeded        : {succeeded}");
            Console.WriteLine($"Failed           : {failures}");
            Console.WriteLine($"Wall-clock time  : {elapsed.TotalSeconds:F3} s");
            Console.WriteLine($"Throughput       : {throughput:F2} ops/sec");

            if (succeeded > 0)
            {
                Console.WriteLine($"Latency min      : {latencies[0]:F2} ms");
                Console.WriteLine($"Latency avg      : {latencies.Average():F2} ms");
                Console.WriteLine($"Latency p50      : {Percentile(latencies, 50):F2} ms");
                Console.WriteLine($"Latency p95      : {Percentile(latencies, 95):F2} ms");
                Console.WriteLine($"Latency p99      : {Percentile(latencies, 99):F2} ms");
                Console.WriteLine($"Latency max      : {latencies[^1]:F2} ms");
            }

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
