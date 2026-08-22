using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a minimal, valid .xlsx via DocumentFormat.OpenXml directly:
    /// controllable body text (one shared-string row per line), general
    /// metadata, and first-layer embeddings. Uses the shared-string table
    /// (rather than inline strings) because that's what
    /// ExtractorOLE's XlsxTextExtractor reads back.
    /// </summary>
    public sealed class XlsxSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Xlsx;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var sharedStringsPart = workbookPart.AddNewPart<SharedStringTablePart>();
                sharedStringsPart.SharedStringTable = new SharedStringTable();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Sample"
                });

                uint rowIndex = 1;
                foreach (var line in DocxSampleGenerator.SplitLines(spec.BodyText))
                {
                    var stringIndex = AddSharedString(sharedStringsPart.SharedStringTable, line);
                    var row = new Row { RowIndex = rowIndex++ };
                    row.Append(new Cell
                    {
                        CellReference = $"A{row.RowIndex}",
                        DataType = CellValues.SharedString,
                        CellValue = new CellValue(stringIndex.ToString())
                    });
                    sheetData.Append(row);
                }

                OpenXmlPackagePropertiesHelper.Apply(document.PackageProperties, spec.Metadata);
                // Embeddings placed on WorksheetPart because WorkbookPart doesn't accept
                // ImagePart/EmbeddedObjectPart directly. NOTE: ExcelExtractor.GetFirstLayerEmbedded
                // (ExtractorOle/ExtractorOLE/Old/Excel/ExcelExtractor.cs) currently only scans
                // workbookPart.Parts, so these won't be found by extraction until that's updated.
                OpenXmlEmbeddingHelper.AddEmbeddings(worksheetPart, spec.Embeddings);

                workbookPart.Workbook.Save();
            }

            return new GeneratedSample
            {
                FileName = "sample.xlsx",
                Content = stream.ToArray()
            };
        }

        private static int AddSharedString(SharedStringTable table, string text)
        {
            var item = new SharedStringItem(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            table.AppendChild(item);
            return table.ChildElements.Count - 1;
        }
    }
}
