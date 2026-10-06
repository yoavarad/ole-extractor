using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>Task #171: fallback picks the OOXML flavor from [Content_Types].xml, one open per package.</summary>
    public class ZipFallbackOpenStrategyTests
    {
        private static ZipFallbackOpenStrategy Build()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<ZipFallbackOpenStrategy>();
        }

        [Fact]
        public void Pptx_OpensOnce()
        {
            var bytes = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content;
            var before = OpenXmlPackages.OpenCount;
            var r = Build().Open(bytes);
            Assert.NotNull(r);
            Assert.Equal(1, OpenXmlPackages.OpenCount - before);
            (r!.ParsedDocument as System.IDisposable)?.Dispose();
        }

        [Fact]
        public void Xlsx_OpensOnce()
        {
            var bytes = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content;
            var before = OpenXmlPackages.OpenCount;
            var r = Build().Open(bytes);
            Assert.NotNull(r);
            Assert.Equal(1, OpenXmlPackages.OpenCount - before);
            (r!.ParsedDocument as System.IDisposable)?.Dispose();
        }

        [Fact]
        public void Docx_OpensOnce()
        {
            var bytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "x" }).Content;
            var before = OpenXmlPackages.OpenCount;
            var r = Build().Open(bytes);
            Assert.NotNull(r);
            Assert.Equal(1, OpenXmlPackages.OpenCount - before);
            (r!.ParsedDocument as System.IDisposable)?.Dispose();
        }
    }
}
