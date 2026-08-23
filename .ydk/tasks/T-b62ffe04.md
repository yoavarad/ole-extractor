---
id: T-b62ffe04
title: Implement Extract() latency/allocation benchmarks per format
story: S-709fe2de
status: open
assignee: null
labels: []
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
updated: '2026-08-19T12:47:24Z'
---

## Description

Implement BenchmarkDotNet benchmarks running Extract() against a fixed 10MB and a fixed ~100MB sample for each of the 6 formats, reporting P50/P95 latency and allocated bytes, per extraction-latency and memory-ceiling.


## Activity Log