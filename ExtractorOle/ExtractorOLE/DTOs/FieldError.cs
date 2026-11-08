namespace ExtractorOLE.DTOs
{
    // [ydk:entity:extraction/FieldError]: one leaf of the result that could not be populated
    // while the call as a whole succeeded. FieldPath uses the result's own property names
    // ("Metadata.Created", "EmbeddedFiles[word/embeddings/oleObject1.bin]"). Message is for
    // display only; it never carries the originating exception's type, message or stack.
    public sealed record FieldError(string FieldPath, FieldErrorCode Code, string Message);

    // Closed vocabulary, chosen by cause, not by leaf. CorruptFile and OversizedNestedContent
    // share their codes with the thrown errors; UnexpectedFieldFailure is the residual bucket
    // and is never thrown.
    public enum FieldErrorCode
    {
        CorruptFile,
        OversizedNestedContent,
        UnexpectedFieldFailure,
    }
}
