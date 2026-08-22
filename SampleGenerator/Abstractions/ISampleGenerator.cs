namespace SampleGenerator.Abstractions
{
    /// <summary>
    /// Authors one synthetic sample file for a single format. Implementations
    /// live under Generators/ - one per <see cref="SampleFormat"/> - so adding
    /// a new format is a drop-in class plus a registration, never a reshape of
    /// this contract.
    /// </summary>
    public interface ISampleGenerator
    {
        SampleFormat Format { get; }

        GeneratedSample Generate(SampleSpec spec);
    }
}
