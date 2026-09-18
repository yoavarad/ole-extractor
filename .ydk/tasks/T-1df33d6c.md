---
id: T-1df33d6c
title: Implement doc first-layer embedded-subfile walk
story: S-32677db6
status: done
assignee: null
labels:
- in-review
dependencies:
- T-c4123b53
spec_refs:
- docs/specs/extraction.md
component_refs:
- ydk:entity:extraction/SubfileItem
- ydk:req:extraction/subfile-scope
test_strategy: 'Unit-test-backed: unit test against a .doc fixture with one valid
  and one corrupt embedded object asserts the valid one appears in Subfiles, the corrupt
  one is omitted and logged, and the call does not throw.'
acceptance_criteria:
- text: First-layer embedded objects and inline media in a .doc fixture appear as
    correct SubfileItem entries (Binary, FileName from CFB storage name, SizeBytes)
  done: false
- text: A subfile that is itself a container is returned as one opaque SubfileItem,
    not recursively unpacked
  done: false
- text: A single corrupt embedded object is omitted from Subfiles and logged; Extract()
    still returns successfully for the rest of the document
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:56Z'
updated: '2026-09-18T21:31:33Z'
---

## Description

Walk the .doc CFB structure for first-layer OLE-embedded objects and inline media, producing SubfileItem entries per subfile-scope. A corrupt embedded object is omitted and logged without failing the overall extraction call.


## Activity Log
### 2026-09-18T11:14:07Z (UTC)
**Verification FAILED:**
FAIL dotnet-test: Determining projects to restore...
  All projects are up-to-date for restore.
  b2xtranslator -> C:\
### 2026-09-18T11:15:32Z (UTC)
## Verification Proof

OK dotnet-build (12.5s)
OK dotnet-format (64.0s)
OK dotnet-quality (97.7s)
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
OK spec-alignment (2.3s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.1s)
OK terraform-paravirt-ec2 (0.1s)
OK terraform-public-ami (0.1s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (18.0s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/97