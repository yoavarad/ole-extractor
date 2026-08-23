using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
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
    /// least 3 first-layer embeddings of at least 2 kinds for docx/pptx.
    /// xlsx is verified structurally instead (see note below), since
    /// detection there depends on task T-02637847 (teaching the extractor to
    /// walk WorksheetPart), which is tracked separately and not yet merged.
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

            // 2 non-image embeddings live on the PresentationPart directly,
            // 2 image embeddings live on the slide (matching how
            // ExtractionHelper.ExtractFirstLayerEmbedded scans PowerPoint).
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("embedded_object_")));
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("slide_image_")));
        }

        [Fact]
        public void Xlsx_MultiEmbeddingSample_PlacesFourEmbeddingsOfTwoKindsOnWorksheetPart()
        {
            // NOTE: ExtractorOLE's current embedded-object scan for xlsx
            // (ExtractionHelper.ExtractFirstLayerEmbedded / the legacy
            // Old/Excel/ExcelExtractor.GetFirstLayerEmbedded) only walks
            // WorkbookPart, not WorksheetPart -- so it does not yet detect
            // these embeddings. That fix is tracked separately by task
            // T-02637847 and is not merged into this branch. This test
            // verifies the part of the contract SampleGenerator owns: the
            // embeddings are present, schema-valid OOXML parts, and placed
            // where the format requires (WorksheetPart, since WorkbookPart
            // cannot hold ImagePart/EmbeddedObjectPart directly).
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new XlsxSampleGenerator().Generate(spec);

            using var stream = new MemoryStream(generated.Content);
            using var document = SpreadsheetDocument.Open(stream, false);
            var worksheetPart = document.WorkbookPart!.WorksheetParts.Single();

            var embeddedParts = worksheetPart.Parts
                .Select(p => p.OpenXmlPart)
                .Where(p => p is EmbeddedObjectPart || p is ImagePart)
                .ToList();

            Assert.Equal(4, embeddedParts.Count);
            Assert.Equal(2, embeddedParts.OfType<ImagePart>().Count());
            Assert.Equal(2, embeddedParts.OfType<EmbeddedObjectPart>().Count());
        }
    }
}
