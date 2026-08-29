using ExtractorOLE.Handlers;
using NPOI.HSSF.UserModel;
using SampleGenerator.Fixtures;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    /// <summary>
    /// Round-trip tests for <see cref="XlsTextExtractor"/> covering
    /// docs/specs/extraction.md's [ydk:req:extraction/unicode-fidelity] on the
    /// legacy .xls (BIFF/HSSFWorkbook) path: multilingual/mixed-script text
    /// and emoji must round-trip exactly, with no mojibake, no truncation,
    /// and no implicit Unicode normalization.
    ///
    /// Fixtures are hand-built directly via NPOI's HSSFWorkbook API (rather
    /// than SampleGenerator.Generators.XlsSampleGenerator, which is a stub -
    /// legacy .xls authoring wasn't wired into that project yet) - the same
    /// approach XlsOpenStrategyTests already uses.
    /// </summary>
    public class XlsTextExtractorTests
    {
        private static byte[] BuildXlsWithCellStrings(params string[] lines)
        {
            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");
            for (int i = 0; i < lines.Length; i++)
            {
                var row = sheet.CreateRow(i);
                var cell = row.CreateCell(0);
                cell.SetCellValue(lines[i]);
            }
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        [Fact]
        public void NoTextualCellContent_ReturnsEmptyString_NeverNull()
        {
            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");
            // A numeric-only cell: no textual content anywhere in the workbook.
            var row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue(42.0);
            using var ms = new MemoryStream();
            workbook.Write(ms);

            var result = new XlsTextExtractor().ExtractText(ms.ToArray());

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void EmptyWorkbook_ReturnsEmptyString_NeverNull()
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1");
            using var ms = new MemoryStream();
            workbook.Write(ms);

            var result = new XlsTextExtractor().ExtractText(ms.ToArray());

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void MultilingualFixture_RoundTripsExactly()
        {
            var lines = MultilingualFixtures.ComposeMixedBody()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var bytes = BuildXlsWithCellStrings(lines);

            var result = new XlsTextExtractor().ExtractText(bytes);

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
            var bytes = BuildXlsWithCellStrings(expected);

            var result = new XlsTextExtractor().ExtractText(bytes);

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
            var bytes = BuildXlsWithCellStrings(expected);

            var result = new XlsTextExtractor().ExtractText(bytes);

            Assert.Equal(expected, result);
            Assert.Equal(expected.EnumerateRunes().ToArray(), result.EnumerateRunes().ToArray());
        }

        [Fact]
        public void CombiningCharacterSequence_NotNormalized()
        {
            // 'e' (U+0065) + COMBINING ACUTE ACCENT (U+0301) - deliberately
            // decomposed, distinct from the precomposed 'é' (U+00E9). If the
            // extractor (or NPOI's BIFF string round-trip) ever applied
            // Unicode normalization, this sequence would collapse to/from the
            // precomposed form and the codepoint sequence would no longer
            // match.
            var expected = "é";
            var bytes = BuildXlsWithCellStrings(expected);

            var result = new XlsTextExtractor().ExtractText(bytes);

            Assert.Equal(expected, result);
            Assert.Equal(expected.Length, result.Length);
            Assert.Equal(expected.EnumerateRunes().ToArray(), result.EnumerateRunes().ToArray());
        }

        [Fact]
        public void CorruptBytes_ReturnsEmptyString_NeverThrows()
        {
            var result = new XlsTextExtractor().ExtractText(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }
    }
}
