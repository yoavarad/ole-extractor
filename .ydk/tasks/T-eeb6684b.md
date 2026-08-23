---
id: T-eeb6684b
title: Downgrade ExtractorOLE.csproj to net8.0
story: S-d01346bc
status: done
assignee: agent
labels:
- in-progress
dependencies: []
spec_refs:
- docs/project-rules.md
component_refs: []
test_strategy: 'Manual verification: dotnet build ExtractorOLE/ExtractorOLE.csproj
  from a clean checkout succeeds with exit code 0.'
acceptance_criteria:
- text: ExtractorOLE.csproj TargetFramework element is net8.0
  done: false
- text: dotnet build ExtractorOLE/ExtractorOLE.csproj succeeds with no new warnings/errors
    introduced by the framework change
  done: false
- text: Existing package references remain compatible with net8.0 (no forced downgrade
    required, or documented if one is)
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T10:32:30Z'
updated: '2026-08-22T11:46:40Z'
---

## Description

Change TargetFramework from net10.0 to net8.0 in ExtractorOLE/ExtractorOLE.csproj per the deliberate downgrade decision recorded in docs/project-rules.md; confirm the project still builds and that existing NuGet package versions (DocumentFormat.OpenXml, Microsoft.Extensions.DependencyInjection) remain compatible with net8.0.


## Activity Log