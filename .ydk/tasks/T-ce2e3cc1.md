---
id: T-ce2e3cc1
title: 'Fix dotnet-build/dotnet-format target discovery: alphabetical-first-csproj
  is unsafe with multiple sibling projects'
story: S-d01346bc
status: in-progress
assignee: agent
labels:
- in-progress
dependencies: []
spec_refs: []
component_refs: []
test_strategy: Add a throwaway sibling csproj file alphabetically before ExtractorOLE.csproj
  in a test fixture / manual repro, confirm dotnet-build's check.py still verifies
  ExtractorOLE.csproj (and any other real project), not just the alphabetically-first
  one.
acceptance_criteria:
- text: dotnet-build and dotnet-format check.py build/format every non-test/non-bench/non-harness
    *.csproj under the repo (or add a real .sln that includes all projects and target
    that), not just an arbitrary alphabetically-first one
  done: false
- text: Adding a new sibling csproj file does not silently change which project the
    pre-push gate verifies
  done: false
- text: Existing behavior (skip .slnx, correctly find ExtractorOLE.Tests.csproj for
    dotnet-test) is preserved
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-22T20:46:03Z'
updated: '2026-08-22T20:58:03Z'
---

## Description

This repo now has multiple sibling .csproj files (ExtractorOLE.csproj, ExtractorOLE.Tests.csproj, ExtractorOLE.Benchmarks.csproj, ExtractorOLE.StressHarness.csproj, SampleGenerator.csproj). .ydk/verifications/dotnet-build/check.py and dotnet-format/check.py fall back to 'first *.csproj found, sorted alphabetically' when no *.sln exists (ExtractorOle.slnx exists but is deliberately excluded from target-discovery since the .NET 8 SDK CLI can't build .slnx -- MSB4068). Alphabetical sort means ExtractorOLE.Benchmarks.csproj now sorts before ExtractorOLE.csproj and silently becomes the pre-push build/format gate's target instead of the actual library -- the gate would stop verifying the real code without any visible warning. This is a ydk-core plugin bug (in check.py's target-discovery logic), not specific to this repo's code, but the fix needs to ship as a project-local plugin override here (or upstream in ydk-core, tracked separately per docs/research/ydk-dotnet-support-recommendation.md).

## Activity Log