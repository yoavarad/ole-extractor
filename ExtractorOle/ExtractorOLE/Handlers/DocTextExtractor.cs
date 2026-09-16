namespace ExtractorOLE.Handlers
{
    // Legacy .doc (OLE-CFB, Word 97-2003) text extractor.
    //
    // Per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md), body-text extraction for this
    // format requires the b2xtranslator .doc->.docx conversion path, which is not wired into
    // this repo yet (NPOI's HWPFDocument, the alternative, lives only in NPOI's scratchpad
    // tree and is not self-compiled here). Registered now - returning empty text - so the DI
    // dispatch registry ([ydk:req:extraction/dependency-injection]) has a real entry for
    // OfficeMimeTypeEnum.WordLegacy and MainExtractor's dispatch is uniform across formats,
    // rather than falling through to "no text extractor registered".
    internal class DocTextExtractor : ITextExtractor
    {
        public string ExtractText(byte[] fileBytes) => string.Empty;
    }
}
