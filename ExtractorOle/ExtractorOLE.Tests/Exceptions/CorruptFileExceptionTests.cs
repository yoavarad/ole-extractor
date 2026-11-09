using ExtractorOLE.Exceptions;
using Xunit;

namespace ExtractorOLE.Tests.Exceptions
{
    public class CorruptFileExceptionTests
    {
        [Fact]
        public void InnerException_IsCarried_WithCodeAndFields()
        {
            var cause = new InvalidDataException("bad zip");

            var ex = new CorruptFileException("application/msword", "unparseable", cause);

            Assert.Same(cause, ex.InnerException);
            Assert.Equal("CORRUPT_FILE", ex.Code);
            Assert.Equal(422, ex.StatusCode);
            Assert.Equal("application/msword", ex.DetectedMimeType);
            Assert.Equal("unparseable", ex.ParseFailureReason);
            Assert.Equal("File could not be parsed as application/msword: unparseable", ex.Message);
        }

        [Fact]
        public void TwoArgumentConstructor_HasNoInnerException()
        {
            Assert.Null(new CorruptFileException("application/msword", "unparseable").InnerException);
        }
    }
}
