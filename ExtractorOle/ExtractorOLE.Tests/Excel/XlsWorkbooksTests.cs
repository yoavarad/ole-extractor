using ExtractorOLE.Helpers.FileTypeStrategy;
using NPOI.HSSF.Record.Crypto;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    /// <summary>
    /// #380: XlsWorkbooks.Load sets the thread-static Biff8EncryptionKey slot immediately before the
    /// synchronous HSSF open and clears it in finally; a password-less open clears it first.
    /// </summary>
    public class XlsWorkbooksTests
    {
        private const string FirstCell = "Encrypted workbook text line one";

        private static EncryptedSamples.EncryptedSample Sample(string fileName) =>
            EncryptedSamples.Enumerate().Single(s => string.Equals(Path.GetFileName(s.Path), fileName, StringComparison.Ordinal));

        [Fact]
        public void Load_WithPassword_ClearsSlotAfterSuccess()
        {
            var sample = Sample("rc4-password.xls");

            using var workbook = XlsWorkbooks.Load(new MemoryStream(File.ReadAllBytes(sample.FullPath)), sample.Password);

            Assert.Equal(FirstCell, workbook.GetSheetAt(0).GetRow(0).GetCell(0).StringCellValue);
            Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
        }

        [Fact]
        public void Load_WithWrongPassword_ClearsSlotAfterThrow()
        {
            var bytes = File.ReadAllBytes(Sample("rc4-password.xls").FullPath);

            Assert.Throws<NPOI.EncryptedDocumentException>(() => XlsWorkbooks.Load(new MemoryStream(bytes), "not-the-password"));
            Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
        }

        [Fact]
        public void Load_WithoutPassword_ClearsAStaleSlotBeforeOpening()
        {
            // A value left on the thread by other code must not apply: the default-key file
            // opens only with the slot null (default key).
            var bytes = File.ReadAllBytes(Sample("default-key.xls").FullPath);
            Biff8EncryptionKey.CurrentUserPassword = "stale";
            try
            {
                using var workbook = XlsWorkbooks.Load(new MemoryStream(bytes), null);

                Assert.Equal(FirstCell, workbook.GetSheetAt(0).GetRow(0).GetCell(0).StringCellValue);
                Assert.Null(Biff8EncryptionKey.CurrentUserPassword);
            }
            finally
            {
                Biff8EncryptionKey.CurrentUserPassword = null;
            }
        }
    }
}
