namespace ExtractorOLE.Configuration
{
    public class MimeDetectionLimits
    {
        public long MaxFileSizeBytes { get; set; } = 104_857_600;               // 100 MB, per ydk:nfr:extraction/max-file-size
        public long MaxDeclaredNestedContentBytes { get; set; } = 2_147_483_648; // 2 GB total per container, per ydk:nfr:extraction/nesting-depth-guard
        public long MaxDeclaredEntryBytes { get; set; } = 1_073_741_824;         // 1 GB per entry/stream, per ydk:nfr:extraction/nesting-depth-guard
    }
}
