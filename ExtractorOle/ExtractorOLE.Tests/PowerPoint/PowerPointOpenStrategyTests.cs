using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Covers T-b792f8e5: PowerPointFormatMetadata parity for pptx (slide count,
    /// notes-slide count, macro-presence flag), mirroring XlsOpenStrategyTests'
    /// pattern for ExcelFormatMetadata. Fixtures start from PptxSampleGenerator's
    /// single-slide output (theme/master/layout chain + optional VbaProjectPart
    /// already covered there) and add extra slides/notes slides directly via
    /// DocumentFormat.OpenXml, the same technique PptxFirstLayerEmbeddedWalkTests
    /// uses to manipulate an already-generated package.
    /// </summary>
    public class PowerPointOpenStrategyTests
    {
        private static byte[] BuildPptxFixture(int totalSlideCount, HashSet<int> notesSlideIndexes, byte[]? vbaProject = null)
        {
            var spec = new SampleSpec { BodyText = "Slide 1", VbaProject = vbaProject };
            var generated = new PptxSampleGenerator().Generate(spec);

            using var ms = new MemoryStream();
            ms.Write(generated.Content, 0, generated.Content.Length);
            ms.Position = 0;

            using (var doc = PresentationDocument.Open(ms, true))
            {
                var presentationPart = doc.PresentationPart!;
                var firstSlidePart = presentationPart.SlideParts.Single();
                var slideLayoutPart = firstSlidePart.SlideLayoutPart!;

                if (notesSlideIndexes.Contains(0))
                {
                    AddNotesSlide(firstSlidePart, "Speaker notes for slide 1");
                }

                var slideIdList = presentationPart.Presentation!.SlideIdList!;
                uint nextSlideId = slideIdList.Elements<SlideId>().Max(s => s.Id!.Value) + 1;

                for (int i = 1; i < totalSlideCount; i++)
                {
                    var slidePart = presentationPart.AddNewPart<SlidePart>();
                    slidePart.AddPart(slideLayoutPart);

                    var shapeTree = EmptyShapeTree();
                    AppendTextShape(shapeTree, 2, $"Slide {i + 1}");
                    slidePart.Slide = new Slide(new CommonSlideData(shapeTree));

                    if (notesSlideIndexes.Contains(i))
                    {
                        AddNotesSlide(slidePart, $"Speaker notes for slide {i + 1}");
                    }

                    slideIdList.Append(new SlideId { Id = nextSlideId, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
                    nextSlideId++;
                }

                presentationPart.Presentation.Save();
            }

            return ms.ToArray();
        }

        private static void AddNotesSlide(SlidePart slidePart, string notesText)
        {
            var notesSlidePart = slidePart.AddNewPart<NotesSlidePart>();
            var shapeTree = EmptyShapeTree();
            AppendTextShape(shapeTree, 2, notesText);
            notesSlidePart.NotesSlide = new NotesSlide(new CommonSlideData(shapeTree));
        }

        private static ShapeTree EmptyShapeTree() => new ShapeTree(
            new NonVisualGroupShapeProperties(
                new NonVisualDrawingProperties { Id = 1, Name = "" },
                new NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new A.TransformGroup()));

        private static void AppendTextShape(ShapeTree shapeTree, uint shapeId, string text)
        {
            shapeTree.AppendChild(new Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = shapeId, Name = $"TextBox {shapeId}" },
                    new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                    new ApplicationNonVisualDrawingProperties()),
                new ShapeProperties(),
                new TextBody(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(new A.Run(new A.Text(text))))));
        }

        [Fact]
        public void Open_NonMacroMultiSlideFixtureWithSomeNotes_PopulatesPowerPointFormatMetadata()
        {
            var bytes = BuildPptxFixture(totalSlideCount: 3, notesSlideIndexes: new HashSet<int> { 0, 2 });

            var result = new PowerPointOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(3, formatMetadata.SlideCount);
            Assert.Equal(2, formatMetadata.NotesSlideCount);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_MacroEnabledFixture_PopulatesHasMacrosTrue()
        {
            var bytes = BuildPptxFixture(totalSlideCount: 1, notesSlideIndexes: new HashSet<int>(), vbaProject: new byte[] { 1, 2, 3, 4 });

            var result = new PowerPointOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(1, formatMetadata.SlideCount);
            Assert.Equal(0, formatMetadata.NotesSlideCount);
            Assert.True(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_NonMacroFixture_PopulatesHasMacrosFalse()
        {
            var bytes = BuildPptxFixture(totalSlideCount: 1, notesSlideIndexes: new HashSet<int>());

            var result = new PowerPointOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result!.FormatMetadata);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_FixtureWithNoMetadataSet_LeavesFileMetadataFieldsNullNotPlaceholder()
        {
            var bytes = BuildPptxFixture(totalSlideCount: 1, notesSlideIndexes: new HashSet<int>());

            var result = new PowerPointOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Null(result!.Metadata.Title);
            Assert.Null(result.Metadata.Creator);
            Assert.Null(result.Metadata.LastModifiedBy);
        }

        [Fact]
        public void Open_CorruptBytes_ReturnsNull()
        {
            var result = new PowerPointOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.Null(result);
        }

        // #308: each pptx format-metadata leaf fails alone at FormatMetadata.<Leaf>.
        public static TheoryData<string> LeafNames => new()
        {
            nameof(PowerPointFormatMetadata.SlideCount),
            nameof(PowerPointFormatMetadata.NotesSlideCount),
            nameof(PowerPointFormatMetadata.HasMacros),
        };

        [Theory]
        [MemberData(nameof(LeafNames))]
        public void Open_OneFailingLeafSource_LosesOnlyThatLeaf(string failing)
        {
            var bytes = BuildPptxFixture(totalSlideCount: 3, notesSlideIndexes: new HashSet<int> { 0, 2 }, vbaProject: new byte[] { 1, 2, 3, 4 });
            var strategy = new PowerPointOpenStrategy(
                new ExtractionHelper(),
                p => failing == nameof(PowerPointFormatMetadata.SlideCount) ? throw new FormatException("x") : p!.SlideParts.Count(),
                p => failing == nameof(PowerPointFormatMetadata.NotesSlideCount) ? throw new FormatException("x") : 2,
                p => failing == nameof(PowerPointFormatMetadata.HasMacros) ? throw new FormatException("x") : true);

            var result = strategy.Open(bytes);

            Assert.NotNull(result);
            var error = Assert.Single(result!.FieldErrors);
            Assert.Equal("FormatMetadata." + failing, error.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result.FormatMetadata);
            Assert.Equal(failing == nameof(PowerPointFormatMetadata.SlideCount) ? 0 : 3, formatMetadata.SlideCount);
            Assert.Equal(failing == nameof(PowerPointFormatMetadata.NotesSlideCount) ? null : 2, formatMetadata.NotesSlideCount);
            Assert.Equal(failing != nameof(PowerPointFormatMetadata.HasMacros), formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_UndeclaredExceptionAfterOpen_PropagatesUnwrapped()
        {
            var bytes = BuildPptxFixture(totalSlideCount: 1, notesSlideIndexes: new HashSet<int>());
            var strategy = new PowerPointOpenStrategy(
                new ExtractionHelper(),
                p => 1,
                p => throw new InsufficientMemoryException(),
                p => false);

            Assert.Throws<InsufficientMemoryException>(() => strategy.Open(bytes));
        }
    }
}
