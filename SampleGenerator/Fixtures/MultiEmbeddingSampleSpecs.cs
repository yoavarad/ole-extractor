using SampleGenerator.Abstractions;

namespace SampleGenerator.Fixtures
{
    /// <summary>
    /// The "multi-embedding" synthetic sample scenario from
    /// docs/specs/dataset-curation.md's Corpus Composition rule #1: a file
    /// with at least 3 first-layer embedded objects/media of at least 2
    /// different kinds, plus multilingual/mixed-script text and emoji in the
    /// body text and at least 2 metadata fields.
    ///
    /// Embedding content, order, and count are fixed (not random) so the
    /// resulting sample's subfile count and filenames are deterministic and
    /// recordable in samples/manifest.json. Shared by SampleGenerator's
    /// Program.cs driver and ExtractorOLE.Tests so both generate/verify the
    /// exact same spec.
    /// </summary>
    public static class MultiEmbeddingSampleSpecs
    {
        // Two distinct 1x1 PNGs (different pixel data) so the two image
        // embeddings aren't byte-for-byte identical.
        public static readonly byte[] PngRedPixel = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        public static readonly byte[] PngGrayPixel = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        public static readonly byte[] BinaryBlobOne = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        public static readonly byte[] BinaryBlobTwo = { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x10, 0x20, 0x30 };

        /// <summary>Builds the shared multi-embedding <see cref="SampleSpec"/>: 4 embeddings across 2 kinds (image/png, application/octet-stream).</summary>
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
                new() { FileName = "diagram1.png", Content = PngRedPixel, ContentType = "image/png" },
                new() { FileName = "diagram2.png", Content = PngGrayPixel, ContentType = "image/png" },
                new() { FileName = "data1.bin", Content = BinaryBlobOne, ContentType = "application/octet-stream" },
                new() { FileName = "data2.bin", Content = BinaryBlobTwo, ContentType = "application/octet-stream" }
            }
        };
    }
}
