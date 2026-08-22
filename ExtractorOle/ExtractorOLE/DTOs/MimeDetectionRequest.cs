using System;

namespace ExtractorOLE.DTOs
{
    // Input to mime detection. FileName travels through for logging/traceability
    // only -- detection MUST NOT use it to decide the result (content-only).
    public class MimeDetectionRequest
    {
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
    }
}
