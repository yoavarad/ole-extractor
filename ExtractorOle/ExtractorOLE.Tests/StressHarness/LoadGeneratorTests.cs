using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExtractorOLE.StressHarness;
using Xunit;

namespace ExtractorOLE.Tests.StressHarness
{
    public class LoadGeneratorTests
    {
        private static List<byte[]> MakeSamples(int count)
        {
            var samples = new List<byte[]>();
            for (int i = 0; i < count; i++)
            {
                samples.Add(new byte[] { (byte)i });
            }
            return samples;
        }

        [Fact]
        public async Task RunAsync_Concurrency50_ReachesPeakObservedConcurrencyOf50()
        {
            var samples = MakeSamples(10);
            var options = new LoadGenerationOptions(Concurrency: 50, Iterations: 200);

            Func<byte[], int> op = bytes =>
            {
                Thread.Sleep(20);
                return bytes.Length;
            };

            var summary = await LoadGenerator.RunAsync(op, samples, options);

            Assert.Equal(50, summary.PeakObservedConcurrency);
        }

        [Fact]
        public async Task RunAsync_Concurrency500_ReachesPeakObservedConcurrencyOf500()
        {
            var samples = MakeSamples(10);
            var options = new LoadGenerationOptions(Concurrency: 500, Iterations: 1000);

            Func<byte[], int> op = bytes =>
            {
                // Wide enough sleep window that all 500 worker threads have time
                // to spin up and overlap before the earliest ones complete.
                Thread.Sleep(200);
                return bytes.Length;
            };

            var summary = await LoadGenerator.RunAsync(op, samples, options);

            Assert.Equal(500, summary.PeakObservedConcurrency);
        }

        [Fact]
        public async Task RunAsync_MixedStrategy_HasOverlapOfSharedAndDistinctFiles()
        {
            var samples = MakeSamples(5);
            var options = new LoadGenerationOptions(Concurrency: 20, Iterations: 200, Strategy: FileSelectionStrategy.Mixed);

            var events = new ConcurrentBag<(byte[] Sample, long StartTicks, long EndTicks)>();

            Func<byte[], int> op = bytes =>
            {
                var start = Stopwatch();
                Thread.Sleep(15);
                var end = Stopwatch();
                events.Add((bytes, start, end));
                return bytes.Length;
            };

            await LoadGenerator.RunAsync(op, samples, options);

            var eventList = events.ToList();

            bool foundOverlap = false;
            foreach (var candidate in eventList)
            {
                var overlapping = eventList.Where(e =>
                    e.StartTicks < candidate.EndTicks && e.EndTicks > candidate.StartTicks).ToList();

                bool hasSameRef = overlapping.Count(e => ReferenceEquals(e.Sample, candidate.Sample)) >= 2;
                bool hasDifferentRef = overlapping.Any(e => !ReferenceEquals(e.Sample, candidate.Sample));

                if (hasSameRef && hasDifferentRef)
                {
                    foundOverlap = true;
                    break;
                }
            }

            Assert.True(foundOverlap, "Expected at least one instant with both a shared-file overlap and a distinct-file overlap.");
        }

        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

        private static long Stopwatch() => Clock.ElapsedTicks;

        [Fact]
        public async Task RunAsync_AllSuccess_ReportsAllSucceededAndAllLatenciesCollected()
        {
            var samples = MakeSamples(3);
            var options = new LoadGenerationOptions(Concurrency: 10, Iterations: 50);

            Func<byte[], int> op = bytes => bytes.Length;

            var summary = await LoadGenerator.RunAsync(op, samples, options);

            Assert.Equal(50, summary.Succeeded);
            Assert.Equal(0, summary.Failed);
            Assert.Equal(50, summary.LatenciesMs.Count);
        }

        [Fact]
        public async Task RunAsync_SomeFailures_CountsFailuresAndCompletesRemaining()
        {
            var samples = MakeSamples(3);
            var options = new LoadGenerationOptions(Concurrency: 10, Iterations: 50);

            Func<byte[], int> op = bytes =>
            {
                if (bytes[0] % 2 == 0)
                {
                    throw new InvalidOperationException("simulated failure");
                }
                return bytes.Length;
            };

            var exception = await Record.ExceptionAsync(() => LoadGenerator.RunAsync(op, samples, options));

            Assert.Null(exception);

            var summary = await LoadGenerator.RunAsync(op, samples, options);

            Assert.Equal(50, summary.TotalCalls);
            Assert.True(summary.Failed > 0);
            Assert.True(summary.Succeeded > 0);
            Assert.Equal(summary.Succeeded + summary.Failed, summary.TotalCalls);
        }

        [Fact]
        public void SelectFile_SharedFile_AlwaysReturnsSameInstance()
        {
            var samples = MakeSamples(5);

            var first = LoadGenerator.SelectFile(0, samples, FileSelectionStrategy.SharedFile);
            var second = LoadGenerator.SelectFile(1, samples, FileSelectionStrategy.SharedFile);
            var third = LoadGenerator.SelectFile(17, samples, FileSelectionStrategy.SharedFile);

            Assert.Same(first, second);
            Assert.Same(first, third);
            Assert.Same(samples[0], first);
        }

        [Fact]
        public void SelectFile_DistinctFiles_RoundRobinsThroughAllSamples()
        {
            var samples = MakeSamples(4);

            var seen = new HashSet<byte[]>();
            for (int i = 0; i < 12; i++)
            {
                var selected = LoadGenerator.SelectFile(i, samples, FileSelectionStrategy.DistinctFiles);
                seen.Add(selected);
                Assert.Same(samples[i % samples.Count], selected);
            }

            Assert.Equal(samples.Count, seen.Count);
        }
    }
}
