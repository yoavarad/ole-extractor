using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using System;

namespace ExtractorOLE
{
    public class MainExtractor
    {
        private readonly IExtractionHelper _helper;

        // Format dispatch is resolved entirely through this registry
        // (ydk:req:extraction/format-extensibility) - MainExtractor never
        // instantiates or references a specific IOpenStrategy/ITextExtractor
        // implementation, or its backing library, directly.
        private readonly IFormatDispatchRegistry _registry;

        public MainExtractor(IExtractionHelper helper, IFormatDispatchRegistry registry)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        private IOpenStrategy? FindOpenStrategyForMime(OfficeMimeTypeEnum mimeType)
        {
            return _registry.GetOpenStrategy(mimeType);
        }

        private static readonly OfficeMimeTypeEnum[] SupportedFormats =
        {
            OfficeMimeTypeEnum.Word, OfficeMimeTypeEnum.Excel, OfficeMimeTypeEnum.PowerPoint,
            OfficeMimeTypeEnum.WordLegacy, OfficeMimeTypeEnum.ExcelLegacy, OfficeMimeTypeEnum.PowerPointLegacy,
        };

        /// <summary>
        /// Extraction entry point ([ydk:contract:extraction/extract]). Trusts
        /// <see cref="ExtractionRequest.DetectedMimeType"/> as-is (no re-detection) and
        /// dispatches to the open + text-extraction components registered for that format
        /// in <see cref="IFormatDispatchRegistry"/>; contains no format-specific parsing.
        /// A null request or null required field throws <see cref="ArgumentNullException"/>
        /// first; a mime type that is not one of the six supported formats throws
        /// <see cref="Exceptions.UnsupportedFormatException"/>.
        /// </summary>
        public DocumentExtractionResult Extract(ExtractionRequest request)
        {
            ValidateRequest(request);

            var format = ResolveFormat(request.DetectedMimeType)
                         ?? throw new Exceptions.UnsupportedFormatException(request.DetectedMimeType);

            ParseTimeErrorGuard.Preflight(request.FileBytes, IsOoxml(format));
            var result = ParseTimeErrorGuard.OpenOrThrow(_registry.GetOpenStrategy(format), request.FileBytes, request.DetectedMimeType);
            result.MimeType = request.DetectedMimeType;

            var extractor = _registry.GetTextExtractor(format);
            if (extractor != null)
            {
                result.ExtractedText = extractor.ExtractText(request.FileBytes) ?? string.Empty;
            }

            return result;
        }

        // Boundary validation: runs before any detection/extraction logic.
        private static void ValidateRequest(ExtractionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.FileBytes == null) throw new ArgumentNullException(nameof(request.FileBytes));
            if (request.FileName == null) throw new ArgumentNullException(nameof(request.FileName));
            if (request.DetectedMimeType == null) throw new ArgumentNullException(nameof(request.DetectedMimeType));
        }

        private static bool IsOoxml(OfficeMimeTypeEnum format) =>
            format is OfficeMimeTypeEnum.Word or OfficeMimeTypeEnum.Excel or OfficeMimeTypeEnum.PowerPoint;

        // Reverse of IExtractionHelper.MimeFor over the six supported formats.
        private OfficeMimeTypeEnum? ResolveFormat(string? mime)
        {
            foreach (var format in SupportedFormats)
            {
                if (string.Equals(_helper.MimeFor(format), mime, StringComparison.OrdinalIgnoreCase)) return format;
            }
            return null;
        }

        public DocumentExtractionResult Extract(byte[] fileBytes)
        {
            var result = new DocumentExtractionResult();

            string? coreMime = null;
            // detect package type as enum
            var packageType = _helper.DetectMimeTypeFromBytes(fileBytes);

            // Try strategy based on detected package MIME first            
            var strategy = FindOpenStrategyForMime(packageType);
            if (strategy != null)
            {
                try
                {
                    var stratResult = strategy.Open(fileBytes);
                    if (stratResult != null)
                    {
                        if (!string.IsNullOrEmpty(stratResult.MimeType)) coreMime = stratResult.MimeType;
                        result = stratResult;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"failed running on found Mime type, switching to fallback. error: {e.Message}");
                    // strategy failed - fall back to trial open order below
                }
            }

            // Determine final mime: priority to coreMime, then package type, then magic
            OfficeMimeTypeEnum finalMimeType = OfficeMimeTypeEnum.OpenXmlUnknown;
            if (packageType != OfficeMimeTypeEnum.OpenXmlUnknown)
            {
                finalMimeType = packageType;

            }
            else if (!string.IsNullOrEmpty(coreMime))
            {
                finalMimeType = _helper.ParseOfficeMimeType(coreMime);
            }
            else
            {
                // fallback via magic bytes
                var magic = _helper.DetectMimeFromMagicBytes(fileBytes);
                finalMimeType = _helper.ParseOfficeMimeType(magic);
            }

            string finalMime = _helper.MimeFor(finalMimeType) ?? coreMime ?? "application/octet-stream";

            result.MimeType = finalMime;

            // Text extraction via registry (enum keyed)
            var extractor = FindHandlerForMime(finalMimeType);
            if (extractor != null)
            {
                try
                {
                    result.ExtractedText = extractor.ExtractText(fileBytes) ?? string.Empty;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Text extraction failed: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"No text extractor registered for MIME '{finalMime}'");
            }

            return result;
        }

        private ITextExtractor? FindHandlerForMime(OfficeMimeTypeEnum mimeType)
        {
            var exact = _registry.GetTextExtractor(mimeType);
            if (exact != null) return exact;

            // fallback: if unknown openxml package, prefer Word then Excel
            if (mimeType == OfficeMimeTypeEnum.OpenXmlUnknown)
            {
                var w = _registry.GetTextExtractor(OfficeMimeTypeEnum.Word);
                if (w != null) return w;
                var x = _registry.GetTextExtractor(OfficeMimeTypeEnum.Excel);
                if (x != null) return x;
                var y = _registry.GetTextExtractor(OfficeMimeTypeEnum.PowerPoint);
                if (y != null) return y;
            }

            return null;
        }

    }
}
