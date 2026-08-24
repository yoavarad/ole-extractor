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
- NPOI's Unicode/RTL/CJK/emoji fidelity on the legacy (doc/xls/ppt) read path is **unverified** — must be empirically tested against the multilingual/emoji test corpus before the i18n requirement can be considered met for legacy formats. This risk is scoped to legacy OLE only — OOXML (docx/xlsx/pptx) parses XML text directly via `DocumentFormat.OpenXml` and isn't exposed to NPOI's codepage-decoding path.
  **Contingency if a script family fails (decided 2026-08-19):** tiered — (1) first attempt a codepage-correction post-process, re-decoding the affected text run using the codepage recorded in the OLE `CodePage`/`\x01CompObj` stream rather than trusting NPOI's default decode; (2) if that doesn't fully resolve it, document the specific script+legacy-format combo as a known v1 limitation, add a test asserting the documented limitation (not a silent pass), and narrow that combo out of the i18n-fidelity acceptance criteria rather than blocking Epic 4 indefinitely. Re-evaluate post-v1 if the limitation matters to a real use case.
- **`ydk` (the personal fork in `C:\Users\yoava\Projects\ydk`, a separate repo — do not patch it as a side effect of work in this repo) has three compounding bugs that make `ydk task ready`/`ydk task start`'s dependency-met check untrustworthy for this repo's whole task DAG.** All three were found and worked around manually on 2026-08-23 (see task T-6ac3f215); if `.ydk/manifest.yaml` drifts stale again before ydk itself is fixed, redo the same workaround:
  1. **Status never syncs on completion.** `ydk task done` does not write `status: done` anywhere reliable — not `.ydk/tasks/<id>.md`'s frontmatter, and not `.ydk/manifest.yaml`. The actual dependency gate — `check_dependencies()` in `ydk/src/ydk/repositories/local/tasks.py` — reads `self._manifest.load()["tasks"][dep_id]["status"]` and only treats a dependency as resolved when that value is exactly `"done"`. Since nothing writes it, manifest.yaml's status fields are frozen at whatever Stage 02 planning set them to, and every dependent task falsely reports "Deps Met: yes" regardless of real merge state. **Workaround:** cross-reference `gh pr list --state merged --json headRefName,mergedAt` (task branches are `task/<task-id>-<slug>` or `quickdev/<task-id>-<slug>`) and `git log main --merges` against every task ID, then hand-correct `tasks[id].status` in `.ydk/manifest.yaml` to `done`/`in-progress`/`open` to match reality. Also correct the `.md` frontmatter `status:` field for consistency, even though it isn't what the gate reads.
  2. **`batch-mapping.json` gets overwritten, not merged.** Running `ydk task create-batch` again replaces `.ydk/batch-mapping.json` wholesale instead of merging in the new batch's slug->real-ID entries, so an earlier batch's mappings (e.g. `"task-net8-downgrade": "T-eeb6684b"`) get silently dropped. This breaks `ydk task start`'s dependency resolution ("Unresolved dependencies: [...]") for any task still referencing the lost slug. **Workaround:** recover the dropped mappings from the placeholder->real-ID tables in `.ydk/reports/stage-02-planning-*.md` (each batch run's report includes one) and merge them back into `batch-mapping.json` by hand — do not just rerun `create-batch`.
  3. **Stale slug references survive ID resolution.** `.ydk/manifest.yaml`'s `tasks:` dict is keyed by real IDs (e.g. `T-eeb6684b:`), but other tasks' `dependencies:` lists can still reference the old placeholder slug (e.g. `task-net8-downgrade`) instead of the resolved ID — resolution updates a task's own key but not other tasks' references to it. (Note: `.ydk/tasks/<id>.md` frontmatter `dependencies:` lists were *not* found to have this problem, only `manifest.yaml`.) **Workaround:** same recovered slug->ID mapping as bug 2, applied to every `dependencies:` entry across all tasks in `manifest.yaml`, not just the ones already known to be broken. A reusable remediation script that does all three corrections (queries `gh pr list`, corrects statuses, resolves slugs) lives at `.ydk/scripts/remediate_task_status.py` — safe to rerun.
- NPOI is self-compiled from Apache-2.0 source via a git submodule at `third_party/npoi` (pinned to tag `2.7.6-rc1`), referenced from `ExtractorOLE.csproj` via `ProjectReference`. NPOI itself is Copyright the Apache Software Foundation / nissl-lab, licensed under Apache License 2.0 (see `third_party/npoi/LICENSE`). **Note:** `2.7.6-rc1` is a pre-release tag -- every NPOI tag past `2.5.6` is an RC, with no final release cut since; chosen anyway to get post-2.5.6 fixes without the 2.8.0+ paid-EULA packaging. Re-pin if a stability issue surfaces or a final release lands.
- **Only `main/NPOI.Core.csproj` is referenced — this gives `.xls` support (`HSSFWorkbook`) only.** NPOI's `.doc` (`HWPF`) and `.ppt` (`HSLF`) legacy readers exist only in NPOI's `scratchpad/` tree, which uses pre-SDK-style `.csproj` files hard-targeted at .NET Framework v2.0, is excluded from NPOI's own CI/build/pack pipeline, and is **not** included in the official NPOI NuGet package for any TFM (verified against the real 2.7.6 nupkg contents). `HSLFSlideShow` does not exist in NPOI's C# port at all — HSLF there is a handful of low-level record classes, not a working PPT reader. Legacy `.doc`/`.ppt` support is an open gap: needs either porting/modernizing NPOI's scratchpad to net8.0 ourselves, or a different library for those two formats. Tracked as a blocker on top of [ADR-001](adrs/001-extraction-library-stack.md).

## Library Stack (locked)

| Concern | Library | ADR |
|---|---|---|
| OOXML (docx/xlsx/pptx) | `DocumentFormat.OpenXml` | [ADR-001](adrs/001-extraction-library-stack.md) |
| Legacy OLE (doc/xls/ppt) | `NPOI` (self-compiled from source; **.xls only so far**, .doc/.ppt blocked — see Known Gotchas) | [ADR-001](adrs/001-extraction-library-stack.md) |
| CFB introspection / mime-sniffing | `OpenMcdf` ≥3.1.3 | [ADR-001](adrs/001-extraction-library-stack.md) |
| Time profiling | `BenchmarkDotNet` | [ADR-002](adrs/002-profiling-and-stress-testing.md) |
| Stress testing | Hand-rolled harness | [ADR-002](adrs/002-profiling-and-stress-testing.md) |
