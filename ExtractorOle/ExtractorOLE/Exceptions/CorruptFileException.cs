using System;

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
            : this(detectedMimeType, parseFailureReason, null)
        {
        }

        // innerException is the format library's parse exception, when there is one (#287).
        public CorruptFileException(string detectedMimeType, string parseFailureReason, Exception? innerException)
            : base($"File could not be parsed as {detectedMimeType}: {parseFailureReason}", innerException)
        {
            DetectedMimeType = detectedMimeType;
            ParseFailureReason = parseFailureReason;
        }
    }
}
