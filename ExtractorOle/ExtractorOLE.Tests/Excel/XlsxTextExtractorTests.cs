using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.Handlers;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    /// <summary>
    /// Round-trip tests for <see cref="XlsxTextExtractor"/> covering
    /// docs/specs/extraction.md's [ydk:req:extraction/unicode-fidelity] on the
    /// .xlsx (OOXML/DocumentFormat.OpenXml) path: multilingual/mixed-script
    /// text and emoji must round-trip exactly, with no mojibake, no
    /// truncation, and no implicit Unicode normalization.
    ///
    /// Textual fixtures are built via SampleGenerator.Generators.XlsxSampleGenerator
    /// (one shared-string row per body-text line). The "no textual content" and
    /// "empty workbook" edge cases are hand-built directly via DocumentFormat.OpenXml
    /// (numeric cells and truly empty sheets aren't something XlsxSampleGenerator
    /// produces), the same approach ExcelOpenStrategyTests already uses.
    /// </summary>
    public class XlsxTextExtractorTests
    {
        private static byte[] BuildXlsxWithNumericCell(double value)
        {
            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                var row = new Row { RowIndex = 1 };
                row.Append(new Cell
                {
                    CellReference = "A1",
                    // No DataType attribute -> defaults to Number per the OOXML spec,
                    // so this cell carries no textual content.
                    CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture))
                });
                sheetData.Append(row);
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Sheet1"
                });

                workbookPart.Workbook.Save();
            }
            return stream.ToArray();
        }

        private static byte[] BuildEmptyXlsxWorkbook()
        {
            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Sheet1"
                });

                workbookPart.Workbook.Save();
            }
            return stream.ToArray();
        }

        [Fact]
        public void NoTextualCellContent_ReturnsEmptyString_NeverNull()
        {
            var bytes = BuildXlsxWithNumericCell(42.0);

            var result = new XlsxTextExtractor().ExtractText(bytes);

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void EmptyWorkbook_ReturnsEmptyString_NeverNull()
        {
            var bytes = BuildEmptyXlsxWorkbook();

            var result = new XlsxTextExtractor().ExtractText(bytes);

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void MultilingualFixture_RoundTripsExactly()
        {
            var body = MultilingualFixtures.ComposeMixedBody();
            var lines = body.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var generated = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = body });

            var result = new XlsxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(string.Join(Environment.NewLine, lines), result);
        }

        [Theory]
        [InlineData(nameof(MultilingualFixtures.Hebrew))]
        [InlineData(nameof(MultilingualFixtures.Arabic))]
        [InlineData(nameof(MultilingualFixtures.Persian))]
        [InlineData(nameof(MultilingualFixtures.Russian))]
        [InlineData(nameof(MultilingualFixtures.Latin))]
        [InlineData(nameof(MultilingualFixtures.Cjk))]
        public void SingleScriptFixture_RoundTripsExactly(string scriptName)
        {
            string expected = scriptName switch
            {
                nameof(MultilingualFixtures.Hebrew) => MultilingualFixtures.Hebrew,
                nameof(MultilingualFixtures.Arabic) => MultilingualFixtures.Arabic,
                nameof(MultilingualFixtures.Persian) => MultilingualFixtures.Persian,
                nameof(MultilingualFixtures.Russian) => MultilingualFixtures.Russian,
                nameof(MultilingualFixtures.Latin) => MultilingualFixtures.Latin,
                nameof(MultilingualFixtures.Cjk) => MultilingualFixtures.Cjk,
                _ => throw new ArgumentOutOfRangeException(nameof(scriptName))
            };
            var generated = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = expected });

            var result = new XlsxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(nameof(MultilingualFixtures.EmojiSimple))]
        [InlineData(nameof(MultilingualFixtures.EmojiFamilyZwj))]
        [InlineData(nameof(MultilingualFixtures.EmojiSkinTone))]
        [InlineData(nameof(MultilingualFixtures.EmojiFlagZwj))]
        public void EmojiFixture_RoundTripsExactly(string fixtureName)
        {
            string expected = fixtureName switch
            {
                nameof(MultilingualFixtures.EmojiSimple) => MultilingualFixtures.EmojiSimple,
                nameof(MultilingualFixtures.EmojiFamilyZwj) => MultilingualFixtures.EmojiFamilyZwj,
                nameof(MultilingualFixtures.EmojiSkinTone) => MultilingualFixtures.EmojiSkinTone,
                nameof(MultilingualFixtures.EmojiFlagZwj) => MultilingualFixtures.EmojiFlagZwj,
                _ => throw new ArgumentOutOfRangeException(nameof(fixtureName))
            };
            var generated = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = expected });

            var result = new XlsxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
            Assert.Equal(expected.EnumerateRunes().ToArray(), result.EnumerateRunes().ToArray());
        }

        [Fact]
        public void CombiningCharacterSequence_NotNormalized()
        {
            // 'e' (U+0065) + COMBINING ACUTE ACCENT (U+0301) - deliberately
            // decomposed, distinct from the precomposed 'é' (U+00E9). If the
            // extractor (or the OpenXml SDK's XML text round-trip) ever applied
            // Unicode normalization, this sequence would collapse to/from the
            // precomposed form and the codepoint sequence would no longer match.
            var expected = "e\u0301";
            var generated = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = expected });

            var result = new XlsxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
            Assert.Equal(expected.Length, result.Length);
            Assert.Equal(expected.EnumerateRunes().ToArray(), result.EnumerateRunes().ToArray());
        }

        [Fact]
        public void CorruptBytes_ReturnsEmptyString_NeverThrows()
        {
            var result = new XlsxTextExtractor().ExtractText(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }
    }
}
