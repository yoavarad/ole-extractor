---
id: T-ce2e3cc1
title: 'Fix dotnet-build/dotnet-format target discovery: alphabetical-first-csproj
  is unsafe with multiple sibling projects'
story: S-d01346bc
status: in-progress
assignee: agent
labels:
- in-review
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
updated: '2026-08-22T21:20:01Z'
---

## Description

This repo now has multiple sibling .csproj files (ExtractorOLE.csproj, ExtractorOLE.Tests.csproj, ExtractorOLE.Benchmarks.csproj, ExtractorOLE.StressHarness.csproj, SampleGenerator.csproj). .ydk/verifications/dotnet-build/check.py and dotnet-format/check.py fall back to 'first *.csproj found, sorted alphabetically' when no *.sln exists (ExtractorOle.slnx exists but is deliberately excluded from target-discovery since the .NET 8 SDK CLI can't build .slnx -- MSB4068). Alphabetical sort means ExtractorOLE.Benchmarks.csproj now sorts before ExtractorOLE.csproj and silently becomes the pre-push build/format gate's target instead of the actual library -- the gate would stop verifying the real code without any visible warning. This is a ydk-core plugin bug (in check.py's target-discovery logic), not specific to this repo's code, but the fix needs to ship as a project-local plugin override here (or upstream in ydk-core, tracked separately per docs/research/ydk-dotnet-support-recommendation.md).

## Activity Log
### 2026-08-22T21:20:01Z (UTC)
## Verification Proof

OK dotnet-build (2.9s)
OK dotnet-format (24.6s)
OK ai-code-review (0.0s)
OK cli-error-handling (0.0s)
OK cli-output-format (0.0s)
OK fastapi-adapter-isolation (0.0s)
OK fastapi-core-purity (0.0s)
OK fastapi-no-mocks-e2e (0.0s)
OK fastapi-no-mocks-integration (0.0s)
OK fastapi-route-delegation (0.0s)
OK fastapi-route-splitting (0.0s)
OK fastapi-service-sync (0.0s)
OK nextjs-file-sizes (0.0s)
OK nextjs-fsd-imports (0.0s)
OK nextjs-fsd-layers (0.0s)
OK nextjs-module-density (0.0s)
OK nextjs-no-direct-env (0.0s)
OK nextjs-no-direct-fetch (0.0s)
OK nextjs-no-direct-ui-imports (0.0s)
OK nextjs-no-query-key-strings (0.0s)
OK nextjs-no-useeffect-fetch (0.0s)
OK nextjs-no-zustand-module-level (0.0s)
OK nextjs-page-purity (0.0s)
OK nextjs-server-client-boundary (0.0s)
OK nextjs-sse-abort-controller (0.0s)
OK python-file-length (0.0s)
OK python-no-future-annotations (0.0s)
OK react-fsd-imports (0.0s)
OK spec-alignment (0.0s)
OK terraform-dangling-resources (0.0s)
OK terraform-external-iam (0.0s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.0s)
OK terraform-paravirt-ec2 (0.0s)
OK terraform-public-ami (0.0s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (5.6s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/12
