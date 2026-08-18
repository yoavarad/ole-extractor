# Performance & Stress Testing Tooling — Tradeoffs

Research spike for `ExtractorOle` (.NET 8). Hard constraint: OSS only. This
document synthesizes two detailed findings docs:
[`benchmarkdotnet.md`](./benchmarkdotnet.md) and
[`stress-testing-tools.md`](./stress-testing-tools.md). It presents
tradeoffs for a human to decide from — nothing here is a locked decision.

## 1. Time Profiling: BenchmarkDotNet vs. Alternatives

This section is short because it's largely uncontested.

**BenchmarkDotNet** (MIT license, `dotnet/BenchmarkDotNet`) is the de facto
standard for .NET method-level benchmarking, actively maintained (v0.15.8,
2025-11-30; preview already cut for 0.16.0), and used internally by the .NET
runtime team itself. The only two other candidates found — **NBench**
(dormant since Feb 2020) and **xunit.performance** (abandoned, superseded by
BenchmarkDotNet) — are not viable alternatives. A manual `Stopwatch` harness
is possible but reintroduces exactly the footguns (JIT warmup, iteration
counts, GC mode, statistical rigor) BenchmarkDotNet exists to solve, with no
offsetting benefit for this use case.

It fits the stated need directly:
- `[ParamsSource]` + `[GlobalSetup]` parameterizes one benchmark method over
  all six sample formats (doc/xls/ppt/docx/xlsx/pptx), loading file bytes
  outside the timed region so only the extraction call itself is measured.
- `[MemoryDiagnoser]` gives allocation/Gen0-1-2 data with no extra tooling.
- P50/P95 are **not** shown by default — they require adding
  `StatisticColumn.P50`/`P95` via a `ManualConfig` and raising iteration
  counts so the percentiles are statistically meaningful. This is a one-time
  config cost, not a fit gap.
- Idiomatic structure is a separate `ExtractorOle.Benchmarks` console
  project referencing the library, not benchmarks embedded in the test
  project (different lifecycle: Release-mode, isolated process launches per
  job, own artifacts directory).

**Caveat worth flagging to whoever implements this:** BenchmarkDotNet's
default GC mode is Workstation; if `ExtractorOle` is actually hosted under
Server GC in production, the benchmark project should match that
(`[GcServer(true)]`) or allocation/latency numbers won't reflect real
deployment behavior.

**Lean:** adopt BenchmarkDotNet — there is no real tradeoff to weigh here.

## 2. Stress Testing: NBomber vs. Hand-Rolled Harness

This section has a genuine, non-obvious finding: **NBomber's current
license disqualifies it under the OSS-only constraint for a company
codebase.**

| | NBomber | Hand-Rolled Harness (+ optional HdrHistogram.NET) |
|---|---|---|
| **License** | NBomber License Agreement v3.0 (eff. 2025-09-01): free for *personal use only*; **any organizational use requires a paid Commercial Subscription**. Pre-v5 releases were Apache 2.0, but the current actively-released line is not. | Harness itself: our own code, no license. HdrHistogram.NET: CC0 / BSD-2-Clause, fully permissive. `dotnet-counters`: MIT (part of `dotnet/diagnostics`). |
| **Fits OSS-only constraint?** | **No**, for organizational/company use — this is a hard blocker, not a soft preference. | Yes, unconditionally. |
| **Maintenance** | Very active: v6.6.0 released 2026-08-17, cadence every 2-6 weeks. Not neglected — the licensing change is deliberate. | N/A (ours) / HdrHistogram.NET source repo active through 2026-03-30 (net8.0 support added), though its **NuGet package hasn't been republished since 2017** — usable but stale, vendoring is a fallback if that matters. |
| **Fit for in-process library calls (not HTTP)** | Good, if licensing weren't a blocker: `Step.Run(async ctx => ...)` wraps arbitrary C# functions, not just HTTP — this is a first-class documented pattern, not a workaround. | Ideal by construction — a `Parallel.ForEachAsync`/`SemaphoreSlim`-gated loop calling the extraction method directly is exactly the shape of the need. |
| **Complexity to adopt** | Low effort *if licensed*: concurrency control, reporting, and metrics come built-in, at the cost of learning its Scenario/Step DSL and taking on a larger dependency. | Small and bounded: ~100-150 lines (concurrency loop + `Stopwatch` + percentile helper), comparable to a decent test fixture. Adding HdrHistogram.NET for percentile math is a minor addition, not a complexity jump. |
| **Latency percentile reporting** | Built-in P50/P75/P95/P99, min/mean/max, HTML/console/CSV reports. | Not built-in — must compute manually (sort + index, correct but doesn't merge cleanly across runs) or via HdrHistogram.NET (bucketed histogram, accurate, thread-safe `SynchronizedHistogram` for concurrent writers, well-understood algorithm from the Java/Go ecosystems). |
| **Throughput reporting** | Built-in RPS/throughput metrics. | Trivial to compute manually (calls / elapsed time) — not a meaningful gap. |
| **Memory/GC metrics** | Built-in GC counters (`gc:loh-size`, `gc:time-in-gc`), CPU/RAM. | `GC.GetAllocatedBytesForCurrentThread()` before/after per call for allocation deltas; `dotnet-counters monitor -p <pid> System.Runtime` runs alongside with zero code changes for live GC gen0/1/2, heap size, and thread-pool queue length. Fully adequate, just requires wiring two tools together instead of one. |
| **Ramping/variable load profiles** | Built-in (constant vs. ramping injection rates). | Not built-in; would need manual addition if load-shape nuance (ramp/spike vs. constant) becomes a requirement — not needed for a first version. |
| **Other OSS candidates** | — | k6 (HTTP/gRPC/WebSocket only — no practical way to drive an in-process C# call without an HTTP wrapper, which defeats the point); Microsoft's Crank (ASP.NET-service-oriented, not library-call-oriented); BenchmarkDotNet (good for single-call baselines, not sustained concurrent-load stress). None of these fit better than the two options above. |

**Lean:** hand-roll a small `Parallel.ForEachAsync`/`SemaphoreSlim` +
`Stopwatch` harness, optionally paired with HdrHistogram.NET for percentile
math, cross-checked live with `dotnet-counters` for GC/thread-pool metrics.
This is the only path that satisfies the OSS-only constraint today. NBomber
would otherwise have been the lower-effort choice on pure engineering
grounds — worth revisiting only if a Commercial Subscription becomes
acceptable or NBomber's licensing changes again.

## Summary — If Forced to Pick Today

- **Time profiling:** BenchmarkDotNet. Effectively uncontested; no real
  tradeoff exists.
- **Stress testing:** hand-rolled `Parallel.ForEachAsync` + `Stopwatch`
  harness, optionally + HdrHistogram.NET for percentiles, + `dotnet-counters`
  for live GC/thread-pool visibility. NBomber is otherwise the stronger
  engineering fit but is disqualified today by its license for
  organizational use — this should be flagged explicitly to whoever signs
  off, since it's a licensing/business decision as much as a technical one,
  not just a "no good OSS candidate exists" situation.

These are leans for a human to confirm, not final decisions.
