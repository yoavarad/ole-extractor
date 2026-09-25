---
id: T-ec4dc427
title: Add curated nested-embedding samples for xls and ppt (parallel to project-proposal-budget.doc)
story: As a developer verifying nested-embedding extraction across all legacy formats,
  I want curated xls and ppt samples with a real nested embedding (like the existing
  doc sample), so corpus coverage isn't doc-only.
status: done
assignee: null
labels: []
dependencies: []
spec_refs: []
component_refs: []
test_strategy: dotnet test on new integration tests; ExtractCorpusManifestTests theory
  covers new manifest entries
acceptance_criteria:
- text: Curated xls sample with real nested embedding added under samples/curated/xls/;
    curated ppt sample with real nested embedding added under samples/curated/ppt/;
    both registered in samples/manifest.json; integration tests analogous to CuratedNestedEmbeddingSampleTests.cs
    assert the nested embedding is one opaque subfile and inner content is not unpacked.
  done: false
milestone: null
complexity: null
gates: []
created: '2026-09-25T10:44:36Z'
updated: '2026-09-25T11:39:55Z'
---

## Description

T-07571b32 built one curated nested-embedding sample (project-proposal-budget.doc: doc->xls->png). User confirmed 'one curated sample' was meant as one-per-format, not one total. Add an xls curated sample (xls embedding a nested object) and a ppt curated sample (ppt embedding a nested object), following CuratedSampleSpecs.cs / CuratedNestedEmbeddingSampleTests.cs conventions from PR #126. Add matching manifest.json entries and integration tests.

## Activity Log