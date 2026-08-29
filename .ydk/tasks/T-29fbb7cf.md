---
id: T-29fbb7cf
title: Wire docx first-layer embeddings/media walk via the generalized walk
story: S-156ec71a
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
test_strategy: 'Unit-test-backed: unit test against a docx fixture with an embedded
  xlsx, an inline image, and one corrupt embedding asserts correct SubfileItem entries
  and correct exclusions, per subfile-scope''s acceptance_criteria.'
acceptance_criteria:
- text: A docx with an embedded xlsx object and an inline PNG both appear in Subfiles
    via the generalized walk
  done: false
- text: Internal package XML parts (styles.xml, theme*.xml, numbering.xml, settings.xml,
    docProps/*.xml, [Content_Types].xml, _rels/*) never appear in Subfiles
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

Wire docx's first-layer embedded-object and inline-media extraction through the generalized ExtractFirstLayerEmbedded walk from Sprint 1 (T-05bd14c7), producing correct SubfileItem entries and excluding internal package XML parts per subfile-scope.


## Activity Log
