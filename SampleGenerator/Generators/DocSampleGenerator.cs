using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Placeholder for legacy .doc authoring. Legacy OLE authoring needs NPOI
    /// compiled from source (docs/adrs/001-extraction-library-stack.md) -
    /// that build step is not wired into this repo yet (docs/project-rules.md,
    /// "Known Gotchas"). This stub keeps the ISampleGenerator contract
    /// identical across all six formats so swapping in a real NPOI-backed
    /// implementation later is a drop-in change, not an API reshape.
    /// </summary>
    public sealed class DocSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Doc;

        public GeneratedSample Generate(SampleSpec spec)
        {
            throw new NotSupportedException(
                "Doc generation requires NPOI compiled from source (ADR-001); not yet wired into this build. " +
                "See docs/adrs/001-extraction-library-stack.md.");
        }
    }
}
