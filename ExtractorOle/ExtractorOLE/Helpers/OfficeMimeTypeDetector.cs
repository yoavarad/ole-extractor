using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.Registry;
using System;

namespace ExtractorOLE.Helpers
{
    /// <summary>
    /// Whole-file MIME detection: orchestrates the DI-registered structural checks in
    /// <see cref="IMimeDetectionRegistry"/>, plus a magic-byte fallback.
    /// </summary>
    public interface IOfficeMimeTypeDetector
    {
        OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes);
        MimeDetectionResult DetectMimeType(MimeDetectionRequest request);
        string DetectMimeFromMagicBytes(byte[] bytes);
    }

    public class OfficeMimeTypeDetector : IOfficeMimeTypeDetector
    {
        private readonly IMimeDetectionRegistry _detectionRegistry;

        // Resolves its structural checks entirely through this DI-registered registry
        // ([ydk:req:extraction/dependency-injection]) - it never constructs a detection
        // component inline.
        public OfficeMimeTypeDetector(IMimeDetectionRegistry detectionRegistry)
        {
            _detectionRegistry = detectionRegistry ?? throw new ArgumentNullException(nameof(detectionRegistry));
        }

        // Dispatches through the DI-registered detection set (IMimeDetectionRegistry):
        // tries every registered structural check (OOXML, then CFB) in order and
        // returns Unknown only when none of them match
        // ([ydk:req:extraction/format-extensibility]).
        public OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException(nameof(fileBytes));
            }

            if (fileBytes.Length == 0)
            {
                return OfficeMimeTypeEnum.OpenXmlUnknown;
            }

            var request = new MimeDetectionRequest { FileBytes = fileBytes };

            foreach (var detector in _detectionRegistry.Detectors)
            {
                var result = detector.Detect(request);
                var mapped = MapDetectedFormat(result.DetectedFormat);
                if (mapped.HasValue)
                {
                    return mapped.Value;
                }
            }

            return OfficeMimeTypeEnum.OpenXmlUnknown;
        }

        // Facade over the DI-registered detection registry: tries every registered
        // structural check (OOXML, then CFB) in registered order and returns the
        // first non-Unknown result, or an Unknown result if none match. Pure
        // orchestration - no format-specific parsing logic here
        // ([ydk:req:extraction/format-extensibility]).
        public MimeDetectionResult DetectMimeType(MimeDetectionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.FileBytes == null)
            {
                throw new ArgumentNullException(nameof(request.FileBytes));
            }

            foreach (var detector in _detectionRegistry.Detectors)
            {
                var result = detector.Detect(request);
                if (result.DetectedFormat != DetectedFormatEnum.Unknown)
                {
                    return result;
                }
            }

            return new MimeDetectionResult
            {
                DetectedFormat = DetectedFormatEnum.Unknown,
                MimeType = "application/octet-stream",
                IsSupported = false,
            };
        }

        // OfficeMimeTypeEnum now has three legacy values (ExcelLegacy, PowerPointLegacy,
        // WordLegacy); Xls, Ppt, and Doc CFB detections are all wired to their legacy enum
        // values, and only Unknown still falls through to OpenXmlUnknown here.
        private static OfficeMimeTypeEnum? MapDetectedFormat(DetectedFormatEnum format) => format switch
        {
            DetectedFormatEnum.Docx => OfficeMimeTypeEnum.Word,
            DetectedFormatEnum.Xlsx => OfficeMimeTypeEnum.Excel,
            DetectedFormatEnum.Pptx => OfficeMimeTypeEnum.PowerPoint,
            DetectedFormatEnum.Xls => OfficeMimeTypeEnum.ExcelLegacy,
            DetectedFormatEnum.Ppt => OfficeMimeTypeEnum.PowerPointLegacy,
            DetectedFormatEnum.Doc => OfficeMimeTypeEnum.WordLegacy,
            _ => null,
        };

        public string DetectMimeFromMagicBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return "application/octet-stream";
            if (bytes.Length >= 4 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F') return "application/pdf";
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes.Length >= 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K' && (bytes[2] == 3 || bytes[2] == 5 || bytes[2] == 7)) return "application/zip";
            return "application/octet-stream";
        }
    }
}
