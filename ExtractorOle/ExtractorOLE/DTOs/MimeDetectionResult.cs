namespace ExtractorOLE.DTOs
{
    public class MimeDetectionResult
    {
        public DetectedFormat DetectedFormat { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public bool IsSupported { get; set; }
    }
}
