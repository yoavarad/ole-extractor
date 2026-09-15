---
id: T-e0a5bfec
title: 'Integration tests: DetectMimeType against every corpus manifest sample'
story: S-f6d7124a
status: done
assignee: agent
labels:
- in-progress
dependencies:
- T-71ab3bc0
- T-fb89c6f3
spec_refs:
- docs/specs/testing-strategy.md
component_refs:
- ydk:contract:extraction/detect-mime-type
test_strategy: 'Corpus-sample-backed: exercises the full corpus manifest end-to-end
  against the real DetectMimeType facade, per testing-strategy.md''s integration-test
  definition.'
acceptance_criteria:
- text: Every corpus sample in samples/manifest.json has a corresponding integration
    test asserting exact expected DetectedFormat
  done: false
- text: No detection component is mocked in these tests
  done: false
- text: Wrong-extension and macro-variant samples assert the correct format independent
    of filename/macro presence
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:21Z'
updated: '2026-09-15T20:45:16Z'
---

## Description

Integration test suite calling DetectMimeType against every sample in samples/manifest.json, asserting the exact expected DetectedFormat recorded in the manifest, with no mocking of the detection components.


## Activity Log