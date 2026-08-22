using Microsoft.Extensions.DependencyInjection;
using SampleGenerator;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;

var services = new ServiceCollection();
ServiceRegistration.Register(services);
var provider = services.BuildServiceProvider();

var generators = provider.GetRequiredService<IDictionary<SampleFormat, ISampleGenerator>>();

var outputDir = args.Length > 0 ? args[0] : "generated-samples";
Directory.CreateDirectory(outputDir);

var pixelPng = Convert.FromBase64String(
    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

var spec = new SampleSpec
{
    BodyText = MultilingualFixtures.ComposeMixedBody(),
    Metadata = new Dictionary<string, string>
    {
        [MetadataFields.Title] = MultilingualFixtures.Hebrew,
        [MetadataFields.Author] = MultilingualFixtures.Arabic,
        [MetadataFields.Subject] = MultilingualFixtures.Cjk,
        [MetadataFields.Comments] = string.Join(" ", MultilingualFixtures.AllEmoji)
    },
    Embeddings = new List<EmbeddedContentSpec>
    {
        new() { FileName = "pixel.png", Content = pixelPng, ContentType = "image/png" }
    }
};

foreach (var (format, generator) in generators)
{
    try
    {
        var sample = generator.Generate(spec);
        var path = Path.Combine(outputDir, sample.FileName);
        File.WriteAllBytes(path, sample.Content);
        Console.WriteLine($"Generated {format}: {path} ({sample.Content.Length} bytes)");
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine($"Skipped {format}: {ex.Message}");
    }
}
