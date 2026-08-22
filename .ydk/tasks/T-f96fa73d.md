---
id: T-f96fa73d
title: Record synthetic samples in samples/manifest.json
story: S-41ebb945
status: open
assignee: null
labels: []
dependencies:
- T-d657bb2a
- T-d91b2a46
- T-4b624fef
spec_refs:
- docs/specs/dataset-curation.md
component_refs: []
test_strategy: "No test \u2014 infra/tooling task; this manifest is the ground truth\
  \ Epic 9's integration tests assert against."
acceptance_criteria:
- text: 18 synthetic samples exist (3 per format x 6 formats) per dataset-curation.md's
    Corpus Composition rule 1
  done: false
- text: Every synthetic sample's manifest entry records format, composition rule satisfied,
    expected detected format, and exact expected text/metadata/subfile-count+filenames
    (not descriptions)
  done: false
- text: Every synthetic sample is tagged self-generated in samples/manifest.json
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:20Z'
updated: '2026-08-19T12:47:24Z'
---

## Description

Record all 18 synthetic samples (3 per format x 6 formats) in samples/manifest.json: format, which composition rule each satisfies, expected detected format, and exact expected text/metadata strings and subfile count+filenames.


## Activity Log