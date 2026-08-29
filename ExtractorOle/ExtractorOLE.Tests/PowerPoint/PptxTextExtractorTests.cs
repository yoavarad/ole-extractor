using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using ExtractorOLE.Handlers;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using System;
using System.IO;
using System.Linq;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Round-trip tests for <see cref="PptxTextExtractor"/> covering docs/specs/
    /// extraction.md's [ydk:req:extraction/unicode-fidelity]: multilingual/mixed-script
    /// text and emoji (including ZWJ sequences and skin-tone modifiers) must round-trip
    /// exactly, with no mojibake, no truncation, and no implicit Unicode normalization.
    /// Mirrors DocxTextExtractorTests' pattern for the docx sibling.
    /// </summary>
    public class PptxTextExtractorTests
    {
        [Fact]
        public void EmptyBody_ReturnsEmptyString_NeverNull()
        {
            var spec = new SampleSpec { BodyText = string.Empty };
            var generated = new PptxSampleGenerator().Generate(spec);

            var result = new PptxTextExtractor().ExtractText(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void MultilingualFixture_RoundTripsExactly()
        {
            var expected = MultilingualFixtures.ComposeMixedBody();
            var spec = new SampleSpec { BodyText = expected };
            var generated = new PptxSampleGenerator().Generate(spec);

            var result = new PptxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CombiningCharacterSequence_NotNormalized()
        {
            // 'e' (U+0065) + COMBINING ACUTE ACCENT (U+0301) - deliberately decomposed,
            // distinct from the precomposed 'é' (U+00E9). If the extractor ever applied
            // Unicode normalization, this sequence would collapse to (or from) the
            // precomposed form and the codepoint sequence would no longer match.
            var expected = "é";
            var spec = new SampleSpec { BodyText = expected };
            var generated = new PptxSampleGenerator().Generate(spec);

            var result = new PptxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
            Assert.Equal(expected.Length, result.Length);
            Assert.Equal(expected.EnumerateRunes().ToArray(), result.EnumerateRunes().ToArray());
        }

        [Fact]
        public void SpeakerNotes_AreIncludedAfterSlideText_RoundTrippingMultilingualContent()
        {
            var slideText = MultilingualFixtures.Latin;
            var notesText = MultilingualFixtures.Cjk;

            var bytes = BuildPptxWithNotes(slideText, notesText);

            var result = new PptxTextExtractor().ExtractText(bytes);

            Assert.Equal(slideText + Environment.NewLine + notesText, result);
        }

        [Fact]
        public void EmojiFixtures_RoundTripExactlyIncludingZwjAndSkinToneSequences()
        {
            var expected = string.Join(Environment.NewLine, MultilingualFixtures.AllEmoji);
            var spec = new SampleSpec { BodyText = expected };
            var generated = new PptxSampleGenerator().Generate(spec);

            var result = new PptxTextExtractor().ExtractText(generated.Content);

            Assert.Equal(expected, result);
        }

        // Builds a minimal single-slide pptx whose slide carries slideText and whose
        // NotesSlidePart carries notesText - starting from PptxSampleGenerator's output
        // (theme/master/layout chain already covered there) and adding the notes slide
        // directly via DocumentFormat.OpenXml, the same technique
        // PptxFirstLayerEmbeddedWalkTests uses to manipulate an already-generated package.
        private static byte[] BuildPptxWithNotes(string slideText, string notesText)
        {
            var spec = new SampleSpec { BodyText = slideText };
            var generated = new PptxSampleGenerator().Generate(spec);

            using var ms = new MemoryStream();
            ms.Write(generated.Content, 0, generated.Content.Length);
            ms.Position = 0;

            using (var doc = PresentationDocument.Open(ms, true))
            {
                var presentationPart = doc.PresentationPart!;
                var slidePart = presentationPart.SlideParts.Single();

                var notesSlidePart = slidePart.AddNewPart<NotesSlidePart>();
                var shapeTree = new ShapeTree(
                    new NonVisualGroupShapeProperties(
                        new NonVisualDrawingProperties { Id = 1, Name = "" },
                        new NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(new A.TransformGroup()));
                shapeTree.AppendChild(new Shape(
                    new NonVisualShapeProperties(
                        new NonVisualDrawingProperties { Id = 2, Name = "Notes Placeholder" },
                        new NonVisualShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new ShapeProperties(),
                    new TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(new A.Run(new A.Text(notesText))))));

                notesSlidePart.NotesSlide = new NotesSlide(new CommonSlideData(shapeTree));

                presentationPart.Presentation!.Save();
            }

            return ms.ToArray();
        }
    }
}
