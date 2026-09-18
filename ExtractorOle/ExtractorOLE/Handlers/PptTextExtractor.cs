using ExtractorOLE.Helpers.FileTypeStrategy;
using System;

namespace ExtractorOLE.Handlers
{
    // Legacy .ppt (OLE-CFB, PowerPoint 97-2003) flat-text extractor.
    //
    // Per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md), NPOI has no working HSLF entry point
    // for .ppt, so the binary presentation is converted in-process to a real .pptx via
    // b2xtranslator's Ppt module, and that package's text is read by the existing PptxTextExtractor
    // (same slide-body-then-notes flat-text shape, one line per text run). The legacy format stores
    // text as either compressed 8-bit (TextBytesAtom) or UTF-16LE (TextCharsAtom); b2xtranslator
    // decodes both into .NET strings and the OpenXml SDK hands them back verbatim, so no encoding
    // conversion or Unicode normalization is applied anywhere on this path
    // ([ydk:req:extraction/unicode-fidelity]).
    //
    // Returns string.Empty (never null) when the file has no textual content or cannot be converted.
    internal class PptTextExtractor : ITextExtractor
    {
        private readonly PptxTextExtractor _pptxTextExtractor = new PptxTextExtractor();

        public string ExtractText(byte[] fileBytes)
        {
            try
            {
                return _pptxTextExtractor.ExtractText(PptToPptxConverter.Convert(fileBytes));
            }
            catch (Exception)
            {
                // swallow and return empty text on failure
            }

            return string.Empty;
        }
    }
}
