---
id: T-ef11e30d
title: Classify macro-enabled OOXML variants (docm/xlsm/pptm) as their base format
story: S-7d040ba7
status: done
assignee: null
labels: []
dependencies:
- T-44812a03
spec_refs:
- docs/specs/mime-detection.md
component_refs:
- ydk:entity:extraction/MimeDetectionResult
test_strategy: 'Unit-test-backed: unit tests against a docm/xlsm/pptm fixture each
  assert DetectedFormat equals the base format''s enum value.'
acceptance_criteria:
- text: A docm file detects as Docx; an xlsm file detects as Xlsx; a pptm file detects
    as Pptx
  done: false
- text: MimeDetectionResult.DetectedFormat gains no separate macro-variant enum values
  done: false
- text: The macro-presence distinction is not surfaced on MimeDetectionResult (it
    surfaces later on FormatMetadata via extraction, per Epic 6)
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:57Z'
updated: '2026-08-29T15:47:20Z'
---

## Description

Ensure the OOXML detection component structurally recognizes macro-enabled variants (docm/xlsm/pptm) but classifies DetectedFormat as the base format (Docx/Xlsx/Pptx), without growing the MimeDetectionResult enum with a parallel macro axis.


## Activity Log
### 2026-08-29T15:47:20Z (UTC)
**Verification FAILED:**
FAIL dotnet-format: === ExtractorOle\ExtractorOLE\ExtractorOLE.csproj ===
C:\Users\yoava\Projects\ole-extractor\.ydk\wor
FAIL dotnet-quality: [dotnet-build] Determining projects to restore...
  Restored C:\Users\yoava\Projects\ole-extractor\.
