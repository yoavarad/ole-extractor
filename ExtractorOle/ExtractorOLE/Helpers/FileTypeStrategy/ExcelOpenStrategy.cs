using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class ExcelOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public ExcelOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            try
            {
                var result = new DocumentExtractionResult();
                using (var ms = new MemoryStream(fileBytes))
                using (var excel = SpreadsheetDocument.Open(ms, false))
                {
                    _helper.ExtractMetadataAndEmbedded(excel, excel.WorkbookPart, result);
                    result.FormatMetadata = BuildExcelFormatMetadata(excel.WorkbookPart);
                    if (string.IsNullOrEmpty(result.MimeType) && excel.CoreFilePropertiesPart != null)
                    {
                        result.MimeType = excel.CoreFilePropertiesPart.ContentType ?? string.Empty;
                    }
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        // Sheet count/names come straight off the workbook's <sheets> element, in
        // workbook (tab) order. ActiveSheetIndex is only populated when the workbook
        // actually carries a <bookViews><workbookView activeTab="..."/> - the OpenXml
        // SDK leaves optional attributes like activeTab as null when absent from the
        // markup, unlike NPOI's HSSFWorkbook which synthesizes a default on read, so no
        // extra presence check is needed here beyond the null propagation itself.
        // HasMacros is true when the workbook part carries a VbaProjectPart (the OOXML
        // macro storage for xlsm/xltm/xlsb-with-macros).
        private static ExcelFormatMetadata BuildExcelFormatMetadata(WorkbookPart? workbookPart)
        {
            var sheetNames = workbookPart?.Workbook?.Sheets?
                .Elements<Sheet>()
                .Select(sheet => sheet.Name?.Value ?? string.Empty)
                .ToList() ?? new List<string>();

            var activeTab = workbookPart?.Workbook?.BookViews?
                .Elements<WorkbookView>()
                .FirstOrDefault()?.ActiveTab;

            return new ExcelFormatMetadata
            {
                SheetCount = sheetNames.Count,
                SheetNames = sheetNames,
                ActiveSheetIndex = activeTab != null ? (int)activeTab.Value : (int?)null,
                HasMacros = workbookPart?.VbaProjectPart != null,
            };
        }
    }
}
