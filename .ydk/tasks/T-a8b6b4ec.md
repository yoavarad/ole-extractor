---
id: T-a8b6b4ec
title: Implement doc flat-text extraction with unicode fidelity
story: S-32677db6
status: open
assignee: null
labels: []
dependencies:
- T-c4123b53
- T-94570c2b
spec_refs:
- docs/specs/extraction.md
component_refs:
- ydk:req:extraction/unicode-fidelity
- ydk:entity:extraction/ExtractionResult
test_strategy: 'Corpus-sample-backed: unit test against a hand-crafted multilingual
  .doc fixture asserts ExtractedText exactly equals the known-correct string; formal
  corpus-manifest assertion lands with Epic 9''s integration tests once Epic 8''s
  corpus exists.'
acceptance_criteria:
- text: ExtractedText is empty string (never null) for a .doc with no body text
  done: false
- text: ExtractedText for a multilingual .doc fixture matches the expected Unicode
    string exactly, byte-for-byte, per the NPOI i18n validation findings from T-94570c2b
  done: false
- text: No Unicode normalization form is applied that changes the source character
    sequence
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:56Z'
updated: '2026-09-18T19:55:07Z'
---

## Description

Extract flattened plain-text body content from .doc via NPOI, ensuring Hebrew/Arabic/Persian/Russian/Latin/CJK text and emoji (including multi-codepoint sequences) round-trip without mojibake, truncation, or unwanted normalization, per unicode-fidelity.


## Activity Log
### 2026-09-18T19:55:07Z (UTC)
Implemented + committed locally (0775961), suite 196/196. Blocked: b2xtranslator fork branch net8-doc-retarget (06e87fb) needs push to yoavarad/b2xtranslator; push denied. See .ydk/proofs/T-a8b6b4ec/orchestrator-progress.md
