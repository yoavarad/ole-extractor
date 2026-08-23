---
id: E-a2efe648
title: Library Setup & Validation
status: open
release: ''
spec_refs:
- docs/adrs/001-extraction-library-stack.md
- docs/project-rules.md
labels: []
milestone: null
created: '2026-08-19T10:31:24Z'
updated: '2026-08-19T10:31:24Z'
---

## Description

Wire the ADR-001-locked libraries (self-compiled NPOI, OpenMcdf>=3.1.3, DocumentFormat.OpenXml already present) into the build, and empirically validate the flagged-as-unverified NPOI Unicode/i18n fidelity on the legacy OLE read path against real multilingual samples. Does not re-research library choice -- ADR-001/002 already lock that decision; this epic is the remaining engineering work to make the picks real and trustworthy.


## Activity Log
