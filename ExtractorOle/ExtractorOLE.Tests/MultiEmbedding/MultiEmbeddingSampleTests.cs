using System.IO;
using System.Linq;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.MultiEmbedding
{
    /// <summary>
    /// Round-trip tests for the "multi-embedding" synthetic sample scenario
    /// (docs/specs/dataset-curation.md Corpus Composition rule #1): generate
    /// via SampleGenerator, then confirm ExtractorOLE actually detects at
    /// least 3 first-layer embeddings of at least 2 kinds for docx/xlsx/pptx.
    /// </summary>
    public class MultiEmbeddingSampleTests
    {
        [Fact]
        public void Docx_MultiEmbeddingSample_DetectsAtLeastThreeEmbeddingsAcrossTwoKinds()
        {
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new DocxSampleGenerator().Generate(spec);

            var strategy = new WordOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(4, result!.EmbeddedFiles.Count);

            var extensions = result.EmbeddedFiles.Select(f => Path.GetExtension(f.FileName)).Distinct().ToList();
            Assert.True(extensions.Count >= 2, $"Expected >=2 distinct kinds, found: {string.Join(",", extensions)}");
            Assert.Contains(".png", extensions);
            Assert.Contains(".bin", extensions);
            Assert.All(result.EmbeddedFiles, f => Assert.True(f.SizeInBytes > 0));
        }

        [Fact]
        public void Pptx_MultiEmbeddingSample_DetectsAtLeastThreeEmbeddingsAcrossTwoKinds()
        {
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new PptxSampleGenerator().Generate(spec);

            var strategy = new PowerPointOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(4, result!.EmbeddedFiles.Count);

            var extensions = result.EmbeddedFiles.Select(f => Path.GetExtension(f.FileName)).Distinct().ToList();
            Assert.True(extensions.Count >= 2, $"Expected >=2 distinct kinds, found: {string.Join(",", extensions)}");
            Assert.Contains(".png", extensions);
            Assert.Contains(".bin", extensions);

            // All 4 embeddings live on the SlidePart (PresentationPart cannot hold
            // EmbeddedObjectPart/EmbeddedPackagePart/ImagePart directly, per
            // ExtractionHelper.ExtractFirstLayerEmbedded's PowerPoint scan).
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("embedded_object_")));
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("slide_image_")));
        }

        [Fact]
        public void Xlsx_MultiEmbeddingSample_DetectsAtLeastThreeEmbeddingsAcrossTwoKinds()
        {
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new XlsxSampleGenerator().Generate(spec);

            var strategy = new ExcelOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(4, result!.EmbeddedFiles.Count);

            var extensions = result.EmbeddedFiles.Select(f => Path.GetExtension(f.FileName)).Distinct().ToList();
            Assert.True(extensions.Count >= 2, $"Expected >=2 distinct kinds, found: {string.Join(",", extensions)}");
            Assert.Contains(".png", extensions);
            Assert.Contains(".bin", extensions);

            // All 4 embeddings live on the WorksheetPart (WorkbookPart cannot hold
            // EmbeddedObjectPart/EmbeddedPackagePart/ImagePart directly, per
            // ExtractionHelper.ExtractFirstLayerEmbedded's generic depth-2 scan).
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("embedded_object_")));
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("slide_image_")));
        }
    }
}
