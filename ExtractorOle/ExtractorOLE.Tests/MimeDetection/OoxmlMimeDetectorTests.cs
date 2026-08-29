using System;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.MimeDetection;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.MimeDetection
{
    public class OoxmlMimeDetectorTests
    {
        [Fact]
        public void Detect_DocmContent_ReturnsDocx()
        {
            // Macro-enabled docm is structurally distinguishable (VbaProjectPart)
            // but must classify as the base format -- no separate macro-variant
            // enum value on DetectedFormat (docs/specs/mime-detection.md).
            var sample = new DocxSampleGenerator().Generate(MacroEmbedSampleSpecs.Build());
            Assert.Equal("sample.docm", sample.FileName);
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = sample.FileName
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Docx, result.DetectedFormat);
            Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_XlsmContent_ReturnsXlsx()
        {
            var sample = new XlsxSampleGenerator().Generate(MacroEmbedSampleSpecs.Build());
            Assert.Equal("sample.xlsm", sample.FileName);
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = sample.FileName
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Xlsx, result.DetectedFormat);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_PptmContent_ReturnsPptx()
        {
            var sample = new PptxSampleGenerator().Generate(MacroEmbedSampleSpecs.Build());
            Assert.Equal("sample.pptm", sample.FileName);
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = sample.FileName
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Pptx, result.DetectedFormat);
            Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.presentation", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_DocxContent_ReturnsDocx()
        {
            var sample = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "hello docx" });
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = sample.FileName
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Docx, result.DetectedFormat);
            Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_XlsxContent_ReturnsXlsx()
        {
            var sample = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = "hello xlsx" });
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = sample.FileName
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Xlsx, result.DetectedFormat);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_PptxContent_ReturnsPptx()
        {
            var sample = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "hello pptx" });
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = sample.FileName
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Pptx, result.DetectedFormat);
            Assert.Equal("application/vnd.openxmlformats-officedocument.presentationml.presentation", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_WrongExtension_StillDetectsCorrectFormat()
        {
            var sample = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "renamed to txt" });
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = "sample.txt"
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Docx, result.DetectedFormat);
        }

        [Fact]
        public void Detect_NoExtension_StillDetectsCorrectFormat()
        {
            var sample = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "no extension" });
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = "sample"
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Docx, result.DetectedFormat);
        }

        [Fact]
        public void Detect_RenamedTxtToDocx_ReturnsUnknown()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = System.Text.Encoding.UTF8.GetBytes("this is plain text content, not a zip"),
                FileName = "fake.docx"
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
            Assert.Equal("application/octet-stream", result.MimeType);
            Assert.False(result.IsSupported);
        }

        [Fact]
        public void Detect_NeverUsesFileNameToDecideResult()
        {
            // Genuine docx content, paired with a filename that lies about the
            // format. The result must follow the content, proving FileName plays
            // no part in the decision.
            var sample = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "content wins" });
            var request = new MimeDetectionRequest
            {
                FileBytes = sample.Content,
                FileName = "totally-not-a-docx.xlsx"
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Docx, result.DetectedFormat);
        }

        [Fact]
        public void Detect_EmptyBytes_ReturnsUnknownWithoutThrowing()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = Array.Empty<byte>(),
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
            Assert.Equal("application/octet-stream", result.MimeType);
            Assert.False(result.IsSupported);
        }

        [Fact]
        public void Detect_NullFileBytes_ReturnsUnknownWithoutThrowing()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = null!,
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }

        [Fact]
        public void Detect_NonZipBytes_ReturnsUnknownWithoutThrowing()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 },
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }
    }
}
