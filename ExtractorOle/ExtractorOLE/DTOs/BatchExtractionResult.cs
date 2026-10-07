using System;

namespace ExtractorOLE.DTOs
{
    // One item of MainExtractor.ExtractManyAsync (ADR-007): exactly one of Result / Error is set.
    // Index is the item's position in the input sequence; results are returned in that order.
    public sealed class BatchExtractionResult
    {
        public int Index { get; init; }
        public string? FileName { get; init; }
        public DocumentExtractionResult? Result { get; init; }
        public Exception? Error { get; init; }
        public bool Succeeded => Error == null;
    }
}
