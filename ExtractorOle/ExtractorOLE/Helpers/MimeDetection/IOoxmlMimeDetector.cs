using ExtractorOLE.DTOs;

namespace ExtractorOLE.Helpers.MimeDetection
{
    // Sibling of the legacy CFB check (see ICfbMimeDetector): distinguishes
    // Word/Excel/PowerPoint OOXML packages by declared part-name structure,
    // without a full document parse.
    public interface IOoxmlMimeDetector
    {
        MimeDetectionResult Detect(MimeDetectionRequest request);
    }
}
