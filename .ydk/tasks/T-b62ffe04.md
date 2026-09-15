---
id: T-b62ffe04
title: Implement Extract() latency/allocation benchmarks per format
story: S-709fe2de
status: blocked-by-dependency-gap
assignee: null
labels:
- blocked-by-dependency-gap
dependencies:
- T-fb06300e
- T-71ab3bc0
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
  done: false
- text: Each run reports P50/P95 latency and allocated-bytes-per-input-byte
  done: false
- text: 10MB benchmarks target P95 < 500ms and 100MB benchmarks target P95 < 5s, per
    extraction-latency's target
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:23Z'
updated: '2026-09-15T20:57:17Z'
---

## Description

Implement BenchmarkDotNet benchmarks running Extract() against a fixed 10MB and a fixed ~100MB sample for each of the 6 formats, reporting P50/P95 latency and allocated bytes, per extraction-latency and memory-ceiling.


## Activity Log
### 2026-09-15T20:57:17Z (UTC)
**Blocked (dependency-gap):** Acceptance criteria require benchmarking Extract() for all 6 formats, but ServiceRegistration.cs (ExtractorOle/ExtractorOLE/ServiceRegistration.cs) only wires 4: Word(docx)/Excel(xlsx)/PowerPoint(pptx)/ExcelLegacy(xls) into IDictionary<OfficeMimeTypeEnum,IOpenStrategy/ITextExtractor>. Legacy .doc and .ppt have no Extract() path at all -- their upstream tasks T-c4123b53 (doc open component) and T-517f89c5 (ppt open component) are themselves status=blocked-by-blocked-by-research, per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md), which requires a b2xtranslator .doc/.ppt-to-OOXML conversion step not yet implemented. Benchmarking Doc/Ppt today would only time a no-op/failure path, not real extraction, producing misleading latency/allocation numbers. Recommend re-scoping to the 4 currently-supported formats now (separate follow-up task for Doc/Ppt once T-c4123b53/T-517f89c5 land) or waiting on those tasks -- needs task-owner decision. Independently re-confirmed same blocker previously surfaced (uncommitted, orphaned) in a stale worktree from an earlier aborted run on 2026-09-13/14; that worktree/branch has been removed and this block is filed properly via ydk.
