namespace ExtractorOLE.DTOs
{
    public class PowerPointFormatMetadata : FormatMetadata
    {
        public int SlideCount { get; set; }
        public int? NotesSlideCount { get; set; }
        public bool HasMacros { get; set; }
    }
}
