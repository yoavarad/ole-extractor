---
id: T-7cdaeceb
title: Produce wrong-extension and corrupt-embedded-object adversarial samples
story: S-6bc8566f
status: open
assignee: null
labels: []
dependencies:
- T-f96fa73d
spec_refs:
- docs/specs/dataset-curation.md
- docs/specs/mime-detection.md
component_refs:
- ydk:req:extraction/content-only-mime-detection
- ydk:req:extraction/subfile-scope
test_strategy: "No test \u2014 infra/tooling task; these samples are asserted against\
  \ by Epic 9's integration tests once recorded in the corpus manifest."
acceptance_criteria:
- text: One wrong-extension sample exists with valid content and a misleading or missing
    filename extension, tagged with its target format
  done: false
- text: One corrupt-embedded-object sample exists with otherwise-valid content and
    exactly one corrupt first-layer embedding, tagged with its target format
  done: false
- text: Both samples' correct expected outcome (correct detected format regardless
    of extension; successful extraction omitting only the corrupt embedding) is recorded
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:21Z'
updated: '2026-08-19T12:47:24Z'
---

## Description

Produce one wrong-extension sample (correct content, misleading or missing filename extension) and one sample with a single corrupt embedded object alongside otherwise-valid content, each tagged with which format(s) it targets.


## Activity Log