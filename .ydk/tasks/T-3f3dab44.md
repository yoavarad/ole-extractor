---
id: T-3f3dab44
title: Map WordFormatMetadata for doc
story: S-32677db6
status: open
assignee: null
labels: []
dependencies:
- T-c4123b53
spec_refs:
- docs/specs/extraction.md
component_refs:
- ydk:entity:extraction/WordFormatMetadata
test_strategy: 'Unit-test-backed: unit tests against a macro-enabled and a non-macro
  .doc fixture assert exact WordFormatMetadata field values including HasMacros.'
acceptance_criteria:
- text: WordFormatMetadata's page/word/character/paragraph/line counts are populated
    from native metadata, not recomputed
  done: false
- text: Company/Manager/Template fields are populated when present, null when absent
  done: false
- text: HasMacros is true for a .doc fixture with a macro storage and false for one
    without
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:56Z'
updated: '2026-08-19T12:42:59Z'
---

## Description

Populate WordFormatMetadata for legacy .doc from NPOI's SummaryInformation/DocumentSummaryInformation: page/word/character/ paragraph/line counts, company/manager/template fields, and the macro-presence flag (VBA macro storage detection).


## Activity Log