using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.DTOs;
using ExtractorOLE.Registry;
using NPOI.HSSF.UserModel;
using System;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    public class ExtractionHelperDetectMimeTypeTests
    {
        private sealed class SpyDetector : IMimeTypeDetector
        {
            public bool WasCalled { get; private set; }

            public MimeDetectionResult Detect(MimeDetectionRequest request)
            {
                WasCalled = true;
                return new MimeDetectionResult
                {
                    DetectedFormat = DetectedFormatEnum.Unknown,
                    MimeType = "application/octet-stream",
                    IsSupported = false
                };
            }
        }

        private static byte[] BuildMinimalXlsBytes()
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1");
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        [Fact]
        public void DetectMimeTypeFromBytes_XlsBytes_ReturnsExcelLegacy()
        {
            var xlsBytes = BuildMinimalXlsBytes();

            var result = new ExtractionHelper().DetectMimeTypeFromBytes(xlsBytes);

            Assert.Equal(OfficeMimeTypeEnum.ExcelLegacy, result);
        }

        [Fact]
        public void DetectMimeTypeFromBytes_NonZipNonCfbBytes_ReturnsOpenXmlUnknown()
        {
            var garbageBytes = new byte[] { 1, 2, 3, 4, 5 };

            var result = new ExtractionHelper().DetectMimeTypeFromBytes(garbageBytes);

            Assert.Equal(OfficeMimeTypeEnum.OpenXmlUnknown, result);
        }

        [Fact]
        public void ParameterlessConstructor_StillUsable()
        {
            var mime = new ExtractionHelper().MimeFor(OfficeMimeTypeEnum.Word);

            Assert.False(string.IsNullOrEmpty(mime));
        }

        [Fact]
        public void DetectMimeTypeFromBytes_NullFileBytes_ThrowsArgumentNullException_BeforeAnyDetectorInvoked()
        {
            var spy = new SpyDetector();
            var registry = new MimeDetectionRegistry(new IMimeTypeDetector[] { spy });
            var helper = new ExtractionHelper(registry);

            Assert.Throws<ArgumentNullException>(() => helper.DetectMimeTypeFromBytes(null!));

            Assert.False(spy.WasCalled);
        }

        [Fact]
        public void DetectMimeTypeFromBytes_EmptyFileBytes_StillReturnsOpenXmlUnknown()
        {
            var result = new ExtractionHelper().DetectMimeTypeFromBytes(Array.Empty<byte>());

            Assert.Equal(OfficeMimeTypeEnum.OpenXmlUnknown, result);
        }
    }
}
