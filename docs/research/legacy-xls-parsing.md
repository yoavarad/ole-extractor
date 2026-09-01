# Legacy .xls parsing -- research spike (T-b70522d2)

Follow-up to `docs/research/legacy-doc-ppt-parsing.md` and ADR-004, which moved .doc/.ppt
body-text parsing off NPOI onto b2xtranslator but left .xls on NPOI's `HSSF` (the one legacy
format NPOI's `main/` tree handles cleanly). This spike asks whether b2xtranslator's `Xls`
module can do the same job for .xls that `Doc`/`Ppt` already do -- and, if so, whether NPOI can
be retired from this project entirely. This is a desk-research pass only; no code was retargeted
or run (see "Open Questions / Next Steps").

## Module overview

`b2xtranslator` (github.com/EvolutionJobs/b2xtranslator) has an `Xls/` folder alongside `Doc/`
and `Ppt/`, symmetric in top-level structure:

- `Xls/b2xtranslator.xls.csproj` -- SDK-style, `netcoreapp2.0`. Same retarget-to-`net8.0` need
  already identified for `Doc`/`Ppt` in ADR-004; no reason to expect it behaves differently here.
- `Xls/XlsFileFormat/` -- roughly 200 record classes (BOF, formulas, charts, autofilter, etc),
  a dedicated `Ptg/` formula-token parser giving full BIFF8 formula support, plus `Structures/`
  and `StyleData/`, `ChartSequences/`.
- `Xls/SpreadsheetMLMapping/` -- 17 files: `WorkbookMapping.cs`, `WorksheetMapping.cs`,
  `FormulaMapping.cs`, `StylesMapping.cs`, `ChartMapping/`, `DrawingMapping.cs`,
  `TextBodyMapping.cs`.

Structurally, `Xls` is comparable in scope/completeness to `Doc`/`Ppt` -- not a stub module.

## Structure/maturity vs. Doc/Ppt

Same pedigree and same open questions as the rest of b2xtranslator (spec-driven 2009 DIaLOGIKa
origin, ported to .NET Core in 2017, stale since 2018-06-26 -- see
`docs/research/legacy-doc-ppt-parsing.md` for the shared history). Nothing found suggests `Xls`
was written to a lower standard than `Doc`/`Ppt`: the BIFF8 formula-token parser (`Ptg/`) in
particular looks like a genuinely complete piece of engineering, not a partial port.

## Adoption evidence

Weaker than `Doc`/`Ppt`. No `Xls`-specific issues or bug reports were found on the fork (ADR-004
characterized `Doc`/`Ppt` adoption as "real if dated" -- no equivalent signal turned up for
`Xls`). One narrow signal did surface: github.com/michaelweber/Macrome, a security-research tool
for crafting malicious XLS macros, includes a modified copy of
`b2xtranslator/Xls/XlsFileFormat/PtgHelper.cs` directly. That is third-party trust in the BIFF8
formula-token parser internals specifically, not evidence of full-file conversion fidelity.

## Critical gap: embedded OLE objects

`Doc`'s `WordprocessingMLMapping/` has a dedicated `OleObjectMapping.cs` (writes embedded OLE
objects into `word/embeddings`) plus `VMLPictureMapping.cs`. `Xls`'s `SpreadsheetMLMapping/` has
no equivalent -- only `DrawingMapping.cs` (charts/shapes) and `TextBodyMapping.cs`.
`Xls/XlsFileFormat/DataExtraction/WorksheetExtractor.cs` handles the `MsoDrawing` record but has
no distinct OLE-object code path; drawing objects only appear to be "partially processed". The
generic `Common/OpenXmlLib/EmbeddedObjectPart.cs` plumbing exists at the library level, but
nothing `Xls`-specific was found that invokes it.

**Conclusion: b2xtranslator's `Xls` conversion likely does not preserve embedded/subfile OLE
objects into `xl/embeddings` the way `Doc`'s conversion does for `word/embeddings`.** This is the
single most consequential finding of this spike -- see "Current repo reality" below for why it
matters even if .xls body text itself moves to b2xtranslator.

## Current repo reality (verified in-code, not just b2x-side)

The repo's own POIFS-based subfile/embedded-object discovery for .xls
(`ExtractorOle/ExtractorOLE/Helpers/ExtractionHelper.cs`, the
`ExtractFirstLayerEmbedded(HSSFWorkbook)` overload, roughly lines 156-221) is **not**
format-agnostic today, despite ADR-004's framing of POIFS as format-agnostic. It is an
Excel-specific code path using `NPOIFSFileSystem` + `EntryUtils.CopyNodes()` against
`HSSFObjectData`, completely separate from the generic `ExtractFirstLayerEmbedded(OpenXmlPart)`
branch (roughly lines 94-154) that already handles real .docx/.pptx/.xlsx subfile discovery
today.

NPOI is used only for .xls in this codebase now (`XlsOpenStrategy.cs`, `XlsTextExtractor.cs`,
`ExtractionHelper.cs`'s Excel branch) -- .doc/.ppt already have zero NPOI code post-ADR-004. That
absence is itself confirming evidence for the gap above: .doc/.ppt's subfile discovery already
runs through the generic OOXML branch against b2x-converted output, which is consistent with
`Doc`'s `OleObjectMapping.cs` actually preserving embeds where `Xls` has no equivalent to do so.

## HPSF -> OOXML core/app property mapping

Near-complete in the general case. OOXML `docProps/core.xml` covers title, creator/author,
subject, keywords, description (comments), lastModifiedBy, lastPrinted, revision, created,
modified. `docProps/app.xml` covers Application, Company, Manager, Template, TotalTime
(cumulative edit duration), Pages/Words/Characters/Lines/Paragraphs, DocSecurity.

Two gaps exist in the general case: HPSF's Thumbnail property (needs a separate
`docProps/thumbnail.jpeg` part) and HPSF custom/user-defined properties (needs
`docProps/custom.xml`). `b2xtranslator`'s `Common/OpenXmlLib` has `CorePropertiesPart.cs`,
`AppPropertiesPart.cs`, and `CustomXmlPropertiesPart.cs` present as container classes, but actual
HPSF-field-population logic was not independently verified in this desk research.

Cross-checked against this repo's actual `FileMetadata` field usage: per
`ExtractorOle/ExtractorOLE/DTOs/DocumentExtractionResult.cs`, the repo currently only populates
Title, Creator (author), Created, Modified, and LastModifiedBy from HPSF for .xls -- all five map
cleanly onto `docProps/core.xml` alone, with no thumbnail/custom dependency for this repo's
actual field set today. Per `docs/specs/extraction.md`, the full `FileMetadata` contract also
includes subject, keywords, comments, application name, revision number, cumulative editing
duration, and last-printed timestamp -- all of which map onto core.xml + app.xml per the general
mapping above. So full `FileMetadata` coverage should be achievable via core+app for all three
legacy formats without needing `custom.xml` or thumbnail parts.

## No real .xls fixtures

No real .xls sample files exist anywhere in the repo. Confirmed: only `samples/curated/{xlsx,
docx,pptx}`, all OOXML. The .xls test fixtures in
`ExtractorOle/ExtractorOLE.Tests/Excel/XlsOpenStrategyTests.cs` are 100% built programmatically
via NPOI in-memory, not real-world files. This blocks the empirical fidelity check
(`test_strategy`) called for in T-b70522d2 until real/representative .xls fixtures are sourced.

## Fork attempt blocked

A fork of b2xtranslator to an org-controlled GitHub account (`yoavarad`) was attempted as part of
this task, but was blocked by this session's tool-permission classifier (`gh repo fork` requires
explicit user approval). The retargeting/build/empirical-fidelity spike could not be executed in
this pass as a result -- this is a desk-research-only pass.

## Alternatives

Same alternative-library landscape already surveyed in `docs/research/legacy-doc-ppt-parsing.md`
applies here (NPOI scratchpad ports, modeverv/dotnet-poi, LibreOffice headless conversion,
commercial SDKs) -- not re-litigated in this doc. The one addition specific to this spike is
OpenMcdf (already a project dependency per ADR-001, MPL-2.0, format-agnostic CFB introspection)
as a possible replacement for NPOI's POIFS specifically, for embedded-object discovery, since
POIFS-equivalent functionality needs to stay regardless of what happens to .xls body text (see
"Critical gap" above). Not evaluated in depth in this pass -- flagged for follow-up.

## Open Questions / Next Steps

1. Source or synthesize real-world-representative .xls fixtures.
2. Get explicit approval and execute the b2xtranslator fork (blocked this pass by the
   tool-permission classifier).
3. Retarget `Xls/b2xtranslator.xls.csproj` to `net8.0` and run the empirical fidelity spike
   (b2x-converted .xlsx vs. current HSSF extraction) before any decision to swap .xls body text.
4. Evaluate OpenMcdf (already a project dependency per ADR-001, MPL-2.0, format-agnostic CFB
   introspection) as a possible NPOI-POIFS replacement for .xls embedded-object discovery
   specifically, as a path to still fully retire NPOI-the-library even though POIFS-equivalent
   functionality must stay.

## Sources

- https://github.com/EvolutionJobs/b2xtranslator (`Xls/`, `Doc/`, `Common/OpenXmlLib/`)
- https://github.com/michaelweber/Macrome
- ECMA-376/OOXML docProps schema (`docProps/core.xml`, `docProps/app.xml`)
