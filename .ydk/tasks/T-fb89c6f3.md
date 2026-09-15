---
id: T-fb89c6f3
title: Add null request/field boundary validation to DetectMimeType
story: S-072bfdf6
status: done
assignee: null
labels:
- in-review
dependencies:
- T-ef73861f
spec_refs:
- docs/specs/overview.md
component_refs:
- ydk:entity:extraction/MimeDetectionRequest
test_strategy: 'Unit-test-backed: unit tests assert a null request and a null FileBytes
  each throw before any detection component executes, verified via a spy/counter on
  the registry.'
acceptance_criteria:
- text: A null MimeDetectionRequest throws immediately, before any detection component
    is invoked
  done: false
- text: A MimeDetectionRequest with a null FileBytes throws immediately, before any
    detection component is invoked
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:59Z'
updated: '2026-09-15T20:18:50Z'
---

## Description

Add standard argument validation to the DetectMimeType facade so a null request object, or a null FileBytes, throws immediately before any detection logic runs — boundary validation, not a domain error.


## Activity Log
### 2026-09-14T20:57:33Z (UTC)
## Verification Proof

OK dotnet-build (3.3s)
OK dotnet-format (30.0s)
OK dotnet-quality (32.8s)
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
OK spec-alignment (1.0s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.1s)
OK terraform-paravirt-ec2 (0.1s)
OK terraform-public-ami (0.1s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (4.4s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/81