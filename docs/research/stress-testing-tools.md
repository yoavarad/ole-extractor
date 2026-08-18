# Stress/Load Testing Tooling for ExtractorOle (Research Spike)

Goal: sustained, concurrent, in-process stress testing of a plain C# library method
(not an HTTP endpoint) on .NET 8, OSS-only. Findings below as of 2026-08-18.

## NBomber

**License.** NBomber's GitHub source (`PragmaticFlow/NBomber`) no longer ships a
root `LICENSE` file (confirmed 404 via GitHub API), and the published NuGet
package is governed by the **NBomber License Agreement v3.0** (effective
2025-09-01). Per the official license page and package terms: NBomber is
**free only for personal use** (hobby projects, tutorials, benchmarks,
educational/review content). **"Any use by, for, or on behalf of an
organization ... requires a valid Commercial Subscription"** with an activation
key. Business/Enterprise tiers exist for company use; Enterprise adds cluster
mode, Kubernetes integration, and scheduled load tests. Older major versions
(v4 and earlier) reportedly used Apache 2.0, but the current, actively
released line (v5+) is source-available/commercial for anything but personal
use. **For a company codebase (ole-extractor), this fails the "OSS only"
hard constraint** — using NBomber here would require purchasing a Business
license.

**Maintenance signal.** Very actively maintained: latest GitHub release is
**v6.6.0 (2026-08-17)**, with releases roughly every 2-6 weeks throughout
2026 (v6.5.0 on 2026-07-15, v6.4.x in May, v6.2.0 in January, etc.). 2,239+
GitHub stars, not archived. This is not a stale project — the licensing
change is deliberate, not neglect.

**API shape for our use case.** NBomber is explicitly protocol-agnostic: a
`Step` can wrap *any* C# async function, not just HTTP — `Step.Run(async ctx
=> { await Extractor.ExtractAsync(...); return Response.Ok(); })`. Scenarios
control concurrency (constant/ramping injection rates, number of parallel
copies) independent of any transport, so calling an in-process library method
under sustained concurrent load is a first-class, well-documented pattern
(not a workaround).

**Strengths.** Built-in percentile reporting (P50/P75/P95/P99, min/mean/max),
RPS/throughput, HTML/console/CSV reports, assertions/thresholds on any
metric, and built-in system metrics including CPU, RAM, and **GC-specific
counters (`gc:loh-size`, `gc:time-in-gc`)** alongside custom
Counter/Gauge metrics. Mature ecosystem, good docs, real distributed/cluster
mode if ever needed.

**Weaknesses/fit gaps.** The licensing blocker is the dominant issue for our
constraint. Secondarily, it's a heavier dependency (whole scenario/reporting
DSL) for what could be a much smaller need.

**Citations.**
- License terms: https://nbomber.com/docs/getting-started/license/
- License agreement v3.0 (NuGet package page): https://www.nuget.org/packages/NBomber/6.6.0/License
- Repo (no LICENSE file at root, confirmed via `gh api repos/PragmaticFlow/NBomber/contents/LICENSE` → 404): https://github.com/PragmaticFlow/NBomber
- Releases: https://github.com/PragmaticFlow/NBomber/releases
- Step docs (arbitrary C# function, not just HTTP): https://nbomber.com/docs/nbomber/scenario/step/
- Reports/metrics docs: https://nbomber.com/docs/reporting/reports/
- License discussion thread: https://github.com/PragmaticFlow/NBomber/issues/596

## Hand-Rolled Harness

**License.** N/A — it's code we own, built entirely on the .NET BCL
(`System.Threading.Tasks`, `System.Diagnostics.Stopwatch`, `GC` APIs) plus
optionally the OSS `dotnet-counters` global tool (part of the MIT-licensed
`dotnet/diagnostics` repo, ships with the .NET SDK tooling ecosystem).

**Maintenance signal.** N/A — no external maintenance risk, but 100% of the
maintenance burden falls on us going forward.

**API shape for our use case.** Ideal fit by construction: a console app that
uses `Parallel.ForEachAsync` (with `MaxDegreeOfParallelism`) or a
`SemaphoreSlim` gate to fire N concurrent calls directly to
`Extractor.ExtractAsync(...)` for a configurable duration/iteration count.
Each call wraps `Stopwatch` timing and `GC.GetAllocatedBytesForCurrentThread()`
before/after for a per-call allocation delta. Latencies collected into a
`List<double>`, sorted, and percentiles computed by index
(`sorted[(int)(sorted.Count * 0.95)]` etc.). `dotnet-counters monitor -p <pid>
System.Runtime` can run alongside for live GC gen0/1/2 counts, heap size, and
thread-pool queue length without any code changes.

**Strengths.** Zero licensing risk, minimal dependency surface, full control
over exactly what's measured (can target extraction-specific concerns like
per-file-type breakdown), trivial to fit into existing test/bench project
conventions, no DSL to learn. Complexity is low: a working version is
roughly 100-150 lines (concurrency loop + Stopwatch + percentile helper),
comparable to a decent xUnit fixture.

**Weaknesses/fit gaps.** No built-in reporting/HTML output — any dashboards
or trend history must be hand-built. Percentile math via sort-and-index is
correct for a single run but doesn't merge cleanly across runs/streaming
data the way a histogram does (see HdrHistogram.NET below). No built-in
ramping load profiles (constant vs. ramp vs. spike) — would need to be added
manually if that nuance matters. We own bugs in the harness itself (e.g., if
the percentile index calculation is off by one, nothing catches it).

**Citations.**
- `dotnet-counters` (OSS, MIT, part of dotnet/diagnostics): https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters
- `Parallel.ForEachAsync` docs: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.foreachasync
- `GC.GetAllocatedBytesForCurrentThread`: https://learn.microsoft.com/en-us/dotnet/api/system.gc.getallocatedbytesforcurrentthread

## HdrHistogram.NET

**License.** Dual-licensed **CC0 (public domain) / BSD 2-Clause** — confirmed
by reading `LICENSE.txt` directly from the repo. Fully permissive OSS, no
constraints for commercial or organizational use. (GitHub's license detector
reports "Other/NOASSERTION" because the dual CC0/BSD text doesn't match a
single SPDX template cleanly, but the underlying terms are unambiguous and
permissive.)

**Maintenance signal.** Mixed signal, worth noting precisely: the **NuGet
package** (`HdrHistogram`, id on nuget.org) has not been republished since
**v2.5.0 on 2017-09-29** — 6 published versions total, nothing since. However,
the **GitHub source repo** (`HdrHistogram/HdrHistogram.NET`, maintained by
Lee Campbell) has recent, real activity: a commit merged **2026-03-30**
dropping `netstandard2.0` in favor of `net8.0`+ support (issue #161/PR #164),
alongside other small fixes in the same window. So the repo is not
abandoned, but the shipped NuGet artifact is stale relative to that work —
if net8.0-specific improvements matter, one may need to build from source or
watch for a new NuGet publish rather than relying on the existing package.

**API shape for our use case.** Simple and purpose-built for exactly this
scenario: create a `LongHistogram(lowestDiscernibleValue, highestTrackableValue,
numberOfSignificantValueDigits)` (or the thread-safe `SynchronizedHistogram`
variant for concurrent writers), call `.RecordValue(elapsedTicks)` per
extraction call from any thread, then call `.GetValueAtPercentile(95.0)`,
`.GetMean()`, etc. after the run, or `.OutputPercentileDistribution(...)` for
a full report. This slots directly into a hand-rolled harness in place of
"sort a List<double> and index into it."

**Strengths.** Purpose-built, memory-bounded histogram (fixed-size buckets
regardless of sample count, unlike storing every raw latency), safe for
concurrent recording via `SynchronizedHistogram`, accurate percentile math
(bucketed, not naive sort), tiny API surface, and it's the same
algorithm/library family widely trusted in Java/Go ecosystems for latency
work — well understood, not exotic.

**Weaknesses/fit gaps.** Stale NuGet package (2017) is a real adoption
friction point even though the license and source are fine; would need to
confirm the old package still behaves correctly on .NET 8 (likely fine, since
it targets netstandard1.3, but not validated by a modern release) or consider
building from source given the March 2026 net8.0 work. No reporting/dashboard
layer — it's a computation primitive only, still needs to be wired into
whatever output format we want.

**Citations.**
- Repo: https://github.com/HdrHistogram/HdrHistogram.NET
- LICENSE.txt (CC0 / BSD 2-Clause dual): https://raw.githubusercontent.com/HdrHistogram/HdrHistogram.NET/master/LICENSE.txt
- NuGet package (last published 2.5.0, 2017-09-29): https://www.nuget.org/packages/HdrHistogram/
- net8.0-only support work (2026-03-30): https://github.com/HdrHistogram/HdrHistogram.NET/issues/161

## Other Candidates Considered

- **k6** (Grafana, AGPL/OSS core): purpose-built for HTTP/gRPC/WebSocket load
  testing with JS-scripted scenarios executed by a Go runtime. There is no
  practical path to have it call an in-process C# method directly — it would
  require standing up a throwaway HTTP/gRPC wrapper around the extraction
  call, which defeats the "plain library call" requirement and adds
  cross-process overhead that would pollute the latency/throughput numbers.
  Not a fit.
- **Crank** (Microsoft, MIT, `dotnet/crank`): a benchmarking orchestration
  tool built around ASP.NET Core scenarios, cross-machine agent/controller
  architecture, and HTTP-centric "job" definitions. It's designed for
  full-application or service-level benchmarking (e.g., comparing ASP.NET
  Core across runtime versions), not for driving concurrent calls into an
  arbitrary in-process library method. Using it here would mean building
  significant scaffolding it doesn't provide out of the box. Not a fit.
- **BenchmarkDotNet** (OSS, MIT): excellent for precise micro-benchmarks
  (mean/error/stddev per single-threaded invocation, memory diagnoser for
  allocations), but it is architecturally aimed at isolated, repeatable
  measurement of a single operation, not sustained many-minutes
  concurrent-load stress scenarios with percentile latency under contention.
  Could complement a stress harness for single-call baseline numbers, but
  doesn't replace it.
- **Conclusion:** for genuine in-process, sustained-concurrency stress
  testing of a plain .NET library call, the realistic OSS options are
  NBomber (blocked here by its license for organizational use) and a
  hand-rolled harness. Nothing else in the OSS .NET ecosystem targets this
  gap directly.

## Complexity to Adopt: NBomber vs. Hand-Rolling

If NBomber's license were not a blocker, it would be the lower-effort choice:
concurrency control, percentile/throughput reporting, and GC metrics all come
for free, at the cost of learning its Scenario/Step DSL and taking on a
larger dependency. Given the license does block organizational use here, the
practical choice is hand-rolling: a `Parallel.ForEachAsync`/`SemaphoreSlim`
loop with `Stopwatch` timing is genuinely small (roughly 100-150 lines) and
well within "minimum code that solves the problem." Pairing it with
HdrHistogram.NET removes the one part of hand-rolling that's easy to get
subtly wrong (percentile computation and per-sample memory growth) for
negligible added complexity, at the cost of depending on a library whose
NuGet package hasn't been refreshed since 2017 (though its permissive license
means vendoring the small amount of relevant source is also a viable
fallback if that staleness becomes a problem). `dotnet-counters` remains a
free, zero-code way to cross-check GC/thread-pool behavior live during any
hand-rolled run.
