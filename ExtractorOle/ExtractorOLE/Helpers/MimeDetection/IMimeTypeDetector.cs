using ExtractorOLE.DTOs;

namespace ExtractorOLE.Helpers.MimeDetection
{
    // Common shape shared by every structural mime-detection component
    // (OoxmlMimeDetector, CfbMimeDetector, ...). DetectMimeType dispatch depends
    // only on this interface via a DI-registered set (see
    // ExtractorOLE.Registry.IMimeDetectionRegistry), so adding a new format's
    // detection component never requires changing dispatch code
    // ([ydk:req:extraction/format-extensibility]).
    public interface IMimeTypeDetector
    {
        MimeDetectionResult Detect(MimeDetectionRequest request);
    }
}
