using ExtractorOLE.DTOs;
using Xunit;

namespace ExtractorOLE.Tests.DTOs
{
    // #303: [ydk:entity:extraction/FieldError] shape and its closed three-value code vocabulary.
    public class FieldErrorTests
    {
        private static readonly FieldErrorCode[] ExpectedCodes =
        {
            FieldErrorCode.CorruptFile,
            FieldErrorCode.OversizedNestedContent,
            FieldErrorCode.UnexpectedFieldFailure,
        };

        [Fact]
        public void Code_VocabularyIsClosedAtThreeValues()
        {
            Assert.Equal(ExpectedCodes, Enum.GetValues<FieldErrorCode>());
        }

        [Fact]
        public void FieldError_CarriesPathCodeAndMessage()
        {
            var error = new FieldError("Metadata.Created", FieldErrorCode.CorruptFile, "m");

            Assert.Equal("Metadata.Created", error.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
            Assert.Equal("m", error.Message);
        }
    }
}
