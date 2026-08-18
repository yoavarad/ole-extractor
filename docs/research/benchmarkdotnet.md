# BenchmarkDotNet Research Spike

## Tool Name

**BenchmarkDotNet** — `dotnet/BenchmarkDotNet` on GitHub, distributed as the `BenchmarkDotNet` NuGet package (plus optional satellites like `BenchmarkDotNet.Diagnostics.Windows`).

## License

MIT License (confirmed via `LICENSE.md` in the repo root: "The MIT License"). Fully OSS-compatible with a closed or open .NET 8 project; no copyleft or attribution obligations beyond standard MIT notice retention.

## Maintenance Signal

- Latest stable release: **v0.15.8**, published to NuGet **2025-11-30**. A `0.16.0-preview.1` was already published 2026-06-30, indicating an active next-version pipeline.
- Release cadence over the last ~14 months has been roughly monthly to bi-monthly: 0.15.0 (2025-05-22) → 0.15.1 (06-09) → 0.15.2 (06-16) → 0.15.3 (09-17) → 0.15.4 (09-24) → 0.15.5 (10-30) → 0.15.6 (11-05) → 0.15.8 (11-30).
- Recent releases add substantive features (Roslyn analyzers to catch incorrect usage at compile time, OpenMetrics/Prometheus exporter, .NET 10 runtime support, WakeLock), not just point fixes — a sign of an actively developed project, not one in maintenance-only mode.
- It is a `dotnet` GitHub org project (same org as the .NET runtime/SDK), used internally by the .NET team, ASP.NET Core, EF Core, and most major .NET OSS libraries for their own perf regression tracking. It is the de facto standard.
- Package has ~76M+ total NuGet downloads at ~60K/day, reinforcing broad, ongoing usage.

Conclusion: actively maintained as of 2026, low risk of abandonment.

## API Shape For Our Use Case

Typical setup for benchmarking `ExtractMetadata(byte[] fileBytes)` across multiple formats (doc/xls/ppt/docx/xlsx/pptx):

```csharp
[MemoryDiagnoser]
[SimpleJob] // or a Job config specifying runtime/iterations
public class ExtractMetadataBenchmarks
{
    private byte[] _fileBytes = null!;

    [ParamsSource(nameof(SampleFiles))]
    public string FileName { get; set; } = "";

    public IEnumerable<string> SampleFiles => new[]
    {
        "sample.doc", "sample.xls", "sample.ppt",
        "sample.docx", "sample.xlsx", "sample.pptx"
    };

    [GlobalSetup]
    public void Setup() =>
        _fileBytes = File.ReadAllBytes(Path.Combine("SampleFiles", FileName));

    [Benchmark]
    public Metadata ExtractMetadata() => Extractor.ExtractMetadata(_fileBytes);
}
```

Key attributes/behaviors:
- `[ParamsSource]` (or `[ArgumentsSource]` for richer/parameterized inputs, e.g. passing byte arrays directly rather than filenames) parameterizes the same `[Benchmark]` method over every sample file, producing one row per format in the results table. `[ArgumentsSource]` historically had a compatibility bug when combined with `[MemoryDiagnoser]` (GH issue #1813); using `[ParamsSource]` with file-loading in `[GlobalSetup]` sidesteps this and is the more common pattern for "same method, N fixture files."
- `[GlobalSetup]` runs once per parameter combination, *outside* the measured region — this is where `File.ReadAllBytes` belongs, so file I/O is never counted in the timed loop, only the extraction call itself.
- `[MemoryDiagnoser]` attribute adds Gen0/Gen1/Gen2 collection counts and **Allocated bytes** columns to the report — this is how allocation tracking is obtained; no separate tool needed.
- Default report columns are `Mean`, `Error`, `StdDev` (and `Median`/`Ratio` depending on config) — **percentiles are not shown by default**. To get P50/P95, you must explicitly add `StatisticColumn.P50` / `StatisticColumn.P95` (predefined `StatisticColumn.P0`...`P100` columns) via a `ManualConfig`/`[Config]` class calling `AddColumn(...)`. BenchmarkDotNet's docs note you should raise `IterationCount` to ~10-20+ so percentiles are computed from enough samples to be meaningful.
- Entry point is `BenchmarkRunner.Run<ExtractMetadataBenchmarks>()` in a console `Main`.

## Structure As A Separate Project

Confirmed idiomatic: a dedicated console project (commonly named `*.Benchmarks`, e.g. `ExtractorOle.Benchmarks`) that references the library under test, with `BenchmarkRunner.Run(...)` in `Program.cs`. This is the pattern used throughout the BenchmarkDotNet docs/samples and across the .NET ecosystem (runtime, EF Core, etc.). Reasons it's kept separate from the unit-test project rather than embedded:
- Benchmarks must run in **Release** configuration with an actual process launch (BenchmarkDotNet spawns isolated toolchain processes per job); test projects are typically run in Debug/CI test-runner contexts, which would produce misleading numbers.
- Test frameworks (xUnit/NUnit/MSTest) and BenchmarkDotNet have different lifecycles and CLI entry points; mixing them risks the test runner trying to execute `[Benchmark]` methods as tests.
- A separate project keeps benchmark result artifacts (`BenchmarkDotNet.Artifacts/`) and reference-file fixtures isolated from the test suite's own output.

## Strengths

- Handles JIT warmup, iteration counts, and outlier removal automatically — removes most footguns of a hand-rolled `Stopwatch` harness.
- Built-in `MemoryDiagnoser` gives allocation data for free; no separate profiler required for a first pass.
- Exports to Markdown/CSV/HTML, which is straightforward to wire into CI for historical trend tracking (e.g. archiving results per commit).
- Roslyn analyzer (added in recent releases) flags common misuse (e.g. missing `[Benchmark]`, non-public benchmark classes) at compile time.

## Weaknesses/Fit Gaps

- Percentile columns (P50/P95) require explicit config — not the default report — and need a higher `IterationCount` to be statistically meaningful, adding runtime cost to CI benchmark runs.
- Each benchmark invocation launches a separate process per job/runtime combination, so a full run across 6 file formats × warmup/iteration counts takes materially longer than a simple timed loop — not ideal for a fast pre-commit hook; better suited to a periodic/nightly job or manual invocation.
- `[GlobalSetup]` placement is easy to get wrong for byte-array-in-memory scenarios: if file loading is accidentally left inside `[Benchmark]`, results silently include I/O overhead. This is a correctness responsibility of whoever writes the benchmark, not something BenchmarkDotNet prevents outright (though the analyzer helps with some misuse patterns).
- Consider `Server GC` (`<ServerGarbageCollection>true</ServerGarbageCollection>` or a `[GcServer(true)]` job modifier) if the extractor's real deployment target runs Server GC — Workstation GC (BenchmarkDotNet's default) can show different allocation/pause behavior than production, so the benchmark project's GC mode should match how `ExtractorOle` is actually hosted.

## Alternatives Considered

BenchmarkDotNet is essentially uncontested for .NET micro/method-level benchmarking today:

| Option | Status | Verdict |
|---|---|---|
| **BenchmarkDotNet** | MIT, active (last release 2025-11-30, preview already cut 2026-06-30) | Recommended |
| **NBench** (petabridge) | Apache 2.0, but latest release is v2.0.1 from **February 2020** — no activity since; built for Akka.NET's macro-level perf-regression assertions rather than fine-grained per-call timing | Not viable — effectively dormant |
| **xunit.performance** | Microsoft-authored, but deprecated/archived years ago in favor of BenchmarkDotNet itself | Not viable — abandoned |
| **Manual `Stopwatch` harness** | No dependency, full control | Only worth it for a single ad-hoc measurement; lacks warmup handling, statistical rigor, GC/allocation tracking, and reproducible reporting that this spike explicitly needs (P50/P95 + allocations across formats) |

No genuine competitor exists in the OSS .NET space at BenchmarkDotNet's maturity level; it should be adopted directly rather than treated as one option among several.

## Citations/Links

- Repo: https://github.com/dotnet/BenchmarkDotNet
- License file: https://github.com/dotnet/BenchmarkDotNet/blob/master/LICENSE.md
- Releases: https://github.com/dotnet/BenchmarkDotNet/releases
- NuGet package (version/date history): https://www.nuget.org/packages/BenchmarkDotNet
- Parameterization docs (`[Params]`/`[ParamsSource]`/`[ArgumentsSource]`): https://benchmarkdotnet.org/articles/features/parameterization.html
- Statistics/columns docs: https://benchmarkdotnet.org/articles/features/statistics.html and https://benchmarkdotnet.org/articles/configs/columns.html
- Percentile sample: https://benchmarkdotnet.org/articles/samples/IntroPercentiles.html
- `ArgumentsSource` + `MemoryDiagnoser` compatibility issue: https://github.com/dotnet/BenchmarkDotNet/issues/1813
- NBench: https://github.com/petabridge/NBench/releases, https://www.nuget.org/packages/NBench/
