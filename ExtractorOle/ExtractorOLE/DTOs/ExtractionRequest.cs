using System;

namespace ExtractorOLE.DTOs
{
    // Input to Extract. DetectedMimeType is the caller-supplied result of a prior
    // DetectMimeType call; Extract trusts it as-is and never re-detects
    // ([ydk:contract:extraction/extract]). FileName travels through for
    // logging/traceability only.
    public class ExtractionRequest
    {
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string DetectedMimeType { get; set; } = string.Empty;
    }
}
