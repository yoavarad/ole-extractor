namespace ExtractorOLE.Configuration
{
    public class MimeDetectionLimits
    {
        public long MaxFileSizeBytes { get; set; } = 104_857_600;               // 100 MB, per ydk:nfr:extraction/max-file-size
        public long MaxDeclaredNestedContentBytes { get; set; } = 2_147_483_648; // 2 GB, per ydk:nfr:extraction/nesting-depth-guard
    }
}
