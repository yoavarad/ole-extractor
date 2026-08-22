using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;

namespace SampleGenerator
{
    public static class ServiceRegistration
    {
        public static void Register(IServiceCollection services)
        {
            services.AddSingleton<DocxSampleGenerator>();
            services.AddSingleton<XlsxSampleGenerator>();
            services.AddSingleton<PptxSampleGenerator>();
            services.AddSingleton<DocSampleGenerator>();
            services.AddSingleton<XlsSampleGenerator>();
            services.AddSingleton<PptSampleGenerator>();

            services.AddSingleton<IDictionary<SampleFormat, ISampleGenerator>>(sp => new Dictionary<SampleFormat, ISampleGenerator>
            {
                [SampleFormat.Docx] = sp.GetRequiredService<DocxSampleGenerator>(),
                [SampleFormat.Xlsx] = sp.GetRequiredService<XlsxSampleGenerator>(),
                [SampleFormat.Pptx] = sp.GetRequiredService<PptxSampleGenerator>(),
                [SampleFormat.Doc] = sp.GetRequiredService<DocSampleGenerator>(),
                [SampleFormat.Xls] = sp.GetRequiredService<XlsSampleGenerator>(),
                [SampleFormat.Ppt] = sp.GetRequiredService<PptSampleGenerator>(),
            });
        }
    }
}
