using ExtractorOLE.Helpers;
using Xunit;

namespace ExtractorOLE.Tests
{
    public class MetadataConversionsTests
    {
        [Fact]
        public void EditTimeToMinutes_ConvertsOle100NsUnitsToWholeMinutes()
        {
            Assert.Equal(126, MetadataConversions.EditTimeToMinutes(7560L * 10_000_000L));
            Assert.Equal(1, MetadataConversions.EditTimeToMinutes(119L * 10_000_000L));
        }

        [Fact]
        public void EditTimeToMinutes_ZeroIsNullNotZero() =>
            Assert.Null(MetadataConversions.EditTimeToMinutes(0));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        public void ParseInt_AbsentOrInvalidIsNull(string? value) =>
            Assert.Null(MetadataConversions.ParseInt(value));

        [Fact]
        public void ParseInt_ParsesRevision() => Assert.Equal(1154, MetadataConversions.ParseInt("1154"));

        [Fact]
        public void NullIfEmpty_EmptyBecomesNull() => Assert.Null(MetadataConversions.NullIfEmpty(""));

        [Fact]
        public void ToUtcOrNull_DefaultAndNullAreNull()
        {
            Assert.Null(MetadataConversions.ToUtcOrNull(default(System.DateTime)));
            Assert.Null(MetadataConversions.ToUtcOrNull(null));
        }
    }
}
