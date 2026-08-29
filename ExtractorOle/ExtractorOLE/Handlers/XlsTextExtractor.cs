using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace ExtractorOLE.Handlers
{
    /// <summary>
    /// Extracts flattened plain-text cell content from legacy .xls (BIFF)
    /// workbooks via NPOI's HSSFWorkbook. Only string-typed cells contribute
    /// text; each row's string cells are joined with a single space and rows
    /// are joined with <see cref="Environment.NewLine"/>, with no trailing
    /// separators - so a workbook with no textual cell content naturally
    /// yields <see cref="string.Empty"/> rather than blank lines.
    ///
    /// Cell text comes back via NPOI's <see cref="ICell.RichStringCellValue"/>
    /// exactly as decoded from the BIFF unicode string record (which stores
    /// each string as either compressed 8-bit or uncompressed UTF-16LE,
    /// chosen automatically by NPOI on write depending on whether the string
    /// fits in the 8-bit range) - no additional encoding conversion or
    /// Unicode normalization is applied here.
    /// </summary>
    internal class XlsTextExtractor : ITextExtractor
    {
        public string ExtractText(byte[] fileBytes)
        {
            try
            {
                using (var ms = new MemoryStream(fileBytes))
                {
                    var workbook = new HSSFWorkbook(ms);
                    var lines = new List<string>();

                    for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                    {
                        var sheet = workbook.GetSheetAt(sheetIndex);
                        foreach (IRow row in sheet)
                        {
                            var cellTexts = new List<string>();
                            foreach (ICell cell in row)
                            {
                                if (cell.CellType != CellType.String) continue;
                                string? text = cell.RichStringCellValue?.String;
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
                // swallow and return empty text on failure
            }

            return string.Empty;
        }
    }
}
