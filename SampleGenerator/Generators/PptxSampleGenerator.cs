using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using SampleGenerator.Abstractions;
using A = DocumentFormat.OpenXml.Drawing;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a minimal, valid .pptx via DocumentFormat.OpenXml directly:
    /// one slide (with the required master/layout/theme chain) carrying the
    /// body text as text-box paragraphs, general metadata, and first-layer
    /// embeddings on the slide part. PresentationPart cannot hold
    /// EmbeddedObjectPart/EmbeddedPackagePart/ImagePart directly (the
    /// OpenXml SDK rejects them there), so both embedding kinds go on the
    /// slide, matching ExtractorOLE's per-slide embedded-object/image scan.
    /// </summary>
    public sealed class PptxSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Pptx;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            using var stream = new MemoryStream();
            using (var document = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
            {
                var presentationPart = document.AddPresentationPart();
                presentationPart.Presentation = new Presentation();

                var themePart = CreateThemePart(presentationPart);
                var slideMasterPart = CreateSlideMasterPart(presentationPart, themePart);
                var slideLayoutPart = CreateSlideLayoutPart(slideMasterPart);
                var slidePart = CreateSlidePart(presentationPart, slideLayoutPart, spec.BodyText);

                presentationPart.Presentation.SlideMasterIdList = new SlideMasterIdList(
                    new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(slideMasterPart) });
                presentationPart.Presentation.SlideIdList = new SlideIdList(
                    new SlideId { Id = 256U, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
                presentationPart.Presentation.SlideSize = new SlideSize { Cx = 9144000, Cy = 6858000 };
                presentationPart.Presentation.NotesSize = new NotesSize { Cx = 6858000, Cy = 9144000 };

                OpenXmlPackagePropertiesHelper.Apply(document.PackageProperties, spec.Metadata);
                OpenXmlEmbeddingHelper.AddEmbeddings(slidePart, spec.Embeddings);

                presentationPart.Presentation.Save();
            }

            return new GeneratedSample
            {
                FileName = "sample.pptx",
                Content = stream.ToArray()
            };
        }

        private static ThemePart CreateThemePart(PresentationPart presentationPart)
        {
            var themePart = presentationPart.AddNewPart<ThemePart>();

            var colorScheme = new A.ColorScheme(
                new A.Dark1Color(new A.SystemColor { Val = A.SystemColorValues.WindowText }),
                new A.Light1Color(new A.SystemColor { Val = A.SystemColorValues.Window }),
                new A.Dark2Color(new A.RgbColorModelHex { Val = "1F497D" }),
                new A.Light2Color(new A.RgbColorModelHex { Val = "EEECE1" }),
                new A.Accent1Color(new A.RgbColorModelHex { Val = "4F81BD" }),
                new A.Accent2Color(new A.RgbColorModelHex { Val = "C0504D" }),
                new A.Accent3Color(new A.RgbColorModelHex { Val = "9BBB59" }),
                new A.Accent4Color(new A.RgbColorModelHex { Val = "8064A2" }),
                new A.Accent5Color(new A.RgbColorModelHex { Val = "4BACC6" }),
                new A.Accent6Color(new A.RgbColorModelHex { Val = "F79646" }),
                new A.Hyperlink(new A.RgbColorModelHex { Val = "0000FF" }),
                new A.FollowedHyperlinkColor(new A.RgbColorModelHex { Val = "800080" }))
            { Name = "Office" };

            var fontScheme = new A.FontScheme(
                new A.MajorFont(
                    new A.LatinFont { Typeface = "Calibri" },
                    new A.EastAsianFont { Typeface = "" },
                    new A.ComplexScriptFont { Typeface = "" }),
                new A.MinorFont(
                    new A.LatinFont { Typeface = "Calibri" },
                    new A.EastAsianFont { Typeface = "" },
                    new A.ComplexScriptFont { Typeface = "" }))
            { Name = "Office" };

            var formatScheme = new A.FormatScheme(
                new A.FillStyleList(new A.NoFill(), new A.NoFill(), new A.NoFill()),
                new A.LineStyleList(
                    new A.Outline(new A.NoFill()),
                    new A.Outline(new A.NoFill()),
                    new A.Outline(new A.NoFill())),
                new A.EffectStyleList(
                    new A.EffectStyle(new A.EffectList()),
                    new A.EffectStyle(new A.EffectList()),
                    new A.EffectStyle(new A.EffectList())),
                new A.BackgroundFillStyleList(new A.NoFill(), new A.NoFill(), new A.NoFill()))
            { Name = "Office" };

            themePart.Theme = new A.Theme(new A.ThemeElements(colorScheme, fontScheme, formatScheme))
            {
                Name = "Sample Theme"
            };

            return themePart;
        }

        private static SlideMasterPart CreateSlideMasterPart(PresentationPart presentationPart, ThemePart themePart)
        {
            var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
            slideMasterPart.AddPart(themePart);

            slideMasterPart.SlideMaster = new SlideMaster(
                new CommonSlideData(EmptyShapeTree()),
                new ColorMap
                {
                    Background1 = A.ColorSchemeIndexValues.Light1,
                    Text1 = A.ColorSchemeIndexValues.Dark1,
                    Background2 = A.ColorSchemeIndexValues.Light2,
                    Text2 = A.ColorSchemeIndexValues.Dark2,
                    Accent1 = A.ColorSchemeIndexValues.Accent1,
                    Accent2 = A.ColorSchemeIndexValues.Accent2,
                    Accent3 = A.ColorSchemeIndexValues.Accent3,
                    Accent4 = A.ColorSchemeIndexValues.Accent4,
                    Accent5 = A.ColorSchemeIndexValues.Accent5,
                    Accent6 = A.ColorSchemeIndexValues.Accent6,
                    Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
                    FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink
                },
                new SlideLayoutIdList());

            return slideMasterPart;
        }

        private static SlideLayoutPart CreateSlideLayoutPart(SlideMasterPart slideMasterPart)
        {
            var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
            slideLayoutPart.SlideLayout = new SlideLayout(new CommonSlideData(EmptyShapeTree()))
            {
                Type = SlideLayoutValues.Blank
            };

            slideMasterPart.SlideMaster!.SlideLayoutIdList!.Append(new SlideLayoutId
            {
                Id = 2147483649U,
                RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart)
            });

            return slideLayoutPart;
        }

        private static SlidePart CreateSlidePart(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, string bodyText)
        {
            var slidePart = presentationPart.AddNewPart<SlidePart>();
            slidePart.AddPart(slideLayoutPart);

            var shapeTree = EmptyShapeTree();

            uint shapeId = 2;
            foreach (var line in DocxSampleGenerator.SplitLines(bodyText))
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
                        new A.Paragraph(new A.Run(new A.Text(line))))));
                shapeId++;
            }

            slidePart.Slide = new Slide(new CommonSlideData(shapeTree));
            return slidePart;
        }

        private static ShapeTree EmptyShapeTree() => new ShapeTree(
            new NonVisualGroupShapeProperties(
                new NonVisualDrawingProperties { Id = 1, Name = "" },
                new NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new A.TransformGroup()));
    }
}
