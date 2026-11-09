using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.Guardrails;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using System;
using System.IO;
using System.Runtime.ExceptionServices;

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
            editTime100Ns > 0 && editTime100Ns / 600_000_000L <= int.MaxValue
                ? (int)(editTime100Ns / 600_000_000L)
                : null;

        public static DateTime? ToUtcOrNull(DateTime? value) =>
            value == null || value == default(DateTime) ? null : value.Value.ToUniversalTime();

        // [ydk:req:extraction/per-field-error-isolation]: each property is its own leaf at
        // "Metadata.<CSharpName>", so one unreadable source property costs only that property.
        public static void Apply(SummaryInformation summary, DocumentExtractionResult result)
        {
            ArgumentNullException.ThrowIfNull(summary);
            ArgumentNullException.ThrowIfNull(result);
            var m = result.Metadata;

            Populate(result, nameof(FileMetadata.Title), () => m.Title = summary.Title);
            Populate(result, nameof(FileMetadata.Creator), () => m.Creator = summary.Author);
            Populate(result, nameof(FileMetadata.Created), () => m.Created = summary.CreateDateTime);
            Populate(result, nameof(FileMetadata.Modified), () => m.Modified = summary.LastSaveDateTime);
            Populate(result, nameof(FileMetadata.LastModifiedBy), () => m.LastModifiedBy = summary.LastAuthor);
            Populate(result, nameof(FileMetadata.Subject), () => m.Subject = NullIfEmpty(summary.Subject));
            Populate(result, nameof(FileMetadata.Comments), () => m.Comments = NullIfEmpty(summary.Comments));
            Populate(result, nameof(FileMetadata.RevisionNumber), () => m.RevisionNumber = ParseInt(summary.RevNumber));
            Populate(result, nameof(FileMetadata.LastPrinted), () => m.LastPrinted = ToUtcOrNull(summary.LastPrinted));
            Populate(result, nameof(FileMetadata.EditingDurationMinutes), () => m.EditingDurationMinutes = EditTimeToMinutes(summary.EditTime));
            Populate(result, nameof(FileMetadata.Keywords), () => m.Keywords = NullIfEmpty(summary.Keywords));
            Populate(result, nameof(FileMetadata.ApplicationName), () => m.ApplicationName = NullIfEmpty(summary.ApplicationName));
        }

        // The FileMetadata leaves Apply populates from SummaryInformation, in Apply's order.
        private static readonly string[] SummaryLeaves =
        {
            nameof(FileMetadata.Title), nameof(FileMetadata.Creator), nameof(FileMetadata.Created),
            nameof(FileMetadata.Modified), nameof(FileMetadata.LastModifiedBy), nameof(FileMetadata.Subject),
            nameof(FileMetadata.Comments), nameof(FileMetadata.RevisionNumber), nameof(FileMetadata.LastPrinted),
            nameof(FileMetadata.EditingDurationMinutes), nameof(FileMetadata.Keywords), nameof(FileMetadata.ApplicationName),
        };

        // Reads the SummaryInformation property set under root and applies it (#288). An absent
        // stream means the document records no such metadata: the leaves stay null with no
        // FieldError. A present stream that does not parse as a summary property set costs every
        // leaf it feeds: each gets a CorruptFile FieldError rather than being skipped silently.
        internal static void ApplySummaryInformation(DirectoryEntry root, DocumentExtractionResult result)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(result);
            if (!root.HasEntry(SummaryInformation.DEFAULT_STREAM_NAME)) return;

            SummaryInformation summary;
            try
            {
                summary = PropertySetFactory.Create(root, SummaryInformation.DEFAULT_STREAM_NAME) as SummaryInformation
                    ?? throw new InvalidDataException("The SummaryInformation stream is not a summary property set.");
            }
            catch (Exception ex) when (ParseFailure.IsParseFailure(ex))
            {
                var unreadable = ex as InvalidDataException
                    ?? new InvalidDataException("The SummaryInformation stream is unreadable.", ex);
                foreach (var leaf in SummaryLeaves)
                {
                    FieldIsolation.TryPopulate(result, MetadataPath(leaf), () => ExceptionDispatchInfo.Throw(unreadable));
                }
                return;
            }

            Apply(summary, result);
        }

        // The field path of one FileMetadata property, e.g. "Metadata.Created".
        internal static string MetadataPath(string propertyName) =>
            $"{nameof(DocumentExtractionResult.Metadata)}.{propertyName}";

        // NPOI's typed HPSF getters unbox the stored value, so a property whose stored variant
        // type does not match its specified type (e.g. a VT_I4 where a VT_FILETIME belongs)
        // surfaces as InvalidCastException. That is a malformed source record, so it is
        // translated here, at the source read only, rather than widening FieldIsolation.Classify.
        private static void Populate(DocumentExtractionResult result, string propertyName, Action populate) =>
            FieldIsolation.TryPopulate(result, MetadataPath(propertyName), () =>
            {
                try
                {
                    populate();
                }
                catch (InvalidCastException ex)
                {
                    throw new InvalidDataException($"SummaryInformation property for {propertyName} has an unexpected type.", ex);
                }
            });
    }
}
