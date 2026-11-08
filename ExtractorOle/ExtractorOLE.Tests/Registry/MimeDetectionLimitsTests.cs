using ExtractorOLE.Configuration;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    public class MimeDetectionLimitsTests
    {
        [Fact]
        public void DefaultZipEntryCountCap_Is10000() => Assert.Equal(10_000, new MimeDetectionLimits().MaxZipEntryCount);
    }
}
