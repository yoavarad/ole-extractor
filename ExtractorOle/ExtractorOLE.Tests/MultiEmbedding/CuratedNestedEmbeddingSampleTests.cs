using System.IO;
using System.Linq;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.MultiEmbedding
{
    /// <summary>
    /// Proves the "subfile that is itself a container" rule from
    /// [ydk:req:extraction/subfile-scope] (docs/specs/extraction.md's Subfiles
    /// section) against the corpus's 2+-level nested-embedding curated samples
    /// (SampleGenerator.Fixtures.CuratedSampleSpecs, recorded in
    /// samples/manifest.json): the outer embedded package appears as exactly
    /// one opaque first-layer subfile, and the inner embedding nested inside
    /// it (a PNG chart, distinctly named so it would be trivially detectable
    /// if surfaced) is never separately listed - no recursive unpacking.
    /// </summary>
    public class CuratedNestedEmbeddingSampleTests
    {
        [Fact]
        public void Docx_MeetingMinutesBoardroom_NestedXlsxEmbedding_IsOneOpaqueSubfile_InnerPngNotUnpacked()
        {
            var spec = CuratedSampleSpecs.BuildMeetingMinutesBoardroom();
            var innerXlsxBytes = spec.Embeddings.Single(e => e.FileName == "budget-snippet.xlsx").Content;

            var generated = new DocxSampleGenerator().Generate(spec);

            var strategy = new WordOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            var subfile = Assert.Single(result!.EmbeddedFiles);

            Assert.EndsWith(".xlsx", subfile.FileName);
            Assert.Equal(innerXlsxBytes, subfile.BinaryData);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName.EndsWith(".png"));
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName == "budget-chart.png");
        }

        [Fact]
        public void Pptx_ProductLaunchDeck_NestedXlsxEmbedding_IsOneOpaqueSubfile_InnerPngNotUnpacked()
        {
            var spec = CuratedSampleSpecs.BuildProductLaunchDeck();
            var innerXlsxBytes = spec.Embeddings.Single(e => e.FileName == "sales-projections.xlsx").Content;

            var generated = new PptxSampleGenerator().Generate(spec);

            var strategy = new PowerPointOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            var subfile = Assert.Single(result!.EmbeddedFiles);

            Assert.EndsWith(".xlsx", subfile.FileName);
            Assert.Equal(innerXlsxBytes, subfile.BinaryData);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName.EndsWith(".png"));
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName == "sales-chart.png");
        }

        [Fact]
        public void Doc_ProjectProposalBudget_NestedXlsEmbedding_IsOneOpaqueSubfile_InnerPngNotUnpacked()
        {
            var spec = CuratedSampleSpecs.BuildProjectProposalBudget();
            var innerXlsBytes = spec.Embeddings.Single(e => e.FileName == "pilot-budget.xls").Content;

            var generated = new DocSampleGenerator().Generate(spec);

            var strategy = new DocOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            var subfile = Assert.Single(result!.EmbeddedFiles);

            // The legacy subfile is the whole ObjectPool storage; its Ole10Native stream holds the inner .xls.
            var fs = new NPOIFSFileSystem(new MemoryStream(subfile.BinaryData));
            try
            {
                var stream = fs.CreateDocumentInputStream("\u0001Ole10Native");
                var content = new byte[stream.Length];
                stream.Read(content, 0, content.Length);
                Assert.Equal(innerXlsBytes, content);
            }
            finally
            {
                fs.Close();
            }

            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName.EndsWith(".png"));
        }

        [Fact]
        public void Xls_InventoryForecast_NestedXlsxEmbedding_IsOneOpaqueSubfile_InnerPngNotUnpacked()
        {
            var spec = CuratedSampleSpecs.BuildInventoryForecast();
            var innerXlsxBytes = spec.Embeddings.Single(e => e.FileName == "reorder-thresholds.xlsx").Content;

            var generated = new XlsSampleGenerator().Generate(spec);

            var strategy = new XlsOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);

            // xls reports the embedded OLE package as one opaque object; the generator's shared 1x1 icon
            // picture (70 bytes) accompanies it. The inner xlsx's own PNG chart is never surfaced.
            var subfile = Assert.Single(result!.EmbeddedFiles, f => f.FileName.EndsWith(".bin"));
            var fs = new NPOIFSFileSystem(new MemoryStream(subfile.BinaryData));
            try
            {
                var stream = fs.CreateDocumentInputStream("Ole10Native");
                var content = new byte[stream.Length];
                stream.Read(content, 0, content.Length);
                // NPOI wraps the payload in an Ole10Native header, so match the inner xlsx bytes within the stream.
                Assert.True(content.AsSpan().IndexOf(innerXlsxBytes) >= 0);
            }
            finally
            {
                fs.Close();
            }

            Assert.Equal(2, result.EmbeddedFiles.Count);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName.EndsWith(".xlsx"));
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName == "threshold-chart.png");
        }

        [Fact]
        public void Ppt_QuarterlyReviewDeck_NestedXlsEmbedding_IsOneOpaqueSubfile_InnerPngNotUnpacked()
        {
            var spec = CuratedSampleSpecs.BuildQuarterlyReviewDeck();
            var innerXlsBytes = spec.Embeddings.Single(e => e.FileName == "regional-kpis.xls").Content;

            var generated = new PptSampleGenerator().Generate(spec);

            var strategy = new PptOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            var subfile = Assert.Single(result!.EmbeddedFiles);

            // The legacy subfile is the whole ObjectPool-style storage; its Ole10Native stream holds the inner .xls.
            var fs = new NPOIFSFileSystem(new MemoryStream(subfile.BinaryData));
            try
            {
                var stream = fs.CreateDocumentInputStream("Ole10Native");
                var content = new byte[stream.Length];
                stream.Read(content, 0, content.Length);
                Assert.Equal(innerXlsBytes, content);
            }
            finally
            {
                fs.Close();
            }

            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName.EndsWith(".png"));
        }
    }
}
