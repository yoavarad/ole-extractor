using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;

namespace ExtractorOLE
{
    public static class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            // Core helpers
            services.AddSingleton<IExtractionHelper, ExtractionHelper>();

            // Open strategies
            services.AddSingleton<WordOpenStrategy>();
            services.AddSingleton<ExcelOpenStrategy>();
            services.AddSingleton<PowerPointOpenStrategy>();
            services.AddSingleton<ZipFallbackOpenStrategy>();

            // Map enum to strategy implementation
            services.AddSingleton<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>(sp =>
            {
                var dict = new Dictionary<OfficeMimeTypeEnum, IOpenStrategy>
                {
                    [OfficeMimeTypeEnum.Word] = sp.GetRequiredService<WordOpenStrategy>(),
                    [OfficeMimeTypeEnum.Excel] = sp.GetRequiredService<ExcelOpenStrategy>(),
                    [OfficeMimeTypeEnum.PowerPoint] = sp.GetRequiredService<PowerPointOpenStrategy>(),
                    [OfficeMimeTypeEnum.OpenXmlUnkown] = sp.GetRequiredService<ZipFallbackOpenStrategy>(),
                };
                return dict;
            });

            // Main extractor
            services.AddSingleton<MainExtractor>();
        }
    }
}
