using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.DTOs;
using System;
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

        public DocumentExtractionResult? Open(byte[] fileBytes) => Open(new MemoryStream(fileBytes, false));

        // Reads the seekable stream lazily (#192, ADR-008); the caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input)
        {
            SpreadsheetDocument excel;
            try
            {
                excel = OpenXmlPackages.OpenSpreadsheet(input);
            }
            catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
            {
                return null;
            }

            try
            {
                var result = new DocumentExtractionResult();
                StrategyIsolation.PopulateEmbedded(result, () => _helper.ExtractMetadataAndEmbedded(excel, excel.WorkbookPart, result));
                result.FormatMetadata = BuildExcelFormatMetadata(excel.WorkbookPart, result);
                if (string.IsNullOrEmpty(result.MimeType) && excel.CoreFilePropertiesPart != null)
                {
                    result.MimeType = excel.CoreFilePropertiesPart.ContentType ?? string.Empty;
                }

                // Kept open for XlsxTextExtractor (#167); MainExtractor disposes it.
                result.ParsedDocument = excel;
                return result;
            }
            catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
            {
                excel.Dispose();
                return null;
            }
            catch
            {
                excel.Dispose();
                throw;
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
        // Each field is its own leaf at FormatMetadata.<Name>; SheetCount and SheetNames are read
        // independently and never reconciled. A failed leaf keeps its default (0 / empty / null / false).
        private static ExcelFormatMetadata BuildExcelFormatMetadata(WorkbookPart? workbookPart, DocumentExtractionResult result)
        {
            var fm = new ExcelFormatMetadata();

            void Leaf(string name, Action populate) => StrategyIsolation.PopulateLeaf(result, name, populate);

            Leaf(nameof(ExcelFormatMetadata.SheetCount), () =>
                fm.SheetCount = workbookPart?.Workbook?.Sheets?.Elements<Sheet>().Count() ?? 0);
            Leaf(nameof(ExcelFormatMetadata.SheetNames), () =>
                fm.SheetNames = workbookPart?.Workbook?.Sheets?
                    .Elements<Sheet>()
                    .Select(sheet => sheet.Name?.Value ?? string.Empty)
                    .ToList() ?? new List<string>());
            Leaf(nameof(ExcelFormatMetadata.ActiveSheetIndex), () =>
            {
                var activeTab = workbookPart?.Workbook?.BookViews?
                    .Elements<WorkbookView>()
                    .FirstOrDefault()?.ActiveTab;
                fm.ActiveSheetIndex = activeTab != null ? (int)activeTab.Value : (int?)null;
            });
            Leaf(nameof(ExcelFormatMetadata.HasMacros), () => fm.HasMacros = workbookPart?.VbaProjectPart != null);
            return fm;
        }
    }
}
