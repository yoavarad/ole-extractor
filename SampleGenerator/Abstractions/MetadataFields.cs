namespace SampleGenerator.Abstractions
{
    /// <summary>
    /// Well-known keys for <see cref="SampleSpec.Metadata"/>, matching the
    /// general metadata fields in docs/specs/extraction.md's FileMetadata.
    /// </summary>
    public static class MetadataFields
    {
        public const string Title = "Title";
        public const string Author = "Author";
        public const string Subject = "Subject";
        public const string Keywords = "Keywords";
        public const string Comments = "Comments";
        public const string LastModifiedBy = "LastModifiedBy";

        /// <summary>ISO-8601 UTC datetime string, e.g. "2026-02-03T00:00:00Z".</summary>
        public const string Created = "Created";

        /// <summary>ISO-8601 UTC datetime string, e.g. "2026-03-19T00:00:00Z".</summary>
        public const string Modified = "Modified";

        /// <summary>OOXML core.xml cp:revision - a string, not an int (e.g. "14").</summary>
        public const string RevisionNumber = "RevisionNumber";
    }
}
