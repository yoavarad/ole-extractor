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

        [Fact]
        public void Slides_AreEmittedInSldIdLstOrder_NotRelationshipOrder_EachFollowedByItsOwnNotes()
        {
            // Slides are added A, B, C (so the package relationships are A, B, C) but the deck
            // order in p:sldIdLst is C, A, B. Each slide carries its own notes text.
            var bytes = BuildPptxWithReorderedSlides();

            var result = new PptxTextExtractor().ExtractText(bytes);

            var expected = string.Join(Environment.NewLine,
                "C slide", "C notes", "A slide", "A notes", "B slide", "B notes");
            Assert.Equal(expected, result);
        }

        [Fact]
        public void DanglingSldIdRelationshipId_IsSkipped_RemainingSlidesStillExtracted()
        {
            var generated = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "A slide" });

            using var ms = new MemoryStream();
            ms.Write(generated.Content, 0, generated.Content.Length);
            ms.Position = 0;

            using (var doc = PresentationDocument.Open(ms, true))
            {
                var slideIds = doc.PresentationPart!.Presentation!.SlideIdList!;
                slideIds.PrependChild(new SlideId { Id = 300U, RelationshipId = "rId999" });
                doc.PresentationPart.Presentation.Save();
            }

            var result = new PptxTextExtractor().ExtractText(ms.ToArray());

            Assert.Equal("A slide", result);
        }

        [Fact]
        public void NoaaSample_BeginsWithDeckSlideOneText_BeforeSlideTwoText()
        {
            // The NOAA deck was reordered after creation: its package relationships list slide
            // parts out of deck order, so a SlideParts walk starts at deck slide 7.
            var bytes = File.ReadAllBytes(FindRepoRootFile(NoaaSamplePath));

            var result = new PptxTextExtractor().ExtractText(bytes);

            // Deck slide 1 is the title slide (first p:sldId); deck slide 2 is "NOAA Organization".
            var slideOneText = "NOAA" + Environment.NewLine + "Transition Team Briefing";
            Assert.StartsWith(slideOneText, result, StringComparison.Ordinal);
            var slideTwoIndex = result.IndexOf("NOAA Organization", StringComparison.Ordinal);
            Assert.True(slideTwoIndex > slideOneText.Length,
                "Slide 2 text ('NOAA Organization') must appear after slide 1 text.");
        }

        private const string NoaaSamplePath = "samples/curated/pptx/noaa-transition-team-briefing-2008.pptx";

        private static string FindRepoRootFile(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
        }

        // Builds a three-slide pptx (relationship order A, B, C; sldIdLst order C, A, B) from
        // PptxSampleGenerator's single-slide output, adding slides B and C plus a notes slide
        // for every slide directly via DocumentFormat.OpenXml.
        private static byte[] BuildPptxWithReorderedSlides()
        {
            var generated = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "A slide" });

            using var ms = new MemoryStream();
            ms.Write(generated.Content, 0, generated.Content.Length);
            ms.Position = 0;

            using (var doc = PresentationDocument.Open(ms, true))
            {
                var presentationPart = doc.PresentationPart!;
                var slideA = presentationPart.SlideParts.Single();
                var layout = slideA.SlideLayoutPart!;

                var slideB = presentationPart.AddNewPart<SlidePart>();
                slideB.AddPart(layout);
                slideB.Slide = new Slide(new CommonSlideData(TextShapeTree("B slide")));

                var slideC = presentationPart.AddNewPart<SlidePart>();
                slideC.AddPart(layout);
                slideC.Slide = new Slide(new CommonSlideData(TextShapeTree("C slide")));

                AddNotes(slideA, "A notes");
                AddNotes(slideB, "B notes");
                AddNotes(slideC, "C notes");

                presentationPart.Presentation!.SlideIdList = new SlideIdList(
                    new SlideId { Id = 258U, RelationshipId = presentationPart.GetIdOfPart(slideC) },
                    new SlideId { Id = 256U, RelationshipId = presentationPart.GetIdOfPart(slideA) },
                    new SlideId { Id = 257U, RelationshipId = presentationPart.GetIdOfPart(slideB) });
                presentationPart.Presentation.Save();
            }

            return ms.ToArray();
        }

        private static void AddNotes(SlidePart slidePart, string notesText)
        {
            var notesSlidePart = slidePart.AddNewPart<NotesSlidePart>();
            notesSlidePart.NotesSlide = new NotesSlide(new CommonSlideData(TextShapeTree(notesText)));
        }

        private static ShapeTree TextShapeTree(string text)
        {
            var shapeTree = new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1, Name = "" },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()));
            shapeTree.AppendChild(new Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = 2, Name = "TextBox 2" },
                    new NonVisualShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new ShapeProperties(),
                new TextBody(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.Run(new A.Text(text))))));
            return shapeTree;
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
