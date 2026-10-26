using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    /// <summary>
    /// Task #251 (ADR-008): MimeDetectionRequest takes FileBytes, a seekable InputStream or a
    /// FilePath with the same exactly-one-source rules as ExtractionRequest.
    /// </summary>
    public class DetectMimeTypeStreamInputTests
    {
        private static IExtractionHelper BuildHelper()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<IExtractionHelper>();
        }

        private static byte[] Generate(DetectedFormat format, string text = "hello")
        {
            var spec = new SampleSpec { BodyText = text };
            return format switch
            {
                DetectedFormat.Docx => new DocxSampleGenerator().Generate(spec).Content,
                DetectedFormat.Xlsx => new XlsxSampleGenerator().Generate(spec).Content,
                DetectedFormat.Pptx => new PptxSampleGenerator().Generate(spec).Content,
                DetectedFormat.Doc => new DocSampleGenerator().Generate(spec).Content,
                DetectedFormat.Xls => new XlsSampleGenerator().Generate(spec).Content,
                _ => new PptSampleGenerator().Generate(spec).Content,
            };
        }

        public static TheoryData<DetectedFormat> Formats => new()
        {
            DetectedFormat.Docx, DetectedFormat.Xlsx, DetectedFormat.Pptx,
            DetectedFormat.Doc, DetectedFormat.Xls, DetectedFormat.Ppt,
        };

        [Theory]
        [MemberData(nameof(Formats))]
        public void DetectMimeType_InputStream_DetectsFormat_AndLeavesStreamOpen(DetectedFormat format)
        {
            var bytes = Generate(format);
            var expected = BuildHelper().DetectMimeType(new MimeDetectionRequest { FileBytes = bytes });
            using var stream = new MemoryStream(bytes);
            stream.Position = 5; // detection must read from position 0 regardless

            var result = BuildHelper().DetectMimeType(new MimeDetectionRequest { InputStream = stream });

            Assert.Equal(format, result.DetectedFormat);
            Assert.Equal(expected.MimeType, result.MimeType);
            Assert.True(stream.CanRead); // not disposed
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void DetectMimeType_FilePath_DetectsFormat_AndReleasesFile(DetectedFormat format)
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(path, Generate(format));
            try
            {
                var result = BuildHelper().DetectMimeType(new MimeDetectionRequest { FilePath = path });

                Assert.Equal(format, result.DetectedFormat);
                File.Delete(path); // throws if the detector left the file open
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void DetectMimeType_MultipleSources_ThrowsArgumentException()
        {
            var bytes = Generate(DetectedFormat.Docx);
            using var stream = new MemoryStream(bytes);
            var helper = BuildHelper();

            Assert.Throws<ArgumentException>(() => helper.DetectMimeType(
                new MimeDetectionRequest { FileBytes = bytes, InputStream = stream }));
            Assert.Throws<ArgumentException>(() => helper.DetectMimeType(
                new MimeDetectionRequest { InputStream = stream, FilePath = "x.docx" }));
            Assert.Throws<ArgumentException>(() => helper.DetectMimeType(
                new MimeDetectionRequest { FileBytes = bytes, FilePath = "x.docx" }));
        }

        [Fact]
        public void DetectMimeType_NonSeekableStream_ThrowsArgumentException()
        {
            using var stream = new NonSeekableStream(Generate(DetectedFormat.Docx));

            Assert.Throws<ArgumentException>(() => BuildHelper().DetectMimeType(
                new MimeDetectionRequest { InputStream = stream }));
        }

        [Theory]
        [InlineData(DetectedFormat.Docx)]
        [InlineData(DetectedFormat.Doc)]
        public void DetectMimeType_InputStream_ReadsFarLessThanWholeFile(DetectedFormat format)
        {
            // Large, poorly compressible body so the file is much bigger than headers/directory.
            var rng = new Random(1);
            var text = new string(Enumerable.Range(0, 2_000_000).Select(_ => (char)rng.Next('a', 'z' + 1)).ToArray());
            var bytes = Generate(format, text);
            using var stream = new CountingStream(bytes);

            var result = BuildHelper().DetectMimeType(new MimeDetectionRequest { InputStream = stream });

            Assert.Equal(format, result.DetectedFormat);
            Assert.True(stream.BytesRead < bytes.Length / 2, $"read {stream.BytesRead} of {bytes.Length}");
        }

        private sealed class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] bytes) : base(bytes) { }
            public override bool CanSeek => false;
        }

        private sealed class CountingStream : MemoryStream
        {
            public long BytesRead { get; private set; }
            public CountingStream(byte[] bytes) : base(bytes) { }
            public override int Read(byte[] buffer, int offset, int count)
            {
                var n = base.Read(buffer, offset, count);
                BytesRead += n;
                return n;
            }
            public override int Read(Span<byte> buffer)
            {
                var n = base.Read(buffer);
                BytesRead += n;
                return n;
            }
        }
    }
}
