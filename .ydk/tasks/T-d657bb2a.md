---
id: T-d657bb2a
title: Generate multi-embedding synthetic samples for all 6 formats
story: S-41ebb945
status: done
assignee: agent
labels:
- in-review
dependencies:
- T-5e9dbd18
spec_refs:
- docs/specs/dataset-curation.md
component_refs:
- ydk:req:extraction/subfile-scope
- ydk:req:extraction/unicode-fidelity
test_strategy: "No test \u2014 infra/tooling task; samples are asserted against by\
  \ Epic 9's integration tests once recorded in the corpus manifest."
acceptance_criteria:
- text: 6 multi-embedding synthetic samples exist (1 per format), each with at least
    3 first-layer embedded objects/media of at least 2 different kinds
  done: false
- text: Each sample's body text and at least 2 metadata fields contain multilingual/mixed-script
    text and emoji
  done: false
- text: Each sample's exact expected subfile count and filenames are known and recordable
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:20Z'
updated: '2026-08-23T18:47:51Z'
---

## Description

Generate one synthetic sample per format (6 total) with at least 3 first-layer embedded objects/media of at least 2 different kinds, including multilingual/emoji content in body text and at least 2 metadata fields.


## Activity Log
### 2026-08-23T18:47:51Z (UTC)
## Verification Proof

OK dotnet-build (2.5s)
OK dotnet-format (15.8s)
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
OK dotnet-test (6.9s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/20