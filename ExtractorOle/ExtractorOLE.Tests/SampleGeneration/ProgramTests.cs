using System;
using System.IO;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.SampleGeneration
{
    // SampleGenerator/Program.cs --adversarial writes the docx fixtures from
    // AdversarialDocxSampleGenerator; the committed files must exist.
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

        [Theory]
        [InlineData("corrupt-body-text.docx")]
        [InlineData("oversized-embedded-object.docx")]
        [InlineData("faulting-embedded-enumeration.docx")]
        public void AdversarialDocxFixture_IsCommittedAsZip(string fileName)
        {
            var bytes = Committed(fileName);
            Assert.True(bytes.Length > 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K');
        }

        [Fact]
        public void Generator_BuildsAllThreeFixtures()
        {
            Assert.NotEmpty(AdversarialDocxSampleGenerator.BuildCorruptBodyText());
            Assert.NotEmpty(AdversarialDocxSampleGenerator.BuildOversizedEmbeddedObject());
            Assert.NotEmpty(AdversarialDocxSampleGenerator.BuildFaultingEmbeddedEnumeration());
        }
    }
}
