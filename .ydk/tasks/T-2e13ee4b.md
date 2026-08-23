---
id: T-2e13ee4b
title: Establish committed baseline and CI regression documentation
story: S-709fe2de
status: open
assignee: null
labels: []
dependencies:
- T-b62ffe04
spec_refs:
- docs/adrs/002-profiling-and-stress-testing.md
- docs/specs/testing-strategy.md
component_refs:
- ydk:nfr:extraction/extraction-latency
test_strategy: "No test \u2014 infra/tooling task; the baseline itself becomes the\
  \ regression-detection mechanism for future benchmark runs."
acceptance_criteria:
- text: A baseline benchmark result is committed to the repository
  done: false
- text: Documentation states that CI fails the profiling task if P95 regresses more
    than 20% against the committed baseline
  done: false
- text: The benchmark run is documented as separate from the unit/integration test
    suite gate
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:23Z'
updated: '2026-08-19T12:47:24Z'
---

## Description

Commit a baseline benchmark result and document what a >20% P95 regression against that baseline means for CI, per extraction-latency's test_method.


## Activity Log