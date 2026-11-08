using System;
using ExtractorOLE.Configuration;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    public class MimeDetectionLimitsValidationTests
    {
        public static TheoryData<long> BadValues => new() { 0, -1, long.MinValue };

        [Theory]
        [MemberData(nameof(BadValues))]
        public void MaxFileSizeBytes_NonPositive_Throws(long v) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new MimeDetectionLimits { MaxFileSizeBytes = v });

        [Theory]
        [MemberData(nameof(BadValues))]
        public void MaxDeclaredNestedContentBytes_NonPositive_Throws(long v) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new MimeDetectionLimits { MaxDeclaredNestedContentBytes = v });

        [Theory]
        [MemberData(nameof(BadValues))]
        public void MaxDeclaredEntryBytes_NonPositive_Throws(long v) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new MimeDetectionLimits { MaxDeclaredEntryBytes = v });

        [Theory]
        [MemberData(nameof(BadValues))]
        public void MaxZipEntryCount_NonPositive_Throws(long v) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new MimeDetectionLimits { MaxZipEntryCount = v });

        [Fact]
        public void Defaults_Unchanged()
        {
            var l = new MimeDetectionLimits();
            Assert.Equal(104_857_600, l.MaxFileSizeBytes);
            Assert.Equal(2_147_483_648, l.MaxDeclaredNestedContentBytes);
            Assert.Equal(1_073_741_824, l.MaxDeclaredEntryBytes);
            Assert.Equal(10_000, l.MaxZipEntryCount);
        }
    }
}
