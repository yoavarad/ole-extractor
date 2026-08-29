using System.Linq;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
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
    }
}
