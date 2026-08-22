---
id: T-4b624fef
title: Generate macro-enabled-with-embedding synthetic samples for all 6 formats
story: S-41ebb945
status: open
assignee: null
labels: []
dependencies:
- T-5e9dbd18
spec_refs:
- docs/specs/dataset-curation.md
component_refs:
- ydk:req:extraction/unicode-fidelity
- ydk:req:extraction/subfile-scope
test_strategy: "No test \u2014 infra/tooling task; samples are asserted against by\
  \ Epic 9's integration tests (macro-variant-misclassification scenario) once recorded\
  \ in the corpus manifest."
acceptance_criteria:
- text: 6 macro-enabled-with-embedding synthetic samples exist (1 per format), each
    with at least one macro/VBA storage and at least one first-layer embedding
  done: false
- text: Each sample's body text and at least 2 metadata fields contain multilingual/mixed-script
    text and emoji
  done: false
- text: Each sample exercises Epic 5/6's macro-variant-misclassification test scenario
    (docm/xlsm/pptm detect as their base format)
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:20Z'
updated: '2026-08-19T12:47:24Z'
---

## Description

Generate one synthetic sample per format (6 total) that is a macro-enabled variant (docm/xlsm/pptm, or legacy doc/xls/ppt with a macro storage) with at least one embedding, including multilingual/ emoji content in body text and at least 2 metadata fields.


## Activity Log