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

        internal static HSSFWorkbook Load(Stream stream)
        {
            _loadCount++;
            return new HSSFWorkbook(stream);
        }
    }
}
