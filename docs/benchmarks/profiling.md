# Profiling workflow

Benchmarks report mean/P95/allocations only. To see *where* the time and allocations go, use the opt-in
EventPipe profiler below. All commands run from the repo root and assume the benchmark samples exist
(`dotnet run -c Release --project SampleGenerator -- --benchmark`).

## 1. Capture a trace for one sample

```
dotnet run -c Release --project ExtractorOle/ExtractorOLE.Benchmarks -- --profile --filter *pptx-10mb*
```

- `--profile` enables `EventPipeProfiler(EventPipeProfile.CpuSampling)`.
- `--profile-alloc` enables `EventPipeProfiler(EventPipeProfile.GcVerbose)` (GC allocation-tick events).
  Use it alone or together with `--profile`.
- Both flags are stripped before BenchmarkDotNet parses the rest of the arguments, so `--filter` etc. work as usual.
  Filter on the `Sample` param value (e.g. `*xls-100mb*`). Several filters may be given space-separated.

Output lands in `BenchmarkDotNet.Artifacts/` (git-ignored), one pair per benchmark case:

```
ExtractorOLE.Benchmarks.Extract10MbBenchmarks.Extract(Sample_ _pptx-10mb_)-<timestamp>.nettrace
ExtractorOLE.Benchmarks.Extract10MbBenchmarks.Extract(Sample_ _pptx-10mb_)-<timestamp>.speedscope.json
```

Profiling perturbs timings; do not compare profiled runs against the baseline in `baseline/`.

## 2. Open the trace

- Flame graph: drop the `.speedscope.json` onto <https://www.speedscope.app> (or `npm i -g speedscope && speedscope <file>`).
  Use the "Left Heavy" / "Sandwich" views to rank self time. Frames named `CPU_TIME` / `UNMANAGED_CODE_TIME`
  are synthetic leaves; attribute them to their parent frame.
- `.nettrace`: open in PerfView (Windows) or Visual Studio (File > Open). Allocation traces (`--profile-alloc`):
  PerfView > "GC Heap Net Mem (Coarse) Sampling" / "GC Heap Alloc Stacks".
- Convert to speedscope yourself: `dotnet tool install -g dotnet-trace`, then
  `dotnet-trace convert <file>.nettrace --format speedscope`.

## 3. Live counters against the StressHarness

```
dotnet tool install -g dotnet-counters
dotnet run -c Release --project ExtractorOle/ExtractorOLE.StressHarness -- 50 200
```

In a second terminal, find the PID and attach:

```
dotnet-counters ps
dotnet-counters monitor --process-id <PID> --counters System.Runtime,ExtractorOLE
```

`System.Runtime` gives CPU, GC heap size, gen0/1/2 collections, allocation rate, thread-pool queue length.
`ExtractorOLE` is the meter from `ExtractionTelemetry` (`extractor.stage.duration`, `extractor.bytes.processed`).
To record a trace from the harness instead: `dotnet-trace collect --process-id <PID> --profile cpu-sampling`.

## 4. Findings

Top self-time frames for the three slowest samples: [profiling-findings.md](profiling-findings.md).
