using ExtractorOLE.StressHarness;
using Xunit;

namespace ExtractorOLE.Tests.StressHarness
{
    public class BatchModeTests
    {
        private static HashSet<string> Set(params string[] names) => new(names);

        [Fact]
        public void ExitCodeFor_ExpectedFailuresOnly_ReturnsZero() => Assert.Equal(0, BatchMode.ExitCodeFor(Set("a"), Set("a"), 0));

        [Fact]
        public void ExitCodeFor_UnexpectedFailure_ReturnsOne() => Assert.Equal(1, BatchMode.ExitCodeFor(Set(), Set("a"), 0));

        [Fact]
        public void ExitCodeFor_ExpectedFailureNowPasses_ReturnsOne() => Assert.Equal(1, BatchMode.ExitCodeFor(Set("a"), Set(), 0));

        [Fact]
        public void ExitCodeFor_Mismatch_ReturnsOne() => Assert.Equal(1, BatchMode.ExitCodeFor(Set(), Set(), 1));
    }
}
