using ExtractorOLE.StressHarness;
using Xunit;

namespace ExtractorOLE.Tests.StressHarness
{
    public class ProgramTests
    {
        [Fact]
        public void ExitCodeFor_ExpectedBaselineFailuresOnly_ReturnsZero() => Assert.Equal(0, Program.ExitCodeFor(10, 3, 0, 0));

        [Fact]
        public void ExitCodeFor_EveryBaselineFailed_ReturnsOne() => Assert.Equal(1, Program.ExitCodeFor(3, 3, 0, 0));

        [Fact]
        public void ExitCodeFor_UnexpectedLoadFailure_ReturnsOne() => Assert.Equal(1, Program.ExitCodeFor(10, 0, 1, 0));

        [Fact]
        public void ExitCodeFor_Mismatch_ReturnsOne() => Assert.Equal(1, Program.ExitCodeFor(10, 0, 0, 1));
    }
}
