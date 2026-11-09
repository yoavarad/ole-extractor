using ExtractorOLE.Helpers;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    // #290: the ambient token an Extract call hands to the built-in loops.
    public class ExtractionCancellationTests
    {
        [Fact]
        public void NoScope_DoesNotThrow()
        {
            ExtractionCancellation.ThrowIfCancellationRequested();
            Assert.False(ExtractionCancellation.Token.CanBeCanceled);
        }

        [Fact]
        public void CancelledScope_Throws_AndDisposeRestoresPreviousToken()
        {
            using var outer = new CancellationTokenSource();
            using var inner = new CancellationTokenSource();
            inner.Cancel();

            using (ExtractionCancellation.Enter(outer.Token))
            {
                using (ExtractionCancellation.Enter(inner.Token))
                {
                    var ex = Assert.Throws<OperationCanceledException>(ExtractionCancellation.ThrowIfCancellationRequested);
                    Assert.Equal(inner.Token, ex.CancellationToken);
                }

                Assert.Equal(outer.Token, ExtractionCancellation.Token);
                ExtractionCancellation.ThrowIfCancellationRequested();
            }

            Assert.Equal(CancellationToken.None, ExtractionCancellation.Token);
        }
    }
}
