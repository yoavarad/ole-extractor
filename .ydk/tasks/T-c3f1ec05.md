---
id: T-c3f1ec05
title: Wire pptx first-layer embeddings/media walk via the generalized walk
story: S-37c18545
status: done
assignee: null
labels: []
dependencies:
- T-05bd14c7
spec_refs:
- docs/specs/extraction.md
component_refs:
- ydk:entity:extraction/SubfileItem
- ydk:req:extraction/subfile-scope
test_strategy: 'Unit-test-backed: unit test against a pptx fixture with an embedded
  object, inline media, and one corrupt embedding asserts correct SubfileItem entries
  and correct exclusions, with a regression check against Sprint 1''s PowerPoint-only
  walk behavior.'
acceptance_criteria:
- text: A pptx with an embedded object and inline media both appear in Subfiles via
    the generalized walk, matching prior PowerPoint-only behavior with no regression
  done: false
- text: Internal package XML parts never appear in Subfiles
  done: false
- text: A single corrupt embedding is omitted from Subfiles and logged without failing
    the overall extraction call
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:58Z'
updated: '2026-08-19T12:42:58Z'
---

## Description

Wire pptx's first-layer embedded-object and inline-media extraction through the generalized ExtractFirstLayerEmbedded walk from Sprint 1 (T-05bd14c7) — the walk this generalization was originally derived from (PowerPoint special-casing) — producing correct SubfileItem entries with no regression from prior PowerPoint-only behavior.


## Activity Log
