using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExtractorOLE.Handlers;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Docx
{
    /// <summary>
    /// Round-trip tests for <see cref="DocxTextExtractor"/> covering
    /// docs/specs/extraction.md's [ydk:req:extraction/unicode-fidelity]:
    /// multilingual/mixed-script text and emoji (including ZWJ sequences and
    /// skin-tone modifiers) must round-trip exactly, with no mojibake, no
    /// truncation, no paragraph merging, and no implicit Unicode
    /// normalization.
    /// </summary>
    public class DocxTextExtractorTests
    {
        [Fact]
        public void EmptyBody_ReturnsEmptyString_NeverNull()
        {
            var spec = new SampleSpec { BodyText = string.Empty };
            var generated = new DocxSampleGenerator().Generate(spec);

            var result = new DocxTextExtractor().ExtractText(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void MultilingualFixture_RoundTripsExactly()
        {
            var expected = MultilingualFixtures.ComposeMixedBody();
            var spec = new SampleSpec { BodyText = expected };
            var generated = new DocxSampleGenerator().Generate(spec);

            var result = new DocxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CombiningCharacterSequence_NotNormalized()
        {
            // 'e' (U+0065) + COMBINING ACUTE ACCENT (U+0301) - deliberately
            // decomposed, distinct from the precomposed 'é' (U+00E9). If the
            // extractor ever applied Unicode normalization, this sequence
            // would collapse to the precomposed form (or vice versa) and the
            // codepoint sequence would no longer match.
            var expected = "é";
            var spec = new SampleSpec { BodyText = expected };
            var generated = new DocxSampleGenerator().Generate(spec);

            var result = new DocxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
            Assert.Equal(expected.Length, result.Length);
            Assert.Equal(expected.EnumerateRunes().ToArray(), result.EnumerateRunes().ToArray());
        }

        [Fact]
        public void TableCellText_IsNotSilentlyDropped()
        {
            var topLevelText = MultilingualFixtures.Latin;
            var tableCellText = MultilingualFixtures.Cjk;

            using var stream = new MemoryStream();
            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                var body = new Body();
                mainPart.Document = new Document(body);

                body.AppendChild(new Paragraph(new Run(new Text(topLevelText) { Space = SpaceProcessingModeValues.Preserve })));

                var table = new Table();
                var tableRow = new TableRow();
                var tableCell = new TableCell(new Paragraph(new Run(new Text(tableCellText) { Space = SpaceProcessingModeValues.Preserve })));
                tableRow.AppendChild(tableCell);
                table.AppendChild(tableRow);
                body.AppendChild(table);

                mainPart.Document.Save();
            }

            var result = new DocxTextExtractor().ExtractText(stream.ToArray());

            Assert.Contains(topLevelText, result);
            Assert.Contains(tableCellText, result);
        }

        private static string ExtractFrom(Body body)
        {
            using var stream = new MemoryStream();
            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(body);
                mainPart.Document.Save();
            }

            return new DocxTextExtractor().ExtractText(stream.ToArray());
        }

        [Fact]
        public void TextBoxParagraph_TextAppearsOnce()
        {
            var inner = new Paragraph(new Run(new Text("boxed")));
            var outer = new Paragraph(
                new Run(new Text("before ")),
                new Run(new Picture(new DocumentFormat.OpenXml.Vml.Shape(new DocumentFormat.OpenXml.Vml.TextBox(new TextBoxContent(inner))))));
            var result = ExtractFrom(new Body(outer));

            Assert.Equal(1, System.Text.RegularExpressions.Regex.Count(result, "boxed"));
            Assert.Contains("before", result);
        }

        [Fact]
        public void TrackedChanges_DeletedTextExcluded_InsertedTextKept()
        {
            var p = new Paragraph(
                new Run(new Text("keep ")),
                new DeletedRun(new Run(new DeletedText("gone"))),
                new InsertedRun(new Run(new Text("added"))));
            var result = ExtractFrom(new Body(p));

            Assert.Equal("keep added", result);
        }
    }
}
