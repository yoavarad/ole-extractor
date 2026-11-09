using System;

namespace ExtractorOLE.Abstractions
{
    // A text extractor that can also read what its format's open strategy already parsed
    // (DocumentExtractionResult.ParsedDocument), so MainExtractor parses each input once (#168).
    internal interface IParsedDocumentTextExtractor : ITextExtractor
    {
        // Returns null when parsedDocument is not a type this extractor reads; the caller then
        // falls back to ExtractText(byte[]).
        string? ExtractText(object parsedDocument);

        // Same, but a failure confined to one part (one worksheet) does not discard the text of the
        // other parts: it is passed to onPartFailure and the walk continues (#288); with null, the
        // first failing part fails the whole text. Out-of-memory and
        // cancellation are never passed on; they propagate. Extractors with no independent parts
        // keep the default, which fails as a whole.
        string? ExtractText(object parsedDocument, Action<Exception>? onPartFailure) => ExtractText(parsedDocument);
    }
}
