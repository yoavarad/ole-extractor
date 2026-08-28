using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Regression coverage for T-90924b6d's registry refactor: confirms the real,
    /// production DI wiring (ServiceRegistration) still dispatches docx/xlsx/xls/pptx
    /// open+extract through MainExtractor exactly as before, now that dispatch is
    /// resolved via IFormatDispatchRegistry instead of MainExtractor's old hardcoded
    /// lookups.
    /// </summary>
    public class MainExtractorDispatchIntegrationTests
    {
        private static MainExtractor BuildExtractor()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<MainExtractor>();
        }

        [Fact]
        public void Docx_OpenAndExtract_RoundTripsBodyText()
        {
            var expected = "Registry dispatch smoke test - docx.";
            var bytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = expected }).Content;

            var result = BuildExtractor().Extract(bytes);

            Assert.Equal(expected, result.ExtractedText);
            Assert.Contains("wordprocessingml", result.MimeType);
        }

        [Fact]
        public void Xlsx_OpenAndExtract_RoundTripsBodyText()
        {
            var expected = "Registry dispatch smoke test - xlsx.";
            var bytes = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = expected }).Content;

            var result = BuildExtractor().Extract(bytes);

            Assert.Equal(expected, result.ExtractedText);
            Assert.Contains("spreadsheetml", result.MimeType);
        }

        [Fact]
        public void Pptx_OpenAndExtract_RoundTripsBodyText()
        {
            var expected = "Registry dispatch smoke test - pptx.";
            var bytes = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = expected }).Content;

            var result = BuildExtractor().Extract(bytes);

            Assert.Equal(expected, result.ExtractedText);
            Assert.Contains("presentationml", result.MimeType);
        }

        [Fact]
        public void Xls_Open_StillPopulatesMetadataViaRegistry()
        {
            // Legacy .xls has no ITextExtractor registered (pre-existing behavior,
            // unchanged by this refactor) - this exercises the IOpenStrategy side of
            // the registry only.
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1");
            workbook.CreateInformationProperties();
            workbook.SummaryInformation.Title = "Registry dispatch smoke test - xls";
            using var ms = new MemoryStream();
            workbook.Write(ms);

            var result = BuildExtractor().Extract(ms.ToArray());

            Assert.Equal("application/vnd.ms-excel", result.MimeType);
            Assert.Equal("Registry dispatch smoke test - xls", result.Metadata.Title);
        }
    }
}
