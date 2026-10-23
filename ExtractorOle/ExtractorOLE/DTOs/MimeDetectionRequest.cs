using System;
using System.IO;

namespace ExtractorOLE.DTOs
{
    // Input to mime detection. FileName travels through for logging/traceability
    // only -- detection MUST NOT use it to decide the result (content-only).
    //
    // The content comes from exactly one of FileBytes, InputStream or FilePath, with the same
    // rules and ownership semantics as ExtractionRequest (ADR-008): more than one source, or a
    // non-seekable/unreadable InputStream, is an ArgumentException. Leave FileBytes at its empty
    // default when using InputStream/FilePath.
    public class MimeDetectionRequest
    {
        public byte[] FileBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Seekable, readable input. The caller owns it: detection reads only headers/directory
        /// from position 0, may leave Position anywhere, and never disposes it.
        /// </summary>
        public Stream? InputStream { get; set; }

        /// <summary>Path of the input file; opened read-only with <see cref="FileShare.Read"/> and closed before detection returns.</summary>
        public string? FilePath { get; set; }

        public string FileName { get; set; } = string.Empty;
    }
}
