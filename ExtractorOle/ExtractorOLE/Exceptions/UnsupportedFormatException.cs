namespace ExtractorOLE.Exceptions
{
    // [ydk:error:extraction/unsupported-format]
    public sealed class UnsupportedFormatException : ExtractorOleException
    {
        public override string Code => "UNSUPPORTED_FORMAT";
        public override int StatusCode => 415;
        public string DetectedMimeType { get; }

        public UnsupportedFormatException(string? detectedMimeType)
            : base($"Mime type '{detectedMimeType}' is not a supported format for extraction.")
        {
            DetectedMimeType = detectedMimeType ?? string.Empty;
        }
    }
}
