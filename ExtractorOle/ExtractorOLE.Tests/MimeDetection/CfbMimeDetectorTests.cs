using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.MimeDetection;
using OpenMcdf;
using System;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.MimeDetection
{
    public class CfbMimeDetectorTests
    {
        // Builds a minimal, real CFB byte array (via OpenMcdf itself) containing a
        // single root-level stream with the given name. This is a hand-crafted
        // fixture exercising the actual on-disk compound-file format, not a
        // parsed/prebuilt sample document -- no full document content is needed
        // since the detector only inspects root entry names.
        private static byte[] BuildCfbWithRootEntry(string entryName)
        {
            using var stream = new MemoryStream();
            using (var root = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
            {
                using var entryStream = root.CreateStream(entryName);
                var data = new byte[] { 1, 2, 3 };
                entryStream.Write(data, 0, data.Length);
            }

            return stream.ToArray();
        }

        [Fact]
        public void Detect_WordDocumentRootEntry_ReturnsDoc()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = BuildCfbWithRootEntry("WordDocument"),
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Doc, result.DetectedFormat);
            Assert.Equal("application/msword", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Theory]
        [InlineData("Workbook")]
        [InlineData("Book")]
        public void Detect_ExcelRootEntry_ReturnsXls(string entryName)
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = BuildCfbWithRootEntry(entryName),
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Xls, result.DetectedFormat);
            Assert.Equal("application/vnd.ms-excel", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_PowerPointDocumentRootEntry_ReturnsPpt()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = BuildCfbWithRootEntry("PowerPoint Document"),
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Ppt, result.DetectedFormat);
            Assert.Equal("application/vnd.ms-powerpoint", result.MimeType);
            Assert.True(result.IsSupported);
        }

        [Fact]
        public void Detect_CfbWithUnrecognizedRootEntry_ReturnsUnknown()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = BuildCfbWithRootEntry("SomeOtherStream"),
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
            Assert.Equal("application/octet-stream", result.MimeType);
            Assert.False(result.IsSupported);
        }

        [Fact]
        public void Detect_NonCfbBytes_ReturnsUnknownWithoutThrowing()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 },
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }

        [Fact]
        public void Detect_EmptyBytes_ReturnsUnknownWithoutThrowing()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = Array.Empty<byte>(),
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }

        [Fact]
        public void Detect_NeverUsesFileNameToDecideResult()
        {
            // WordDocument-signature CFB content, paired with a filename that lies
            // about the format. The result must follow the content, proving
            // FileName plays no part in the decision.
            var request = new MimeDetectionRequest
            {
                FileBytes = BuildCfbWithRootEntry("WordDocument"),
                FileName = "totally-not-a-word-doc.ppt"
            };
            var detector = new CfbMimeDetector();

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Doc, result.DetectedFormat);
        }
    }
}
