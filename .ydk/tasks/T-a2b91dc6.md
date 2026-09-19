---
id: T-a2b91dc6
title: Implement unsupported-format rejection and null-argument validation in Extract
story: S-51d277f1
status: done
assignee: null
labels: []
dependencies:
- T-3b4e70f4
spec_refs:
- docs/specs/extraction.md
- docs/specs/overview.md
component_refs:
- ydk:error:extraction/unsupported-format
- ydk:entity:extraction/ExtractionRequest
test_strategy: 'Unit-test-backed: unit tests assert unsupported-format for an arbitrary/Unknown
  mime string, and assert a null request and each null required field throw before
  any detection/extraction logic executes, verified via a spy/counter.'
acceptance_criteria:
- text: Extract() raises unsupported-format when DetectedMimeType isn't one of the
    six supported mime types (including the Unknown sentinel application/octet-stream)
  done: false
- text: A null ExtractionRequest throws immediately, before any of the above logic
    runs
  done: false
- text: An ExtractionRequest with a null required field (FileBytes, FileName, or DetectedMimeType)
    throws immediately, before any of the above logic runs
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:59Z'
updated: '2026-09-19T16:38:08Z'
---

## Description

Implement unsupported-format error handling for a DetectedMimeType that isn't one of the six supported formats, and standard argument validation so a null request or null required field throws immediately before any detection/extraction logic runs.


## Activity Log