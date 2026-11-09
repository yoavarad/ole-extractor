using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using ExtractorOLE.Abstractions;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Handlers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using System.IO.Compression;
using System.Text;
using Xunit;
using A = DocumentFormat.OpenXml.Drawing;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// #309: a flat-text failure surfaces as one FieldError at ExtractedText (empty text),
    /// while a genuinely empty body stays silent.
    /// </summary>
    public class MainExtractorTextFailureTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private static DocumentExtractionResult Extract(byte[] bytes, string fileName, string mime = DocxMime)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<MainExtractor>().Extract(
                new ExtractionRequest { FileBytes = bytes, FileName = fileName, DetectedMimeType = mime });
        }

        // Valid package whose document.xml is not well-formed XML: opens fine, body read throws.
        private static byte[] CorruptBodyDocx()
        {
            var good = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "hello" }).Content;
            using var ms = new MemoryStream();
            ms.Write(good);
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
            {
                zip.GetEntry("word/document.xml")!.Delete();
                using var w = zip.CreateEntry("word/document.xml").Open();
                w.Write(Encoding.UTF8.GetBytes("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>oops"));
            }
            return ms.ToArray();
        }

        [Fact]
        public void CorruptBodyDocx_EmptyTextAndOneExtractedTextFieldError()
        {
            var result = Extract(CorruptBodyDocx(), "corrupt-body-text.docx");

            Assert.Equal(string.Empty, result.ExtractedText);
            var error = Assert.Single(result.FieldErrors, e => e.FieldPath == "ExtractedText");
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
        }

        [Fact]
        public void ValidEmptyBodyDocx_NoFieldError()
        {
            var bytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = string.Empty }).Content;

            var result = Extract(bytes, "empty.docx");

            Assert.Equal(string.Empty, result.ExtractedText);
            Assert.Empty(result.FieldErrors);
        }

        private const string PptxMime = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // Package opens under the mime but its main part is another flavor: reported, not silent "".
        [Theory]
        [InlineData(XlsxMime, "docx")]
        [InlineData(PptxMime, "docx")]
        [InlineData(DocxMime, "xlsx")]
        public void WrongFlavorPackage_EmptyTextAndCorruptFileFieldError(string mime, string sampleKind)
        {
            byte[] bytes = sampleKind == "docx"
                ? new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "hello" }).Content
                : new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = "hello" }).Content;

            var result = Extract(bytes, "wrong-flavor." + sampleKind, mime);

            Assert.Equal(string.Empty, result.ExtractedText);
            var error = Assert.Single(result.FieldErrors, e => e.FieldPath == "ExtractedText");
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
        }

        // Three-slide deck (alpha, beta, gamma) whose second slide's XML is then replaced by
        // malformed XML: the package and the deck's slide list stay intact.
        private static byte[] PptxWithCorruptSecondSlide()
        {
            var generated = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "alpha" });
            using var ms = new MemoryStream();
            ms.Write(generated.Content);
            string secondSlideEntry;
            ms.Position = 0;
            using (var doc = PresentationDocument.Open(ms, true))
            {
                var presentationPart = doc.PresentationPart!;
                var first = presentationPart.SlideParts.Single();
                var layout = first.SlideLayoutPart!;

                var second = presentationPart.AddNewPart<SlidePart>();
                second.AddPart(layout);
                second.Slide = new Slide(new CommonSlideData(SlideTextTree("beta")));
                var third = presentationPart.AddNewPart<SlidePart>();
                third.AddPart(layout);
                third.Slide = new Slide(new CommonSlideData(SlideTextTree("gamma")));

                presentationPart.Presentation!.SlideIdList = new SlideIdList(
                    new SlideId { Id = 256U, RelationshipId = presentationPart.GetIdOfPart(first) },
                    new SlideId { Id = 257U, RelationshipId = presentationPart.GetIdOfPart(second) },
                    new SlideId { Id = 258U, RelationshipId = presentationPart.GetIdOfPart(third) });
                presentationPart.Presentation.Save();
                secondSlideEntry = second.Uri.ToString().TrimStart('/');
            }

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
            {
                zip.GetEntry(secondSlideEntry)!.Delete();
                using var w = zip.CreateEntry(secondSlideEntry).Open();
                w.Write(Encoding.UTF8.GetBytes("<p:sld xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\"><p:cSld><p:spTree>oops"));
            }
            return ms.ToArray();
        }

        private static ShapeTree SlideTextTree(string text)
        {
            var tree = new ShapeTree(
                new NonVisualGroupShapeProperties(
                    new NonVisualDrawingProperties { Id = 1, Name = "" },
                    new NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup()));
            tree.AppendChild(new Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties { Id = 2, Name = "TextBox 2" },
                    new NonVisualShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new ShapeProperties(),
                new TextBody(new A.BodyProperties(), new A.ListStyle(), new A.Paragraph(new A.Run(new A.Text(text))))));
            return tree;
        }

        [Fact]
        public void PptxWithCorruptMiddleSlide_KeepsOtherSlidesAndRecordsOneExtractedTextError()
        {
            var result = Extract(PptxWithCorruptSecondSlide(), "corrupt-slide.pptx", PptxMime);

            Assert.Contains("alpha", result.ExtractedText, StringComparison.Ordinal);
            Assert.Contains("gamma", result.ExtractedText, StringComparison.Ordinal);
            Assert.DoesNotContain("beta", result.ExtractedText, StringComparison.Ordinal);
            var error = Assert.Single(result.FieldErrors, e => e.FieldPath == "ExtractedText");
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
            Assert.Single(result.FieldErrors);
        }

        [Fact]
        public void PptxWithCorruptMiddleSlide_ByteExtractPathStillThrows()
        {
            Assert.ThrowsAny<Exception>(() => new PptxTextExtractor().ExtractText(PptxWithCorruptSecondSlide()));
        }

        // Legacy .ppt text reads the converted deck with the same per-slide isolation (#288).
        [Fact]
        public void PptConvertedDeckWithCorruptMiddleSlide_KeepsOtherSlidesAndReportsOnce()
        {
            var failures = new List<Exception>();

            var text = ((IParsedDocumentTextExtractor)new PptTextExtractor()).ExtractText(PptxWithCorruptSecondSlide(), failures.Add);

            Assert.Contains("alpha", text, StringComparison.Ordinal);
            Assert.Contains("gamma", text, StringComparison.Ordinal);
            Assert.DoesNotContain("beta", text, StringComparison.Ordinal);
            Assert.Single(failures);
        }

        // OutOfMemoryException is not isolatable: it propagates instead of becoming a FieldError.
        [Fact]
        public void TextExtractorOutOfMemory_Propagates()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var registry = new OomRegistry(provider.GetRequiredService<IFormatDispatchRegistry>());
            var extractor = new MainExtractor(provider.GetRequiredService<IExtractionHelper>(), registry);
            var request = new ExtractionRequest
            {
                FileBytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "hello" }).Content,
                FileName = "oom.docx",
                DetectedMimeType = DocxMime,
            };

            Assert.ThrowsAny<OutOfMemoryException>(() => extractor.Extract(request));
        }

        private sealed class OomRegistry : IFormatDispatchRegistry
        {
            private readonly IFormatDispatchRegistry _inner;

            public OomRegistry(IFormatDispatchRegistry inner) => _inner = inner;

            public IOpenStrategy? GetOpenStrategy(OfficeMimeType format) => _inner.GetOpenStrategy(format);

            public ITextExtractor? GetTextExtractor(OfficeMimeType format) => new OomExtractor();

            private sealed class OomExtractor : ITextExtractor
            {
                // An OutOfMemoryException (CA2201 reserves the base type).
                public string ExtractText(byte[] fileBytes) => throw new InsufficientMemoryException();
            }
        }
    }
}
