using ExtractorOLE.StressHarness;
using Xunit;

namespace ExtractorOLE.Tests.StressHarness
{
    public class ProgramTests
    {
        private static HashSet<string> Set(params string[] names) => new(names);

        [Fact]
        public void ExitCodeFor_ExpectedFailuresOnly_ReturnsZero() => Assert.Equal(0, Program.ExitCodeFor(Set("a", "b"), Set("a", "b"), 0, 0));

        [Fact]
        public void ExitCodeFor_UnexpectedFailure_ReturnsOne() => Assert.Equal(1, Program.ExitCodeFor(Set("a"), Set("a", "c"), 0, 0));

        [Fact]
        public void ExitCodeFor_ExpectedFailureNowPasses_ReturnsOne() => Assert.Equal(1, Program.ExitCodeFor(Set("a", "b"), Set("a"), 0, 0));

        [Fact]
        public void ExitCodeFor_UnexpectedLoadFailure_ReturnsOne() => Assert.Equal(1, Program.ExitCodeFor(Set(), Set(), 1, 0));

        [Fact]
        public void ExitCodeFor_Mismatch_ReturnsOne() => Assert.Equal(1, Program.ExitCodeFor(Set(), Set(), 0, 1));

        [Fact]
        public void LoadNamedSamples_SkipsEncryptedFolder()
        {
            var dir = Directory.CreateTempSubdirectory();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir.FullName, "encrypted"));
                File.WriteAllBytes(Path.Combine(dir.FullName, "encrypted", "e.docx"), new byte[] { 1 });
                File.WriteAllBytes(Path.Combine(dir.FullName, "ok.docx"), new byte[] { 2 });

                var samples = Program.LoadNamedSamples(dir.FullName, out _);

                Assert.Equal("ok.docx", Assert.Single(samples).Name);
            }
            finally
            {
                dir.Delete(true);
            }
        }

        [Fact]
        public void LoadNamedSamples_NamesAreRelativeSlashPaths()
        {
            var dir = Directory.CreateTempSubdirectory();
            try
            {
                Directory.CreateDirectory(Path.Combine(dir.FullName, "sub"));
                File.WriteAllBytes(Path.Combine(dir.FullName, "sub", "x.bin"), new byte[] { 1 });
                File.WriteAllText(Path.Combine(dir.FullName, "manifest.json"), "{}");

                var samples = Program.LoadNamedSamples(dir.FullName, out var resolved);

                Assert.Equal(dir.FullName, resolved);
                Assert.Equal("sub/x.bin", Assert.Single(samples).Name);
            }
            finally
            {
                dir.Delete(true);
            }
        }
    }
}
