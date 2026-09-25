---
id: S-fe24ecff
title: Wire Program.cs to invoke both public functions on a real file
epic: E-d4ac3268
status: done
spec_refs:
- docs/specs/overview.md
acceptance_criteria:
- text: Running the ExtractorOLE console app against a real sample file prints real,
    non-placeholder detection and extraction output
  done: false
- text: Program.cs invokes detection then extraction in sequence
  done: false
labels: []
milestone: null
created: '2026-08-19T10:31:24Z'
updated: '2026-08-19T10:31:24Z'
---

## Description

Update Program.cs so it actually calls the detect and extract entry points against a real file on disk and prints/logs real output, replacing any placeholder/no-op behavior.


## Activity Log
