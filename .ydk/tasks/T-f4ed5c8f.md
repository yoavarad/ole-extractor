---
id: T-f4ed5c8f
title: Integration tests for corpus-sample-backed adversarial scenarios
story: S-f6d7124a
status: done
assignee: null
labels: []
dependencies:
- T-e0a5bfec
- T-7916a198
spec_refs:
- docs/specs/testing-strategy.md
- docs/specs/overview.md
component_refs:
- ydk:error:extraction/corrupt-file
- ydk:error:extraction/truncated-container
- ydk:error:extraction/oversized-nested-content
- ydk:error:extraction/password-protected
- ydk:req:extraction/content-only-mime-detection
- ydk:req:extraction/subfile-scope
test_strategy: 'Corpus-sample-backed: every corpus-sample-backed adversarial scenario
  from overview.md has a passing test per testing-strategy.md''s three-mechanism coverage
  rule.'
acceptance_criteria:
- text: Each of the 7 shared adversarial-set samples has a passing test asserting
    the correct specific error (or, for corrupt-embedded-object, the correct omit-and-log
    behavior)
  done: false
- text: Each format's macro-enabled synthetic sample has a passing test asserting
    correct base-format classification (macro-variant-misclassification scenario)
  done: false
- text: The 2+-level-nesting curated sample has a passing test asserting the outer
    embedding is one opaque SubfileItem
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:22Z'
updated: '2026-09-25T11:53:17Z'
---

## Description

Integration tests covering every corpus-sample-backed adversarial scenario from overview.md via the three-mechanism coverage rule: corrupt, truncated, zero-byte, oversized, password-protected, wrong-extension, and corrupt-embedded-object (shared adversarial set); macro-variant-misclassification (per-format macro synthetic sample); deeply-nested embedding (the curated 2+-level-nesting sample).


## Activity Log
### 2026-09-25T11:53:17Z (UTC)
Closed without its own PR. Delivered by: T-7916a198 | Reason: Already satisfied: ExtractCorpusManifestTests covers corpus adversarial/nesting/macro samples (38 pass); delivered by T-7916a198 and T-e0a5bfec.