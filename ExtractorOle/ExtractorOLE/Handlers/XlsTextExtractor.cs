using ExtractorOLE.Abstractions;
using ExtractorOLE.Helpers.FileTypeStrategy;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using ExtractorOLE.Helpers;

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
    internal class XlsTextExtractor : IParsedDocumentTextExtractor
    {
        public string ExtractText(byte[] fileBytes)
        {
            using (var ms = new MemoryStream(fileBytes))
            {
                return Walk(Sheets(XlsWorkbooks.Load(ms)), onSheetFailure: null);
            }
        }

        // parsedDocument is the workbook XlsOpenStrategy already loaded; caller owns it.
        public string? ExtractText(object parsedDocument) => ExtractText(parsedDocument, onPartFailure: null);

        // A failing sheet costs only its own text (rows already read from it, if any, are kept); the
        // other sheets are still read (#288). Fetching each sheet is inside that per-sheet boundary.
        public string? ExtractText(object parsedDocument, Action<Exception>? onPartFailure)
        {
            if (parsedDocument is not HSSFWorkbook workbook) return null;
            return Walk(Sheets(workbook), onPartFailure);
        }

        // One deferred fetch per sheet, so a sheet that cannot be fetched fails only itself.
        private static List<Func<ISheet>> Sheets(HSSFWorkbook workbook)
        {
            var sheets = new List<Func<ISheet>>();
            for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
            {
                int index = sheetIndex;
                sheets.Add(() => workbook.GetSheetAt(index));
            }
            return sheets;
        }

        // With onSheetFailure null, the first failing sheet fails the whole walk.
        internal static string Walk(IEnumerable<Func<ISheet>> sheets, Action<Exception>? onSheetFailure)
        {
            var lines = new List<string>();

            foreach (var getSheet in sheets)
            {
                ExtractionCancellation.ThrowIfCancellationRequested();
                try
                {
                    AppendSheet(getSheet(), lines);
                }
                catch (Exception ex) when (onSheetFailure != null && FieldIsolation.IsIsolatable(ex))
                {
                    onSheetFailure(ex);
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static void AppendSheet(ISheet sheet, List<string> lines)
        {
            foreach (IRow row in sheet)
            {
                ExtractionCancellation.ThrowIfCancellationRequested();
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
    }
}
