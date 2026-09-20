---
id: T-afb7e032
title: Extract pptx text in deck slide order instead of relationship order
story: S-37c18545
status: in-review
assignee: null
labels:
- bug
- in-review
dependencies: []
spec_refs:
- docs/specs/extraction.md
- docs/specs/testing-strategy.md
component_refs:
- ydk:contract:extraction/extract
test_strategy: Unit test on a synthetic reordered deck (deterministic) plus a corpus-sample-backed
  order assertion on the NOAA pptx, which is the known reordered deck.
acceptance_criteria:
- text: PptxTextExtractor emits slides in the order of p:sldIdLst in presentation.xml,
    each slide followed by its own notes text, as it did before
  done: false
- text: A test using the NOAA sample (samples/curated/pptx/noaa-transition-team-briefing-2008.pptx)
    asserts that the extracted text begins with the text of deck slide 1 (the first
    p:sldId) and that slide 1 text appears before slide 2 text
  done: false
- text: A synthetic unit test builds a pptx whose relationship order differs from
    sldIdLst order (for example slides added as A,B,C with sldIdLst listing C,A,B)
    and asserts output order C,A,B
  done: false
- text: Slide count, notes count and other behavior of the pptx handlers are unchanged;
    the full dotnet test suite passes
  done: false
- text: Unicode fidelity is preserved (no normalization added); existing pptx unicode
    tests still pass
  done: false
milestone: null
complexity: null
gates: []
created: '2026-09-20T08:12:32Z'
updated: '2026-09-20T10:29:59Z'
---

## Description

Bug. ExtractorOle/ExtractorOLE/Handlers/PptxTextExtractor.cs iterates presentationPart.SlideParts, which yields slides in package relationship order, not in deck order. The deck order is defined by the p:sldIdLst element in presentation.xml (each p:sldId references its slide part by relationship id). For decks whose slides were reordered after creation the flat text is therefore in the wrong order; the extracted text for samples/curated/pptx/noaa-transition-team-briefing-2008.pptx starts at what is slide 7 in the deck. Fix by resolving slides through PresentationPart.Presentation.SlideIdList and GetPartById(slideId.RelationshipId), keeping the existing per-slide behavior (slide body text, then that slide's notes text). Check whether the legacy/other slide walks that enumerate SlideParts (for example slide counting or embedded walk) need the same ordering, and only change flat-text extraction unless order also affects subfile output.


## Activity Log
### 2026-09-20T10:16:46Z (UTC)
**Verification FAILED:**
FAIL dotnet-test: Determining projects to restore...
  All projects are up-to-date for restore.
  b2xtranslator -> C:\
### 2026-09-20T10:29:59Z (UTC)
## Verification Proof

OK dotnet-build (4.8s)
OK dotnet-format (40.1s)
OK dotnet-quality (41.9s)
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
OK spec-alignment (2.0s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.2s)
OK terraform-paravirt-ec2 (0.2s)
OK terraform-public-ami (0.2s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (14.7s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/123