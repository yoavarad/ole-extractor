using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
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

// Opt-in profiling (docs/benchmarks/profiling.md): --profile = EventPipe CPU sampling (.nettrace),
// --profile-alloc = EventPipe GC verbose (allocation tick events). Stripped before BenchmarkDotNet parses args.
bool cpu = args.Contains("--profile");
bool alloc = args.Contains("--profile-alloc");
args = args.Where(a => a != "--profile" && a != "--profile-alloc").ToArray();

IConfig config = ManualConfig.CreateEmpty();
if (cpu) config = config.AddDiagnoser(new EventPipeProfiler(EventPipeProfile.CpuSampling));
if (alloc) config = config.AddDiagnoser(new EventPipeProfiler(EventPipeProfile.GcVerbose));

// No arguments: run everything instead of showing the interactive switcher menu.
var summaries = BenchmarkSwitcher.FromAssembly(typeof(ExtractionBenchmarks).Assembly).Run(args.Length == 0 ? ["--filter", "*"] : args, config);

// BenchmarkDotNet's Run does not set an exit code; fail on critical validation errors (e.g. build failure, invalid setup).
return summaries.Any(s => s.HasCriticalValidationErrors) ? 1 : 0;
