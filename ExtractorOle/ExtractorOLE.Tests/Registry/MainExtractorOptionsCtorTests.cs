using ExtractorOLE.Abstractions;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>Non-DI construction: limits passed via ctor reach the guardrails; defaults unchanged.</summary>
    public class MainExtractorOptionsCtorTests
    {
        private sealed class NullRegistry : IFormatDispatchRegistry
        {
            public IOpenStrategy? GetOpenStrategy(OfficeMimeType format) => null;
            public ITextExtractor? GetTextExtractor(OfficeMimeType format) => null;
        }

        private static ExtractionRequest Req(int size) => new()
        {
            FileBytes = new byte[size],
            FileName = "a.docx",
            DetectedMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        };

        [Fact]
        public void CustomMaxFileSize_ViaCtor_RejectsOversizedFile_WithoutDi()
        {
            var limits = new MimeDetectionLimits { MaxFileSizeBytes = 10 };
            var extractor = new MainExtractor(new ExtractionHelper(limits), new NullRegistry(), limits);
            var ex = Record.Exception(() => extractor.Extract(Req(11)));
            Assert.NotNull(ex);
            Assert.Contains("size", ex!.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void DefaultCtor_SmallFile_NotRejectedBySizeGuard()
        {
            var extractor = new MainExtractor(new ExtractionHelper(), new NullRegistry());
            var ex = Record.Exception(() => extractor.Extract(Req(11)));
            Assert.True(ex == null || !ex.Message.Contains("size", StringComparison.OrdinalIgnoreCase));
        }
    }
}
