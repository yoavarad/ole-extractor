using ExtractorOLE.DTOs;
using Xunit;

namespace ExtractorOLE.Tests.DTOs
{
    public class MimeDetectionRequestTests
    {
        [Fact]
        public void Password_DefaultsToNull_AndIsSetAtInit()
        {
            Assert.Null(new MimeDetectionRequest().Password);
            Assert.Equal("pw", new MimeDetectionRequest { Password = "pw" }.Password);
        }
    }
}
