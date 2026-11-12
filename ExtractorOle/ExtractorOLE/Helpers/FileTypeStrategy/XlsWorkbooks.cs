using NPOI.HSSF.Record.Crypto;
using NPOI.HSSF.UserModel;
using System.IO;
using System;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Single entry point for loading an HSSFWorkbook so tests can assert a legacy .xls is
    // parsed once per Extract() (XlsOpenStrategy hands it to XlsTextExtractor); LoadCount
    // counts loads on the calling thread.
    internal static class XlsWorkbooks
    {
        // Per-thread so tests running in parallel do not see each other's loads; Extract() is synchronous.
        [ThreadStatic]
        private static int _loadCount;

        internal static int LoadCount => _loadCount;

        internal static HSSFWorkbook Load(Stream stream) => Load(stream, null);

        // NPOI reads the xls password from the thread-static Biff8EncryptionKey slot while the
        // constructor decrypts the records. It is set immediately before the synchronous open and
        // cleared in finally on the same thread; a password-less open clears it first, so a value
        // left on this thread by other code never applies ([ydk:req:extraction/password-decryption]).
        // A null slot means Excel's default key (VelvetSweatshop).
        internal static HSSFWorkbook Load(Stream stream, string? password)
        {
            _loadCount++;
            Biff8EncryptionKey.CurrentUserPassword = RequestPasswords.HasPassword(password) ? password : null;
            try
            {
                // Same as HSSFWorkbook(Stream) (root directory, nodes preserved), but stream input is read lazily (#236).
                return new HSSFWorkbook(CfbFileSystems.Open(stream));
            }
            finally
            {
                Biff8EncryptionKey.CurrentUserPassword = null;
            }
        }
    }
}
