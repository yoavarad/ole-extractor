using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.DTOs;
using NPOI.HSSF.UserModel;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    public class ExtractionHelperDetectMimeTypeTests
    {
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

            var result = new ExtractionHelper(new CfbMimeDetector()).DetectMimeTypeFromBytes(xlsBytes);

            Assert.Equal(OfficeMimeTypeEnum.ExcelLegacy, result);
        }

        [Fact]
        public void DetectMimeTypeFromBytes_NonZipNonCfbBytes_ReturnsOpenXmlUnknown()
        {
            var garbageBytes = new byte[] { 1, 2, 3, 4, 5 };

            var result = new ExtractionHelper(new CfbMimeDetector()).DetectMimeTypeFromBytes(garbageBytes);

            Assert.Equal(OfficeMimeTypeEnum.OpenXmlUnknown, result);
        }

        [Fact]
        public void ParameterlessConstructor_StillUsable()
        {
            var mime = new ExtractionHelper().MimeFor(OfficeMimeTypeEnum.Word);

            Assert.False(string.IsNullOrEmpty(mime));
        }
    }
}
