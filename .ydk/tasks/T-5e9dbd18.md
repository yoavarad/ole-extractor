---
id: T-5e9dbd18
title: Build synthetic sample generator scaffolding and shared multilingual/emoji
  fixture library
story: S-41ebb945
status: done
assignee: agent
labels:
- in-progress
dependencies:
- T-a5f1d074
spec_refs:
- docs/specs/dataset-curation.md
- docs/specs/extraction.md
component_refs:
- ydk:req:extraction/unicode-fidelity
test_strategy: "No test \u2014 infra/tooling task; correctness is verified indirectly\
  \ by the samples the scaffolding produces."
acceptance_criteria:
- text: A generator harness can programmatically author doc/xls/ppt/docx/xlsx/pptx
    files with controllable body text, metadata fields, and embeddings
  done: false
- text: A shared fixture library provides multilingual/mixed-script text and emoji
    strings reusable across all 6 formats' generation code
  done: false
- text: Fixture strings cover Hebrew, Arabic, Persian, Russian, Latin-script, and
    CJK text plus at least one multi-codepoint emoji sequence
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:20Z'
updated: '2026-08-22T18:15:32Z'
---

## Description

Build the programmatic generator project/harness for synthetic samples and a shared fixture library of multilingual (Hebrew/Arabic/Persian/ Russian/Latin/CJK) text and emoji (including multi-codepoint sequences) reused across all 6 formats' generated body text and metadata fields.


## Activity Log