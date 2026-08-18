using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;

namespace ExtractorOLE
{
    public class MainExtractor
    {
        private readonly Dictionary<OfficeMimeTypeEnum, ITextExtractor> _registry = new();

        // Strategy map injected (enum-keyed)
        private readonly IDictionary<OfficeMimeTypeEnum, IOpenStrategy> _openStrategies;

        private readonly IExtractionHelper _helper;

        public MainExtractor(IExtractionHelper helper, IDictionary<OfficeMimeTypeEnum, IOpenStrategy> openStrategies)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _openStrategies = openStrategies ?? new Dictionary<OfficeMimeTypeEnum, IOpenStrategy>();
            RegisterBuiltInHandlers();
        }

        public void Register(OfficeMimeTypeEnum mimeType, ITextExtractor handler)
        {
            if (handler == null) return;
            _registry[mimeType] = handler;
        }

        private void RegisterBuiltInHandlers()
        {
            Register(OfficeMimeTypeEnum.Word, new Handlers.DocxTextExtractor());
            Register(OfficeMimeTypeEnum.Excel, new Handlers.XlsxTextExtractor());
            Register(OfficeMimeTypeEnum.PowerPoint, new Handlers.PptxTextExtractor());
        }

        private IOpenStrategy? FindOpenStrategyForMime(OfficeMimeTypeEnum mimeType)
        {
            if (_openStrategies != null && _openStrategies.TryGetValue(mimeType, out var strat)) return strat;
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
            OfficeMimeTypeEnum finalMimeType = OfficeMimeTypeEnum.OpenXmlUnkown;
            if (packageType != OfficeMimeTypeEnum.OpenXmlUnkown)
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
            if (_registry.TryGetValue(mimeType, out var exact)) return exact;

            // fallback: if unknown openxml package, prefer Word then Excel
            if (mimeType == OfficeMimeTypeEnum.OpenXmlUnkown)
            {
                if (_registry.TryGetValue(OfficeMimeTypeEnum.Word, out var w)) return w;
                if (_registry.TryGetValue(OfficeMimeTypeEnum.Excel, out var x)) return x;
                if (_registry.TryGetValue(OfficeMimeTypeEnum.PowerPoint, out var y)) return y;
            }

            return null;
        }

    }
}
