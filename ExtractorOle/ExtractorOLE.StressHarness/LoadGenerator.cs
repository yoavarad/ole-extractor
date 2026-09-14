using System.Collections.Concurrent;
using System.Diagnostics;

namespace ExtractorOLE.StressHarness
{
    public enum FileSelectionStrategy { SharedFile, DistinctFiles, Mixed }

    public sealed record LoadGenerationOptions(int Concurrency, int Iterations, FileSelectionStrategy Strategy = FileSelectionStrategy.Mixed);

    public sealed record LoadGenerationSummary(
        int TotalCalls, int Succeeded, int Failed, TimeSpan Elapsed,
        IReadOnlyList<double> LatenciesMs, int PeakObservedConcurrency);

    public static class LoadGenerator
    {
        public static async Task<LoadGenerationSummary> RunAsync<TResult>(
            Func<byte[], TResult> operation,
            IReadOnlyList<byte[]> samples,
            LoadGenerationOptions options,
            CancellationToken cancellationToken = default)
        {
            var latencies = new ConcurrentBag<double>();
            long failures = 0;
            int inFlight = 0;
            int peakConcurrency = 0;

            // The default ThreadPool only injects new worker threads gradually
            // (hill-climbing, roughly one every ~500ms once starved), which caps
            // how many concurrent operations can actually start well below
            // higher requested concurrency levels. Raise the floor and force the
            // pool to actually create that many threads up front so the timed
            // run below isn't throttled by lazy thread creation. A small buffer
            // above Concurrency absorbs incidental background work (e.g. the
            // hosting process's own threads) so it can't transiently steal a
            // slot our own operations need to reach the requested peak.
            var warmThreads = options.Concurrency + Math.Max(8, options.Concurrency / 20);
            ThreadPool.GetMinThreads(out var currentMinWorker, out var currentMinIocp);
            if (currentMinWorker < warmThreads)
            {
                ThreadPool.SetMinThreads(warmThreads, currentMinIocp);
            }
            await WarmUpThreadPoolAsync(warmThreads).ConfigureAwait(false);

            using var gate = new SemaphoreSlim(options.Concurrency);

            var overall = Stopwatch.StartNew();

            // Single thread-pool hop per call: Task.Run schedules the whole
            // gated-and-timed body (including the synchronous `operation`) on
            // one pool thread, rather than an outer worker loop awaiting a
            // second inner Task.Run, which would need two pool threads per
            // concurrent unit of work.
            var tasks = new Task[options.Iterations];
            for (int index = 0; index < options.Iterations; index++)
            {
                var capturedIndex = index;
                tasks[index] = Task.Run(async () =>
                {
                    var acquired = false;
                    try
                    {
                        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                        acquired = true;

                        var bytes = SelectFile(capturedIndex, samples, options.Strategy);

                        var current = Interlocked.Increment(ref inFlight);
                        UpdatePeak(ref peakConcurrency, current);

                        var sw = Stopwatch.StartNew();
                        try
                        {
                            operation(bytes);
                            sw.Stop();
                            latencies.Add(sw.Elapsed.TotalMilliseconds);
                        }
                        finally
                        {
                            Interlocked.Decrement(ref inFlight);
                        }
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref failures);
                    }
                    finally
                    {
                        if (acquired)
                        {
                            gate.Release();
                        }
                    }
                }, cancellationToken);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            overall.Stop();

            var latencyList = latencies.ToList();
            return new LoadGenerationSummary(
                TotalCalls: options.Iterations,
                Succeeded: latencyList.Count,
                Failed: (int)failures,
                Elapsed: overall.Elapsed,
                LatenciesMs: latencyList,
                PeakObservedConcurrency: peakConcurrency);
        }

        private static async Task WarmUpThreadPoolAsync(int concurrency)
        {
            var warmup = new Task[concurrency];
            for (int i = 0; i < concurrency; i++)
            {
                warmup[i] = Task.Run(() => Thread.Sleep(1));
            }
            await Task.WhenAll(warmup).ConfigureAwait(false);
        }

        private static void UpdatePeak(ref int peak, int candidate)
        {
            int initial, computed;
            do
            {
                initial = Volatile.Read(ref peak);
                if (candidate <= initial) return;
                computed = candidate;
            }
            while (Interlocked.CompareExchange(ref peak, computed, initial) != initial);
        }

        public static byte[] SelectFile(int callIndex, IReadOnlyList<byte[]> samples, FileSelectionStrategy strategy)
        {
            if (samples.Count == 0)
            {
                throw new ArgumentException("At least one sample is required.", nameof(samples));
            }

            switch (strategy)
            {
                case FileSelectionStrategy.SharedFile:
                    return samples[0];

                case FileSelectionStrategy.DistinctFiles:
                    return samples[callIndex % samples.Count];

                case FileSelectionStrategy.Mixed:
                default:
                    // Interleave: even calls hit the shared "hot" sample (shared-state case),
                    // odd calls round-robin across the remaining samples (different-files case),
                    // so both happen concurrently within the same batch.
                    if (callIndex % 2 == 0 || samples.Count == 1)
                    {
                        return samples[0];
                    }

                    var remainingCount = samples.Count - 1;
                    var offset = (callIndex / 2) % remainingCount;
                    return samples[1 + offset];
            }
        }
    }
}
