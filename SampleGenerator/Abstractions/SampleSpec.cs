namespace SampleGenerator.Abstractions
{
    /// <summary>
    /// Everything a single <see cref="ISampleGenerator"/> needs to author one
    /// sample file: body text, general metadata fields, and embeddings.
    /// Callers typically populate this from
    /// <see cref="SampleGenerator.Fixtures.MultilingualFixtures"/> so every
    /// format's generated content shares the same multilingual/emoji coverage.
    /// </summary>
    public class SampleSpec
    {
        public string BodyText { get; set; } = string.Empty;

        /// <summary>Keyed by <see cref="MetadataFields"/> constants.</summary>
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        public IList<EmbeddedContentSpec> Embeddings { get; set; } = new List<EmbeddedContentSpec>();
    }
}
