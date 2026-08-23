---
id: T-fd49ef39
title: Implement CFB directory-entry detection component for doc/xls/ppt
story: S-3bc6b929
status: in-progress
assignee: agent
labels:
- in-progress
dependencies:
- T-d7c81051
- T-2ac2f140
spec_refs:
- docs/specs/mime-detection.md
component_refs:
- ydk:contract:extraction/detect-mime-type
- ydk:entity:extraction/MimeDetectionRequest
- ydk:entity:extraction/MimeDetectionResult
- ydk:req:extraction/content-only-mime-detection
test_strategy: 'Unit-test-backed: unit tests feed hand-crafted CFB byte arrays with
  each of the three known root-entry signatures and assert the correct DetectedFormat,
  plus one CFB file with an unrecognized root entry asserting Unknown.'
acceptance_criteria:
- text: A CFB detection component distinguishes doc/xls/ppt by inspecting only root
    storage/stream names via OpenMcdf, with no full document parse
  done: false
- text: Detection never reads request.FileName to decide the result
  done: false
- text: A CFB-structured file matching none of the three known root-entry signatures
    returns the Unknown result, never an exception
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:56Z'
updated: '2026-08-22T18:14:48Z'
---

## Description

Implement a detection component that opens FileBytes as a compound file via OpenMcdf and inspects root storage/stream names ("WordDocument", "Workbook"/"Book", "PowerPoint Document") to distinguish doc/xls/ppt, without performing a full document parse.


## Activity Log