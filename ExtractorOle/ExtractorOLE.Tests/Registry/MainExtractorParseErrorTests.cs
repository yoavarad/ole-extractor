using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Record.Crypto;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Covers T-61477563: Extract(ExtractionRequest) raises the specific parse-time errors
    /// password-protected, corrupt-file and truncated-container (never a generic exception,
    /// never a silently empty result), using the real DI wiring and the shared adversarial
    /// samples under samples/adversarial.
    /// </summary>
    public class MainExtractorParseErrorTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string DocMime = "application/msword";
        private const string XlsMime = "application/vnd.ms-excel";

        private static MainExtractor BuildExtractor()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        }

        private static byte[] Sample(string fileName)
        {
            var relative = Path.Combine("samples", "adversarial", fileName);
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Could not locate '{relative}' walking up from {AppContext.BaseDirectory}");
        }

        private static T Extract<T>(byte[] bytes, string mime) where T : ExtractorOleException =>
            Assert.Throws<T>(() => BuildExtractor().Extract(
                new ExtractionRequest { FileBytes = bytes, FileName = "fixture", DetectedMimeType = mime }));

        private static byte[] BuildXls(Action<HSSFWorkbook>? configure = null)
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1").CreateRow(0).CreateCell(0).SetCellValue("secret");
            configure?.Invoke(workbook);
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        // ---- truncated-container ----

        [Fact]
        public void ZeroLengthOoxml_ThrowsTruncatedContainer()
        {
            var ex = Extract<TruncatedContainerException>(Sample("zero-byte.docx"), DocxMime);

            Assert.Equal("TRUNCATED_CONTAINER", ex.Code);
            Assert.Equal(0, ex.ActualBytes);
            Assert.Equal(22, ex.ExpectedBytes);
        }

        [Fact]
        public void ZeroLengthLegacy_ThrowsTruncatedContainer_WithCfbHeaderSizeExpected()
        {
            var ex = Extract<TruncatedContainerException>(Array.Empty<byte>(), DocMime);

            Assert.Equal(0, ex.ActualBytes);
            Assert.Equal(512, ex.ExpectedBytes);
        }

        [Fact]
        public void ZipWhoseEocdPointsPastActualBytes_ThrowsTruncatedContainer()
        {
            var bytes = Sample("truncated-container.docx");

            var ex = Extract<TruncatedContainerException>(bytes, DocxMime);

            Assert.Equal(bytes.Length, ex.ActualBytes);
            // Central directory offset 3022 + size 670 + 22-byte EOCD: the original file's length.
            Assert.Equal(3714, ex.ExpectedBytes);
        }

        [Fact]
        public void CfbCutShort_ThrowsTruncatedContainer()
        {
            var full = BuildXls();
            var cut = full[..(full.Length - 600)];

            var ex = Extract<TruncatedContainerException>(cut, XlsMime);

            Assert.Equal(cut.Length, ex.ActualBytes);
            Assert.True(ex.ExpectedBytes > ex.ActualBytes);
        }

        [Fact]
        public void CfbShorterThanItsHeader_ThrowsTruncatedContainer()
        {
            var header = BuildXls()[..100];

            var ex = Extract<TruncatedContainerException>(header, XlsMime);

            Assert.Equal(512, ex.ExpectedBytes);
            Assert.Equal(100, ex.ActualBytes);
        }

        // ---- password-protected ----

        [Fact]
        public void EncryptedOoxmlInCfbWrapper_ThrowsPasswordProtected()
        {
            var ex = Extract<PasswordProtectedException>(Sample("password-protected.docx"), DocxMime);

            Assert.Equal("PASSWORD_PROTECTED", ex.Code);
        }

        [Fact]
        public void EncryptedOoxmlInCfbWrapper_ThrowsPasswordProtected_RegardlessOfClaimedFormat()
        {
            Extract<PasswordProtectedException>(Sample("password-protected.docx"), DocMime);
        }

        [Fact]
        public void PasswordProtectedXls_LibraryEncryptedDocumentException_ThrowsPasswordProtected()
        {
            byte[] encrypted;
            Biff8EncryptionKey.CurrentUserPassword = "secret";
            try
            {
                encrypted = BuildXls();
            }
            finally
            {
                Biff8EncryptionKey.CurrentUserPassword = null;
            }

            Extract<PasswordProtectedException>(encrypted, XlsMime);
        }

        // ---- corrupt-file ----

        [Fact]
        public void ZipWithDestroyedCentralDirectory_ThrowsCorruptFile()
        {
            var ex = Extract<CorruptFileException>(Sample("corrupt-container.docx"), DocxMime);

            Assert.Equal("CORRUPT_FILE", ex.Code);
            Assert.Equal(DocxMime, ex.DetectedMimeType);
            Assert.False(string.IsNullOrWhiteSpace(ex.ParseFailureReason));
        }

        [Fact]
        public void NonContainerBytesClaimedAsOoxml_ThrowsCorruptFile()
        {
            var ex = Extract<CorruptFileException>(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, DocxMime);

            Assert.Equal(DocxMime, ex.DetectedMimeType);
        }

        [Fact]
        public void NonContainerBytesClaimedAsLegacy_ThrowsCorruptFile()
        {
            var ex = Extract<CorruptFileException>(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, DocMime);

            Assert.Equal(DocMime, ex.DetectedMimeType);
        }

        [Fact]
        public void CfbWithGarbageDirectory_ThrowsCorruptFile()
        {
            var bytes = BuildXls();
            // Directory sector start is read from header offset 0x30; overwrite that sector
            // (fully inside the file, so it is corrupt, not truncated).
            var dirSector = BitConverter.ToUInt32(bytes, 0x30);
            var start = (int)((dirSector + 1) * 512);
            Array.Fill(bytes, (byte)0xFF, start, 512);

            Extract<CorruptFileException>(bytes, XlsMime);
        }
    }
}
