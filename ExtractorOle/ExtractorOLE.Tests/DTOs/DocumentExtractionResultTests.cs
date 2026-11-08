using ExtractorOLE.DTOs;
using Xunit;

namespace ExtractorOLE.Tests.DTOs
{
    // #303: FieldErrors is additive and empty, never null, on a new result.
    public class DocumentExtractionResultTests
    {
        [Fact]
        public void NewResult_HasEmptyNonNullFieldErrors()
        {
            var result = new DocumentExtractionResult();

            Assert.NotNull(result.FieldErrors);
            Assert.Empty(result.FieldErrors);
        }
    }
}
