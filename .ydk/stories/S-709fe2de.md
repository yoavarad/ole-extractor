---
id: S-709fe2de
title: BenchmarkDotNet profiling project
epic: E-3ffdc4d5
status: open
spec_refs:
- docs/specs/testing-strategy.md
- docs/adrs/002-profiling-and-stress-testing.md
acceptance_criteria:
- text: BenchmarkDotNet project runs Extract() against a fixed 10MB and a fixed ~100MB
    sample for each of the 6 formats, reporting P50/P95 and allocations
  done: false
- text: Benchmark run is separate from the unit/integration test suite gate
  done: false
- text: Report compares against a committed baseline, documenting what a >20% P95
    regression means for CI
  done: false
labels: []
milestone: null
created: '2026-08-19T10:31:25Z'
updated: '2026-08-19T10:31:25Z'
---

## Description

Separate BenchmarkDotNet project measuring Extract() latency and memory allocation per format against fixed 10MB and ~100MB corpus samples, per ADR-002 and the extraction-latency/memory-ceiling NFRs.


## Activity Log
