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
            Assert.Equal("samples/curated/docx/password-protected-decrypted.docx", sample.UnencryptedTwin);
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

        [Fact]
        public void Manifest_OoxmlEncryptedSamples_CarryPasswordAndTwinPerKind()
        {
            var samples = EncryptedSamples.Enumerate().ToDictionary(s => System.IO.Path.GetFileName(s.Path), StringComparer.Ordinal);

            Assert.Equal("pässwörd-Ωmega", samples["agile-nonascii-password.xlsx"].Password);
            Assert.Equal("samples/curated/xlsx/project-status-report.xlsx", samples["agile-nonascii-password.xlsx"].UnencryptedTwin);
            Assert.Equal("pptx-agile-1", samples["agile-password.pptx"].Password);
            Assert.Equal("samples/curated/pptx/product-launch-deck.pptx", samples["agile-password.pptx"].UnencryptedTwin);
            Assert.Equal("tamper-agile-1", samples["agile-tampered-package.xlsx"].Password);
            Assert.Equal("samples/curated/xlsx/project-status-report.xlsx", samples["agile-tampered-package.xlsx"].UnencryptedTwin);

            // The embedding document is unencrypted: neither field.
            Assert.Null(samples["embeds-encrypted-package.docx"].Password);
            Assert.Null(samples["embeds-encrypted-package.docx"].UnencryptedTwin);
        }

        [Fact]
        public void Enumerate_TwinsExistOnDisk()
        {
            var repoRoot = System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(EncryptedSamples.Enumerate()[0].FullPath)))!;
            foreach (var twin in EncryptedSamples.Enumerate().Select(s => s.UnencryptedTwin).Where(t => t is not null))
            {
                Assert.True(File.Exists(System.IO.Path.Combine(repoRoot, twin!)), twin);
            }
        }

        [Fact]
        public void Enumerate_OnlyEmbeddingDocumentHasNoPassword()
        {
            var passwordless = EncryptedSamples.Enumerate().Where(s => s.Password is null).Select(s => s.Path);

            Assert.Equal(new[] { "samples/encrypted/embeds-encrypted-package.docx" }, passwordless);
        }
    }
}
