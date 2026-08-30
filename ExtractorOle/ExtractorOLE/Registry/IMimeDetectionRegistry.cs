using ExtractorOLE.Helpers.MimeDetection;
using System.Collections.Generic;

namespace ExtractorOLE.Registry
{
    /// <summary>
    /// DI-registered set of structural mime-detection components
    /// ([ydk:req:extraction/format-extensibility]). DetectMimeType dispatch
    /// (see ExtractionHelper.DetectMimeTypeFromBytes) tries each registered
    /// detector in order and returns Unknown only when none of them match -
    /// adding a new format's detection component is a matter of registering
    /// it here (see ServiceRegistration), not editing dispatch code.
    /// </summary>
    public interface IMimeDetectionRegistry
    {
        IReadOnlyList<IMimeTypeDetector> Detectors { get; }
    }
}
