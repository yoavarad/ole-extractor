# Benchmark baseline and regression policy

Implements the `test_method` of [ydk:nfr:extraction/extraction-latency] (see ADR-002).

## Committed baseline

`docs/benchmarks/baseline/` holds the BenchmarkDotNet output (CSV + GitHub-markdown tables) for
`Extract()` on the fixed 10MB and ~100MB samples, all 6 formats (P50/P95/allocations).

- Captured: 2026-09-25, Windows 11, .NET 8.0.30 runtime (SDK 9.0.317), X64 RyuJIT AVX2, Release build.
- 10MB tier: 3 warmups, 30 iterations. 100MB tier: 1 warmup, 15 iterations (`RunStrategy.Monitoring`).
- All samples met their P95 targets (500 ms @ 10MB, 5 s @ 100MB); slowest is `xls-100mb` at P95 ~2.30 s.

Regenerate samples and re-run:

```
dotnet run -c Release --project SampleGenerator -- --benchmark
dotnet run -c Release --project ExtractorOle/ExtractorOLE.Benchmarks
```

Results land in `BenchmarkDotNet.Artifacts/results/` (git-ignored). To refresh the baseline, copy the
`*-report.csv` and `*-report-github.md` files into `docs/benchmarks/baseline/` in a dedicated PR that
explains why the baseline moved (intentional performance change, new runtime, new reference hardware).

## Regression rule

A sample regresses when its P95 grows by more than 20% against the committed baseline
(`P95_new > 1.20 * P95_baseline`, compared per `Sample` row in the CSV). Baseline numbers are
hardware-dependent, so the comparison is only meaningful on the machine class used to capture
the baseline; re-baseline when that changes.

The `Benchmarks` workflow (`.github/workflows/benchmarks.yml`) runs on demand only
(Actions → Benchmarks → Run workflow). It regenerates the samples, runs the benchmarks, uploads the
results, and applies this rule via `.github/scripts/compare_benchmarks.py` — as **warnings**, not
failures, because GitHub-hosted runners are noisier than, and differ from, the machine the baseline
was captured on. Treat a warning as a prompt to re-run locally against the baseline.

## Separate from the test gate

The benchmark run is a separate job from the unit/integration test suite (`ExtractorOLE.Tests`).
Benchmarks are not part of `dotnet test` and do not gate the standard test run; the profiling task and
its baseline comparison run on their own (see `docs/specs/testing-strategy.md`, "Time profiling").
