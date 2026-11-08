using System;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.SampleGeneration
{
    // SampleGenerator/Program.cs --adversarial writes the three legacy per-field-isolation fixtures
    // into samples/adversarial/; their content is verified in AdversarialSampleGeneratorTests.
    public class ProgramTests
    {
        [Theory]
        [InlineData("corrupt-metadata-property.doc")]
        [InlineData("corrupt-sheet-records.xls")]
        [InlineData("unnamed-embedded-storage.doc")]
        public void AdversarialFlag_LegacyFixtureIsCommitted(string fileName)
        {
            var relative = Path.Combine("samples", "adversarial", fileName);
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate))
                {
                    Assert.True(new FileInfo(candidate).Length > 0);
                    return;
                }
            }
            Assert.Fail($"{relative} not found");
        }
    }
}
