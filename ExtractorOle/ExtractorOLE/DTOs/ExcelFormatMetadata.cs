using System.Collections.Generic;

namespace ExtractorOLE.DTOs
{
    // Base of the format-specific metadata discriminated union on DocumentExtractionResult.
    // Concrete subtypes: ExcelFormatMetadata (this file); WordFormatMetadata /
    // PowerPointFormatMetadata are added by their own mapping tasks.
    public abstract class FormatMetadata
    {
    }

    public class ExcelFormatMetadata : FormatMetadata
    {
        public int SheetCount { get; set; }
        public List<string> SheetNames { get; set; } = new();
        public int? ActiveSheetIndex { get; set; }
        public bool HasMacros { get; set; }
    }
}
