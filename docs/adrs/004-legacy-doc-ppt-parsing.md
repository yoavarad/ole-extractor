# ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)

## Status
Accepted

## Context
ADR-001 decided to self-compile NPOI from Apache-2.0 source for legacy OLE format support
(doc/xls/ppt). T-d6de875f (self-compiling NPOI as a git submodule, not yet merged) found that
assumption only holds for **.xls**: `HSSFWorkbook` lives in NPOI's modern, SDK-style `main/HSSF/`
tree and builds cleanly for net8.0. `HWPFDocument` (.doc) and HSLF (.ppt) exist only in NPOI's
`scratchpad/` tree, which is excluded from NPOI's own CI/build/pack pipeline, uses non-SDK-style
`.csproj` files hard-targeted at .NET Framework 2.0, and is not shipped in NPOI's own published
NuGet package for any target framework. T-bac988f5 (this task) researched what to do instead for
.doc and .ppt specifically. Full research notes: `docs/research/legacy-doc-ppt-parsing.md`.

One useful finding from that research: NPOI's `main/` tree also contains `HPSF` (the
`SummaryInformation`/`DocumentSummaryInformation` property-set reader) and `POIFS` (generic
CFB/OLE2 directory access) -- both format-agnostic, both already net8.0-buildable today. This
means the [ydk:entity:extraction/FileMetadata] acceptance criteria on T-c4123b53/T-517f89c5 are
**not** blocked by the scratchpad problem this ADR is about; only flat-text body extraction (and,
to a lesser extent, subfile discovery) is. Implementers should wire metadata via HPSF/POIFS
immediately regardless of this ADR's outcome.

## Decision
- **.doc**: use **b2xtranslator** (BSD-3-Clause, github.com/EvolutionJobs/b2xtranslator) to
  convert the legacy binary .doc to a real .docx in-process, then feed the result through the
  project's already-built `DocumentFormat.OpenXml` extraction path -- no separate doc-specific
  body-parsing/metadata-mapping code.
- **.ppt**: same approach -- b2xtranslator's Ppt module converts .ppt to .pptx, then the
  existing OpenXml pptx path handles it.
- Both format-specific `.csproj`s (`Doc/b2xtranslator.doc.csproj`, `Ppt/b2xtranslator.ppt.csproj`)
  need retargeting from `netcoreapp2.0` to `net8.0`; b2xtranslator has zero external
  dependencies, so this is expected to be a small, low-risk spike (TFM bump + fix whatever's
  since gone obsolete in the BCL), not a scratchpad-style rewrite.
- Per ADR-001's own unicode-fidelity caveat for legacy formats: the b2x conversion step itself
  must be included in the empirical multilingual-corpus testing already required before the
  i18n requirement is considered met for .doc/.ppt -- this ADR does not presume it passes.

## Alternatives Considered
- **Port NPOI's own scratchpad HWPF/HSLF to SDK-style/net8.0 ourselves**: proven mechanically
  feasible -- a public fork (IllidanS4/npoi, Apache-2.0) already retargeted both
  `scratchpad/HWPF` and `scratchpad/HSLF` to SDK-style `netstandard2.0` with a small, identified
  diff (drop `AllowPartiallyTrustedCallers`, retarget TFM). Good option for .doc specifically --
  HWPF's `HWPFDocument` -> `Range`/`Paragraph` surface is reasonably complete and has real-world
  use (the derivative `NPOI.HWPFCore` NuGet package has 160K+ downloads and two real
  dependents). Rejected as the *primary* choice for **symmetry**: HSLF has no working
  `HSLFSlideShow`-equivalent entry point anywhere in NPOI's C# scratchpad (confirmed via GitHub
  code search -- every C# `HSLFSlideShow` hit is Java/Apache-POI or Android; NPOI's HSLF is real
  Model/Record classes but missing the "open file -> assemble slides" glue), so this route would
  need materially more original engineering for .ppt than for .doc, and would leave the project
  running two different legacy-format libraries/approaches instead of one. Kept as the
  documented fallback for **.doc** if b2xtranslator's fidelity fails empirical testing.
- **modeverv/dotnet-poi** (Apache-2.0, from-scratch HSSF/HWPF/HSLF/XSSF/XWPF/XSLF port,
  structurally the most complete single answer found): rejected on trust/provenance grounds, not
  license grounds. Its entire commit history spans ~48 hours (2026-05-07 to 2026-05-09), the repo
  is 3 months old with 1 star and no independent adoption, and the commit pattern reads as rapid,
  largely-unattended AI-agent-driven development. Not an acceptable foundation for a shipped
  product's legacy-format parsing without independent scrutiny this project has no way to
  provide. Worth re-evaluating if it accumulates real-world usage later.
- **LibreOffice headless subprocess conversion** (`soffice --headless --convert-to`): most
  mature/best-fidelity option evaluated, and license-clean for subprocess use (no linking).
  Rejected as disproportionate for now -- requires bundling a 500MB+ desktop office suite in the
  runtime environment, per-file process-spawn latency, and a larger sandboxing surface for a
  service that processes untrusted input, versus an in-process, zero-extra-runtime-dependency
  library. Documented as the fallback of last resort if both b2xtranslator and a scratchpad port
  fail fidelity testing.
- **Antiword / wvWare**: rejected -- .doc-only (no ppt), GPL/LGPL, effectively unmaintained since
  the mid-2000s, no fidelity advantage over b2xtranslator.
- **Aspose.Words / Aspose.Slides / GemBox / Syncfusion** (commercial SDKs): rejected -- same
  class of paid-license problem this repo already rejected NPOI 2.8+'s NuGet EULA and NBomber
  for (ADR-001, ADR-002). Not OSS-only-compliant.

## Consequences
- .doc and .ppt no longer go through NPOI at all for body text -- NPOI's role narrows to .xls
  (HSSF) plus HPSF/POIFS for cross-format metadata and subfile discovery on all three legacy
  formats. This project now has three legacy-format-adjacent OSS dependencies instead of one
  (NPOI, b2xtranslator, OpenMcdf), each format-agnostic where possible.
- b2xtranslator is unmaintained since 2018 -- any bug we hit against a real-world .doc/.ppt file
  is ours to patch (fork-and-fix), the same risk class as self-porting NPOI's scratchpad would
  have been, just against different source. Budget for this in the Dataset Curation / i18n
  empirical-testing epic.
- Retargeting b2xtranslator's two csproj files from `netcoreapp2.0` to `net8.0` is a small
  spike that should happen early in T-c4123b53/T-517f89c5 (or as a shared prerequisite task) --
  not assumed zero-risk, just expected low-risk given zero external dependencies.
- T-c4123b53 and T-517f89c5's acceptance criteria referencing `HWPFDocument`/`HSLFSlideShow`
  directly are now stale and need rewriting to reference b2xtranslator-then-OpenXml instead;
  their FileMetadata criteria can proceed immediately via HPSF/POIFS independent of this
  rewrite. See each task's Activity Log for a pointer back here.
- If b2xtranslator's fidelity (especially Unicode/RTL/CJK/emoji round-trip) fails empirical
  testing, the documented fallback order is: (1) port NPOI scratchpad HWPF for .doc specifically
  using IllidanS4/npoi's retarget as a recipe, (2) LibreOffice headless subprocess conversion for
  whichever format still fails.

## Note (2026-09-02)

T-b70522d2 researched extending b2xtranslator to .xls, to answer whether NPOI can be fully
retired from this project. Finding: b2x's `Xls` module is structurally comparable to `Doc`/`Ppt`
(same retarget-to-`net8.0` need, full BIFF8 record/formula support) but has weaker adoption
evidence and, critically, its `SpreadsheetMLMapping` has no `OleObjectMapping.cs` equivalent --
unlike `Doc`, it does not appear to preserve embedded OLE objects into `xl/embeddings` on
conversion. Combined with the repo's own POIFS-based .xls subfile discovery being Excel-specific
(not truly format-agnostic despite this ADR's earlier framing), the decision is:
**NPOI is not fully retired** -- POIFS-equivalent subfile/embedded-object discovery must stay for
.xls regardless of which library handles .xls body text, at least until either b2x's `Xls`
embedded-object handling is proven otherwise or replaced with OpenMcdf-based discovery.

Whether .xls body-text specifically moves to b2xtranslator (vs. staying on NPOI HSSF) remains
undecided pending an empirical fidelity spike, which is blocked on: sourcing real .xls fixtures
(none exist in the repo) and forking b2xtranslator to an org-controlled repo (attempted, blocked
by this session's tool-permission classifier -- needs explicit user approval to
`gh repo fork EvolutionJobs/b2xtranslator`).

Full findings: `docs/research/legacy-xls-parsing.md`. Tracking task: T-b70522d2.
