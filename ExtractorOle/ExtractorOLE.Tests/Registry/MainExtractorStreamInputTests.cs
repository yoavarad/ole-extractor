using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.FileTypeStrategy;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Task #192 (ADR-008): ExtractionRequest takes the input as FileBytes, a seekable InputStream
    /// or a FilePath, and Extract gives the same result for all three. OOXML packages open straight
    /// from the stream; the caller's stream is left open, a FilePath stream is closed.
    /// </summary>
    public class MainExtractorStreamInputTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string PptxMime = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        private const string DocMime = "application/msword";
        private const string XlsMime = "application/vnd.ms-excel";
        private const string PptMime = "application/vnd.ms-powerpoint";

        private static MainExtractor BuildExtractor(long? maxFileSizeBytes = null)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services, maxFileSizeBytes != null ? new MimeDetectionLimits { MaxFileSizeBytes = maxFileSizeBytes.Value } : null);
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<MainExtractor>();
        }

        private static byte[] Generate(string mime, string text)
        {
            var spec = new SampleSpec { BodyText = text };
            return mime switch
            {
                DocxMime => new DocxSampleGenerator().Generate(spec).Content,
                XlsxMime => new XlsxSampleGenerator().Generate(spec).Content,
                PptxMime => new PptxSampleGenerator().Generate(spec).Content,
                DocMime => new DocSampleGenerator().Generate(spec).Content,
                XlsMime => new XlsSampleGenerator().Generate(spec).Content,
                _ => new PptSampleGenerator().Generate(spec).Content,
            };
        }

        private static ExtractionRequest Request(string mime) =>
            new ExtractionRequest { FileName = "fixture", DetectedMimeType = mime };

        private static void AssertSameResult(DocumentExtractionResult expected, DocumentExtractionResult actual)
        {
            Assert.Equal(expected.ExtractedText, actual.ExtractedText);
            Assert.Equal(expected.MimeType, actual.MimeType);
            Assert.Equal(expected.EmbeddedFiles.Count, actual.EmbeddedFiles.Count);
            Assert.Null(actual.ParsedDocument);
        }

        [Theory]
        [InlineData(DocxMime)]
        [InlineData(XlsxMime)]
        [InlineData(PptxMime)]
        [InlineData(DocMime)]
        [InlineData(XlsMime)]
        [InlineData(PptMime)]
        public void StreamAndPathInput_MatchByteArrayInput(string mime)
        {
            const string text = "stream parity";
            var bytes = Generate(mime, text);
            var extractor = BuildExtractor();

            var fromBytes = extractor.Extract(new ExtractionRequest { FileBytes = bytes, FileName = "fixture", DetectedMimeType = mime });

            using var stream = new MemoryStream(bytes, writable: false);
            stream.Position = 5; // Extract reads from the start regardless of Position
            var streamRequest = Request(mime);
            streamRequest.InputStream = stream;
            var fromStream = extractor.Extract(streamRequest);

            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, bytes);
                var pathRequest = Request(mime);
                pathRequest.FilePath = path;
                var fromPath = extractor.Extract(pathRequest);

                Assert.Contains(text, fromBytes.ExtractedText);
                AssertSameResult(fromBytes, fromStream);
                AssertSameResult(fromBytes, fromPath);
            }
            finally
            {
                File.Delete(path); // throws if Extract left the file open
            }
        }

        [Theory]
        [InlineData(DocxMime)]
        [InlineData(XlsxMime)]
        [InlineData(PptxMime)]
        public void StreamInput_OoxmlOpensPackageOnceFromTheCallerStream_AndLeavesItOpen(string mime)
        {
            var bytes = Generate(mime, "single open");
            var extractor = BuildExtractor();
            using var stream = new MemoryStream(bytes, writable: false);
            var request = Request(mime);
            request.InputStream = stream;

            var before = OpenXmlPackages.OpenCount;
            extractor.Extract(request);

            Assert.Equal(1, OpenXmlPackages.OpenCount - before);
            Assert.True(stream.CanRead); // caller owns the stream: not disposed by Extract
        }

        [Fact]
        public void OoxmlOpenStrategy_StreamOverload_DoesNotDisposeTheStream()
        {
            var bytes = Generate(DocxMime, "strategy");
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var strategy = new WordOpenStrategy(services.BuildServiceProvider().GetRequiredService<ExtractorOLE.Helpers.IExtractionHelper>());
            using var stream = new MemoryStream(bytes, writable: false);

            var result = strategy.Open(stream);

            Assert.NotNull(result);
            Assert.IsType<DocumentFormat.OpenXml.Packaging.WordprocessingDocument>(result!.ParsedDocument);
            ((IDisposable)result.ParsedDocument!).Dispose();
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void NonSeekableStream_IsRejected()
        {
            var request = Request(DocxMime);
            request.InputStream = new NonSeekableStream(Generate(DocxMime, "x"));

            Assert.Throws<ArgumentException>(() => BuildExtractor().Extract(request));
        }

        [Fact]
        public void MoreThanOneInputSource_IsRejected()
        {
            var bytes = Generate(DocxMime, "x");
            var extractor = BuildExtractor();

            var bytesAndStream = Request(DocxMime);
            bytesAndStream.FileBytes = bytes;
            bytesAndStream.InputStream = new MemoryStream(bytes);
            Assert.Throws<ArgumentException>(() => extractor.Extract(bytesAndStream));

            var bytesAndPath = Request(DocxMime);
            bytesAndPath.FileBytes = bytes;
            bytesAndPath.FilePath = "unused.docx";
            Assert.Throws<ArgumentException>(() => extractor.Extract(bytesAndPath));

            var streamAndPath = Request(DocxMime);
            streamAndPath.InputStream = new MemoryStream(bytes);
            streamAndPath.FilePath = "unused.docx";
            Assert.Throws<ArgumentException>(() => extractor.Extract(streamAndPath));
        }

        [Fact]
        public void MissingFilePath_ThrowsFileNotFound()
        {
            var request = Request(DocxMime);
            request.FilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".docx");

            Assert.Throws<FileNotFoundException>(() => BuildExtractor().Extract(request));
        }

        [Fact]
        public void OversizedStream_IsRejectedFromItsLength_WithoutReadingIt()
        {
            var request = Request(DocxMime);
            request.InputStream = new UnreadableContentStream(length: 11);

            var ex = Assert.Throws<FileTooLargeException>(() => BuildExtractor(maxFileSizeBytes: 10).Extract(request));

            Assert.Equal(11, ex.ActualBytes);
        }

        [Fact]
        public void OversizedFilePath_IsRejectedFromItsLength_BeforeReading()
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, new byte[11]); // not a valid docx: reading it would raise a different error
                var request = Request(DocxMime);
                request.FilePath = path;

                var ex = Assert.Throws<FileTooLargeException>(() => BuildExtractor(maxFileSizeBytes: 10).Extract(request));

                Assert.Equal(11, ex.ActualBytes);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void FailedOoxmlOpenAttempt_LeavesCallerStreamOpen_SoALaterAttemptSucceeds()
        {
            var bytes = Generate(DocxMime, "retry after failed open");
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var helper = services.BuildServiceProvider().GetRequiredService<ExtractorOLE.Helpers.IExtractionHelper>();
            var word = new WordOpenStrategy(helper);
            // A non-package stream is a true open failure: a Word-flavored package opened as Excel no longer
            // throws since per-field isolation (it yields FieldErrors), so corrupt bytes are used instead.
            using var stream = new MemoryStream();
            stream.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            stream.Position = 0;

            // The failed open must leave the caller's stream usable.
            Assert.ThrowsAny<Exception>(() => word.Open(stream));
            Assert.True(stream.CanRead);
            Assert.True(stream.CanSeek);

            // Replace the contents with a valid package; the same stream serves the later attempt.
            stream.SetLength(0);
            stream.Write(bytes);
            stream.Position = 0;
            var result = word.Open(stream);
            Assert.NotNull(result);
            ((IDisposable)result!.ParsedDocument!).Dispose();
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void ZipFallback_StreamOpen_ReturnsPackageAndLeavesStreamOpen()
        {
            var bytes = Generate(XlsxMime, "fallback trial");
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var helper = services.BuildServiceProvider().GetRequiredService<ExtractorOLE.Helpers.IExtractionHelper>();
            var fallback = new ZipFallbackOpenStrategy(new WordOpenStrategy(helper), new ExcelOpenStrategy(helper), new PowerPointOpenStrategy(helper));
            using var stream = new MemoryStream(bytes, writable: false);

            var result = fallback.Open(stream);

            Assert.NotNull(result);
            Assert.IsType<DocumentFormat.OpenXml.Packaging.SpreadsheetDocument>(result!.ParsedDocument);
            ((IDisposable)result.ParsedDocument!).Dispose();
            Assert.True(stream.CanRead);
        }

        [Fact]
        public void CutShortStreamInput_RaisesTheSameParseTimeErrorAsBytes()
        {
            var bytes = Generate(DocxMime, "cut short");
            var cut = bytes.AsSpan(0, bytes.Length / 2).ToArray();
            var extractor = BuildExtractor();

            var bytesError = Record.Exception(() => extractor.Extract(new ExtractionRequest { FileBytes = cut, FileName = "fixture", DetectedMimeType = DocxMime }));
            var request = Request(DocxMime);
            request.InputStream = new MemoryStream(cut);
            var streamError = Record.Exception(() => extractor.Extract(request));

            Assert.IsAssignableFrom<ExtractorOleException>(bytesError);
            Assert.Equal(bytesError!.GetType(), streamError?.GetType());
        }

        [Theory]
        [InlineData("text/plain")]                // unsupported format
        [InlineData(DocxMime)]                    // corrupt container
        public void FilePath_IsClosedWhenExtractThrows(string mime)
        {
            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
                var request = Request(mime);
                request.FilePath = path;

                Assert.IsAssignableFrom<ExtractorOleException>(Record.Exception(() => BuildExtractor().Extract(request)));
            }
            finally
            {
                File.Delete(path); // throws if Extract left the file open
            }
        }

        [Fact]
        public void EmptyStream_RaisesTheSameErrorAsEmptyBytes()
        {
            var extractor = BuildExtractor();
            var bytesError = Record.Exception(() => extractor.Extract(Request(DocxMime)));
            var request = Request(DocxMime);
            request.InputStream = new MemoryStream();

            Assert.IsType<TruncatedContainerException>(bytesError);
            Assert.IsType<TruncatedContainerException>(Record.Exception(() => extractor.Extract(request)));
        }

        [Fact]
        public async Task ExtractManyAsync_MixedInputSources_MatchByteArrayResults()
        {
            var docx = Generate(DocxMime, "batch docx");
            var xlsx = Generate(XlsxMime, "batch xlsx");
            var path = Path.GetTempFileName();
            try
            {
                await File.WriteAllBytesAsync(path, xlsx);
                using var stream = new MemoryStream(docx, writable: false);
                var byStream = Request(DocxMime);
                byStream.InputStream = stream;
                var byPath = Request(XlsxMime);
                byPath.FilePath = path;
                var byBytes = Request(PptxMime);
                byBytes.FileBytes = Generate(PptxMime, "batch pptx");

                var results = await BuildExtractor().ExtractManyAsync(new[] { byStream, byPath, byBytes }, maxDegreeOfParallelism: 3);

                Assert.All(results, r => Assert.Null(r.Error));
                Assert.Contains("batch docx", results[0].Result!.ExtractedText);
                Assert.Contains("batch xlsx", results[1].Result!.ExtractedText);
                Assert.Contains("batch pptx", results[2].Result!.ExtractedText);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private sealed class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] bytes) : base(bytes) { }
            public override bool CanSeek => false;
        }

        // Seekable stream with a length whose content must never be read.
        private sealed class UnreadableContentStream : Stream
        {
            private readonly long _length;
            public UnreadableContentStream(long length) => _length = length;
            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _length;
            public override long Position { get; set; }
            public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("content was read");
            public override long Seek(long offset, SeekOrigin origin) => Position = offset;
            public override void Flush() { }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
