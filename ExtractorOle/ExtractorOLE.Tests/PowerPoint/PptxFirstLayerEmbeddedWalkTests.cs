using System;
using System.IO;
using System.Linq;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Covers T-c3f1ec05: pptx now goes through ExtractionHelper's generalized
    /// two-phase first-layer-embedded walk (no PowerPoint-specific branching).
    /// Pins the "no regression vs. the old PowerPoint-only walk" behavior and
    /// the corrupt-part-is-skipped-not-fatal contract, mirroring the docx/xlsx
    /// sibling tests (DocxEmbeddedXlsxAndCorruptionTests,
    /// MultiEmbeddingSampleTests.Xlsx_..._OneCorruptEmbedding_...) that use the
    /// same ZipEntryCorruptor-based technique.
    /// </summary>
    public class PptxFirstLayerEmbeddedWalkTests
    {
        private static GeneratedSample GenerateTwoEmbeddingSample()
        {
            var spec = new SampleSpec
            {
                BodyText = "hello world",
                Embeddings = new System.Collections.Generic.List<EmbeddedContentSpec>
                {
                    new() { FileName = "obj1.bin", Content = new byte[] { 1, 2, 3, 4 }, ContentType = "application/octet-stream" },
                    new() { FileName = "img1.png", Content = new byte[] { 5, 6, 7, 8, 9 }, ContentType = "image/png" }
                }
            };
            return new PptxSampleGenerator().Generate(spec);
        }

        [Fact]
        public void Pptx_EmbeddedObjectAndInlineMedia_BothAppearInSubfiles_MatchingPriorPowerPointOnlyBehavior()
        {
            var generated = GenerateTwoEmbeddingSample();
            var strategy = new PowerPointOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(2, result!.EmbeddedFiles.Count);
            Assert.Equal(1, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("embedded_object_")));
            Assert.Equal(1, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("slide_image_")));
            Assert.All(result.EmbeddedFiles, f => Assert.True(f.SizeInBytes > 0));
        }

        [Fact]
        public void Pptx_FirstLayerWalk_NeverIncludesInternalPackageXmlParts()
        {
            var generated = GenerateTwoEmbeddingSample();
            var strategy = new PowerPointOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(2, result!.EmbeddedFiles.Count);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.PackagePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Pptx_MultiEmbeddingSample_OneCorruptEmbedding_IsOmittedWithoutFailingExtraction()
        {
            // Three embeddings (2 good + 1 to-be-corrupted) all on the same
            // SlidePart, same as the real MultiEmbeddingSampleSpecs fixture
            // used elsewhere for pptx - this exercises the realistic "one bad
            // embedding among several good ones on the same slide" case.
            byte[] corruptContent = { 10, 11, 12, 13, 14, 15 };
            var spec = new SampleSpec
            {
                BodyText = "hello world",
                Embeddings = new System.Collections.Generic.List<EmbeddedContentSpec>
                {
                    new() { FileName = "good1.bin", Content = new byte[] { 1, 2, 3, 4 }, ContentType = "application/octet-stream" },
                    new() { FileName = "good2.png", Content = new byte[] { 5, 6, 7, 8, 9 }, ContentType = "image/png" },
                    new() { FileName = "corrupt.bin", Content = corruptContent, ContentType = "application/octet-stream" }
                }
            };
            var generated = new PptxSampleGenerator().Generate(spec);

            string corruptedPartUri;
            using (var pres = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(new MemoryStream(generated.Content), false))
            {
                var slidePart = pres.PresentationPart!.SlideParts.Single();

                // Identify the target by its distinctive content bytes rather than
                // ordinal position, so this doesn't depend on relationship/Parts
                // enumeration order matching spec list order.
                var targetPart = slidePart.Parts
                    .Select(p => p.OpenXmlPart)
                    .OfType<DocumentFormat.OpenXml.Packaging.EmbeddedObjectPart>()
                    .Single(p =>
                    {
                        using var s = p.GetStream();
                        using var buf = new MemoryStream();
                        s.CopyTo(buf);
                        return buf.ToArray().SequenceEqual(corruptContent);
                    });

                corruptedPartUri = targetPart.Uri.OriginalString;
            }

            var entryName = corruptedPartUri.TrimStart('/');
            var corruptedBytes = ZipEntryCorruptor.CorruptEntryData(generated.Content, entryName);

            var originalError = Console.Error;
            var capturedError = new StringWriter();
            Console.SetError(capturedError);

            ExtractorOLE.DTOs.DocumentExtractionResult? result;
            try
            {
                var strategy = new PowerPointOpenStrategy(new ExtractionHelper());
                result = strategy.Open(corruptedBytes);
            }
            finally
            {
                Console.SetError(originalError);
            }

            Assert.NotNull(result);
            Assert.Equal(2, result!.EmbeddedFiles.Count);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.PackagePath == corruptedPartUri);
            Assert.False(string.IsNullOrEmpty(capturedError.ToString()));
        }
    }
}
