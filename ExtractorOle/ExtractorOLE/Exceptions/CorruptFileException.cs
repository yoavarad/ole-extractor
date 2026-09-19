namespace ExtractorOLE.Exceptions
{
    // [ydk:error:extraction/corrupt-file]
    public sealed class CorruptFileException : ExtractorOleException
    {
        public override string Code => "CORRUPT_FILE";
        public override int StatusCode => 422;
        public string DetectedMimeType { get; }
        public string ParseFailureReason { get; }

        public CorruptFileException(string detectedMimeType, string parseFailureReason)
            : base($"File could not be parsed as {detectedMimeType}: {parseFailureReason}")
        {
            DetectedMimeType = detectedMimeType;
            ParseFailureReason = parseFailureReason;
        }
    }
}
