using ExtractorOLE.Abstractions;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// #290: Extract(request, token) and ExtractManyAsync observe cancellation before parsing and
    /// at the stage boundaries, and never report it as a field error or batch item error.
    /// </summary>
    public sealed class MainExtractorCancellationTests : IDisposable
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private readonly ServiceProvider _provider;

        public MainExtractorCancellationTests()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            _provider = services.BuildServiceProvider();
        }

        public void Dispose() => _provider.Dispose();

        private static ExtractionRequest Docx() => new()
        {
            FileBytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "hello" }).Content,
            FileName = "sample.docx",
            DetectedMimeType = DocxMime,
        };

        private (MainExtractor Extractor, HookRegistry Registry) Build(Action onOpen)
        {
            var registry = new HookRegistry(_provider.GetRequiredService<IFormatDispatchRegistry>(), onOpen);
            return (new MainExtractor(_provider.GetRequiredService<IExtractionHelper>(), registry), registry);
        }

        [Fact]
        public void PreCancelledToken_ThrowsBeforeOpen()
        {
            var (extractor, registry) = Build(() => { });
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAny<OperationCanceledException>(() => extractor.Extract(Docx(), cts.Token));
            Assert.Equal(0, registry.Opens);
        }

        [Fact]
        public void PreCancelledToken_NullRequest_StillThrowsArgumentNull()
        {
            var (extractor, _) = Build(() => { });
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<ArgumentNullException>(() => extractor.Extract(null!, cts.Token));
        }

        [Fact]
        public void CancelledDuringOpen_StopsAtNextBoundary_BeforeTextExtraction()
        {
            using var cts = new CancellationTokenSource();
            var (extractor, registry) = Build(cts.Cancel);

            Assert.ThrowsAny<OperationCanceledException>(() => extractor.Extract(Docx(), cts.Token));
            Assert.Equal(1, registry.Opens);
            Assert.Equal(0, registry.TextExtractorLookups);
        }

        [Fact]
        public void CancelledDuringTextExtraction_Throws()
        {
            using var cts = new CancellationTokenSource();
            var (extractor, registry) = Build(() => { });
            registry.OnTextExtract = cts.Cancel;

            Assert.ThrowsAny<OperationCanceledException>(() => extractor.Extract(Docx(), cts.Token));
            Assert.Equal(1, registry.TextExtractorLookups);
        }

        [Fact]
        public void UncancelledToken_MatchesTokenlessExtract()
        {
            var (extractor, _) = Build(() => { });
            using var cts = new CancellationTokenSource();

            var withToken = extractor.Extract(Docx(), cts.Token);
            var without = extractor.Extract(Docx());

            Assert.Equal(without.ExtractedText, withToken.ExtractedText);
            Assert.Contains("hello", withToken.ExtractedText, StringComparison.Ordinal);
            Assert.Empty(withToken.FieldErrors);
        }

        [Fact]
        public async Task ExtractManyAsync_InFlightItemObservesCancellation()
        {
            using var cts = new CancellationTokenSource();
            var (extractor, registry) = Build(cts.Cancel);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => extractor.ExtractManyAsync(new[] { Docx() }, 1, cts.Token));
            Assert.Equal(1, registry.Opens);
            Assert.Equal(0, registry.TextExtractorLookups);
        }

        // Delegates to the real registry; the open strategy runs onOpen (the "stage callback")
        // before opening, and both lookups are counted.
        private sealed class HookRegistry : IFormatDispatchRegistry
        {
            private readonly IFormatDispatchRegistry _inner;
            private readonly Action _onOpen;
            private int _opens;
            private int _textExtractorLookups;

            public HookRegistry(IFormatDispatchRegistry inner, Action onOpen)
            {
                _inner = inner;
                _onOpen = onOpen;
            }

            public int Opens => _opens;

            public int TextExtractorLookups => _textExtractorLookups;

            // When set, the text extractor runs it before delegating to the real one.
            public Action? OnTextExtract { get; set; }

            public IOpenStrategy? GetOpenStrategy(OfficeMimeType format) => new HookStrategy(this, _inner.GetOpenStrategy(format)!);

            public ITextExtractor? GetTextExtractor(OfficeMimeType format)
            {
                Interlocked.Increment(ref _textExtractorLookups);
                var inner = _inner.GetTextExtractor(format);
                return OnTextExtract == null || inner == null ? inner : new HookTextExtractor(OnTextExtract, inner);
            }

            private sealed class HookTextExtractor : ITextExtractor
            {
                private readonly Action _onExtract;
                private readonly ITextExtractor _inner;

                public HookTextExtractor(Action onExtract, ITextExtractor inner)
                {
                    _onExtract = onExtract;
                    _inner = inner;
                }

                public string ExtractText(byte[] fileBytes)
                {
                    _onExtract();
                    return _inner.ExtractText(fileBytes);
                }
            }

            private sealed class HookStrategy : IOpenStrategy
            {
                private readonly HookRegistry _owner;
                private readonly IOpenStrategy _inner;

                public HookStrategy(HookRegistry owner, IOpenStrategy inner)
                {
                    _owner = owner;
                    _inner = inner;
                }

                public DocumentExtractionResult? Open(byte[] fileBytes)
                {
                    Hook();
                    return _inner.Open(fileBytes);
                }

                public DocumentExtractionResult? Open(Stream input)
                {
                    Hook();
                    return _inner.Open(input);
                }

                private void Hook()
                {
                    Interlocked.Increment(ref _owner._opens);
                    _owner._onOpen();
                }
            }
        }
    }
}
