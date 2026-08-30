using ExtractorOLE.Helpers.MimeDetection;
using System;
using System.Collections.Generic;

namespace ExtractorOLE.Registry
{
    /// <summary>
    /// Default <see cref="IMimeDetectionRegistry"/> implementation: an ordered,
    /// immutable list of detection components built once at DI-registration
    /// time (see ServiceRegistration) and never mutated afterward, so it is
    /// safe to share as a singleton across concurrent detection calls.
    /// </summary>
    public sealed class MimeDetectionRegistry : IMimeDetectionRegistry
    {
        public IReadOnlyList<IMimeTypeDetector> Detectors { get; }

        public MimeDetectionRegistry(IReadOnlyList<IMimeTypeDetector> detectors)
        {
            Detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        }
    }
}
