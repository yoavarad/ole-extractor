---
id: QD-e3ba80
title: File follow-up tasks from the corpus integration test and load test flakiness
status: in-review
type: quickdev
updated: '2026-09-20T09:49:19Z'
labels:
- in-review
---

File follow-up tasks from the corpus integration test and load test flakiness
### 2026-09-20T09:48:05Z (UTC)
**Verification FAILED:**
FAIL dotnet-build: Plugin timed out after 120s
### 2026-09-20T09:49:19Z (UTC)
## Verification Proof

OK dotnet-build (9.2s)
OK dotnet-format (77.7s)
OK dotnet-quality (89.9s)
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
OK spec-alignment (5.0s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.1s)
OK terraform-paravirt-ec2 (0.1s)
OK terraform-public-ami (0.1s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (40.6s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/122