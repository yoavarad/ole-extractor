namespace ExtractorOLE.Exceptions
{
    // [ydk:error:extraction/truncated-container]
    public sealed class TruncatedContainerException : ExtractorOleException
    {
        public override string Code => "TRUNCATED_CONTAINER";
        public override int StatusCode => 422;
        public long ExpectedBytes { get; }
        public long ActualBytes { get; }

        public TruncatedContainerException(long expectedBytes, long actualBytes)
            : base($"File appears truncated: expected {expectedBytes} bytes of container structure, got {actualBytes}.")
        {
            ExpectedBytes = expectedBytes;
            ActualBytes = actualBytes;
        }
    }
}
