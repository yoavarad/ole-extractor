using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
    internal class XlsxTextExtractor : ITextExtractor
    {
        public string ExtractText(byte[] fileBytes)
        {
            try
            {
                using (var ms = new MemoryStream(fileBytes))
                using (var doc = SpreadsheetDocument.Open(ms, false))
                {
                    var workbookPart = doc.WorkbookPart;
                    if (workbookPart == null) return string.Empty;

                    var sharedStrings = BuildSharedStringLookup(workbookPart);
                    var lines = new List<string>();

                    foreach (var worksheetPart in workbookPart.WorksheetParts)
                    {
                        var sheetData = worksheetPart.Worksheet?.Elements<SheetData>().FirstOrDefault();
                        if (sheetData == null) continue;

                        foreach (var row in sheetData.Elements<Row>())
                        {
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

                    return string.Join(Environment.NewLine, lines);
                }
            }
            catch (Exception)
            {
                // swallow and return empty
            }

            return string.Empty;
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
                return cell.InlineString?.Text?.Text;
            }

            return null;
        }
    }
}
