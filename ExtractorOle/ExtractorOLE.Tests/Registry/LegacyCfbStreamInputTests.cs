using System.Text.Json;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Task #194 (ADR-008): legacy CFB (xls/doc/ppt) and zip-fallback strategies open from the
    /// caller's seekable stream with the same result as byte[], and leave the stream open.
    /// </summary>
    public class LegacyCfbStreamInputTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string PptxMime = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        private const string DocMime = "application/msword";
        private const string XlsMime = "application/vnd.ms-excel";
        private const string PptMime = "application/vnd.ms-powerpoint";

        private static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider();
        }

        private static MainExtractor BuildExtractor() => BuildProvider().GetRequiredService<MainExtractor>();

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

        private static OfficeMimeTypeEnum LegacyKind(string mime) => mime switch
        {
            XlsMime => OfficeMimeTypeEnum.ExcelLegacy,
            DocMime => OfficeMimeTypeEnum.WordLegacy,
            _ => OfficeMimeTypeEnum.PowerPointLegacy,
        };

        private static IOpenStrategy LegacyStrategy(string mime) =>
            BuildProvider().GetRequiredService<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>()[LegacyKind(mime)];

        private static ZipFallbackOpenStrategy BuildZipFallback()
        {
            var helper = BuildProvider().GetRequiredService<IExtractionHelper>();
            return new ZipFallbackOpenStrategy(new WordOpenStrategy(helper), new ExcelOpenStrategy(helper), new PowerPointOpenStrategy(helper));
        }

        private static ExtractionRequest Request(string mime) =>
            new ExtractionRequest { FileName = "fixture", DetectedMimeType = mime };

        private static string? Json(object? value) =>
            value == null ? null : JsonSerializer.Serialize(value, value.GetType());

        [Theory]
        [InlineData(XlsMime)]
        [InlineData(DocMime)]
        [InlineData(PptMime)]
        public void StrategyOpen_StreamMatchesBytes_AndLeavesStreamOpen(string mime)
        {
            var bytes = Generate(mime, "strategy parity");
            var strategy = LegacyStrategy(mime);
            using var stream = new DisposeTrackingStream(bytes);
            stream.Position = 0;

            var fromBytes = strategy.Open(bytes);
            var fromStream = strategy.Open(stream);
            Assert.Equal(0, stream.DisposeCount);

            Assert.NotNull(fromBytes);
            Assert.NotNull(fromStream);
            Assert.Equal(fromBytes!.Metadata.Title, fromStream!.Metadata.Title);
            Assert.Equal(fromBytes.MimeType, fromStream.MimeType);
            Assert.Equal(fromBytes.EmbeddedFiles.Count, fromStream.EmbeddedFiles.Count);
            Assert.Equal(Json(fromBytes.FormatMetadata), Json(fromStream.FormatMetadata));

            (fromBytes.ParsedDocument as IDisposable)?.Dispose();
            (fromStream.ParsedDocument as IDisposable)?.Dispose();
            Assert.Equal(0, stream.DisposeCount);

            Assert.True(stream.CanRead);
            stream.Position = 0;
            Assert.NotEqual(-1, stream.ReadByte());
        }

        [Theory]
        [InlineData(XlsMime)]
        [InlineData(DocMime)]
        [InlineData(PptMime)]
        public void Extract_CallerFileStream_StaysOpen_AndMatchesBytes(string mime)
        {
            const string text = "file stream parity";
            var bytes = Generate(mime, text);
            var extractor = BuildExtractor();
            var fromBytes = extractor.Extract(new ExtractionRequest { FileBytes = bytes, FileName = "fixture", DetectedMimeType = mime });

            var path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, bytes);
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var request = Request(mime);
                    request.InputStream = fs;

                    var fromStream = extractor.Extract(request);

                    Assert.Equal(fromBytes.ExtractedText, fromStream.ExtractedText);
                    Assert.Contains(text, fromStream.ExtractedText);
                    Assert.True(fs.CanRead);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Extract_XlsStream_LoadsWorkbookOnce()
        {
            var bytes = Generate(XlsMime, "xls once");
            var extractor = BuildExtractor();
            using var stream = new MemoryStream(bytes, false);
            var request = Request(XlsMime);
            request.InputStream = stream;

            var before = XlsWorkbooks.LoadCount;
            extractor.Extract(request);

            Assert.Equal(1, XlsWorkbooks.LoadCount - before);
        }

        [Fact]
        public void Extract_PptStream_ConvertsOnce()
        {
            var bytes = Generate(PptMime, "ppt once");
            var extractor = BuildExtractor();
            using var stream = new MemoryStream(bytes, false);
            var request = Request(PptMime);
            request.InputStream = stream;

            var before = PptToPptxConverter.ConvertCallCount;
            extractor.Extract(request);

            Assert.Equal(1, PptToPptxConverter.ConvertCallCount - before);
        }

        [Fact]
        public void Extract_DocStream_ExtractsTextFromTheStream()
        {
            const string text = "doc stream body";
            var bytes = Generate(DocMime, text);

            using (var probe = new MemoryStream(bytes, false))
            {
                var opened = ((DocOpenStrategy)LegacyStrategy(DocMime)).Open(probe);
                Assert.NotNull(opened);
                Assert.IsAssignableFrom<Stream>(opened!.ParsedDocument);
                var parsedText = new ExtractorOLE.Handlers.DocTextExtractor().ExtractText((object)opened.ParsedDocument!);
                Assert.NotNull(parsedText);
                Assert.Contains(text, parsedText);
                (opened.ParsedDocument as IDisposable)?.Dispose();
            }

            var extractor = BuildExtractor();
            var fromBytes = extractor.Extract(new ExtractionRequest { FileBytes = bytes, FileName = "fixture", DetectedMimeType = DocMime });
            using var stream = new MemoryStream(bytes, false);
            var request = Request(DocMime);
            request.InputStream = stream;

            var fromStream = extractor.Extract(request);

            Assert.Contains(text, fromStream.ExtractedText);
            Assert.Equal(fromBytes.ExtractedText, fromStream.ExtractedText);
        }

        [Theory]
        [InlineData(DocxMime)]
        [InlineData(XlsxMime)]
        [InlineData(PptxMime)]
        public void ZipFallback_StreamMatchesBytes_AndLeavesStreamOpen(string mime)
        {
            var bytes = Generate(mime, "zip fallback");
            var strategy = BuildZipFallback();
            using var stream = new DisposeTrackingStream(bytes);

            var fromBytes = strategy.Open(bytes);
            var fromStream = strategy.Open(stream);
            Assert.Equal(0, stream.DisposeCount);

            Assert.NotNull(fromBytes);
            Assert.NotNull(fromStream);
            Assert.Equal(fromBytes!.MimeType, fromStream!.MimeType);
            Assert.Equal(fromBytes.EmbeddedFiles.Count, fromStream.EmbeddedFiles.Count);

            (fromBytes.ParsedDocument as IDisposable)?.Dispose();
            (fromStream.ParsedDocument as IDisposable)?.Dispose();
            Assert.Equal(0, stream.DisposeCount);

            Assert.True(stream.CanRead);
        }

        [Fact]
        public void ZipFallback_StreamThatIsNotAZip_ReturnsNull_AndLeavesStreamOpen()
        {
            var bytes = Enumerable.Repeat((byte)0x41, 64).ToArray();
            using var stream = new MemoryStream(bytes, false);

            var result = BuildZipFallback().Open(stream);

            Assert.Null(result);
            Assert.True(stream.CanRead);
        }

        [Theory]
        [InlineData(DocMime)]
        [InlineData(XlsMime)]
        [InlineData(PptMime)]
        public void Extract_CorruptStream_RaisesTheSameErrorAsBytes(string mime)
        {
            var bytes = new byte[4096];
            Array.Fill(bytes, (byte)0xFF);
            new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }.CopyTo(bytes, 0);
            var extractor = BuildExtractor();

            var fromBytes = Record.Exception(() => extractor.Extract(new ExtractionRequest { FileBytes = bytes, FileName = "fixture", DetectedMimeType = mime }));
            using var stream = new MemoryStream(bytes);
            var request = Request(mime);
            request.InputStream = stream;
            var fromStream = Record.Exception(() => extractor.Extract(request));

            Assert.IsAssignableFrom<ExtractorOLE.Exceptions.ExtractorOleException>(fromBytes);
            Assert.IsAssignableFrom<ExtractorOLE.Exceptions.ExtractorOleException>(fromStream);
            Assert.Equal(fromBytes!.GetType(), fromStream!.GetType());
        }

        [Theory]
        [InlineData(XlsMime)]
        [InlineData(DocMime)]
        [InlineData(PptMime)]
        public void CfbFileSystems_StreamInputIsReadLazily_ByteInputIsBuffered(string mime)
        {
            var bytes = Generate(mime, "lazy cfb");
            using var stream = new DisposeTrackingStream(bytes);

            // #236: caller stream (wrapped in NonClosingStream by Open(Stream)) is read on demand, not copied
            using (var lazy = CfbFileSystems.Open(new NonClosingStream(stream)))
            {
                Assert.IsType<NPOI.POIFS.NIO.StreamBackedDataSource>(lazy.Data);
                Assert.NotEmpty(lazy.Root.EntryNames);
            }
            Assert.Equal(0, stream.DisposeCount);
            Assert.True(stream.CanRead);

            // Byte[] input path unchanged: NPOI buffers the MemoryStream
            using var buffered = CfbFileSystems.Open(new MemoryStream(bytes));
            Assert.IsType<NPOI.POIFS.NIO.ByteArrayBackedDataSource>(buffered.Data);
        }

        private sealed class DisposeTrackingStream : MemoryStream
        {
            public int DisposeCount;

            public DisposeTrackingStream(byte[] bytes) : base(bytes, writable: false) { }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }
        }
    }
}
