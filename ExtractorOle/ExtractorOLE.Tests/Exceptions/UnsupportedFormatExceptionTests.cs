using ExtractorOLE.Exceptions;
using Xunit;

namespace ExtractorOLE.Tests.Exceptions
{
    public class UnsupportedFormatExceptionTests
    {
        [Fact]
        public void LongMime_IsCappedInPropertyAndMessage()
        {
            var ex = new UnsupportedFormatException(new string('x', 5000));
            Assert.Equal(100, ex.DetectedMimeType.Length);
            Assert.True(ex.Message.Length < 300);
        }

        [Fact]
        public void ShortMime_IsEchoedUnchanged_AndNullBecomesEmpty()
        {
            Assert.Equal("text/plain", new UnsupportedFormatException("text/plain").DetectedMimeType);
            Assert.Equal(string.Empty, new UnsupportedFormatException(null).DetectedMimeType);
        }
    }
}
