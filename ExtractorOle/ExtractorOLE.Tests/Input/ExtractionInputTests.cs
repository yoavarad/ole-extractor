using ExtractorOLE.Helpers.Input;
using Xunit;

namespace ExtractorOLE.Tests.Input;

/// <summary>#379: a decrypted package is wrapped as owned stream input.</summary>
public class ExtractionInputTests
{
    [Fact]
    public void FromDecrypted_ExposesStreamInputRewound_AndDisposesItOnDispose()
    {
        var decrypted = new MemoryStream(new byte[] { 1, 2, 3 });
        decrypted.Position = 2;

        var input = ExtractionInput.FromDecrypted(decrypted);

        Assert.Null(input.OriginalBytes);
        Assert.Equal(3, input.Length);
        Assert.Equal(0, input.Stream!.Position);
        Assert.Equal(new byte[] { 1, 2, 3 }, input.Bytes);

        input.Dispose();
        Assert.False(decrypted.CanRead);
    }

    [Fact]
    public void FromDecrypted_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ExtractionInput.FromDecrypted(null!));
    }
}
