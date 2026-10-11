using System;
using System.IO;

namespace ExtractorOLE.DTOs
{
    // Input to Extract. DetectedMimeType is the caller-supplied result of a prior
    // DetectMimeType call; Extract trusts it as-is and never re-detects
    // ([ydk:contract:extraction/extract]). FileName travels through for
    // logging/traceability only.
    //
    // The file content comes from exactly one of FileBytes, InputStream or FilePath (ADR-008).
    // Setting InputStream or FilePath alongside a non-empty FileBytes, or both together, is an
    // ArgumentException. Leave FileBytes at its empty default when using InputStream/FilePath: a
    // null FileBytes is still an ArgumentNullException, as before.
    public class ExtractionRequest
    {
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Seekable, readable input (ADR-008). The caller owns it: Extract reads it from position 0,
        /// may leave Position anywhere, and never disposes it. Keep it open until Extract returns and
        /// do not share one stream between concurrent requests. A non-seekable or unreadable stream is
        /// rejected with <see cref="ArgumentException"/>; spool such input to a temp file and pass
        /// <see cref="FilePath"/> instead.
        /// </summary>
        public Stream? InputStream { get; set; }

        /// <summary>
        /// Path of the input file (ADR-008). Extract opens it read-only with
        /// <see cref="FileShare.Read"/> and closes it before returning.
        /// </summary>
        public string? FilePath { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string DetectedMimeType { get; set; } = string.Empty;
    }
}
