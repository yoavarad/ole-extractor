using ExtractorOLE.DTOs;
using Xunit;

namespace ExtractorOLE.Tests.DTOs
{
    public class ExtractionRequestTests
    {
        [Fact]
        public void Password_DefaultsToNull_AndIsSetAtInit()
        {
            Assert.Null(new ExtractionRequest().Password);
            Assert.Equal("pw", new ExtractionRequest { Password = "pw" }.Password);
        }
    }
}
