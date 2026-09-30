using DocumentFormat.OpenXml.Packaging;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Single entry point for opening a docx/xlsx/pptx input read-only (#167). The open strategy
    // opens the package once and hands it to the text extractor via
    // DocumentExtractionResult.ParsedDocument; OpenCount lets tests assert that.
    internal static class OpenXmlPackages
    {
        // Per-thread so parallel tests don't see each other's opens; Extract is synchronous.
        [System.ThreadStatic] private static int _openCount;

        internal static int OpenCount => _openCount;

        internal static WordprocessingDocument OpenWord(byte[] fileBytes)
        {
            _openCount++;
            return WordprocessingDocument.Open(new MemoryStream(fileBytes, false), false);
        }

        internal static SpreadsheetDocument OpenSpreadsheet(byte[] fileBytes)
        {
            _openCount++;
            return SpreadsheetDocument.Open(new MemoryStream(fileBytes, false), false);
        }

        internal static PresentationDocument OpenPresentation(byte[] fileBytes)
        {
            _openCount++;
            return PresentationDocument.Open(new MemoryStream(fileBytes, false), false);
        }
    }
}
