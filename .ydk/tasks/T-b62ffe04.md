---
id: T-b62ffe04
title: Implement Extract() latency/allocation benchmarks per format
story: S-709fe2de
status: done
assignee: null
labels:
- in-review
dependencies:
- T-fb06300e
- T-71ab3bc0
- T-f0963fc3
spec_refs:
- docs/specs/testing-strategy.md
- docs/adrs/002-profiling-and-stress-testing.md
component_refs:
- ydk:nfr:extraction/extraction-latency
- ydk:nfr:extraction/memory-ceiling
test_strategy: "No test \u2014 this task IS the profiling mechanism referenced by\
  \ testing-strategy.md's Time Profiling test type, run separately from the unit/integration\
  \ suite gate."
acceptance_criteria:
- text: BenchmarkDotNet project runs Extract() against a fixed 10MB and a fixed ~100MB
    sample for each of the 6 formats
  done: true
- text: Each run reports P50/P95 latency and allocated-bytes-per-input-byte
  done: true
- text: 10MB benchmarks target P95 < 500ms and 100MB benchmarks target P95 < 5s, per
    extraction-latency's target
  done: true
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:23Z'
updated: '2026-09-20T08:10:38Z'
---

## Description

Implement BenchmarkDotNet benchmarks running Extract() against a fixed 10MB and a fixed ~100MB sample for each of the 6 formats, reporting P50/P95 latency and allocated bytes, per extraction-latency and memory-ceiling.


## Activity Log
### 2026-09-15T20:57:17Z (UTC)
**Blocked (dependency-gap):** Acceptance criteria require benchmarking Extract() for all 6 formats, but ServiceRegistration.cs (ExtractorOle/ExtractorOLE/ServiceRegistration.cs) only wires 4: Word(docx)/Excel(xlsx)/PowerPoint(pptx)/ExcelLegacy(xls) into IDictionary<OfficeMimeTypeEnum,IOpenStrategy/ITextExtractor>. Legacy .doc and .ppt have no Extract() path at all -- their upstream tasks T-c4123b53 (doc open component) and T-517f89c5 (ppt open component) are themselves status=blocked-by-blocked-by-research, per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md), which requires a b2xtranslator .doc/.ppt-to-OOXML conversion step not yet implemented. Benchmarking Doc/Ppt today would only time a no-op/failure path, not real extraction, producing misleading latency/allocation numbers. Recommend re-scoping to the 4 currently-supported formats now (separate follow-up task for Doc/Ppt once T-c4123b53/T-517f89c5 land) or waiting on those tasks -- needs task-owner decision. Independently re-confirmed same blocker previously surfaced (uncommitted, orphaned) in a stale worktree from an earlier aborted run on 2026-09-13/14; that worktree/branch has been removed and this block is filed properly via ydk.
### 2026-09-19T16:26:56Z (UTC)
**Unblocked (dependency-gap resolved):** the 2026-09-15 blocker (legacy .doc/.ppt had no Extract() path) no longer holds on main. T-c4123b53 (doc open), T-517f89c5 (ppt open) and T-a8b6b4ec (doc text extraction) are done, as are T-40fc15b4 (ppt text) and the facade task T-3b4e70f4 (PR #107); ServiceRegistration.cs now wires open strategies (Word/Excel/PowerPoint/ExcelLegacy/WordLegacy/PowerPointLegacy) and text extractors for all 6 formats, and the original dependencies T-fb06300e and T-71ab3bc0 are done. `ydk task unblock` set status in-progress; corrected by hand to `open` (nobody is working it, and `ydk task ready` only lists `open`) and removed the stale `blocked-by-dependency-gap` label.

**Verified after unblock:**
1. With status `open` and its original two dependencies, `ydk task ready` listed T-b62ffe04 with deps met. It was NOT listed while the status was `in-progress` (ready only lists `open`), which is why the status was reset to `open`. After adding the T-f0963fc3 dependency below it shows deps-pending in `ydk task list`, and T-f0963fc3 is the ready one.
2. Dependency on the Extract guardrail/error tasks T-3335d397 / T-a2b91dc6 / T-61477563: NOT added; no spec text justifies it. Neither testing-strategy.md nor ADR-002 mention them, and a benchmark only exercises the success path. Risk, recorded so the implementer does not time the wrong thing: `MainExtractor.Extract(ExtractionRequest)` on main trusts `DetectedMimeType` and runs no size check at all today; T-3335d397 will add file-too-large before parsing at `MimeDetectionLimits.MaxFileSizeBytes` = 104,857,600 bytes (strictly `>`) and oversized-nested-content at 2 GiB declared size. So the "~100MB" sample must be <= 104,857,600 bytes and must not declare > 2 GiB uncompressed (avoid highly compressible padding), otherwise the benchmark would silently time a rejection path once those tasks land. Also note the existing ExtractionBenchmarks.cs stub calls the legacy `Extract(byte[])` overload, not the `Extract(ExtractionRequest)` facade; the benchmark should target the facade with DetectedMimeType set. Downstream T-2e13ee4b (committed baseline) is best done after the guardrail tasks merge so the baseline includes their pre-parse overhead; that ordering is not enforced here.

**New dependency added:** T-f0963fc3 (Generate fixed 10MB and ~100MB benchmark samples for all 6 formats). The acceptance criteria need 12 fixed inputs; `samples/` has no file above 6.8MB and the benchmark project is a single-docx stub. Direction is T-b62ffe04 depends on T-f0963fc3 (no cycle; `ydk task validate-dag` clean). Acceptance criteria of this task are unchanged.
### 2026-09-19T21:56:04Z (UTC)
## Verification Proof

OK dotnet-build (9.6s)
OK dotnet-format (71.3s)
OK dotnet-quality (81.5s)
OK ai-code-review (0.0s)
OK cli-error-handling (0.0s)
OK cli-output-format (0.0s)
OK fastapi-adapter-isolation (0.0s)
OK fastapi-core-purity (0.0s)
OK fastapi-no-mocks-e2e (0.0s)
OK fastapi-no-mocks-integration (0.0s)
OK fastapi-route-delegation (0.0s)
OK fastapi-route-splitting (0.0s)
OK fastapi-service-sync (0.0s)
OK nextjs-file-sizes (0.0s)
OK nextjs-fsd-imports (0.0s)
OK nextjs-fsd-layers (0.0s)
OK nextjs-module-density (0.0s)
OK nextjs-no-direct-env (0.0s)
OK nextjs-no-direct-fetch (0.0s)
OK nextjs-no-direct-ui-imports (0.0s)
OK nextjs-no-query-key-strings (0.0s)
OK nextjs-no-useeffect-fetch (0.0s)
OK nextjs-no-zustand-module-level (0.0s)
OK nextjs-page-purity (0.0s)
OK nextjs-server-client-boundary (0.0s)
OK nextjs-sse-abort-controller (0.0s)
OK python-file-length (0.0s)
OK python-no-future-annotations (0.0s)
OK react-fsd-imports (0.0s)
OK spec-alignment (1.6s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.2s)
OK terraform-paravirt-ec2 (0.2s)
OK terraform-public-ami (0.1s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (58.0s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/119
### 2026-09-19T21:58:06Z (UTC)
PR https://github.com/yoavarad/ole-extractor/pull/119. Benchmarks for 6 formats x 10MB/~100MB run against Extract(ExtractionRequest); all 12 cases met P95 targets (10MB max 174.78 ms xls; 100MB max 2461 ms xls vs 5 s). Alloc B/input B: doc 1.29, docx 8.61, xlsx 5.82, pptx 6.07, ppt 14.83, xls 39.48 (10MB tier; 100MB tier ~same). Caveats: doc/ppt samples are padded fixtures with tiny extracted text (283/74 chars) so they measure container open only; 100MB tier uses 15 iterations (P95 near max); earlier ~14s xls one-off not reproduced (cold 2.38s, warm mean 2.04s). Results in .ydk/proofs/T-b62ffe04/benchmark-results.md.