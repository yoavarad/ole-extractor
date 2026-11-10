using ExtractorOLE.Abstractions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.Collections.Generic;
using System.Linq;
using ExtractorOLE.Helpers;

namespace ExtractorOLE.Handlers
{
    /// <summary>
    /// Extracts flattened plain-text cell content from .xlsx (OOXML) workbooks
    /// via DocumentFormat.OpenXml. Only textual cells (shared-string or
    /// inline-string type) contribute text - numbers, booleans, dates, errors,
    /// and formula-cached values are not textual content and are skipped, so a
    /// workbook with no textual cell content naturally yields
    /// <see cref="string.Empty"/> rather than leaking numeric values. Each
    /// row's textual cells are joined with a single space and rows are joined
    /// with <see cref="Environment.NewLine"/>, with no trailing separators.
    ///
    /// Cell text comes back via the OpenXml SDK's XML deserialization of the
    /// shared-string table / inline string element exactly as stored (OOXML
    /// text content is UTF-8 in the package) - no additional encoding
    /// conversion or Unicode normalization is applied here.
    /// </summary>
    internal class XlsxTextExtractor : IParsedDocumentTextExtractor
    {
        public string ExtractText(byte[] fileBytes)
        {
            using (var doc = OpenXmlPackages.OpenSpreadsheet(fileBytes))
            {
                return ExtractText(doc);
            }
        }

        // parsedDocument is the package ExcelOpenStrategy already opened (#167); caller owns it.
        public string? ExtractText(object parsedDocument) => ExtractText(parsedDocument, onPartFailure: null);

        // A failing worksheet costs only its own text (rows already read from it, if any, are kept);
        // the other worksheets are still read (#288). The shared-string table is not per sheet: its
        // failure fails the whole text.
        public string? ExtractText(object parsedDocument, Action<Exception>? onPartFailure)
        {
            if (parsedDocument is not SpreadsheetDocument doc) return null;
            return ExtractText(doc, onPartFailure);
        }

        // With onSheetFailure null, the first failing worksheet fails the whole walk.
        private static string ExtractText(SpreadsheetDocument doc, Action<Exception>? onSheetFailure = null)
        {
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return string.Empty;
            // Loading the root rejects a main part that is not a workbook (another OOXML flavor under
            // the xlsx mime), so it is reported instead of read as an empty workbook (#288).
            _ = workbookPart.Workbook;

            var sharedStrings = BuildSharedStringLookup(workbookPart);
            var lines = new List<string>();

            // Same order as WorkbookPart.WorksheetParts; each part is resolved inside its own boundary.
            foreach (var pair in workbookPart.Parts)
            {
                ExtractionCancellation.ThrowIfCancellationRequested();
                try
                {
                    if (pair.OpenXmlPart is WorksheetPart worksheetPart) AppendSheet(worksheetPart, sharedStrings, lines);
                }
                catch (Exception ex) when (onSheetFailure != null && FieldIsolation.IsIsolatable(ex))
                {
                    onSheetFailure(ex);
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static void AppendSheet(WorksheetPart worksheetPart, List<string> sharedStrings, List<string> lines)
        {
            var sheetData = worksheetPart.Worksheet?.Elements<SheetData>().FirstOrDefault();
            if (sheetData == null) return;

            foreach (var row in sheetData.Elements<Row>())
            {
                ExtractionCancellation.ThrowIfCancellationRequested();
                var cellTexts = new List<string>();
                foreach (var cell in row.Elements<Cell>())
                {
                    string? text = GetCellText(cell, sharedStrings);
                    if (!string.IsNullOrEmpty(text))
                    {
                        cellTexts.Add(text);
                    }
                }

                if (cellTexts.Count > 0)
                {
                    lines.Add(string.Join(" ", cellTexts));
                }
            }
        }

        private static List<string> BuildSharedStringLookup(WorkbookPart workbookPart)
        {
            var stringTablePart = workbookPart.SharedStringTablePart;
            if (stringTablePart?.SharedStringTable == null)
            {
                return new List<string>();
            }

            return stringTablePart.SharedStringTable
                .Elements<SharedStringItem>()
                .Select(item => item.InnerText)
                .ToList();
        }

        // Only shared-string and inline-string cells carry literal text - a cell
        // with no "t" (type) attribute defaults to numeric per the OOXML spec, and
        // "str" is a formula's cached string result rather than authored text, so
        // neither contributes to flat-text extraction here.
        private static string? GetCellText(Cell cell, List<string> sharedStrings)
        {
            var dataType = cell.DataType?.Value;

            if (dataType == CellValues.SharedString)
            {
                var rawValue = cell.CellValue?.InnerText;
                if (rawValue != null && int.TryParse(rawValue, out int index) && index >= 0 && index < sharedStrings.Count)
                {
                    return sharedStrings[index];
                }
                return null;
            }

            if (dataType == CellValues.InlineString)
            {
                return cell.InlineString?.InnerText;
            }

            return null;
        }
    }
}
