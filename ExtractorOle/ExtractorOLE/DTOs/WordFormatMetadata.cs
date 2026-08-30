namespace ExtractorOLE.DTOs
{
    // Word-specific metadata (doc and docx), sourced from OOXML Extended
    // (app.xml) Properties for docx/docm. Fields absent from the source
    // format's property store are null, per ydk:entity:extraction/WordFormatMetadata.
    public class WordFormatMetadata : FormatMetadata
    {
        public int? PageCount { get; set; }
        public int? WordCount { get; set; }
        public int? CharacterCount { get; set; }
        public int? ParagraphCount { get; set; }
        public int? LineCount { get; set; }
        public string? Company { get; set; }
        public string? Manager { get; set; }
        public string? Template { get; set; }
        public bool HasMacros { get; set; }
    }
}
