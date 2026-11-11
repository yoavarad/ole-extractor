using Xunit;

namespace ExtractorOLE.Tests
{
    public class EncryptedSamplesTests
    {
        [Fact]
        public void Enumerate_ReturnsEncryptedFolderSamplesWithManifestPassword()
        {
            var sample = Assert.Single(EncryptedSamples.Enumerate(), s => s.Path == "samples/encrypted/password-protected.docx");

            Assert.Equal("solrcell", sample.Password);
            Assert.Null(sample.UnencryptedTwin);
            Assert.True(File.Exists(sample.FullPath));
        }

        [Fact]
        public void Enumerate_ReturnsOnlyFilesUnderEncryptedFolder()
        {
            Assert.All(EncryptedSamples.Enumerate(), s => Assert.StartsWith("samples/encrypted/", s.Path));
        }

        [Fact]
        public void OldAdversarialPath_NoLongerExists()
        {
            var sample = Assert.Single(EncryptedSamples.Enumerate(), s => s.Path.EndsWith("password-protected.docx", StringComparison.Ordinal));
            var legacy = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(sample.FullPath))!, "adversarial", "password-protected.docx");
            Assert.False(File.Exists(legacy));
        }
    }
}
