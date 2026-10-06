using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Batch
{
    // MainExtractor.ExtractManyAsync (ADR-007): input order, per-item errors, cancellation, and
    // equality with serial Extract() over the samples/manifest.json corpus.
    public class ExtractManyAsyncTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private static readonly Lazy<MainExtractor> Extractor = new(() =>
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        });

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

        // Every manifest entry that records its expected mime (mixed formats, incl. deliberately
        // rejected samples, so the batch carries both successes and per-item errors).
        private static List<ExtractionRequest> ManifestRequests()
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(FindRepoRootFile("samples/manifest.json")));
            return doc.RootElement.GetProperty("samples").EnumerateArray()
                .Where(e => e.TryGetProperty("expectedDetectedFormat", out var f) && f.TryGetProperty("mimeType", out _))
                .Select(e =>
                {
                    var path = e.GetProperty("path").GetString()!;
                    return new ExtractionRequest
                    {
                        FileBytes = File.ReadAllBytes(FindRepoRootFile(path)),
                        FileName = Path.GetFileName(path),
                        DetectedMimeType = e.GetProperty("expectedDetectedFormat").GetProperty("mimeType").GetString()!,
                    };
                })
                .ToList();
        }

        private static ExtractionRequest ValidDocx() => new()
        {
            FileBytes = File.ReadAllBytes(FindRepoRootFile("samples/synthetic/multi-embedding/sample.docx")),
            FileName = "sample.docx",
            DetectedMimeType = DocxMime,
        };

        private static string Embedded(DocumentExtractionResult r) =>
            string.Join(",", r.EmbeddedFiles.Select(f => $"{f.FileName}:{f.SizeInBytes}:{f.BinaryData.Length}"));

        private static string Outcome(Func<DocumentExtractionResult> extract)
        {
            try
            {
                var r = extract();
                return $"ok|{r.MimeType}|{r.ExtractedText}|{Embedded(r)}";
            }
            catch (Exception ex)
            {
                return $"err|{ex.GetType().FullName}";
            }
        }

        private static string Outcome(BatchExtractionResult item) =>
            item.Succeeded
                ? $"ok|{item.Result!.MimeType}|{item.Result.ExtractedText}|{Embedded(item.Result)}"
                : $"err|{item.Error!.GetType().FullName}";

        [Fact]
        public async Task ManifestBatch_InParallel_MatchesSerialExtract_InInputOrder()
        {
            var requests = ManifestRequests();
            // Repeat so more items than cores run concurrently, including same-file concurrency.
            requests = Enumerable.Repeat(requests, 3).SelectMany(r => r).ToList();
            Assert.True(requests.Count > 10, "corpus too small for a meaningful batch");

            var serial = requests.Select(r => Outcome(() => Extractor.Value.Extract(r))).ToList();
            var batch = await Extractor.Value.ExtractManyAsync(requests, Math.Max(4, Environment.ProcessorCount));

            Assert.Equal(requests.Count, batch.Count);
            for (int i = 0; i < batch.Count; i++)
            {
                Assert.Equal(i, batch[i].Index);
                Assert.Equal(requests[i].FileName, batch[i].FileName);
                Assert.Equal(serial[i], Outcome(batch[i]));
            }
            Assert.Contains(batch, b => b.Succeeded);
            Assert.Contains(batch, b => !b.Succeeded);
        }

        [Fact]
        public async Task FailingInputs_AreReportedPerItem_WithoutFailingTheBatch()
        {
            var requests = new[]
            {
                ValidDocx(),
                new ExtractionRequest { FileBytes = new byte[] { 1, 2, 3 }, FileName = "x.txt", DetectedMimeType = "text/plain" },
                null!,
                ValidDocx(),
            };

            var batch = await Extractor.Value.ExtractManyAsync(requests, 2);

            Assert.Equal(4, batch.Count);
            Assert.True(batch[0].Succeeded);
            Assert.Null(batch[0].Error);
            Assert.False(string.IsNullOrEmpty(batch[0].Result!.ExtractedText));
            Assert.IsType<UnsupportedFormatException>(batch[1].Error);
            Assert.Null(batch[1].Result);
            Assert.Equal("x.txt", batch[1].FileName);
            Assert.IsType<ArgumentNullException>(batch[2].Error);
            Assert.Null(batch[2].FileName);
            Assert.True(batch[3].Succeeded);
        }

        [Fact]
        public async Task CancelledToken_ThrowsOperationCanceled()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => Extractor.Value.ExtractManyAsync(new[] { ValidDocx() }, 1, cts.Token));
        }

        [Fact]
        public async Task CancelMidBatch_StopsStartingItems_AndThrowsOperationCanceled()
        {
            using var cts = new CancellationTokenSource();
            int yielded = 0;
            IEnumerable<ExtractionRequest> Requests()
            {
                for (int i = 0; i < 1000; i++)
                {
                    if (i == 2) cts.Cancel();
                    yielded++;
                    yield return ValidDocx();
                }
            }

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => Extractor.Value.ExtractManyAsync(Requests(), 1, cts.Token));
            Assert.True(yielded < 1000, $"enumeration continued after cancel ({yielded} items)");
        }

        [Fact]
        public async Task EmptyInput_ReturnsEmptyList()
        {
            Assert.Empty(await Extractor.Value.ExtractManyAsync(Array.Empty<ExtractionRequest>()));
        }

        [Fact]
        public async Task InvalidArguments_ThrowUpFront()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Extractor.Value.ExtractManyAsync(null!));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Extractor.Value.ExtractManyAsync(new[] { ValidDocx() }, 0));
        }
    }
}
