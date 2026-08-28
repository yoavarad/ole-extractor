using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
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
    /// the corrupt-part-is-skipped-not-fatal contract.
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
        public void Pptx_CorruptEmbedding_IsOmittedAndLoggedWithoutFailingExtraction()
        {
            // Distinctive content for the object we will corrupt, so we can
            // confirm it's absent from the surviving items without relying on
            // ordinal position.
            byte[] corruptContent = { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };

            // Two good embeddings live on the SlidePart as usual.
            var generated = GenerateTwoEmbeddingSample();

            // The part-to-corrupt is added to a DIFFERENT Phase-2 container
            // (SlideMasterPart) than the two good embeddings (SlidePart).
            // OpenXmlPartContainer.Parts eagerly builds its whole relationship
            // map on first enumeration and throws if any relationship in that
            // SAME container is dangling - so a corrupt relationship takes
            // down its own container's parts as a unit. Isolating the corrupt
            // part in its own container is what lets the two good SlidePart
            // embeddings survive.
            string entryName;
            byte[] withThirdEmbedding;
            using (var ms = new MemoryStream())
            {
                ms.Write(generated.Content, 0, generated.Content.Length);
                ms.Position = 0;
                using (var pres = PresentationDocument.Open(ms, true))
                {
                    var slideMasterPart = pres.PresentationPart!.SlideMasterParts.Single();
                    var corruptPart = slideMasterPart.AddNewPart<EmbeddedObjectPart>("application/octet-stream");
                    entryName = corruptPart.Uri.ToString().TrimStart('/');
                    using var cs = new MemoryStream(corruptContent);
                    corruptPart.FeedData(cs);
                    pres.Save();
                }
                withThirdEmbedding = ms.ToArray();
            }

            // Corrupt that physical zip entry so the part can no longer be
            // resolved, using ZipArchive framework APIs only (no hand-rolled
            // OPC bytes). Deleting the entry leaves the relationship/content-type
            // declarations intact (dangling) while the physical entry is gone.
            // Confirmed empirically (against this build of DocumentFormat.OpenXml)
            // that this throws InvalidOperationException ("Part ... doesn't exist
            // in the package") from inside OpenXmlPartContainer.Parts' enumerator
            // (get_Parts()+MoveNext()) - i.e. at the `foreach (var partPair in
            // container.Parts)` statement itself in ExtractFirstLayerEmbedded,
            // not inside the loop body. That's caught by the per-container
            // try/catch around Phase 2's inner foreach and logged via
            // "Skipping unreadable parts under container '...'", not inside
            // ExtractPartData's GetStream()/CopyTo() (that catch guards parts
            // that DO resolve but fail to read).
            byte[] corrupted;
            using (var ms = new MemoryStream())
            {
                ms.Write(withThirdEmbedding, 0, withThirdEmbedding.Length);
                ms.Position = 0;
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
                {
                    var entry = archive.GetEntry(entryName);
                    Assert.NotNull(entry);
                    entry!.Delete();
                }
                corrupted = ms.ToArray();
            }

            var originalOut = Console.Out;
            var captured = new StringWriter();
            Console.SetOut(captured);
            ExtractorOLE.DTOs.DocumentExtractionResult? result;
            try
            {
                var strategy = new PowerPointOpenStrategy(new ExtractionHelper());
                result = strategy.Open(corrupted);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.NotNull(result);
            Assert.Equal(2, result!.EmbeddedFiles.Count);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.BinaryData.SequenceEqual(corruptContent));
            Assert.Contains("Skipping unreadable parts under container", captured.ToString());
        }
    }
}
