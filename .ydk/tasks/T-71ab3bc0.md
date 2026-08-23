---
id: T-71ab3bc0
title: Assemble and finalize samples/manifest.json as the ground-truth corpus manifest
story: S-6bc8566f
status: open
assignee: null
labels: []
dependencies:
- T-f96fa73d
- T-c573e72e
- T-3bddbe75
- T-83b4a029
- T-7cdaeceb
spec_refs:
- docs/specs/dataset-curation.md
component_refs:
- ydk:nfr:extraction/nesting-depth-guard
test_strategy: "No test \u2014 infra/tooling task; this is the manifest-validation\
  \ gate Epic 9's integration tests depend on."
acceptance_criteria:
- text: samples/manifest.json contains entries for all 18 synthetic + 12 curated +
    7 adversarial samples (37 total)
  done: false
- text: Every sample file on disk under samples/ has a corresponding manifest entry;
    a sample without a manifest entry does not count toward corpus coverage
  done: false
- text: Every manifest entry's format, composition-rule, expected-detected-format,
    and exact-expected-content fields are present and non-placeholder
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:21Z'
updated: '2026-08-19T12:47:24Z'
---

## Description

Merge the synthetic, curated, and adversarial manifest entries into one finalized samples/manifest.json, validating every sample file on disk has a corresponding manifest entry and every manifest entry points to a real sample file, closing out Dataset Curation as the ground truth the test suite asserts against.


## Activity Log