---
id: T-3bddbe75
title: Produce corrupt, truncated, and zero-byte adversarial samples
story: S-6bc8566f
status: done
assignee: null
labels: []
dependencies:
- T-f96fa73d
spec_refs:
- docs/specs/dataset-curation.md
- docs/specs/extraction.md
component_refs:
- ydk:error:extraction/corrupt-file
- ydk:error:extraction/truncated-container
test_strategy: "No test \u2014 infra/tooling task; these samples are asserted against\
  \ by Epic 9's integration tests once recorded in the corpus manifest."
acceptance_criteria:
- text: One corrupt sample exists that is structurally malformed and unparseable,
    tagged with its target format(s)
  done: true
- text: One truncated sample exists whose declared container size exceeds its actual
    byte length, tagged with its target format(s)
  done: true
- text: One zero-byte sample exists, tagged with its target format(s)
  done: true
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:21Z'
updated: '2026-09-13T19:30:42Z'
---

## Description

Produce one corrupt sample (structurally malformed, unparseable), one truncated sample (declared size exceeds actual byte length), and one zero-byte sample, derived from valid synthetic samples where practical, each tagged with which format(s) it targets.


## Activity Log