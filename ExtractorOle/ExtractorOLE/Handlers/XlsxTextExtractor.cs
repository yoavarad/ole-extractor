using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExtractorOLE.Handlers
{
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

                    // Pre-cache shared strings
                    List<string> sharedStrings = new List<string>();
                    var stringTablePart = workbookPart.SharedStringTablePart;
                    if (stringTablePart?.SharedStringTable != null)
                    {
                        sharedStrings = stringTablePart.SharedStringTable
                            .Elements<SharedStringItem>()
                            .Select(x => x.InnerText)
                            .ToList();
                    }

                    StringBuilder sb = new StringBuilder();
                    foreach (var worksheetPart in workbookPart.WorksheetParts)
                    {
                        var sheetData = worksheetPart.Worksheet?.Elements<SheetData>().FirstOrDefault();
                        if (sheetData == null) continue;

                        foreach (var row in sheetData.Elements<Row>())
                        {
                            foreach (var cell in row.Elements<Cell>())
                            {
                                string rawValue = cell.CellValue?.InnerText ?? string.Empty;
                                string value = rawValue;
                                if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                                {
                                    if (int.TryParse(rawValue, out int idx) && idx >= 0 && idx < sharedStrings.Count)
                                        value = sharedStrings[idx];
                                }

                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    sb.Append(value).Append(' ');
                                }
                            }
                            sb.AppendLine();
                        }
                    }

                    return sb.ToString().Trim();
                }
            }
            catch (Exception)
            {
                // swallow and return empty
            }

            return string.Empty;
        }
    }
}
