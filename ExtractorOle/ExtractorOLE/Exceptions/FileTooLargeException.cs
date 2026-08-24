namespace ExtractorOLE.Exceptions
{
    public sealed class FileTooLargeException : ExtractorOleException
    {
        public override string Code => "FILE_TOO_LARGE";
        public override int StatusCode => 413;
        public long ActualBytes { get; }
        public long LimitBytes { get; }

        public FileTooLargeException(long actualBytes, long limitBytes)
            : base($"File size {actualBytes} bytes exceeds configured limit of {limitBytes} bytes.")
        {
            ActualBytes = actualBytes;
            LimitBytes = limitBytes;
        }
    }
}
