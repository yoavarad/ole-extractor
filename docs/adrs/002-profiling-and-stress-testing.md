# ADR-002: Time profiling and stress testing approach

## Status
Accepted

## Context
Need (1) time profiling to measure per-format extraction latency/allocations and catch regressions, and (2) a stress-test mechanism to assess behavior under concurrent load. Research spike (`docs/research/tooling-tradeoffs.md`, `benchmarkdotnet.md`, `stress-testing-tools.md`) evaluated OSS candidates.

## Decision
- **Time profiling**: `BenchmarkDotNet` (MIT) — as a separate benchmark project referencing the extraction library.
- **Stress testing**: hand-rolled console harness — `Parallel.ForEachAsync`/`SemaphoreSlim` for concurrency control + `Stopwatch` for timing, optionally augmented with `HdrHistogram.NET` for latency percentiles and `dotnet-counters`/`GC.GetAllocatedBytesForCurrentThread` for memory/GC visibility.

## Alternatives Considered
- **NBomber** for stress testing: rejected — its current license (v3.0, effective 2025-09-01) requires a paid Commercial Subscription for any organizational use, which fails the project's OSS-only constraint. It would otherwise have been the stronger engineering fit (built-in scenario/step model, reporting) — this is a licensing rejection, not a technical one.
- Any BenchmarkDotNet alternative: none found worth weighing — effectively uncontested for .NET micro-benchmarking.

## Consequences
- Stress-test harness is bespoke, not off-the-shelf — more implementation work than adopting NBomber would have been, and its output format/reporting is ours to design (tracked in the Stress test harness task).
- If the OSS-only constraint is ever relaxed, NBomber remains the stronger option to revisit.
