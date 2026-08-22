---
id: T-47fea79d
title: Implement docx flat-text extraction with unicode fidelity
story: S-156ec71a
status: open
assignee: null
labels: []
dependencies:
- T-90924b6d
spec_refs:
- docs/specs/extraction.md
component_refs:
- ydk:req:extraction/unicode-fidelity
- ydk:entity:extraction/ExtractionResult
test_strategy: 'Corpus-sample-backed: unit test against a hand-crafted multilingual
  docx fixture asserts ExtractedText exactly equals the known-correct string; formal
  corpus-manifest assertion lands with Epic 9''s integration tests once Epic 8''s
  corpus exists.'
acceptance_criteria:
- text: ExtractedText is empty string (never null) for a docx with no body text
  done: false
- text: ExtractedText for a multilingual docx fixture matches the expected Unicode
    string exactly
  done: false
- text: No Unicode normalization form is applied that changes the source character
    sequence
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:57Z'
updated: '2026-08-19T12:42:57Z'
---

## Description

Extract flattened plain-text body content from docx via DocumentFormat.OpenXml, ensuring multilingual text and emoji round-trip without mojibake or truncation (OOXML is already UTF-8-native, but text flattening logic itself must not introduce loss).


## Activity Log
