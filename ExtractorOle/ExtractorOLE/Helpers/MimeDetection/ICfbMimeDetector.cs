using ExtractorOLE.DTOs;

namespace ExtractorOLE.Helpers.MimeDetection
{
    // Sibling of the OOXML/ZIP-based check (see IExtractionHelper.DetectMimeTypeFromBytes):
    // distinguishes the legacy CFB/compound-file family (doc/xls/ppt) by root
    // storage/stream names only, without a full document parse.
    public interface ICfbMimeDetector
    {
        MimeDetectionResult Detect(MimeDetectionRequest request);
    }
}
