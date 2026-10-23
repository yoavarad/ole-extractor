using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Task #201: a legacy .xls input is loaded into an HSSFWorkbook exactly once per
    /// Extract() call - XlsOpenStrategy hands its workbook to XlsTextExtractor.
    /// </summary>
    public class MainExtractorXlsSingleLoadTests
    {
        private const string XlsMime = "application/vnd.ms-excel";

        private static MainExtractor BuildExtractor()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        }

        private static byte[] Generate(string text) =>
            new XlsSampleGenerator().Generate(new SampleSpec { BodyText = text }).Content;

        [Fact]
        public void ExtractRequest_LoadsWorkbookExactlyOnce()
        {
            const string text = "single load";
            var extractor = BuildExtractor();
            var bytes = Generate(text);

            var before = XlsWorkbooks.LoadCount;
            var result = extractor.Extract(new ExtractionRequest { FileBytes = bytes, FileName = "fixture", DetectedMimeType = XlsMime });
            var loads = XlsWorkbooks.LoadCount - before;

            Assert.Equal(1, loads);
            Assert.Contains(text, result.ExtractedText);
        }

        [Fact]
        public void ExtractBytes_LoadsWorkbookExactlyOnce()
        {
            const string text = "single load";
            var extractor = BuildExtractor();
            var bytes = Generate(text);

            var before = XlsWorkbooks.LoadCount;
#pragma warning disable CS0618 // exercises the [Obsolete] Extract(byte[]) shim
            var result = extractor.Extract(bytes);
#pragma warning restore CS0618
            var loads = XlsWorkbooks.LoadCount - before;

            Assert.Equal(1, loads);
            Assert.Contains(text, result.ExtractedText);
        }
    }
}
