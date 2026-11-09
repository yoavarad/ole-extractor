using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using Xunit;

namespace ExtractorOLE.Tests.PowerPoint
{
    // #290: b2xtranslator has no cancellation hook, so the converter checks the token only
    // between its stages; a cancelled token stops it before the conversion starts.
    public class PptToPptxConverterTests
    {
        private static byte[] Fixture() =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "PowerPoint", "Fixtures", "with_textbox.ppt"));

        [Fact]
        public void Convert_NoCancellation_ProducesPptxPackage()
        {
            var pptx = PptToPptxConverter.Convert(Fixture());

            Assert.True(pptx.Length > 4);
            Assert.Equal((byte)'P', pptx[0]);
            Assert.Equal((byte)'K', pptx[1]);
        }

        [Fact]
        public void Convert_CancelledToken_ThrowsOperationCanceled()
        {
            var bytes = Fixture();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            using var scope = ExtractionCancellation.Enter(cts.Token);

            Assert.ThrowsAny<OperationCanceledException>(() => PptToPptxConverter.Convert(bytes));
        }
    }
}
