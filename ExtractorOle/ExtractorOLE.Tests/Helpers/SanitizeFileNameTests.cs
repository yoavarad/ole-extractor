using ExtractorOLE.Helpers;
using Xunit;

namespace ExtractorOLE.Tests.Helpers;

public class SanitizeFileNameTests
{
    [Theory]
    [InlineData("..\\..\\x", "x")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("a\\b/c.bin", "c.bin")]
    [InlineData("..", "unnamed")]
    [InlineData("", "unnamed")]
    [InlineData("a\u0001b\0c", "a_b_c")]
    [InlineData("C:\\win\\evil.exe", "evil.exe")]
    [InlineData("Ole10Native", "Ole10Native")]
    [InlineData("MBD0001A2B3", "MBD0001A2B3")]
    public void SanitizeFileName_HostileNames_AreSafe(string input, string expected)
    {
        string result = EmbeddedFileExtractor.SanitizeFileName(input);

        Assert.Equal(expected, result);
        Assert.DoesNotContain('/', result);
        Assert.DoesNotContain('\\', result);
        Assert.NotEqual("..", result);
    }
}
