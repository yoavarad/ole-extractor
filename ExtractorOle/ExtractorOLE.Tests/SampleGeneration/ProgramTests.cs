using System;
using System.IO;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.SampleGeneration
{
    // SampleGenerator/Program.cs --adversarial writes the docx fixtures from
    // AdversarialDocxSampleGenerator; the committed files must match what it produces.
    public class ProgramTests
    {
        private static byte[] Committed(string fileName)
        {
            var relative = Path.Combine("samples", "adversarial", fileName);
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
                dir = dir.Parent;
            }
            throw new FileNotFoundException(relative);
        }

        [Fact]
        public void OversizedEmbeddedObject_CommittedFileMatchesGenerator() =>
            Assert.Equal(AdversarialDocxSampleGenerator.BuildOversizedEmbeddedObject(), Committed("oversized-embedded-object.docx"));

        [Fact]
        public void CorruptBodyText_CommittedFileMatchesGenerator() =>
            Assert.Equal(AdversarialDocxSampleGenerator.BuildCorruptBodyText(), Committed("corrupt-body-text.docx"));
    }
}
