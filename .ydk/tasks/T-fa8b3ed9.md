---
id: T-fa8b3ed9
title: Add Subject, Comments, RevisionNumber, LastPrinted and EditingDuration to FileMetadata
  or record why not
story: S-f6d7124a
status: done
assignee: null
labels: []
dependencies:
- T-7916a198
spec_refs:
- docs/specs/extraction.md
- docs/specs/dataset-curation.md
- docs/specs/testing-strategy.md
- docs/adrs/004-legacy-doc-ppt-parsing.md
component_refs:
- ydk:entity:extraction/FileMetadata
test_strategy: 'Corpus-sample-backed: exact expected values from samples/manifest.json
  (genuine revision-history samples) asserted through MainExtractor.Extract, plus
  unit tests for the EditTime to minutes conversion and the null-when-absent rule.'
acceptance_criteria:
- text: FileMetadata exposes Subject, Comments, RevisionNumber (int?), LastPrinted
    (DateTime?, UTC) and EditingDurationMinutes (int?) as nullable properties, with
    the naming reconciled against FileMetadata.yaml (mapping documented if names differ)
  done: true
- text: 'All six formats populate them from native properties where present: OOXML
    from core.xml and app.xml, legacy from OLE SummaryInformation, with EditTime converted
    to whole minutes so legacy and OOXML use the same unit'
  done: true
- text: A field absent in the source is null, never a placeholder string or 0
  done: true
- text: The corpus integration test asserts Subject, Comments, RevisionNumber, LastPrinted
    and EditingDurationMinutes against samples/manifest.json for every sample whose
    manifest records them, including the NOAA pptx (RevisionNumber 1154, 11229 minutes)
    and the legacy doc (RevisionNumber 20, 126 minutes)
  done: true
- text: If instead a field is judged out of scope, an ADR or spec note records the
    decision and the manifest marks those fields as intentionally not extracted, and
    no manifest expectation is left silently unasserted
  done: false
- text: Full dotnet test suite passes
  done: true
milestone: null
complexity: null
gates: []
created: '2026-09-20T08:12:33Z'
updated: '2026-09-25T11:34:21Z'
---

## Description

docs/specs/extraction.md (General Metadata) and .ydk/components/entity/extraction/FileMetadata.yaml say FileMetadata carries subject, keywords, comments, application name, revision number, cumulative editing duration (EditingDurationMinutes, minutes, normalized across OLE EditTime and OOXML TotalTime) and last-printed timestamp (LastPrintedAt). The implementation in ExtractorOle/ExtractorOLE/DTOs/DocumentExtractionResult.cs only has Title, Creator, Created, Modified and LastModifiedBy, so the corpus integration test (PR #120, not yet merged when filed) cannot assert the Subject, Comments, RevisionNumber, LastPrinted and EditingDurationMinutes values that samples/manifest.json records (for example the genuine revision-history samples: NOAA pptx RevisionNumber 1154, TotalTime 11229 minutes; legacy doc RevisionNumber 20, edit time 7560 seconds = 126 minutes). Also note the current names differ from the entity (Creator vs Author, Created/Modified vs CreatedAt/ModifiedAt); align or record the mapping. Keywords and ApplicationName should be checked in the same pass. The spec lists these fields as required, so the default resolution is to implement them: add the properties to FileMetadata, populate them for OOXML (docProps/core.xml cp:revision, cp:lastPrinted, dc:subject, dc:description as comments; docProps/app.xml TotalTime) and legacy formats (OLE SummaryInformation via NPOI/HPSF, EditTime 100 ns units converted to minutes), null when absent, and assert them in the corpus test. Only if the team decides a field is out of scope, record the decision (a small ADR under docs/adrs or a spec note) and mark the manifest expectation as intentionally not extracted.


## Activity Log
### 2026-09-25T11:24:40Z (UTC)
**pr-body-validation FAILED:**
FAIL: Missing requirements: screenshot_for_ui
PASS: PR body contains command output in code block
PASS: PR body contains ## Summary section
PASS: PR body contains ## Test Plan section
FAIL: UI file changes detected — PR body must include screenshots
### 2026-09-25T11:25:42Z (UTC)
**pr-body-validation FAILED:**
FAIL: Missing requirements: screenshot_for_ui
PASS: PR body contains command output in code block
PASS: PR body contains ## Summary section
PASS: PR body contains ## Test Plan section
FAIL: UI file changes detected — PR body must include screenshots
### 2026-09-25T11:26:03Z (UTC)
**pr-body-validation FAILED:**
FAIL: Missing requirements: screenshot_for_ui
PASS: PR body contains command output in code block
PASS: PR body contains ## Summary section
PASS: PR body contains ## Test Plan section
FAIL: UI file changes detected — PR body must include screenshots
### 2026-09-25T11:28:19Z (UTC)
## Verification Proof

OK dotnet-build (7.3s)
OK dotnet-format (65.1s)
OK dotnet-quality (71.4s)
OK ai-code-review (0.0s)
OK cli-error-handling (0.0s)
OK cli-output-format (0.0s)
OK fastapi-adapter-isolation (0.0s)
OK fastapi-core-purity (0.0s)
OK fastapi-no-mocks-e2e (0.0s)
OK fastapi-no-mocks-integration (0.0s)
OK fastapi-route-delegation (0.0s)
OK fastapi-route-splitting (0.0s)
OK fastapi-service-sync (0.0s)
OK nextjs-file-sizes (0.0s)
OK nextjs-fsd-imports (0.0s)
OK nextjs-fsd-layers (0.0s)
OK nextjs-module-density (0.0s)
OK nextjs-no-direct-env (0.0s)
OK nextjs-no-direct-fetch (0.0s)
OK nextjs-no-direct-ui-imports (0.0s)
OK nextjs-no-query-key-strings (0.0s)
OK nextjs-no-useeffect-fetch (0.0s)
OK nextjs-no-zustand-module-level (0.0s)
OK nextjs-page-purity (0.0s)
OK nextjs-server-client-boundary (0.0s)
OK nextjs-sse-abort-controller (0.0s)
OK python-file-length (0.0s)
OK python-no-future-annotations (0.0s)
OK react-fsd-imports (0.0s)
OK spec-alignment (1.5s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.1s)
OK terraform-paravirt-ec2 (0.1s)
OK terraform-public-ami (0.1s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (31.9s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/135
### 2026-09-25T11:32:11Z (UTC)
**pr-body-validation FAILED:**
FAIL: Missing requirements: screenshot_for_ui
PASS: PR body contains command output in code block
PASS: PR body contains ## Summary section
PASS: PR body contains ## Test Plan section
FAIL: UI file changes detected — PR body must include screenshots
