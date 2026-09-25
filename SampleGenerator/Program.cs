using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;

var services = new ServiceCollection();
ServiceRegistration.Register(services);
var provider = services.BuildServiceProvider();

var generators = provider.GetRequiredService<IDictionary<SampleFormat, ISampleGenerator>>();

var outputDir = args.Length > 0 ? args[0] : "generated-samples";

if (args.Length > 0 && args[0] == "--adversarial")
{
    GenerateAdversarialSamples();
    return;
}

// T-f0963fc3: the 12 fixed benchmark samples (10MB + ~100MB x 6 formats) go to the gitignored
// samples/benchmark/ (or the directory given as the second argument), never into the corpus dirs
// below. Each SHA-256 is checked against samples/manifest.json's benchmarkSamples; exit code 1 on
// any mismatch. Usage: dotnet run --project SampleGenerator -- --benchmark [outputDir]
if (args.Length > 0 && args[0] == "--benchmark")
{
    var benchmarkDir = args.Length > 1 ? args[1] : Path.Combine("samples", "benchmark");
    var ok = BenchmarkSampleGenerator.WriteAll(benchmarkDir, Path.Combine("samples", "manifest.json"));
    Environment.Exit(ok ? 0 : 1);
    return;
}

Directory.CreateDirectory(outputDir);

// docs/specs/dataset-curation.md composition rule #1 ("multi-embedding"):
// at least 3 first-layer embedded objects/media of at least 2 different
// kinds, plus multilingual/mixed-script text and emoji in body text and at
// least 2 metadata fields.
var multiEmbeddingSpec = MultiEmbeddingSampleSpecs.Build();

foreach (var (format, generator) in generators)
{
    try
    {
        var sample = generator.Generate(multiEmbeddingSpec);
        var path = Path.Combine(outputDir, sample.FileName);
        File.WriteAllBytes(path, sample.Content);
        Console.WriteLine($"Generated {format}: {path} ({sample.Content.Length} bytes)");
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine($"Skipped {format}: {ex.Message}");
    }
}

// docs/specs/dataset-curation.md composition rule #3
// (macro-enabled-with-embedding): a macro-enabled variant (docm/xlsm/pptm)
// with at least one macro/VBA storage and at least one first-layer
// embedding, plus multilingual/mixed-script text and emoji in body text
// and at least 2 metadata fields. The legacy doc/xls/ppt generators
// (ADR-005) author the macro storage as a "Macros" / "_VBA_PROJECT_CUR"
// storage inside the same binary file.
var macroEmbedOutputDir = Path.Combine("samples", "synthetic", "macro-embed");
Directory.CreateDirectory(macroEmbedOutputDir);
var macroEmbedSpec = MacroEmbedSampleSpecs.Build();

foreach (var (format, generator) in generators)
{
    try
    {
        var sample = generator.Generate(macroEmbedSpec);
        var path = Path.Combine(macroEmbedOutputDir, sample.FileName);
        File.WriteAllBytes(path, sample.Content);
        Console.WriteLine($"Generated macro-embed {format}: {path} ({sample.Content.Length} bytes)");
    }
    catch (NotSupportedException ex)
    {
        Console.WriteLine($"Skipped macro-embed {format}: {ex.Message}");
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

// docs/specs/dataset-curation.md's "curated real-world-style samples" composition rule
// (2 per format: one with 2+ levels of nested embedding, one with a
// authorship/revision-history property chain). Scoped down to docx/xlsx/pptx here (a
// product-owner decision), plus one legacy .doc nested-embedding sample now that the
// legacy generators exist (ADR-005; T-07571b32). See CuratedSampleSpecs for full rationale.
var curatedSamples = new (SampleFormat Format, string FileName, SampleSpec Spec)[]
{
    (SampleFormat.Doc, "project-proposal-budget.doc", CuratedSampleSpecs.BuildProjectProposalBudget()),
    (SampleFormat.Docx, "meeting-minutes-boardroom.docx", CuratedSampleSpecs.BuildMeetingMinutesBoardroom()),
    (SampleFormat.Docx, "invoice-acme-corp.docx", CuratedSampleSpecs.BuildInvoiceAcmeCorp()),
    (SampleFormat.Xlsx, "project-status-report.xlsx", CuratedSampleSpecs.BuildProjectStatusReport()),
    (SampleFormat.Xlsx, "budget-quarterly-2026.xlsx", CuratedSampleSpecs.BuildBudgetQuarterly2026()),
    (SampleFormat.Pptx, "product-launch-deck.pptx", CuratedSampleSpecs.BuildProductLaunchDeck()),
    (SampleFormat.Pptx, "training-onboarding-slides.pptx", CuratedSampleSpecs.BuildTrainingOnboardingSlides())
};

foreach (var (format, fileName, spec) in curatedSamples)
{
    var curatedFormatDir = Path.Combine("samples", "curated", format.ToString().ToLowerInvariant());
    Directory.CreateDirectory(curatedFormatDir);

    var sample = generators[format].Generate(spec);
    var path = Path.Combine(curatedFormatDir, fileName);
    File.WriteAllBytes(path, sample.Content);
    Console.WriteLine($"Generated curated {format}: {path} ({sample.Content.Length} bytes)");
}

// docs/specs/dataset-curation.md's shared adversarial set: "one
// oversized-declared-size sample (zip/CFB bomb)". Small on disk, but its
// declared internal size (forged FAT sector count) implies well over the
// nesting-depth-guard's 2GB total cap.
var adversarialOutputDir = Path.Combine("samples", "adversarial");
Directory.CreateDirectory(adversarialOutputDir);

var sizeBombPath = Path.Combine(adversarialOutputDir, "oversized-declared-size-bomb.doc");
var sizeBombBytes = AdversarialSampleGenerator.BuildCfbSizeBomb();
File.WriteAllBytes(sizeBombPath, sizeBombBytes);
Console.WriteLine($"Generated adversarial oversized-declared-size-bomb: {sizeBombPath} ({sizeBombBytes.Length} bytes)");

// docs/specs/dataset-curation.md Corpus Composition's shared adversarial set
// (not per-format): one wrong-extension sample (correct content, misleading
// filename extension) and one sample with a single corrupt embedded object
// alongside otherwise-valid content (exercises the per-subfile-failure rule
// in extraction.md). Both are tagged to docx as their target format. Gated
// behind the --adversarial CLI flag so running this generator normally
// doesn't touch these files.
void GenerateAdversarialSamples()
{
    var adversarialDir = Path.Combine("samples", "adversarial");
    Directory.CreateDirectory(adversarialDir);

    // Wrong-extension sample: a real docx package saved with a misleading
    // .txt extension. Content-based MIME detection must still identify it
    // as docx regardless of the filename.
    var wrongExtensionSpec = new SampleSpec
    {
        BodyText = "This is a valid Word document that has been saved with a misleading .txt file extension."
    };
    var wrongExtensionSample = new DocxSampleGenerator().Generate(wrongExtensionSpec);
    var wrongExtensionPath = Path.Combine(adversarialDir, "wrong-extension-sample.txt");
    File.WriteAllBytes(wrongExtensionPath, wrongExtensionSample.Content);
    Console.WriteLine($"Generated adversarial wrong-extension docx: {wrongExtensionPath} ({wrongExtensionSample.Content.Length} bytes)");

    // Corrupt-embedded-object sample: a docx with 3 first-layer embeddings
    // (xlsx, png, and a corrupt-target blob) whose corrupt-target zip entry
    // is bitwise-corrupted after generation. Extraction must succeed and
    // return the xlsx and png, silently omitting the corrupt embedding.
    var corruptSpec = DocxEmbeddedXlsxSampleSpecs.BuildWithCorruptTarget();
    corruptSpec.BodyText = "This document is otherwise valid but contains one embedded object that has been deliberately corrupted.";
    var goodBytes = new DocxSampleGenerator().Generate(corruptSpec).Content;

    string corruptTargetUri;
    using (var ms = new MemoryStream(goodBytes))
    using (var word = WordprocessingDocument.Open(ms, false))
    {
        var corruptPart = word.MainDocumentPart!.Parts
            .Select(p => p.OpenXmlPart)
            .Single(p => p.ContentType == DocxEmbeddedXlsxSampleSpecs.CorruptTargetContentType);
        corruptTargetUri = corruptPart.Uri.ToString();
    }

    var entryName = corruptTargetUri.TrimStart('/');
    var corruptedBytes = ZipEntryCorruptor.CorruptEntryData(goodBytes, entryName);
    var corruptedPath = Path.Combine(adversarialDir, "corrupt-embedded-object.docx");
    File.WriteAllBytes(corruptedPath, corruptedBytes);
    Console.WriteLine($"Generated adversarial corrupt-embedded-object docx: {corruptedPath} ({corruptedBytes.Length} bytes)");
}

// docs/specs/dataset-curation.md's shared adversarial set (not per-format):
// corrupt-container and truncated-container samples, derived from the
// existing valid synthetic multi-embedding docx - T-3bddbe75. A third
// zero-byte sample is also written here since it belongs alongside the
// other two in samples/adversarial/, though it needs no derivation.
var adversarialDir = Path.Combine("samples", "adversarial");
Directory.CreateDirectory(adversarialDir);

var validDocxBytes = File.ReadAllBytes(Path.Combine("samples", "synthetic", "multi-embedding", "sample.docx"));

var corruptContainerBytes = CorruptedContainerSampleGenerator.CorruptCentralDirectory(validDocxBytes);
var corruptContainerPath = Path.Combine(adversarialDir, "corrupt-container.docx");
File.WriteAllBytes(corruptContainerPath, corruptContainerBytes);
Console.WriteLine($"Generated adversarial corrupt-container: {corruptContainerPath} ({corruptContainerBytes.Length} bytes)");

var truncatedContainerBytes = CorruptedContainerSampleGenerator.TruncateKeepingEocd(validDocxBytes, 200);
var truncatedContainerPath = Path.Combine(adversarialDir, "truncated-container.docx");
File.WriteAllBytes(truncatedContainerPath, truncatedContainerBytes);
Console.WriteLine($"Generated adversarial truncated-container: {truncatedContainerPath} ({truncatedContainerBytes.Length} bytes)");

var zeroBytePath = Path.Combine(adversarialDir, "zero-byte.docx");
File.WriteAllBytes(zeroBytePath, []);
Console.WriteLine($"Generated adversarial zero-byte: {zeroBytePath} (0 bytes)");
