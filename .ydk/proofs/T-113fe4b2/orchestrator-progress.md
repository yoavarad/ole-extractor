# T-113fe4b2 Orchestrator Progress

## Status: STARTED - investigation phase

## Done
- Ran `ydk task start T-113fe4b2` from main repo root -> worktree created at
  C:\Users\yoava\Projects\ole-extractor\.ydk\worktrees\T-113fe4b2
  on branch task/T-113fe4b2-report-throughputlatencymemory
  Confirmed via `git status`/`git branch --show-current` inside the worktree (clean, correct branch).
- Spawned cavecrew-investigator (agentId a38e0fa4c3d7ad68a) to survey:
  - ExtractorOle/ExtractorOLE.StressHarness/LoadGenerator.cs + Program.cs (current state, existing metrics hooks)
  - the two public extraction functions (Extract, DetectMimeType) and any static/shared mutable state in the
    library (format registry, ExtractionHelper, DI singleton vs transient registrations)
  - .ydk/tasks/T-113fe4b2.md, docs/adrs/002-profiling-and-stress-testing.md, docs/specs/testing-strategy.md
  - whether an existing benchmark project (T-b62ffe04, in-progress) already covers latency/allocation so I don't duplicate

## Done (cont.)
- Investigator findings received: harness logic all lives in ExtractorOle/ExtractorOLE.StressHarness/Program.cs
  (no separate LoadGenerator.cs despite briefing assumption). Already has throughput + latency percentiles
  (Program.cs:114-150). MISSING: memory-under-load reporting, and correctness verification of concurrent
  results (currently only counts exceptions, never checks results are actually correct).
  Two public functions: MainExtractor.Extract (MainExtractor.cs:30) and
  IExtractionHelper.DetectMimeTypeFromBytes (ExtractionHelper.cs:284). Extract() calls DetectMimeTypeFromBytes
  internally, so the harness (calling only Extract) already indirectly exercises the mime-detection path
  including its static mutable counters (OoxmlMimeDetector.PackageOpenAttemptCount,
  CfbMimeDetector.OpenMcdfParseAttemptCount) under concurrency - decided NOT in scope to separately wire a
  standalone DetectMimeType call (that gap belongs to T-96b5181b's own AC, not this task).
  Existing benchmark project ExtractorOLE.Benchmarks (BenchmarkDotNet, [MemoryDiagnoser]) is a DIFFERENT
  per-format micro-benchmark tool, not to be confused with/duplicated by this stress harness.
  ADR-002 confirms: memory/GC visibility via GC.GetAllocatedBytesForCurrentThread is the suggested optional
  approach; report format is ours to design.
  samples/curated/docx/ has 4 real .docx files - good samplesDir arg for a real "different files" evidence run.
- Designed plan: (A) baseline-vs-concurrent correctness check (call Extract once per sample before the load
  run, compare every concurrent result's MimeType+ExtractedText against its sample's baseline, count
  mismatches) to give real evidence for AC2/AC3, not just "no exceptions". (B) memory-under-load reporting via
  GC.GetTotalAllocatedBytes/GetTotalMemory/CollectionCount deltas + Process.PeakWorkingSet64. (C) extend
  PrintSummary with new "Memory under load" and "Shared-state / correctness check" console sections;
  non-zero Environment.ExitCode if mismatches > 0.
- Spawned cavecrew-builder (agentId ad80b26610d995d19) with full precise spec to implement A/B/C, single file
  ExtractorOle/ExtractorOLE.StressHarness/Program.cs only, in the worktree.

## Not yet done
- Awaiting builder result (async, notification pending)
- Review diff (cavecrew-reviewer)
- Run dotnet build/test + actually RUN the stress harness for real throughput/latency/memory numbers
  (use samples/curated/docx as samplesDir, concurrency higher than default e.g. 64, for real evidence),
  capture output as proof under .ydk/proofs/T-113fe4b2/
- `ydk task done T-113fe4b2` FROM INSIDE THE WORKTREE (cd .ydk/worktrees/T-113fe4b2 first, verify branch)
- Verify PR via `gh pr view --json changedFiles,files,additions,deletions` for non-trivial diff

## CRITICAL REMINDER
ALWAYS cd into C:\Users\yoava\Projects\ole-extractor\.ydk\worktrees\T-113fe4b2 before any git/ydk/dotnet command
for this task. Verify with git status/git branch before `ydk task done`.

## Exact next step
Wait for investigator agentId a38e0fa4c3d7ad68a notification, then design the metrics/report plan and spawn
the developer subagent (builder or general-purpose depending on scope) with the worktree path explicitly stated.
