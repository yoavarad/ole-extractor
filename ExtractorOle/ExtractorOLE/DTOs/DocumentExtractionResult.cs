using System;
using System.Collections.Generic;
using System.Text;

namespace ExtractorOLE.DTOs
{
    public class DocumentExtractionResult
    {
        // Requirement #2: The file MIME TYPE
        public string MimeType { get; set; } = string.Empty;

        // Requirement #1: The metadata of the file
        public FileMetadata Metadata { get; set; } = new();

        // Format-specific metadata (discriminated union - see FormatMetadata / ExcelFormatMetadata
        // in ExcelFormatMetadata.cs). Null when the open strategy for this format doesn't yet
        // populate it.
        public FormatMetadata? FormatMetadata { get; set; }

        // Requirement #3: The flat text in the file
        public string ExtractedText { get; set; } = string.Empty;

        // Requirement #4: A list of the first-layer embedded files
        public List<EmbeddedFileItem> EmbeddedFiles { get; set; } = new();
    }

    public class FileMetadata
    {
        public string? Title { get; set; }
        public string? Creator { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? Modified { get; set; }
        public string? LastModifiedBy { get; set; }
        public string? Subject { get; set; }
        public string? Comments { get; set; }
        public int? RevisionNumber { get; set; }
        public DateTime? LastPrinted { get; set; }
        public int? EditingDurationMinutes { get; set; }
        public string? Keywords { get; set; }
        public string? ApplicationName { get; set; }
    }

    public class EmbeddedFileItem
    {
        public byte[] BinaryData { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string PackagePath { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
    }
}
