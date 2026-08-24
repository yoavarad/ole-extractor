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

// Large-body synthetic sample: docs/specs/dataset-curation.md's Corpus Composition
// rule 1's second synthetic variant ("a file with a text body exceeding 100,000
// words"), with multilingual/mixed-script text and emoji in the body and in at
// least 2 metadata fields. BodyText and Metadata are fully deterministic
// (ComposeLargeBody cycles fixed fixtures - no RNG), so the exact expected text
// is stable and recordable in the corpus manifest. Note: the *zip container*
// DocumentFormat.OpenXml produces embeds a wall-clock timestamp and a
// randomly-generated GUID part name per run (System.IO.Packaging internals,
// not controllable via public API), so the raw file bytes are not guaranteed
// identical across regenerations even though the text/metadata content is.
var largeBodyOutputDir = Path.Combine("samples", "synthetic", "large-body");
Directory.CreateDirectory(largeBodyOutputDir);

var largeBodySpec = new SampleSpec
{
    BodyText = MultilingualFixtures.ComposeLargeBody(105_000),
    Metadata = new Dictionary<string, string>
    {
        [MetadataFields.Title] = MultilingualFixtures.Hebrew,
        [MetadataFields.Author] = MultilingualFixtures.Arabic,
        [MetadataFields.Subject] = MultilingualFixtures.Cjk,
        [MetadataFields.Comments] = string.Join(" ", MultilingualFixtures.AllEmoji)
    }
};

foreach (var (format, generator) in generators)
{
    try
    {
        var sample = generator.Generate(largeBodySpec);
        var path = Path.Combine(largeBodyOutputDir, $"large-body{Path.GetExtension(sample.FileName)}");
        File.WriteAllBytes(path, sample.Content);
        Console.WriteLine($"Generated large-body {format}: {path} ({sample.Content.Length} bytes)");
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine($"Skipped large-body {format}: {ex.Message}");
    }
}
