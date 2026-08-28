using SampleGenerator.Abstractions;
using SampleGenerator.Generators;

namespace SampleGenerator.Fixtures
{
    /// <summary>
    /// The "docx with a real embedded xlsx object" scenario used to prove
    /// ExtractorOLE's generalized first-layer embedded walk (see
    /// ExtractionHelper.ExtractFirstLayerEmbedded) works for docx specifically:
    /// a genuine embedded xlsx package plus an inline PNG, and (in
    /// <see cref="BuildWithCorruptTarget"/>) a third embedding whose zip entry
    /// gets corrupted post-generation to exercise the "one bad embedding must
    /// not fail the whole extraction" path.
    ///
    /// The xlsx bytes are produced by <see cref="XlsxSampleGenerator"/> itself
    /// (not hand-rolled XML) so the embedded object is a genuinely valid xlsx
    /// package, matching this repo's synthetic-fixture conventions.
    /// </summary>
    public static class DocxEmbeddedXlsxSampleSpecs
    {
        public const string CorruptTargetContentType = "application/x-corrupt-target-test";

        // Fixed seed (not random) so the corrupt-target blob is deterministic
        // and reproducible across runs, matching this repo's fixture convention.
        private static readonly byte[] CorruptTargetBlob = BuildCorruptTargetBlob();

        private static byte[] BuildCorruptTargetBlob()
        {
            var buf = new byte[512];
            new Random(1337).NextBytes(buf);
            return buf;
        }

        private static byte[] BuildEmbeddedXlsxBytes() =>
            new XlsxSampleGenerator().Generate(new SampleSpec()).Content;

        /// <summary>Builds a spec with exactly 2 embeddings: a real embedded xlsx object and an inline PNG.</summary>
        public static SampleSpec Build() => new()
        {
            Embeddings = new List<EmbeddedContentSpec>
            {
                new()
                {
                    FileName = "embedded.xlsx",
                    Content = BuildEmbeddedXlsxBytes(),
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                },
                new()
                {
                    FileName = "image.png",
                    Content = MultiEmbeddingSampleSpecs.PngRedPixel,
                    ContentType = "image/png"
                }
            }
        };

        /// <summary>
        /// Builds the same 2 embeddings as <see cref="Build"/>, plus a 3rd
        /// embedding with a distinctive content type
        /// (<see cref="CorruptTargetContentType"/>) so a test can locate its
        /// zip entry by content type and corrupt it after generation,
        /// regardless of OpenXml's internal part-ordering.
        /// </summary>
        public static SampleSpec BuildWithCorruptTarget()
        {
            var spec = Build();
            spec.Embeddings.Add(new EmbeddedContentSpec
            {
                FileName = "corrupt-target.bin",
                Content = CorruptTargetBlob,
                ContentType = CorruptTargetContentType
            });
            return spec;
        }
    }
}
