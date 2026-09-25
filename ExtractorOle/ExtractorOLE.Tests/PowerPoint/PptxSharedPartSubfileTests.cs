using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using Xunit;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// T-205594d7: a subfile is a distinct package part ([ydk:req:extraction/subfile-scope] speaks
    /// of embeddings/ and media parts, not relationships), so a part related from several hosts is
    /// listed exactly once, and media hosted deeper than a slide (slide -> vmlDrawing -> image, the
    /// legacy OLE-preview layout real decks use) is still found.
    /// </summary>
    public class PptxSharedPartSubfileTests
    {
        private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 1, 2, 3, 4 };

        private static DocumentExtractionResult Extract(byte[] pptx)
        {
            using var doc = PresentationDocument.Open(new MemoryStream(pptx), false);
            var result = new DocumentExtractionResult();
            new ExtractionHelper().ExtractFirstLayerEmbedded(result, doc.PresentationPart!);
            return result;
        }

        private static ImagePart NewImage(SlidePart host) => Fill(host.AddImagePart(ImagePartType.Png));

        private static ImagePart NewImage(VmlDrawingPart host) => Fill(host.AddImagePart(ImagePartType.Png));

        private static ImagePart Fill(ImagePart image)
        {
            using var s = image.GetStream();
            s.Write(PngBytes);
            return image;
        }

        private static byte[] BuildDeck(Action<PresentationPart> build)
        {
            var ms = new MemoryStream();
            using (var doc = PresentationDocument.Create(ms, DocumentFormat.OpenXml.PresentationDocumentType.Presentation))
            {
                var presentationPart = doc.AddPresentationPart();
                presentationPart.Presentation = new DocumentFormat.OpenXml.Presentation.Presentation();
                build(presentationPart);
            }
            return ms.ToArray();
        }

        [Fact]
        public void Pptx_ImagePartSharedByTwoSlides_IsListedOnce()
        {
            var deck = BuildDeck(p =>
            {
                var slide1 = p.AddNewPart<SlidePart>();
                var slide2 = p.AddNewPart<SlidePart>();
                var image = NewImage(slide1);
                slide2.AddPart(image);
            });

            var result = Extract(deck);

            var only = Assert.Single(result.EmbeddedFiles);
            Assert.EndsWith(".png", only.FileName);
            Assert.Equal(PngBytes.Length, only.SizeInBytes);
        }

        [Fact]
        public void Pptx_DistinctImagePartsOnTwoSlides_AreEachListed()
        {
            var deck = BuildDeck(p =>
            {
                NewImage(p.AddNewPart<SlidePart>());
                NewImage(p.AddNewPart<SlidePart>());
            });

            Assert.Equal(2, Extract(deck).EmbeddedFiles.Count);
        }

        [Fact]
        public void Pptx_ImageHostedBySlideVmlDrawing_IsListedOnceEvenWhenAlsoOnASlide()
        {
            var deck = BuildDeck(p =>
            {
                var slide = p.AddNewPart<SlidePart>();
                var vml = slide.AddNewPart<VmlDrawingPart>();
                var shared = NewImage(vml);
                slide.AddPart(shared);          // same part reachable at two depths
                NewImage(vml);                  // plus one only reachable through the vmlDrawing
            });

            var result = Extract(deck);

            Assert.Equal(2, result.EmbeddedFiles.Count);
            Assert.Equal(2, result.EmbeddedFiles.Select(f => f.PackagePath).Distinct().Count());
        }
    }
}
