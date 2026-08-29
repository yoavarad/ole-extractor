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
            // Core helpers
            services.AddSingleton<IExtractionHelper, ExtractionHelper>();

            // Mime detection (legacy CFB family)
            services.AddSingleton(new MimeDetectionLimits());
            services.AddSingleton<ICfbMimeDetector, CfbMimeDetector>();

            // Mime detection (OOXML family)
            services.AddSingleton<IOoxmlMimeDetector, OoxmlMimeDetector>();

            // Mime detection registry (ydk:req:extraction/format-extensibility,
            // ydk:req:extraction/dependency-injection): DetectMimeType dispatch
            // (ExtractionHelper.DetectMimeTypeFromBytes) tries each of these
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

            // Map enum to strategy implementation
            services.AddSingleton<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>(sp =>
            {
                var dict = new Dictionary<OfficeMimeTypeEnum, IOpenStrategy>
                {
                    [OfficeMimeTypeEnum.Word] = sp.GetRequiredService<WordOpenStrategy>(),
                    [OfficeMimeTypeEnum.Excel] = sp.GetRequiredService<ExcelOpenStrategy>(),
                    [OfficeMimeTypeEnum.PowerPoint] = sp.GetRequiredService<PowerPointOpenStrategy>(),
                    [OfficeMimeTypeEnum.OpenXmlUnknown] = sp.GetRequiredService<ZipFallbackOpenStrategy>(),
                    [OfficeMimeTypeEnum.ExcelLegacy] = sp.GetRequiredService<XlsOpenStrategy>(),
                };
                return dict;
            });

            // Text-extraction components (one per format, stateless singletons)
            services.AddSingleton<DocxTextExtractor>();
            services.AddSingleton<XlsxTextExtractor>();
            services.AddSingleton<PptxTextExtractor>();
            services.AddSingleton<XlsTextExtractor>();

            services.AddSingleton<IDictionary<OfficeMimeTypeEnum, ITextExtractor>>(sp =>
            {
                var dict = new Dictionary<OfficeMimeTypeEnum, ITextExtractor>
                {
                    [OfficeMimeTypeEnum.Word] = sp.GetRequiredService<DocxTextExtractor>(),
                    [OfficeMimeTypeEnum.Excel] = sp.GetRequiredService<XlsxTextExtractor>(),
                    [OfficeMimeTypeEnum.PowerPoint] = sp.GetRequiredService<PptxTextExtractor>(),
                    [OfficeMimeTypeEnum.ExcelLegacy] = sp.GetRequiredService<XlsTextExtractor>(),
                };
                return dict;
            });

            // Format dispatch registry (ydk:req:extraction/format-extensibility):
            // the single DI-registered seam MainExtractor resolves open/text-extraction
            // components through, keyed by format.
            services.AddSingleton<IFormatDispatchRegistry>(sp => new FormatDispatchRegistry(
                sp.GetRequiredService<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>(),
                sp.GetRequiredService<IDictionary<OfficeMimeTypeEnum, ITextExtractor>>()));

            // Main extractor
            services.AddSingleton<MainExtractor>();
        }
    }
}
