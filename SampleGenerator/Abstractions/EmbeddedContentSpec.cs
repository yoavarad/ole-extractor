namespace SampleGenerator.Abstractions
{
    /// <summary>
    /// One first-layer embedded object or inline media item to author into a
    /// generated sample (per docs/specs/dataset-curation.md's "3+ first-layer
    /// embedded objects/media of at least 2 different kinds" composition rule).
    /// </summary>
    public class EmbeddedContentSpec
    {
        public string FileName { get; set; } = string.Empty;

        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// MIME content type of <see cref="Content"/>, e.g. "image/png".
        /// Generators use this to decide how to embed the content
        /// (as inline media vs. an opaque embedded object).
        /// </summary>
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
