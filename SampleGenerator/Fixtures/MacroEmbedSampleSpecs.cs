using SampleGenerator.Abstractions;

namespace SampleGenerator.Fixtures
{
    /// <summary>
    /// The "macro-enabled-with-embedding" synthetic sample scenario from
    /// docs/specs/dataset-curation.md's Corpus Composition rule #1: a
    /// macro-enabled variant (docm/xlsm/pptm, or legacy doc/xls/ppt with a
    /// macro storage) carrying at least one macro/VBA storage and at least
    /// one first-layer embedding, plus multilingual/mixed-script text and
    /// emoji in the body text and at least 2 metadata fields.
    ///
    /// This is also the fixture Epic 5/6's macro-variant-misclassification
    /// test scenario exercises: docm/xlsm/pptm must be detected as their base
    /// format (docx/xlsx/pptx), not as a separate format.
    ///
    /// The VBA project content is arbitrary placeholder bytes - callers only
    /// need a structurally-valid macro storage (a <c>VbaProjectPart</c> with
    /// the right content type) present, not a functioning macro. Shared by
    /// SampleGenerator's Program.cs driver and ExtractorOLE.Tests so both
    /// generate/verify the exact same spec.
    /// </summary>
    public static class MacroEmbedSampleSpecs
    {
        public static readonly byte[] VbaProjectPlaceholder = System.Text.Encoding.ASCII.GetBytes(
            "PLACEHOLDER-VBA-PROJECT-STRUCTURAL-ONLY-NOT-A-FUNCTIONING-MACRO");

        public static readonly byte[] PngPixel = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        /// <summary>Builds the shared macro-enabled-with-embedding <see cref="SampleSpec"/>: one macro/VBA storage plus one first-layer embedding (image/png).</summary>
        public static SampleSpec Build() => new()
        {
            BodyText = MultilingualFixtures.ComposeMixedBody(),
            Metadata = new Dictionary<string, string>
            {
                [MetadataFields.Title] = MultilingualFixtures.Hebrew,
                [MetadataFields.Author] = MultilingualFixtures.Arabic,
                [MetadataFields.Subject] = MultilingualFixtures.Cjk,
                [MetadataFields.Comments] = string.Join(" ", MultilingualFixtures.AllEmoji),
                [MetadataFields.LastModifiedBy] = MultilingualFixtures.Russian
            },
            Embeddings = new List<EmbeddedContentSpec>
            {
                new() { FileName = "pixel.png", Content = PngPixel, ContentType = "image/png" }
            },
            VbaProject = VbaProjectPlaceholder
        };
    }
}
