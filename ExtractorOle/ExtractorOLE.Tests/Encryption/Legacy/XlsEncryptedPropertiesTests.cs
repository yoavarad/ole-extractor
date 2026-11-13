using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy
{
    public class XlsEncryptedPropertiesTests
    {
        private const string XlsMime = "application/vnd.ms-excel";

        private static POIFSFileSystem Open(string fileName)
        {
            var sample = EncryptedSamples.Enumerate().Single(s => string.Equals(Path.GetFileName(s.Path), fileName, StringComparison.Ordinal));
            return new POIFSFileSystem(new MemoryStream(File.ReadAllBytes(sample.FullPath)));
        }

        [Fact]
        public void CryptoApiEncryptedProperties_AppliesToAndHasFilePassHeader()
        {
            using var fs = Open("cryptoapi-encrypted-properties.xls");

            Assert.True(XlsEncryptedProperties.AppliesTo(fs.Root));
            Assert.NotNull(XlsEncryptedProperties.ReadFilePassHeader(fs.Root, XlsMime));
        }

        [Fact]
        public void BinaryRc4Xls_DoesNotApplyAndHasNoCryptoApiHeader()
        {
            using var fs = Open("rc4-password.xls");

            Assert.False(XlsEncryptedProperties.AppliesTo(fs.Root));
            Assert.Null(XlsEncryptedProperties.ReadFilePassHeader(fs.Root, XlsMime));
        }

        [Fact]
        public void PlainWorkbook_HasNoFilePassHeader()
        {
            using var fs = new POIFSFileSystem();
            fs.CreateDocument(new MemoryStream(new byte[] { 0x09, 0x08, 0x04, 0x00, 0, 0, 0, 0, 0x0A, 0x00, 0x00, 0x00 }), "Workbook");

            Assert.False(XlsEncryptedProperties.AppliesTo(fs.Root));
            Assert.Null(XlsEncryptedProperties.ReadFilePassHeader(fs.Root, XlsMime));
        }
    }
}
