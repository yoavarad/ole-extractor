using ExtractorOLE.Abstractions;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using ExtractorOLE.DTOs;
using ExtractorOLE.Handlers;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.Registry;

namespace ExtractorOLE
{
    public static class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            // Core helpers: mime mapping, mime detection and embedded-file extraction,
            // with IExtractionHelper as a thin facade over the three.
            services.AddSingleton<IMimeTypeMapper, MimeTypeMapper>();
            services.AddSingleton<IOfficeMimeTypeDetector, OfficeMimeTypeDetector>();
            services.AddSingleton<IEmbeddedFileExtractor, EmbeddedFileExtractor>();
            services.AddSingleton<IExtractionHelper>(sp => new ExtractionHelper(
                sp.GetRequiredService<IMimeTypeMapper>(),
                sp.GetRequiredService<IOfficeMimeTypeDetector>(),
                sp.GetRequiredService<IEmbeddedFileExtractor>()));

            // Pre-parse Extract guardrails, sharing the one configured limits instance
            // with mime detection (ydk:nfr:extraction/max-file-size, nesting-depth-guard).
            services.AddSingleton(sp => new ExtractionGuardrails(sp.GetRequiredService<MimeDetectionLimits>()));

            // Mime detection (legacy CFB family)
            services.AddSingleton(new MimeDetectionLimits());
            services.AddSingleton<ICfbMimeDetector, CfbMimeDetector>();

            // Mime detection (OOXML family)
            services.AddSingleton<IOoxmlMimeDetector, OoxmlMimeDetector>();

            // Mime detection registry (ydk:req:extraction/format-extensibility,
            // ydk:req:extraction/dependency-injection): DetectMimeType dispatch
            // (OfficeMimeTypeDetector.DetectMimeTypeFromBytes) tries each of these
            // structural checks in order via this DI-registered seam, instead of
            // constructing a detector inline in orchestration code.
            services.AddSingleton<IMimeDetectionRegistry>(sp => new MimeDetectionRegistry(new IMimeTypeDetector[]
            {
                (IMimeTypeDetector)sp.GetRequiredService<IOoxmlMimeDetector>(),
                (IMimeTypeDetector)sp.GetRequiredService<ICfbMimeDetector>(),
            }));

            // Open strategies
            services.AddSingleton<WordOpenStrategy>();
            services.AddSingleton<ExcelOpenStrategy>();
            services.AddSingleton<PowerPointOpenStrategy>();
            services.AddSingleton<ZipFallbackOpenStrategy>();
            services.AddSingleton<XlsOpenStrategy>();
            services.AddSingleton<PptOpenStrategy>();
            services.AddSingleton<DocOpenStrategy>();

            // Map enum to strategy implementation
            services.AddSingleton<IDictionary<OfficeMimeType, IOpenStrategy>>(sp =>
            {
                var dict = new Dictionary<OfficeMimeType, IOpenStrategy>
                {
                    [OfficeMimeType.Word] = sp.GetRequiredService<WordOpenStrategy>(),
                    [OfficeMimeType.Excel] = sp.GetRequiredService<ExcelOpenStrategy>(),
                    [OfficeMimeType.PowerPoint] = sp.GetRequiredService<PowerPointOpenStrategy>(),
                    [OfficeMimeType.OpenXmlUnknown] = sp.GetRequiredService<ZipFallbackOpenStrategy>(),
                    [OfficeMimeType.ExcelLegacy] = sp.GetRequiredService<XlsOpenStrategy>(),
                    [OfficeMimeType.PowerPointLegacy] = sp.GetRequiredService<PptOpenStrategy>(),
                    [OfficeMimeType.WordLegacy] = sp.GetRequiredService<DocOpenStrategy>(),
                };
                return dict;
            });

            // Text-extraction components (one per format, stateless singletons)
            services.AddSingleton<DocxTextExtractor>();
            services.AddSingleton<XlsxTextExtractor>();
            services.AddSingleton<PptxTextExtractor>();
            services.AddSingleton<XlsTextExtractor>();
            services.AddSingleton<DocTextExtractor>();
            services.AddSingleton<PptTextExtractor>();

            services.AddSingleton<IDictionary<OfficeMimeType, ITextExtractor>>(sp =>
            {
                var dict = new Dictionary<OfficeMimeType, ITextExtractor>
                {
                    [OfficeMimeType.Word] = sp.GetRequiredService<DocxTextExtractor>(),
                    [OfficeMimeType.Excel] = sp.GetRequiredService<XlsxTextExtractor>(),
                    [OfficeMimeType.PowerPoint] = sp.GetRequiredService<PptxTextExtractor>(),
                    [OfficeMimeType.ExcelLegacy] = sp.GetRequiredService<XlsTextExtractor>(),
                    [OfficeMimeType.WordLegacy] = sp.GetRequiredService<DocTextExtractor>(),
                    [OfficeMimeType.PowerPointLegacy] = sp.GetRequiredService<PptTextExtractor>(),
                };
                return dict;
            });

            // Format dispatch registry (ydk:req:extraction/format-extensibility):
            // the single DI-registered seam MainExtractor resolves open/text-extraction
            // components through, keyed by format.
            services.AddSingleton<IFormatDispatchRegistry>(sp => new FormatDispatchRegistry(
                sp.GetRequiredService<IDictionary<OfficeMimeType, IOpenStrategy>>(),
                sp.GetRequiredService<IDictionary<OfficeMimeType, ITextExtractor>>()));

            // Main extractor
            services.AddSingleton(sp => new MainExtractor(sp.GetRequiredService<IExtractionHelper>(), sp.GetRequiredService<IFormatDispatchRegistry>(), sp.GetRequiredService<ExtractionGuardrails>()));
        }
    }
}
