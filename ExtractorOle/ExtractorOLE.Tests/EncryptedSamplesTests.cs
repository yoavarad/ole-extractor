using NPOI;
using NPOI.HSSF.Record.Crypto;
using NPOI.HSSF.UserModel;
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

        private const string Rc4Xls = "samples/encrypted/rc4-password.xls";
        private const string CryptoApiXls = "samples/encrypted/cryptoapi-encrypted-properties.xls";
        private const string DefaultKeyXls = "samples/encrypted/default-key.xls";

        [Fact]
        public void Manifest_LegacyXlsSamples_CarryPasswordAndTwinPerDatasetCuration()
        {
            var samples = EncryptedSamples.Enumerate();
            var rc4 = Assert.Single(samples, s => s.Path == Rc4Xls);
            var cryptoApi = Assert.Single(samples, s => s.Path == CryptoApiXls);
            var defaultKey = Assert.Single(samples, s => s.Path == DefaultKeyXls);

            Assert.False(string.IsNullOrEmpty(rc4.Password));
            Assert.False(string.IsNullOrEmpty(cryptoApi.Password));
            Assert.Null(defaultKey.Password);
            Assert.All(new[] { rc4, cryptoApi }, s => Assert.StartsWith("samples/synthetic/encrypted-twins/", s.UnencryptedTwin));
            Assert.All(new[] { rc4, cryptoApi, defaultKey }, s =>
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(s.FullPath)))!, s.UnencryptedTwin!))));
        }

        [Theory]
        [InlineData(Rc4Xls)]
        [InlineData(CryptoApiXls)]
        public void PasswordXls_OpensWithHssfOnlyWithItsPassword(string path)
        {
            var sample = Assert.Single(EncryptedSamples.Enumerate(), s => s.Path == path);

            Assert.Equal("Encrypted workbook text line one", OpenFirstCell(sample.FullPath, sample.Password));
            Assert.Throws<EncryptedDocumentException>(() => OpenFirstCell(sample.FullPath, "wrong-password"));
            Assert.Throws<EncryptedDocumentException>(() => OpenFirstCell(sample.FullPath, null));
        }

        [Fact]
        public void DefaultKeyXls_OpensWithHssfWithoutPassword()
        {
            var sample = Assert.Single(EncryptedSamples.Enumerate(), s => s.Path == DefaultKeyXls);

            Assert.Equal("Encrypted workbook text line one", OpenFirstCell(sample.FullPath, null));
        }

        private static string? OpenFirstCell(string fullPath, string? password)
        {
            Biff8EncryptionKey.CurrentUserPassword = password;
            try
            {
                using var stream = File.OpenRead(fullPath);
                return new HSSFWorkbook(stream).GetSheetAt(0).GetRow(0).GetCell(0).StringCellValue;
            }
            finally
            {
                Biff8EncryptionKey.CurrentUserPassword = null;
            }
        }
    }
}
