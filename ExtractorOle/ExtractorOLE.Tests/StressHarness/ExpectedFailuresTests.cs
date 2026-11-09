using ExtractorOLE.StressHarness;
using Xunit;

namespace ExtractorOLE.Tests.StressHarness
{
    public class ExpectedFailuresTests
    {
        [Fact]
        public void Load_ReadsOnlyEntriesWithExpectedBehavior_StrippingSamplesPrefix()
        {
            var dir = Directory.CreateTempSubdirectory();
            try
            {
                File.WriteAllText(
                    Path.Combine(dir.FullName, "manifest.json"),
                    "{\"samples\":[{\"path\":\"samples/adversarial/bad.docx\",\"expectedBehavior\":{}},{\"path\":\"samples/ok.docx\"}]}");

                var expected = ExpectedFailures.Load(dir.FullName);

                Assert.Equal("adversarial/bad.docx", Assert.Single(expected));
            }
            finally
            {
                dir.Delete(true);
            }
        }

        [Fact]
        public void Load_NoManifestOrDirectory_ReturnsEmpty()
        {
            Assert.Empty(ExpectedFailures.Load(null));
            Assert.Empty(ExpectedFailures.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))));
        }

        [Fact]
        public void ExitCodeFor_IdenticalSets_ReturnsZero() =>
            Assert.Equal(0, ExpectedFailures.ExitCodeFor(new HashSet<string> { "a" }, new HashSet<string> { "a" }));

        [Fact]
        public void ExitCodeFor_DifferentSets_ReturnsOne() =>
            Assert.Equal(1, ExpectedFailures.ExitCodeFor(new HashSet<string> { "a" }, new HashSet<string> { "b" }));
    }
}
