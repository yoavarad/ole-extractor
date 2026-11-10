using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Batch
{
    // MainExtractor.ExtractManyStreamingAsync (ADR-011): same items as ExtractManyAsync, completion order,
    // and memory bounded by back-pressure (at most 2 x MaxDOP results alive besides the consumer's).
    public class ExtractManyStreamingAsyncTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private static readonly Lazy<MainExtractor> Extractor = new(() =>
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        });

        private static readonly Lazy<byte[]> DocxBytes = new(() =>
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "samples/synthetic/multi-embedding/sample.docx");
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
                dir = dir.Parent;
            }

            throw new FileNotFoundException("samples/synthetic/multi-embedding/sample.docx not found");
        });

        private static ExtractionRequest ValidDocx() => new()
        {
            FileBytes = DocxBytes.Value,
            FileName = "sample.docx",
            DetectedMimeType = DocxMime,
        };

        [Fact]
        public async Task Streaming_YieldsSameItemsAsListBatch()
        {
            var requests = new[]
            {
                ValidDocx(),
                new ExtractionRequest { FileBytes = new byte[] { 1, 2, 3 }, FileName = "x.txt", DetectedMimeType = "text/plain" },
                null!,
                ValidDocx(),
                ValidDocx(),
            };

            var list = await Extractor.Value.ExtractManyAsync(requests, 2);
            var streamed = new List<BatchExtractionResult>();
            await foreach (var item in Extractor.Value.ExtractManyStreamingAsync(requests, 2)) streamed.Add(item);

            Assert.Equal(requests.Length, streamed.Count);
            Assert.Equal(Enumerable.Range(0, requests.Length), streamed.Select(s => s.Index).Order());
            foreach (var s in streamed)
            {
                var expected = list[s.Index];
                Assert.Equal(expected.FileName, s.FileName);
                Assert.Equal(expected.Succeeded, s.Succeeded);
                Assert.Equal(expected.Error?.GetType(), s.Error?.GetType());
                Assert.Equal(expected.Result?.ExtractedText, s.Result?.ExtractedText);
            }
            Assert.IsType<UnsupportedFormatException>(streamed.Single(s => s.Index == 1).Error);
            Assert.IsType<ArgumentNullException>(streamed.Single(s => s.Index == 2).Error);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        public async Task SlowConsumer_BoundsItemsPulledFromInput(int dop)
        {
            const int total = 200;
            int pulled = 0;
            IEnumerable<ExtractionRequest> Requests()
            {
                for (int i = 0; i < total; i++)
                {
                    Interlocked.Increment(ref pulled);
                    yield return ValidDocx();
                }
            }

            // Consumer reads one item then stalls; the producer must stop at the channel bound.
            var enumerator = Extractor.Value.ExtractManyStreamingAsync(Requests(), dop).GetAsyncEnumerator();
            try
            {
                Assert.True(await enumerator.MoveNextAsync());

                // dop queued + dop held by workers + 1 held by the consumer. Wait until the producer
                // reaches the bound or stops pulling, so a slow machine cannot pass before it gets there.
                int bound = 2 * dop + 1;
                int last = -1, stable = 0;
                var deadline = DateTime.UtcNow.AddSeconds(30);
                while (DateTime.UtcNow < deadline && Volatile.Read(ref pulled) <= bound && stable < 5)
                {
                    await Task.Delay(100);
                    int now = Volatile.Read(ref pulled);
                    stable = now == last ? stable + 1 : 0;
                    last = now;
                }

                int observed = Volatile.Read(ref pulled);
                Assert.True(observed >= dop + 1, $"producer pulled only {observed} items; expected it to fill the pipeline");
                Assert.True(observed <= bound, $"pulled {observed} items with a stalled consumer; bound {bound}");
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            Assert.True(Volatile.Read(ref pulled) < total, "early dispose did not stop the producer");
        }

        [Fact]
        public async Task CancelMidStream_ThrowsOperationCanceled()
        {
            using var cts = new CancellationTokenSource();
            var requests = Enumerable.Range(0, 50).Select(_ => ValidDocx());

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in Extractor.Value.ExtractManyStreamingAsync(requests, 1, cts.Token))
                {
                    await cts.CancelAsync();
                }
            });
        }

        [Fact]
        public async Task WithCancellation_CancelsTheStream()
        {
            using var cts = new CancellationTokenSource();
            var requests = Enumerable.Range(0, 50).Select(_ => ValidDocx());
            int seen = 0;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in Extractor.Value.ExtractManyStreamingAsync(requests, 2).WithCancellation(cts.Token))
                {
                    if (++seen == 1) await cts.CancelAsync();
                }
            });
            Assert.Equal(1, seen);
        }

        [Fact]
        public async Task EarlyBreak_StopsTheStreamWithoutError()
        {
            int seen = 0;
            await foreach (var _ in Extractor.Value.ExtractManyStreamingAsync(Enumerable.Range(0, 50).Select(_ => ValidDocx()), 2))
            {
                if (++seen == 2) break;
            }
            Assert.Equal(2, seen);
        }

        [Fact]
        public async Task EnumeratorException_FailsTheStream()
        {
            static IEnumerable<ExtractionRequest> Broken()
            {
                yield return ValidDocx();
                throw new InvalidOperationException("source failed");
            }

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var _ in Extractor.Value.ExtractManyStreamingAsync(Broken(), 1)) { }
            });
        }

        [Fact]
        public async Task EmptyInput_YieldsNothing()
        {
            await foreach (var _ in Extractor.Value.ExtractManyStreamingAsync(Array.Empty<ExtractionRequest>()))
            {
                Assert.Fail("expected no items");
            }
        }

        [Fact]
        public void InvalidArguments_ThrowOnCall()
        {
            Assert.Throws<ArgumentNullException>(() => Extractor.Value.ExtractManyStreamingAsync(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => Extractor.Value.ExtractManyStreamingAsync(new[] { ValidDocx() }, 0));
        }
    }
}
