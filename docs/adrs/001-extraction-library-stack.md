# ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)

## Status
Accepted

## Context
Need OSS C# libraries to parse two format families: OOXML (docx/xlsx/pptx, zip-based) and legacy binary OLE/CFB (doc/xls/ppt), plus a lightweight way to sniff a legacy file's exact type from its CFB directory structure without a full parse. Research spike (`docs/research/library-tradeoffs.md`, `documentformat.openxml.md`, `npoi.md`, `openmcdf.md`, `legacy-landscape-scan.md`) evaluated candidates against license, maturity, API fit, and i18n/emoji handling.

## Decision
- **OOXML (docx/xlsx/pptx)**: keep `DocumentFormat.OpenXml` (MIT, Microsoft-maintained) — already the working baseline in the existing scaffold; no case found to replace it.
- **Legacy OLE (doc/xls/ppt)**: `NPOI`, compiled from its Apache-2.0 source ourselves rather than consuming the precompiled NuGet package.
- **CFB introspection / mime-sniffing**: `OpenMcdf` (MPL-2.0), pinned to **≥3.1.3** (patches a directory-cycle DoS CVE below that version).

## Alternatives Considered
- **NPOI via precompiled NuGet (2.8.0+)**: rejected — that package ships under a EULA requiring a paid maintenance-fee subscription for revenue-generating organizations. The underlying source is still Apache-2.0, so self-compiling avoids the fee while keeping the same library.
- **Pin NPOI to a pre-2.8.0 NuGet release**: rejected in favor of self-compiling — would freeze us on an older, unpatched feature/bugfix set.
- **Hand-roll legacy binary format parsing via OpenMcdf alone**: rejected — far more implementation work than adopting NPOI, for a harder/older format family with no clear payoff over self-compiling NPOI.
- **NPOI for OOXML too** (it can do both): rejected — no reason to replace the already-working `DocumentFormat.OpenXml` path.

## Note (2026-08-23)
Refined by **ADR-004**: this decision's "Legacy OLE (doc/xls/ppt): NPOI" line held only for
**.xls**. NPOI's `.doc`/`.ppt` body parsers (HWPF/HSLF) turned out to live in a scratchpad tree
NPOI itself doesn't build or ship (see ADR-004 for the full finding and the resulting decision
to use `b2xtranslator` for .doc/.ppt body text instead). NPOI (main tree: HSSF/HPSF/POIFS)
remains the answer for .xls and for cross-format metadata/subfile discovery on all three legacy
formats.

## Note (2026-09-02)
The Consequences section below flagged legacy-path Unicode/RTL/CJK/emoji fidelity as
**unverified**. That is now resolved for **.xls**: task T-94570c2b ran the empirical test
harness added in commit `8b6ae55` (`ExtractorOLE.Tests/Excel/XlsTextExtractorTests.cs`), which
round-trips NPOI-written `.xls` fixtures through `XlsTextExtractor.ExtractText` against the
multilingual/emoji corpus in `SampleGenerator/Fixtures/MultilingualFixtures.cs`. Result: **15/15
tests passed** — Hebrew, Arabic, Persian, Russian, Latin, and Cjk script fixtures, plus all 4
emoji fixtures (simple, ZWJ family, skin-tone modifier, ZWJ flag), round-trip with exact string
equality; no mojibake, truncation, or silent normalization observed. **.doc/.ppt remain
unverified** — per ADR-004, those formats use `b2xtranslator` rather than NPOI's HWPF/HSLF, so
this result does not extend to them; their i18n fidelity needs its own empirical pass once that
path is implemented. See `docs/project-rules.md` (Known Gotchas) for the full detail.

## Consequences
- Build pipeline needs an NPOI-from-source compile step (new CI/build work, tracked in the Library Evaluation / Project restructure epics).
- Legacy-path Unicode/RTL/CJK/emoji fidelity is **unverified** in NPOI per the research spike — must be empirically tested against real multilingual samples in the Dataset Curation epic before the i18n requirement can be considered met for legacy formats.
- Two different libraries now own two different format families (OpenXml for OOXML, NPOI for legacy) — acceptable per the original plan ("different libraries for different subfile types").
