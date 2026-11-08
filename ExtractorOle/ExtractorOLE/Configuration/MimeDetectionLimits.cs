using System;

namespace ExtractorOLE.Configuration
{
    /// <summary>
    /// Extraction guardrail limits. Immutable after construction (init-only); every limit must be
    /// positive, so a host cannot disable a guard with 0 or -1 (which would wrap to ~2^64 in the
    /// unsigned comparisons).
    /// </summary>
    public class MimeDetectionLimits
    {
        private readonly long _maxFileSizeBytes = 104_857_600;
        private readonly long _maxDeclaredNestedContentBytes = 2_147_483_648;
        private readonly long _maxDeclaredEntryBytes = 1_073_741_824;
        private readonly long _maxZipEntryCount = 10_000;

        public long MaxFileSizeBytes { get => _maxFileSizeBytes; init => _maxFileSizeBytes = Positive(value, nameof(MaxFileSizeBytes)); }                                     // 100 MB, per ydk:nfr:extraction/max-file-size
        public long MaxDeclaredNestedContentBytes { get => _maxDeclaredNestedContentBytes; init => _maxDeclaredNestedContentBytes = Positive(value, nameof(MaxDeclaredNestedContentBytes)); } // 2 GB total per container, per ydk:nfr:extraction/nesting-depth-guard
        public long MaxDeclaredEntryBytes { get => _maxDeclaredEntryBytes; init => _maxDeclaredEntryBytes = Positive(value, nameof(MaxDeclaredEntryBytes)); }                 // 1 GB per entry/stream, per ydk:nfr:extraction/nesting-depth-guard
        public long MaxZipEntryCount { get => _maxZipEntryCount; init => _maxZipEntryCount = Positive(value, nameof(MaxZipEntryCount)); }                                      // EOCD entry-count cap, checked before any ZipArchive/Package open

        private static long Positive(long value, string name) =>
            value > 0 ? value : throw new ArgumentOutOfRangeException(name, value, "Extraction limit must be greater than zero.");
    }
}
