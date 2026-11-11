using ExtractorOLE.Helpers;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    public class RequestPasswordsTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", true)]
        [InlineData("secret", true)]
        public void HasPassword_TreatsNullAndEmptyAsNoPassword(string? password, bool expected)
        {
            Assert.Equal(expected, RequestPasswords.HasPassword(password));
        }
    }
}
