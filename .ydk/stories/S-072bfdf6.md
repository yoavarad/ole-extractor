---
id: S-072bfdf6
title: Wire the DetectMimeType public facade to the registry
epic: E-bd43bc02
status: done
spec_refs:
- docs/specs/overview.md
- docs/specs/mime-detection.md
acceptance_criteria:
- text: DetectMimeType dispatches to the registry-resolved detection component per
    format family
  done: false
- text: A null request or null FileBytes throws immediately before any detection logic
    runs
  done: false
- text: The public facade contains no format-specific parsing logic itself
  done: false
labels: []
milestone: null
created: '2026-08-19T10:31:25Z'
updated: '2026-08-19T10:31:25Z'
---

## Description

Wire the public DetectMimeType(MimeDetectionRequest) facade to the format-dispatch registry (both OOXML and CFB detection components), returning MimeDetectionResult.


## Activity Log
