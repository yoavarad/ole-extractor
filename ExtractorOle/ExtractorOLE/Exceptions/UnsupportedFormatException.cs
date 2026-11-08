namespace ExtractorOLE.Exceptions
{
    // [ydk:error:extraction/unsupported-format]
    public sealed class UnsupportedFormatException : ExtractorOleException
    {
        public override string Code => "UNSUPPORTED_FORMAT";
        public override int StatusCode => 415;
        public string DetectedMimeType { get; }

        private const int MaxEchoLength = 100;

        public UnsupportedFormatException(string? detectedMimeType)
            : base($"Mime type '{Cap(detectedMimeType)}' is not a supported format for extraction.")
        {
            DetectedMimeType = Cap(detectedMimeType);
        }

        private static string Cap(string? value) =>
            value is null ? string.Empty
            : value.Length <= MaxEchoLength ? value
            : value.Substring(0, MaxEchoLength);
    }
}
