using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Placeholder for legacy .ppt authoring. See DocSampleGenerator for why:
    /// legacy OLE authoring needs NPOI compiled from source
    /// (docs/adrs/001-extraction-library-stack.md), which is not wired into
    /// this build yet.
    /// </summary>
    public sealed class PptSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Ppt;

        public GeneratedSample Generate(SampleSpec spec)
        {
            throw new NotSupportedException(
                "Ppt generation requires NPOI compiled from source (ADR-001); not yet wired into this build. " +
                "See docs/adrs/001-extraction-library-stack.md.");
        }
    }
}
