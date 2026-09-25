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
  done: false
- text: 'All six formats populate them from native properties where present: OOXML
    from core.xml and app.xml, legacy from OLE SummaryInformation, with EditTime converted
    to whole minutes so legacy and OOXML use the same unit'
  done: false
- text: A field absent in the source is null, never a placeholder string or 0
  done: false
- text: The corpus integration test asserts Subject, Comments, RevisionNumber, LastPrinted
    and EditingDurationMinutes against samples/manifest.json for every sample whose
    manifest records them, including the NOAA pptx (RevisionNumber 1154, 11229 minutes)
    and the legacy doc (RevisionNumber 20, 126 minutes)
  done: false
- text: If instead a field is judged out of scope, an ADR or spec note records the
    decision and the manifest marks those fields as intentionally not extracted, and
    no manifest expectation is left silently unasserted
  done: false
- text: Full dotnet test suite passes
  done: false
milestone: null
complexity: null
gates: []
created: '2026-09-20T08:12:33Z'
updated: '2026-09-25T11:34:21Z'
---

## Description

docs/specs/extraction.md (General Metadata) and .ydk/components/entity/extraction/FileMetadata.yaml say FileMetadata carries subject, keywords, comments, application name, revision number, cumulative editing duration (EditingDurationMinutes, minutes, normalized across OLE EditTime and OOXML TotalTime) and last-printed timestamp (LastPrintedAt). The implementation in ExtractorOle/ExtractorOLE/DTOs/DocumentExtractionResult.cs only has Title, Creator, Created, Modified and LastModifiedBy, so the corpus integration test (PR #120, not yet merged when filed) cannot assert the Subject, Comments, RevisionNumber, LastPrinted and EditingDurationMinutes values that samples/manifest.json records (for example the genuine revision-history samples: NOAA pptx RevisionNumber 1154, TotalTime 11229 minutes; legacy doc RevisionNumber 20, edit time 7560 seconds = 126 minutes). Also note the current names differ from the entity (Creator vs Author, Created/Modified vs CreatedAt/ModifiedAt); align or record the mapping. Keywords and ApplicationName should be checked in the same pass. The spec lists these fields as required, so the default resolution is to implement them: add the properties to FileMetadata, populate them for OOXML (docProps/core.xml cp:revision, cp:lastPrinted, dc:subject, dc:description as comments; docProps/app.xml TotalTime) and legacy formats (OLE SummaryInformation via NPOI/HPSF, EditTime 100 ns units converted to minutes), null when absent, and assert them in the corpus test. Only if the team decides a field is out of scope, record the decision (a small ADR under docs/adrs or a spec note) and mark the manifest expectation as intentionally not extracted.


## Activity Log