using BenchmarkDotNet.Running;
using ExtractorOLE.Benchmarks;

// Fail fast, before BenchmarkDotNet spawns anything, if the fixed samples are absent.
try
{
    BenchmarkSamples.ResolveDir();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

// No arguments: run everything instead of showing the interactive switcher menu.
BenchmarkSwitcher.FromAssembly(typeof(ExtractionBenchmarks).Assembly).Run(args.Length == 0 ? new[] { "--filter", "*" } : args);
return 0;
