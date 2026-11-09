using ExtractorOLE.StressHarness;
using Xunit;

namespace ExtractorOLE.Tests.StressHarness
{
    public class BatchModeTests
    {
        [Fact]
        public void ExitCodeFor_SomeSucceedNoMismatch_ReturnsZero() => Assert.Equal(0, BatchMode.ExitCodeFor(10, 2, 0));

        [Fact]
        public void ExitCodeFor_EverythingFailed_ReturnsOne() => Assert.Equal(1, BatchMode.ExitCodeFor(10, 0, 0));

        [Fact]
        public void ExitCodeFor_Mismatch_ReturnsOne() => Assert.Equal(1, BatchMode.ExitCodeFor(10, 10, 1));
    }
}
