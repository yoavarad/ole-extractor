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

// docs/specs/dataset-curation.md composition rule #1 ("multi-embedding"):
// at least 3 first-layer embedded objects/media of at least 2 different
// kinds, plus multilingual/mixed-script text and emoji in body text and at
// least 2 metadata fields.
var spec = MultiEmbeddingSampleSpecs.Build();

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
