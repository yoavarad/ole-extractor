namespace ExtractorOLE.Exceptions
{
    public sealed class OversizedNestedContentException : ExtractorOleException
    {
        public override string Code => "OVERSIZED_NESTED_CONTENT";
        public override int StatusCode => 422;
        public string DeclaredMetric { get; }

        public OversizedNestedContentException(string declaredMetric)
            : base($"Rejected: declared internal size/nesting ({declaredMetric}) exceeds configured limits.")
        {
            DeclaredMetric = declaredMetric;
        }
    }
}
