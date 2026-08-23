---
id: T-1df33d6c
title: Implement doc first-layer embedded-subfile walk
story: S-32677db6
status: open
assignee: null
labels: []
dependencies:
- T-c4123b53
spec_refs:
- docs/specs/extraction.md
component_refs:
- ydk:entity:extraction/SubfileItem
- ydk:req:extraction/subfile-scope
test_strategy: 'Unit-test-backed: unit test against a .doc fixture with one valid
  and one corrupt embedded object asserts the valid one appears in Subfiles, the corrupt
  one is omitted and logged, and the call does not throw.'
acceptance_criteria:
- text: First-layer embedded objects and inline media in a .doc fixture appear as
    correct SubfileItem entries (Binary, FileName from CFB storage name, SizeBytes)
  done: false
- text: A subfile that is itself a container is returned as one opaque SubfileItem,
    not recursively unpacked
  done: false
- text: A single corrupt embedded object is omitted from Subfiles and logged; Extract()
    still returns successfully for the rest of the document
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:56Z'
updated: '2026-08-19T12:42:59Z'
---

## Description

Walk the .doc CFB structure for first-layer OLE-embedded objects and inline media, producing SubfileItem entries per subfile-scope. A corrupt embedded object is omitted and logged without failing the overall extraction call.


## Activity Log