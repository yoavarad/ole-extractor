using ExtractorOLE.Abstractions;
using ExtractorOLE.Handlers;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using SampleGenerator.Fixtures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ExtractorOLE.Helpers;

namespace ExtractorOLE.Tests.Excel
{
    /// <summary>
    /// Round-trip tests for <see cref="XlsTextExtractor"/> covering
    /// docs/specs/extraction.md's [ydk:req:extraction/unicode-fidelity] on the
    /// legacy .xls (BIFF/HSSFWorkbook) path: multilingual/mixed-script text
    /// and emoji must round-trip exactly, with no mojibake, no truncation,
    /// and no implicit Unicode normalization.
    ///
    /// Fixtures are hand-built directly via NPOI's HSSFWorkbook API (these
    /// tests predate SampleGenerator.Generators.XlsSampleGenerator, which
    /// SampleGeneration/XlsSampleGeneratorTests now covers) - the same
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

        private static HSSFSheet SheetWith(string text)
        {
            var sheet = (HSSFSheet)new HSSFWorkbook().CreateSheet("S");
            sheet.CreateRow(0).CreateCell(0).SetCellValue(text);
            return sheet;
        }

        public class ThrowingSheetProxy : System.Reflection.DispatchProxy
        {
            public Exception Failure { get; set; } = null!;

            protected override object? Invoke(System.Reflection.MethodInfo? targetMethod, object?[]? args) => throw Failure;
        }

        private static ISheet ThrowingSheet(Exception failure)
        {
            var proxy = System.Reflection.DispatchProxy.Create<ISheet, ThrowingSheetProxy>();
            ((ThrowingSheetProxy)(object)proxy).Failure = failure;
            return proxy;
        }

        [Fact]
        public void Walk_BadSheet_WithCallback_KeepsHealthySheetsAndReportsOnce()
        {
            var failures = new List<Exception>();
            var sheets = new[] { SheetWith("alpha"), ThrowingSheet(new InvalidOperationException()), SheetWith("gamma") };

            var text = XlsTextExtractor.Walk(sheets, failures.Add);

            Assert.Contains("alpha", text, StringComparison.Ordinal);
            Assert.Contains("gamma", text, StringComparison.Ordinal);
            Assert.IsType<InvalidOperationException>(Assert.Single(failures));
        }

        [Fact]
        public void Walk_BadSheet_WithoutCallback_Throws()
        {
            var sheets = new[] { SheetWith("alpha"), ThrowingSheet(new InvalidOperationException()), SheetWith("gamma") };

            Assert.Throws<InvalidOperationException>(() => XlsTextExtractor.Walk(sheets, onSheetFailure: null));
        }

        [Fact]
        public void Walk_OutOfMemorySheet_PropagatesEvenWithCallback()
        {
            var failures = new List<Exception>();
            // An OutOfMemoryException (CA2201 reserves the base type).
            var sheets = new[] { SheetWith("alpha"), ThrowingSheet(new InsufficientMemoryException()) };

            Assert.ThrowsAny<OutOfMemoryException>(() => XlsTextExtractor.Walk(sheets, failures.Add));
            Assert.Empty(failures);
        }

        [Fact]
        public void Walk_CancelledInsideSheet_PropagatesEvenWithCallback()
        {
            var failures = new List<Exception>();
            var sheets = new[] { SheetWith("alpha"), ThrowingSheet(new OperationCanceledException()) };

            Assert.Throws<OperationCanceledException>(() => XlsTextExtractor.Walk(sheets, failures.Add));
            Assert.Empty(failures);
        }

        [Fact]
        public void ParsedWorkbook_WithCallback_ReturnsAllSheetsAndNoFailures()
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("A").CreateRow(0).CreateCell(0).SetCellValue("alpha");
            workbook.CreateSheet("B").CreateRow(0).CreateCell(0).SetCellValue("gamma");
            var failures = new List<Exception>();

            var text = ((IParsedDocumentTextExtractor)new XlsTextExtractor()).ExtractText(workbook, failures.Add);

            Assert.Contains("alpha", text, StringComparison.Ordinal);
            Assert.Contains("gamma", text, StringComparison.Ordinal);
            Assert.Empty(failures);
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
        public void CorruptBytes_Throws()
        {
            Assert.ThrowsAny<Exception>(() => new XlsTextExtractor().ExtractText(new byte[] { 0x00, 0x01, 0x02, 0x03 }));
        }

        // #290: the sheet/slide/embedded loops observe the Extract call's cancellation token.
        [Fact]
        public void ExtractText_CancelledToken_ThrowsOperationCanceled()
        {
            var bytes = BuildXlsWithCellStrings("a", "b");
            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();
            using var scope = ExtractionCancellation.Enter(cts.Token);

            Assert.ThrowsAny<OperationCanceledException>(() => new XlsTextExtractor().ExtractText(bytes));
        }
    }
}
