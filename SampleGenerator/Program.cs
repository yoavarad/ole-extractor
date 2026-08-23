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

// docs/specs/dataset-curation.md composition rule #3
// (macro-enabled-with-embedding): a macro-enabled variant (docm/xlsm/pptm)
// with at least one macro/VBA storage and at least one first-layer
// embedding, plus multilingual/mixed-script text and emoji in body text
// and at least 2 metadata fields. Legacy doc/xls/ppt macro authoring is a
// known, intentional gap (ADR-001; NPOI not yet wired into this build) -
// see DocSampleGenerator/XlsSampleGenerator/PptSampleGenerator.
var spec = MacroEmbedSampleSpecs.Build();

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
