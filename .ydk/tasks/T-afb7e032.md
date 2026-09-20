---
id: T-afb7e032
title: Extract pptx text in deck slide order instead of relationship order
story: S-37c18545
status: open
assignee: null
labels:
- bug
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
updated: '2026-09-20T08:12:32Z'
---

## Description

Bug. ExtractorOle/ExtractorOLE/Handlers/PptxTextExtractor.cs iterates presentationPart.SlideParts, which yields slides in package relationship order, not in deck order. The deck order is defined by the p:sldIdLst element in presentation.xml (each p:sldId references its slide part by relationship id). For decks whose slides were reordered after creation the flat text is therefore in the wrong order; the extracted text for samples/curated/pptx/noaa-transition-team-briefing-2008.pptx starts at what is slide 7 in the deck. Fix by resolving slides through PresentationPart.Presentation.SlideIdList and GetPartById(slideId.RelationshipId), keeping the existing per-slide behavior (slide body text, then that slide's notes text). Check whether the legacy/other slide walks that enumerate SlideParts (for example slide counting or embedded walk) need the same ordering, and only change flat-text extraction unless order also affects subfile output.


## Activity Log
