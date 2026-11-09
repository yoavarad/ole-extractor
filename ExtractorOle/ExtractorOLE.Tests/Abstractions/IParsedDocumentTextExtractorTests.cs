using ExtractorOLE.Abstractions;
using Xunit;

namespace ExtractorOLE.Tests.Abstractions
{
    public class IParsedDocumentTextExtractorTests
    {
        // An extractor with no independent parts keeps the default: the part-failure overload
        // delegates to ExtractText(object), so a failure fails the whole text (#288).
        private sealed class WholeTextExtractor : IParsedDocumentTextExtractor
        {
            public string ExtractText(byte[] fileBytes) => throw new NotSupportedException();

            public string? ExtractText(object parsedDocument) =>
                parsedDocument is string s ? s : throw new InvalidOperationException("whole failure");
        }

        [Fact]
        public void DefaultPartFailureOverload_DelegatesToSingleArgument()
        {
            IParsedDocumentTextExtractor extractor = new WholeTextExtractor();
            var failures = new List<Exception>();

            Assert.Equal("text", extractor.ExtractText("text", failures.Add));
            Assert.Throws<InvalidOperationException>(() => extractor.ExtractText(42, failures.Add));
            Assert.Empty(failures);
        }
    }
}
