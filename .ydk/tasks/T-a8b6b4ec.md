---
id: T-a8b6b4ec
title: Implement doc flat-text extraction with unicode fidelity
story: S-32677db6
status: done
assignee: null
labels: []
dependencies:
- T-c4123b53
- T-94570c2b
spec_refs:
- docs/specs/extraction.md
component_refs:
- ydk:req:extraction/unicode-fidelity
- ydk:entity:extraction/ExtractionResult
test_strategy: 'Corpus-sample-backed: unit test against a hand-crafted multilingual
  .doc fixture asserts ExtractedText exactly equals the known-correct string; formal
  corpus-manifest assertion lands with Epic 9''s integration tests once Epic 8''s
  corpus exists.'
acceptance_criteria:
- text: ExtractedText is empty string (never null) for a .doc with no body text
  done: true
- text: ExtractedText for a multilingual .doc fixture matches the expected Unicode
    string exactly, byte-for-byte, per the NPOI i18n validation findings from T-94570c2b
  done: true
- text: No Unicode normalization form is applied that changes the source character
    sequence
  done: true
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:42:56Z'
updated: '2026-09-19T08:09:17Z'
---

## Description

Extract flattened plain-text body content from .doc via NPOI, ensuring Hebrew/Arabic/Persian/Russian/Latin/CJK text and emoji (including multi-codepoint sequences) round-trip without mojibake, truncation, or unwanted normalization, per unicode-fidelity.


## Activity Log
### 2026-09-18T19:55:07Z (UTC)
Implemented + committed locally (0775961), suite 196/196. Blocked: b2xtranslator fork branch net8-doc-retarget (06e87fb) needs push to yoavarad/b2xtranslator; push denied. See .ydk/proofs/T-a8b6b4ec/orchestrator-progress.md
### 2026-09-18T20:03:18Z (UTC)
**pr-body-validation FAILED:**
FAIL: Missing requirements: screenshot_for_ui
PASS: PR body contains command output in code block
PASS: PR body contains ## Summary section
PASS: PR body contains ## Test Plan section
FAIL: UI file changes detected — PR body must include screenshots
### 2026-09-18T20:06:10Z (UTC)
**pr-body-validation FAILED:**
FAIL: Missing requirements: screenshot_for_ui
PASS: PR body contains command output in code block
PASS: PR body contains ## Summary section
PASS: PR body contains ## Test Plan section
FAIL: UI file changes detected — PR body must include screenshots
### 2026-09-18T20:06:37Z (UTC)
## Verification Proof

OK dotnet-build (7.3s)
OK dotnet-format (49.6s)
OK dotnet-quality (56.8s)
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
OK spec-alignment (1.6s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.1s)
OK terraform-paravirt-ec2 (0.1s)
OK terraform-public-ami (0.1s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (16.2s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/102
### 2026-09-19T08:09:17Z (UTC)
**Resolved:** The 2026-09-18T19:55:07Z 'push denied' block is obsolete. The b2xtranslator net8.0 retarget merged upstream (yoavarad/b2xtranslator#11) and the submodule is pinned to master (PR #96, #106). PR #102 merged; T-f8ccfe08 was closed as delivered by this task (PR #108). All 3 acceptance criteria confirmed by ExtractorOLE.Tests/Doc/DocTextExtractorTests.cs (empty-not-null, exact multilingual match, no normalization); DocTextExtractorTests + MainExtractorExtractRequestTests pass (21/21) on main.