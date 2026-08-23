---
id: T-d6de875f
title: Self-compile NPOI from Apache-2.0 source and reference it
story: S-e496f356
status: done
assignee: agent
labels:
- in-progress
dependencies:
- T-eeb6684b
spec_refs:
- docs/adrs/001-extraction-library-stack.md
- docs/project-rules.md
component_refs: []
test_strategy: 'Manual verification: dotnet build succeeds; a smoke-test line of code
  referencing an NPOI type (e.g. NPOI.HPSF or NPOI.POIFS) compiles.'
acceptance_criteria:
- text: NPOI is built from its own Apache-2.0-licensed source, not consumed as the
    precompiled NuGet package
  done: false
- text: ExtractorOLE.csproj references the self-compiled NPOI assembly/project
  done: false
- text: dotnet build ExtractorOLE.slnx succeeds and NPOI types are importable from
    the extraction library
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T10:32:30Z'
updated: '2026-08-22T21:03:43Z'
---

## Description

Set up a build step that compiles NPOI from its Apache-2.0 source (not the precompiled NuGet package, which carries a paid-maintenance EULA at 2.8.0+) and reference the resulting assembly/project from ExtractorOLE.csproj, per ADR-001.


## Activity Log