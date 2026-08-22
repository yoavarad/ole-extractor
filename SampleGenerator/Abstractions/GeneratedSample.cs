namespace SampleGenerator.Abstractions
{
    /// <summary>The bytes and suggested filename produced by an <see cref="ISampleGenerator"/>.</summary>
    public class GeneratedSample
    {
        public string FileName { get; set; } = string.Empty;

        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}
