---
id: T-d6de875f
title: Self-compile NPOI from Apache-2.0 source and reference it
story: S-e496f356
status: in-progress
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

- 2026-08-23: Wired NPOI as a git submodule (third_party/npoi, pinned to tag 2.7.6-rc1) referenced via ProjectReference from ExtractorOLE.csproj. dotnet build succeeds, 0 errors/warnings. Found ADR-001's assumption only holds for .xls (HSSFWorkbook, in main/NPOI.Core.csproj) -- .doc (HWPFDocument) and .ppt (HSLF) only exist in NPOI's non-SDK-style, .NET-Framework-2.0-only scratchpad/ tree, not shipped in NPOI's own NuGet package. Filed T-bac988f5 to resolve doc/ppt separately; blocked T-c4123b53 and T-517f89c5 on it. Note for future checkouts: `git submodule update --init` is needed after cloning/pulling since submodules only record a gitlink, not file content.