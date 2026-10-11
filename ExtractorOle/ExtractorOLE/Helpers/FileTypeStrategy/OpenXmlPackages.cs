using DocumentFormat.OpenXml.Packaging;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Single entry point for opening a docx/xlsx/pptx input read-only (#167). The open strategy
    // opens the package once and hands it to the text extractor via
    // DocumentExtractionResult.ParsedDocument; OpenCount lets tests assert that.
    // Stream overloads (#192, ADR-008) read a seekable stream lazily; the package does not own the
    // stream, so the caller disposes it after disposing the package.
    internal static class OpenXmlPackages
    {
        // Per-thread so parallel tests don't see each other's opens; Extract is synchronous.
        [System.ThreadStatic] private static int _openCount;

        internal static int OpenCount => _openCount;

        internal static WordprocessingDocument OpenWord(byte[] fileBytes) => OpenWord(new MemoryStream(fileBytes, false));

        internal static WordprocessingDocument OpenWord(Stream input)
        {
            _openCount++;
            return WordprocessingDocument.Open(input, false);
        }

        internal static SpreadsheetDocument OpenSpreadsheet(byte[] fileBytes) => OpenSpreadsheet(new MemoryStream(fileBytes, false));

        internal static SpreadsheetDocument OpenSpreadsheet(Stream input)
        {
            _openCount++;
            return SpreadsheetDocument.Open(input, false);
        }

        internal static PresentationDocument OpenPresentation(byte[] fileBytes) => OpenPresentation(new MemoryStream(fileBytes, false));

        internal static PresentationDocument OpenPresentation(Stream input)
        {
            _openCount++;
            return PresentationDocument.Open(input, false);
        }
    }
}
