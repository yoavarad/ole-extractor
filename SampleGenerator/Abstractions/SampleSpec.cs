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

        /// <summary>
        /// Raw VBA project binary to author as a macro storage (e.g. a
        /// <c>VbaProjectPart</c> for OOXML formats). Content can be arbitrary
        /// placeholder bytes - callers only need a structurally-valid macro
        /// storage present, not a functioning macro. Null means "no macro" -
        /// generators author the plain (non-macro-enabled) document type.
        /// </summary>
        public byte[]? VbaProject { get; set; }
    }
}
