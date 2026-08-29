using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    public class ExcelOpenStrategyTests
    {
        // Builds a minimal, valid .xlsx/.xlsm directly via DocumentFormat.OpenXml: one
        // WorksheetPart per name in sheetNames (workbook/tab order), an optional
        // <bookViews><workbookView activeTab="..."/> when activeSheetIndex is given, and
        // an optional VbaProjectPart (arbitrary placeholder bytes; no functioning macro
        // required) when includeVbaProject is set.
        private static byte[] BuildXlsxFixtureBytes(string[] sheetNames, int? activeSheetIndex, bool includeVbaProject)
        {
            var documentType = includeVbaProject
                ? SpreadsheetDocumentType.MacroEnabledWorkbook
                : SpreadsheetDocumentType.Workbook;

            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, documentType))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                if (activeSheetIndex.HasValue)
                {
                    workbookPart.Workbook.Append(new BookViews(
                        new WorkbookView { ActiveTab = (uint)activeSheetIndex.Value }));
                }

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                uint sheetId = 1;
                foreach (var name in sheetNames)
                {
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    worksheetPart.Worksheet = new Worksheet(new SheetData());

                    sheets.Append(new Sheet
                    {
                        Id = workbookPart.GetIdOfPart(worksheetPart),
                        SheetId = sheetId++,
                        Name = name
                    });
                }

                if (includeVbaProject)
                {
                    var vbaProjectPart = workbookPart.AddNewPart<VbaProjectPart>();
                    using var vbaStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
                    vbaProjectPart.FeedData(vbaStream);
                }

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        [Fact]
        public void Open_MacroEnabledMultiSheetXlsxFixture_PopulatesExcelFormatMetadata()
        {
            var sheetNames = new[] { "Sheet1", "Data", "Summary" };
            var bytes = BuildXlsxFixtureBytes(sheetNames, activeSheetIndex: 1, includeVbaProject: true);

            var result = new ExcelOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<ExcelFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(3, formatMetadata.SheetCount);
            Assert.Equal(sheetNames, formatMetadata.SheetNames);
            Assert.Equal(1, formatMetadata.ActiveSheetIndex);
            Assert.True(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_NonMacroMultiSheetXlsxFixtureWithoutActiveTab_PopulatesExcelFormatMetadataWithNullActiveSheetAndNoMacros()
        {
            var sheetNames = new[] { "Sheet1", "Data", "Summary" };
            var bytes = BuildXlsxFixtureBytes(sheetNames, activeSheetIndex: null, includeVbaProject: false);

            var result = new ExcelOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<ExcelFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(3, formatMetadata.SheetCount);
            Assert.Equal(sheetNames, formatMetadata.SheetNames);
            Assert.Null(formatMetadata.ActiveSheetIndex);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_CorruptBytes_ReturnsNull()
        {
            var result = new ExcelOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.Null(result);
        }
    }
}
