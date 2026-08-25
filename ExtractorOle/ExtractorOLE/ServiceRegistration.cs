using ExtractorOLE.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.MimeDetection;

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

            // Main extractor
            services.AddSingleton<MainExtractor>();
        }
    }
}
