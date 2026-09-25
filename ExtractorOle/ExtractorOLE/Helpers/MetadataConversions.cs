using NPOI.HPSF;
using System;

namespace ExtractorOLE.Helpers
{
    // Shared property conversions so legacy (OLE SummaryInformation) and OOXML (core.xml/app.xml)
    // populate FileMetadata identically. Absent source values yield null, never 0 or "".
    public static class MetadataConversions
    {
        public static string? NullIfEmpty(string? value) =>
            string.IsNullOrEmpty(value) ? null : value;

        public static int? ParseInt(string? value) =>
            int.TryParse(value?.Trim(), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;

        // OLE EditTime is a FILETIME duration in 100 ns units; OOXML TotalTime is already minutes.
        public static int? EditTimeToMinutes(long editTime100Ns) =>
            editTime100Ns > 0 ? (int)(editTime100Ns / 600_000_000L) : null;

        public static DateTime? ToUtcOrNull(DateTime? value) =>
            value == null || value == default(DateTime) ? null : value.Value.ToUniversalTime();

        public static void Apply(NPOI.HPSF.SummaryInformation summary, ExtractorOLE.DTOs.FileMetadata metadata)
        {
            metadata.Title = summary.Title;
            metadata.Creator = summary.Author;
            metadata.Created = summary.CreateDateTime;
            metadata.Modified = summary.LastSaveDateTime;
            metadata.LastModifiedBy = summary.LastAuthor;
            metadata.Subject = NullIfEmpty(summary.Subject);
            metadata.Comments = NullIfEmpty(summary.Comments);
            metadata.RevisionNumber = ParseInt(summary.RevNumber);
            metadata.LastPrinted = ToUtcOrNull(summary.LastPrinted);
            metadata.EditingDurationMinutes = EditTimeToMinutes(summary.EditTime);
            metadata.Keywords = NullIfEmpty(summary.Keywords);
            metadata.ApplicationName = NullIfEmpty(summary.ApplicationName);
        }
    }
}
