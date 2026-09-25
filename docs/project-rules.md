# Project Rules

Conventions, preferences, and domain knowledge.

## Product Context

C# library that takes an Office file's bytes and filename, detects its exact MIME type from content alone (doc/xls/ppt/docx/xlsx/pptx as the minimum — architecture should not preclude adding more formats later), then extracts general + format-specific metadata, flat text, and first-layer embedded subfiles (OLE objects and inline media). Backed by a curated+generated complex-file test corpus, time profiling, and a stress-test harness. OSS C# libraries only.

## Brownfield Constraints

Repo `ExtractorOle/` has a pre-existing scaffold:
- Working OOXML path (docx/xlsx/pptx) via `DocumentFormat.OpenXml`, DI-wired Strategy (open) + Handler (text) pattern via `Microsoft.Extensions.DependencyInjection`.
- `Old/` folder is dead code — confirmed via graphify (zero live edges into the active path); superseded by the Strategy/Handler refactor.
- `.csproj` targets net10.0; downgrading to net8.0 is a deliberate, explicit task (not an oversight — .NET 10 is current LTS as of 2026-08-18, .NET 8 chosen anyway).
- No legacy OLE/CFB (doc/xls/ppt) support at all yet — net-new work.
- No tests, no samples, no docs prior to this effort.

## Conventions

- **Dependency injection**: continue using DI (`Microsoft.Extensions.DependencyInjection`, as already wired in `ServiceRegistration.cs`) for new components — strategies, extractors, detectors — rather than static/manual instantiation.
- **Extensibility over the 6-format floor**: doc/xls/ppt/docx/xlsx/pptx are the minimum required formats, not the ceiling. Design the mime-detection and extraction contracts so a new format (e.g. rtf, odt) can be added without reshaping the public API.
- **Internationalization**: text and metadata extraction MUST correctly handle exotic scripts and mixed content — Hebrew, Arabic, Persian, Russian, Latin-based, and East Asian (CJK) languages — plus emoji (including surrogate-pair / multi-codepoint sequences). No mojibake, no truncation on multi-byte boundaries, no silent normalization that drops characters. This applies to both flat extracted text and metadata fields (title/author/etc.). Treat this as a first-class correctness requirement for the test corpus (Stage 02 Dataset Curation epic must include multilingual/emoji samples) and for adversarial review (Stage 01).

## Known Gotchas

- NPOI's precompiled NuGet package (2.8.0+) carries a paid maintenance-fee EULA for revenue-generating orgs — we compile NPOI from its Apache-2.0 source instead. See [ADR-001](adrs/001-extraction-library-stack.md).
- NBomber's license (v3.0+) requires a paid Commercial Subscription for org use — excluded, using a hand-rolled stress harness instead. See [ADR-002](adrs/002-profiling-and-stress-testing.md).
- OpenMcdf must be pinned to ≥3.1.3 — versions below that have a directory-cycle DoS CVE.
- NPOI's Unicode/RTL/CJK/emoji fidelity on `.xls` is **verified** (empirically tested 2026-09-02, task T-94570c2b): `ExtractorOLE.Tests/Excel/XlsTextExtractorTests.cs` round-trips NPOI-written `.xls` fixtures through `XlsTextExtractor.ExtractText` and asserts exact string equality against the known-good strings in `SampleGenerator/Fixtures/MultilingualFixtures.cs`. Full run: **15/15 passed** — all 6 script families PASS (Hebrew, Arabic, Persian, Russian, Latin, Cjk) and all 4 emoji fixtures PASS (EmojiSimple, EmojiFamilyZwj, EmojiSkinTone, EmojiFlagZwj), plus edge cases (empty workbook, no textual content, corrupt bytes, combining character sequence). No mojibake, truncation, or silent normalization observed for `.xls`. This risk remains **unverified** for `.doc`/`.ppt` — those legacy readers are not yet implemented (see the gotcha below on NPOI being `.xls` only so far) and NPOI's HWPF/HSLF code paths use different text-decoding logic than the HSSF path exercised here, so this result does not extend to them. This risk is scoped to legacy OLE only — OOXML (docx/xlsx/pptx) parses XML text directly via `DocumentFormat.OpenXml` and isn't exposed to NPOI's codepage-decoding path.
  **Contingency if a `.doc`/`.ppt` script family fails once those readers land (decided 2026-08-19):** tiered — (1) first attempt a codepage-correction post-process, re-decoding the affected text run using the codepage recorded in the OLE `CodePage`/`\x01CompObj` stream rather than trusting NPOI's default decode; (2) if that doesn't fully resolve it, document the specific script+legacy-format combo as a known v1 limitation, add a test asserting the documented limitation (not a silent pass), and narrow that combo out of the i18n-fidelity acceptance criteria rather than blocking Epic 4 indefinitely. Re-evaluate post-v1 if the limitation matters to a real use case.
- Unicode/RTL/CJK/emoji fidelity on legacy `.doc` is **verified** (task T-a8b6b4ec) for the b2xtranslator `.doc`->`.docx` path (ADR-004; not NPOI HWPF): `ExtractorOLE.Tests/Doc/DocTextExtractorTests.cs` reads a Word-authored `.doc` (`Doc/Fixtures/multilingual.doc`) and asserts exact equality against the `MultilingualFixtures` strings (all 6 script families, all 4 emoji fixtures incl. ZWJ/skin-tone, plus a decomposed combining-character sequence - no normalization). One real fidelity gap was found and fixed in the `yoavarad/b2xtranslator` fork (branch `net8-doc-retarget`): `DocumentMapping.writeText` wrote characters one UTF-16 code unit at a time, so any supplementary-plane character (emoji) made `XmlWriter` throw "The surrogate pair is invalid" and the whole conversion failed; it now writes surrogate pairs whole. Other `.doc` fidelity risk (older single-byte code-page runs beyond what the fixture covers) remains unexercised; `.ppt` is still unverified.
- **Task backend = GitHub Issues** (since 2026-09-25, task 146; see [ADR-006](adrs/006-github-issues-task-backend.md)). The old local store (`.ydk/tasks`, `.ydk/manifest.yaml`, `batch-mapping.json`, the remediate script, `task-sync.yml`) is gone; its history lives in git up to commit `36d03f9`.
  - `.ydk/config.yaml` has `project.remote: github`. Task/epic/story IDs are bare GitHub issue numbers (shared numbering with PRs).
  - `ydk task done` puts `Closes #N` in the PR body, so the issue closes on merge -- no sync workflow needed.
  - Chores go under the standing epic #144 "Maintenance" / story #145 "Chores": `ydk task create --story 145 ...`.
  - **Do not use `ydk task quick`** -- it always writes local `.ydk/tasks` QD files regardless of backend. Create a task under story 145 instead, until ydk is patched.
  - Broken on the github backend: `ydk task add-gate`/`check-gates` (gates never parsed back), `ydk task tdd` (stage dropped), `ydk task archive-done` (rewrites the issue body -- don't use), and `ydk task list --status in-progress|done` / `ydk task list --epic` (return empty -- use `gh issue list --label ...`). `ydk task list` shows only `task`-labelled issues.
  - `ydk` is the personal fork in `C:\Users\yoava\Projects\ydk`, a separate repo -- do not patch it as a side effect of work in this repo.
- NPOI is self-compiled from Apache-2.0 source via a git submodule at `third_party/npoi` (pinned to tag `2.7.6-rc1`), referenced from `ExtractorOLE.csproj` via `ProjectReference`. NPOI itself is Copyright the Apache Software Foundation / nissl-lab, licensed under Apache License 2.0 (see `third_party/npoi/LICENSE`). **Note:** `2.7.6-rc1` is a pre-release tag -- every NPOI tag past `2.5.6` is an RC, with no final release cut since; chosen anyway to get post-2.5.6 fixes without the 2.8.0+ paid-EULA packaging. Re-pin if a stability issue surfaces or a final release lands.
- **Only `main/NPOI.Core.csproj` is referenced — this gives `.xls` support (`HSSFWorkbook`) only.** NPOI's `.doc` (`HWPF`) and `.ppt` (`HSLF`) legacy readers exist only in NPOI's `scratchpad/` tree, which uses pre-SDK-style `.csproj` files hard-targeted at .NET Framework v2.0, is excluded from NPOI's own CI/build/pack pipeline, and is **not** included in the official NPOI NuGet package for any TFM (verified against the real 2.7.6 nupkg contents). `HSLFSlideShow` does not exist in NPOI's C# port at all — HSLF there is a handful of low-level record classes, not a working PPT reader. Legacy `.doc`/`.ppt` support is an open gap: needs either porting/modernizing NPOI's scratchpad to net8.0 ourselves, or a different library for those two formats. Tracked as a blocker on top of [ADR-001](adrs/001-extraction-library-stack.md).
- **Authoring** legacy `.doc`/`.ppt` for `SampleGenerator` (T-b856b14e) is separate from reading them: no OSS library here writes `.doc`/`.ppt` body content, so `DocSampleGenerator`/`PptSampleGenerator` write minimal [MS-DOC]/[MS-PPT] binaries themselves on NPOI POIFS, and `XlsSampleGenerator` uses `HSSFWorkbook`. See [ADR-005](adrs/005-legacy-doc-ppt-authoring.md) (including the caveat that real Office openability is unverified).

## Library Stack (locked)

| Concern | Library | ADR |
|---|---|---|
| OOXML (docx/xlsx/pptx) | `DocumentFormat.OpenXml` | [ADR-001](adrs/001-extraction-library-stack.md) |
| Legacy OLE (doc/xls/ppt) | `NPOI` (self-compiled from source; **.xls only so far**, .doc/.ppt blocked — see Known Gotchas) | [ADR-001](adrs/001-extraction-library-stack.md) |
| CFB introspection / mime-sniffing | `OpenMcdf` ≥3.1.3 | [ADR-001](adrs/001-extraction-library-stack.md) |
| Time profiling | `BenchmarkDotNet` | [ADR-002](adrs/002-profiling-and-stress-testing.md) |
| Stress testing | Hand-rolled harness | [ADR-002](adrs/002-profiling-and-stress-testing.md) |
