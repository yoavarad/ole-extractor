---
id: T-fb89c6f3
title: Add null request/field boundary validation to DetectMimeType
story: S-072bfdf6
status: open
assignee: null
labels: []
dependencies:
- T-ef73861f
spec_refs:
- docs/specs/overview.md
component_refs:
- ydk:entity:extraction/MimeDetectionRequest
test_strategy: 'Unit-test-backed: unit tests assert a null request and a null FileBytes
  each throw before any detection component executes, verified via a spy/counter on
  the registry.'
acceptance_criteria:
- text: A null MimeDetectionRequest throws immediately, before any detection component
    is invoked
  done: false
- text: A MimeDetectionRequest with a null FileBytes throws immediately, before any
    detection component is invoked
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:59Z'
updated: '2026-08-19T12:43:00Z'
---

## Description

Add standard argument validation to the DetectMimeType facade so a null request object, or a null FileBytes, throws immediately before any detection logic runs — boundary validation, not a domain error.


## Activity Log