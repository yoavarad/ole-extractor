namespace ExtractorOLE
{
    // A text extractor that can also read what its format's open strategy already parsed
    // (DocumentExtractionResult.ParsedDocument), so MainExtractor parses each input once (#168).
    internal interface IParsedDocumentTextExtractor : ITextExtractor
    {
        // Returns null when parsedDocument is not a type this extractor reads; the caller then
        // falls back to ExtractText(byte[]).
        string? ExtractText(object parsedDocument);
    }
}
