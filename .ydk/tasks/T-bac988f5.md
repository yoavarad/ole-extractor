---
id: T-bac988f5
title: Research legacy .doc/.ppt parsing library (NPOI mainline covers only .xls)
story: S-e496f356
status: in-progress
assignee: agent
labels:
- in-progress
dependencies: []
spec_refs: []
component_refs: []
test_strategy: 'Manual: a spike/prototype using the chosen approach successfully opens
  a real .doc or .ppt fixture file'
acceptance_criteria:
- text: Decision recorded (ADR update or new ADR) on how .doc and .ppt open components
    will be implemented, given NPOI mainline doesn't provide them
  done: true
- text: T-c4123b53 and T-517f89c5 unblocked with a concrete, buildable path forward
    (or explicitly re-scoped/re-written if the approach changes their acceptance criteria)
  done: true
milestone: null
complexity: null
gates: []
created: '2026-08-23T04:48:14Z'
updated: '2026-08-23T05:54:08Z'
---

## Description

T-d6de875f wired NPOI as a source-compiled submodule per ADR-001, but found ADR-001's assumption wrong for two of the three legacy formats: HWPFDocument (.doc) and HSLF (.ppt, HSLFSlideShow) only exist in NPOI's scratchpad/ tree, which uses non-SDK-style .csproj files hard-targeted at .NET Framework 2.0 and isn't even included in NPOI's own published NuGet package (confirmed by inspecting the real 2.7.6 nupkg contents). Only HSSFWorkbook (.xls) builds cleanly for net8.0 via this self-compiled path. This task scopes the decision for .doc/.ppt: either (a) port/modernize NPOI's scratchpad HWPF/HSLF code to SDK-style net8.0 ourselves, or (b) source a different OSS library for these two formats specifically, or (c) some other approach. Should produce a decision recorded as an ADR update or new ADR, unblocking T-c4123b53 (doc open component) and T-517f89c5 (ppt open component), both currently blocked on this.

## Activity Log

- 2026-08-23: Researched .doc/.ppt legacy body-text parsing options given NPOI's HWPF/HSLF
  live only in its unbuilt, non-SDK-style scratchpad tree. Found NPOI's HPSF/POIFS (main
  tree, already net8.0-buildable) decouple FileMetadata extraction from this problem
  entirely -- not blocked regardless of body-text approach. Recommended, as a DRAFT decision
  pending human sign-off (ADR-004, docs/adrs/004-legacy-doc-ppt-parsing.md): b2xtranslator
  (BSD-3-Clause) to convert .doc->.docx and .ppt->.pptx in-process, then reuse the existing
  DocumentFormat.OpenXml extraction path for both. Alternatives researched and documented:
  porting NPOI scratchpad HWPF/HSLF ourselves (feasible per a validated third-party retarget
  precedent, github.com/IllidanS4/npoi, kept as .doc-specific fallback), modeverv/dotnet-poi
  (rejected on provenance/trust grounds -- ~48 hours of commit history), and LibreOffice
  headless subprocess conversion (kept as last-resort fallback). Full spike notes:
  docs/research/legacy-doc-ppt-parsing.md. Left pointer notes in T-c4123b53 and T-517f89c5
  Activity Logs; ADR-001 updated with a cross-reference note. ADR-004 committed as Proposed,
  not Accepted -- requires explicit human sign-off before implementation starts.
