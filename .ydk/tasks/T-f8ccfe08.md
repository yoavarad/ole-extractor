---
id: T-f8ccfe08
title: Retarget b2xtranslator Doc module to net8.0 and wire .doc body-text extraction
story: S-32677db6
status: done
assignee: null
labels: []
dependencies:
- task_id: T-c4123b53
  type: discovered-from
spec_refs:
- docs/adrs/004-legacy-doc-ppt-parsing.md
- docs/specs/extraction.md
component_refs:
- ydk:entity:extraction/FileMetadata
- ydk:entity:extraction/ExtractionResult
test_strategy: Unit test converts a known .doc fixture via b2xtranslator and asserts
  extracted body text matches expected content; corrupt/unconvertible .doc input test
  asserts graceful degradation, not a crash; empirical fidelity check (Unicode/RTL/CJK)
  against multilingual corpus per ADR-001's caveat
acceptance_criteria:
- text: b2xtranslator.doc.csproj retargeted from netcoreapp2.0 to net8.0 and builds
    cleanly; DocOpenStrategy converts a real .doc to .docx in-process via b2xtranslator
    and feeds it through the existing OpenXml docx extraction path; unconvertible/corrupt
    .doc input degrades gracefully (no crash, documented behavior); wired into DI/FormatDispatchRegistry
    following T-517f89c5's PptOpenStrategy pattern
  done: true
milestone: null
complexity: null
gates:
- id: G-a8825928
  type: human
  description: Upstream b2xtranslator net8.0 retarget merged (yoavarad/b2xtranslator#10)
  status: resolved
  config:
    issue_url: https://github.com/yoavarad/b2xtranslator/issues/10
  resolved_at: '2026-09-19T07:12:10Z'
created: '2026-09-16T20:03:25Z'
updated: '2026-09-19T07:48:37Z'
---

## Description

ADR-004 decided .doc body-text goes via b2xtranslator (.doc -> .docx in-process conversion), then the existing DocumentFormat.OpenXml docx extraction path -- no separate doc-specific body parser. T-c4123b53 (PR #92) implemented FileMetadata via HPSF/POIFS per ADR-004 but stubbed body-text extraction empty, deferring this retarget. T-517f89c5 (PR #93) already did the analogous Ppt-module retarget/wiring as a reference implementation -- follow the same pattern (third_party/b2xtranslator submodule, Doc module, netcoreapp2.0 -> net8.0 retarget) for Doc/HWPF instead of Ppt/HSLF.

## Activity Log
### 2026-09-18T10:51:34Z (UTC)
**Blocked (upstream-dependency):** b2xtranslator Doc module net8.0 retarget filed upstream: https://github.com/yoavarad/b2xtranslator/issues/10. Not to be executed in this repo.
### 2026-09-19T07:12:12Z (UTC)
Task unblocked. Resuming.
