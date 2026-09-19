using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using System.Reflection;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Covers T-3b4e70f4: Extract(ExtractionRequest) dispatches, via the real production
    /// DI wiring (ServiceRegistration) and IFormatDispatchRegistry, to the open +
    /// text-extraction components of each of the six supported formats, using the
    /// caller-supplied DetectedMimeType as-is. No format-handling component is mocked.
    /// </summary>
    public class MainExtractorExtractRequestTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string PptxMime = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        private const string DocMime = "application/msword";
        private const string XlsMime = "application/vnd.ms-excel";
        private const string PptMime = "application/vnd.ms-powerpoint";

        private static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider();
        }

        private static string FindRepoRootFile(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
        }

        private static ExtractionRequest Request(byte[] bytes, string mime) =>
            new() { FileBytes = bytes, FileName = "fixture", DetectedMimeType = mime };

        private static byte[] BuildXls(string text)
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1").CreateRow(0).CreateCell(0).SetCellValue(text);
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        [Fact]
        public void Docx_DispatchesToWordComponents()
        {
            const string text = "Extract facade dispatch - docx.";
            var bytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = text }).Content;

            var result = BuildProvider().GetRequiredService<MainExtractor>().Extract(Request(bytes, DocxMime));

            Assert.Equal(DocxMime, result.MimeType);
            Assert.Equal(text, result.ExtractedText);
        }

        [Fact]
        public void Xlsx_DispatchesToExcelComponents()
        {
            const string text = "Extract facade dispatch - xlsx.";
            var bytes = new XlsxSampleGenerator().Generate(new SampleSpec { BodyText = text }).Content;

            var result = BuildProvider().GetRequiredService<MainExtractor>().Extract(Request(bytes, XlsxMime));

            Assert.Equal(XlsxMime, result.MimeType);
            Assert.Equal(text, result.ExtractedText);
        }

        [Fact]
        public void Pptx_DispatchesToPowerPointComponents()
        {
            const string text = "Extract facade dispatch - pptx.";
            var bytes = new PptxSampleGenerator().Generate(new SampleSpec { BodyText = text }).Content;

            var result = BuildProvider().GetRequiredService<MainExtractor>().Extract(Request(bytes, PptxMime));

            Assert.Equal(PptxMime, result.MimeType);
            Assert.Equal(text, result.ExtractedText);
        }

        [Fact]
        public void Doc_DispatchesToLegacyWordComponents()
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile("ExtractorOle/ExtractorOLE.Tests/Doc/Fixtures/multilingual.doc"));

            var result = BuildProvider().GetRequiredService<MainExtractor>().Extract(Request(bytes, DocMime));

            Assert.Equal(DocMime, result.MimeType);
            Assert.Contains(MultilingualFixtures.Hebrew, result.ExtractedText, StringComparison.Ordinal);
        }

        [Fact]
        public void Xls_DispatchesToLegacyExcelComponents()
        {
            const string text = "Extract facade dispatch - xls.";

            var result = BuildProvider().GetRequiredService<MainExtractor>().Extract(Request(BuildXls(text), XlsMime));

            Assert.Equal(XlsMime, result.MimeType);
            Assert.Equal(text, result.ExtractedText);
        }

        [Fact]
        public void Ppt_DispatchesToLegacyPowerPointComponents()
        {
            var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "PowerPoint", "Fixtures", "with_textbox.ppt"));

            var result = BuildProvider().GetRequiredService<MainExtractor>().Extract(Request(bytes, PptMime));

            Assert.Equal(PptMime, result.MimeType);
            Assert.Contains("Hello, World!!!", result.ExtractedText, StringComparison.Ordinal);
        }

        // Counts calls to the helper's detection members while delegating everything to the
        // real ExtractionHelper.
        public class DetectionCountingProxy : DispatchProxy
        {
            public IExtractionHelper Inner { get; set; } = null!;
            public int DetectionCalls { get; private set; }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod!.Name.StartsWith("Detect")) DetectionCalls++;
                return targetMethod.Invoke(Inner, args);
            }
        }

        [Fact]
        public void Extract_UsesDetectedMimeTypeAsIs_NeverReDetects()
        {
            using var provider = BuildProvider();
            var helper = DispatchProxy.Create<IExtractionHelper, DetectionCountingProxy>();
            var proxy = (DetectionCountingProxy)(object)helper;
            proxy.Inner = provider.GetRequiredService<IExtractionHelper>();
            var extractor = new MainExtractor(helper, provider.GetRequiredService<IFormatDispatchRegistry>());
            const string text = "Extract facade no re-detect.";
            var bytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = text }).Content;

            var result = extractor.Extract(Request(bytes, DocxMime));

            Assert.Equal(text, result.ExtractedText);
            Assert.Equal(0, proxy.DetectionCalls);
        }
    }
}
