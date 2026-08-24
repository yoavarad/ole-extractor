using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a minimal, valid .xlsx (or .xlsm, when <see cref="SampleSpec.VbaProject"/>
    /// is set) via DocumentFormat.OpenXml directly: controllable body text
    /// (one shared-string row per line), general metadata, first-layer
    /// embeddings, and - for the macro-enabled variant - a structurally-valid
    /// VBA project storage (arbitrary placeholder bytes; no functioning macro
    /// is required). The macro-enabled variant must still be detected as base
    /// format xlsx, not as a separate format (Epic 5/6's
    /// macro-variant-misclassification scenario). Uses the shared-string
    /// table (rather than inline strings) because that's what
    /// ExtractorOLE's XlsxTextExtractor reads back.
    /// </summary>
    public sealed class XlsxSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Xlsx;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            var isMacroEnabled = spec.VbaProject is not null;
            var documentType = isMacroEnabled
                ? SpreadsheetDocumentType.MacroEnabledWorkbook
                : SpreadsheetDocumentType.Workbook;

            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, documentType))
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
                // ImagePart/EmbeddedObjectPart directly.
                OpenXmlEmbeddingHelper.AddEmbeddings(worksheetPart, spec.Embeddings);

                if (spec.VbaProject is { } vbaProject)
                {
                    var vbaProjectPart = workbookPart.AddNewPart<VbaProjectPart>();
                    using var vbaStream = new MemoryStream(vbaProject);
                    vbaProjectPart.FeedData(vbaStream);
                }

                workbookPart.Workbook.Save();
            }

            return new GeneratedSample
            {
                FileName = isMacroEnabled ? "sample.xlsm" : "sample.xlsx",
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
