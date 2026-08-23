using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Placeholder for legacy .xls authoring. See DocSampleGenerator for why:
    /// legacy OLE authoring needs NPOI compiled from source
    /// (docs/adrs/001-extraction-library-stack.md), which is not wired into
    /// this build yet.
    /// This also means the xls corpus's macro-enabled-with-embedding
    /// synthetic sample (dataset-curation.md composition rule #3) is a
    /// known, intentional gap for this format until NPOI is wired in.
    /// </summary>
    public sealed class XlsSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Xls;

        public GeneratedSample Generate(SampleSpec spec)
        {
            throw new NotSupportedException(
                "Xls generation requires NPOI compiled from source (ADR-001); not yet wired into this build. " +
                "See docs/adrs/001-extraction-library-stack.md.");
        }
    }
}
