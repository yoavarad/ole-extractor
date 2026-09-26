# Project Retrospective: ole-extractor (Stage 04)

**Date:** 2026-09-25
**Scope:** The whole project, from Stage 01 planning (2026-08-18) to the last merge (PR #139, 2026-09-25). All 11 epics and 22 stories.
**Why this is late:** No retro ran after any epic. Nothing in YDK signals that an epic has ended: epic and story statuses never change when their last task closes (see [Process findings, item 1](#1-nothing-signals-that-an-epic-has-ended)). So this one retro covers every epic.

Sources: every `.ydk/tasks/*.md` activity log, `.ydk/manifest.yaml`, `activity.md`, `docs/project-rules.md`, the ADRs, the Stage 02 planning reports and git history. `ydk memory retrospective` / `ydk memory audit` were not used because this project has no ChromaDB index or extracted memories (`memory.auto_extract` was never on). This retro was put together by hand from those sources.

---

## 1. What shipped

| | Count |
|---|---|
| Epics / stories | 11 / 22, all done |
| Planned + follow-up tasks (`T-`) | 89, all done |
| Quick tasks (`QD-`) | 33 (29 done, 4 merged but still marked `in-progress`, see §6) |
| PRs referenced by tasks | 47 task-linked PRs, numbered #4 to #139 |
| Test suite at the end | 379 passing, 1 pre-existing skip (per T-5080b00a) |
| ADRs | 5 (ADR-003 to ADR-005 were written during execution) |

The product works end to end. `DetectMimeType` and `Extract` handle all 6 formats (doc/xls/ppt/docx/xlsx/pptx) through the DI registry. They return general metadata and format-specific metadata, flat text with verified Unicode/RTL/CJK/emoji fidelity on all 6 formats, and first-layer subfiles. All 6 error manifests are enforced. The work is backed by a 39-sample corpus manifest, a corpus integration suite, BenchmarkDotNet benchmarks (all 12 cases meet their P95 targets) and a concurrency harness that reaches 500 concurrent calls.

### Per epic

| Epic | Tasks | How it went |
|---|---|---|
| Project Restructure (E-d4ac3268) | 7 | Went smoothly. Two of its 7 tasks were actually YDK tooling fixes (T-ce2e3cc1 build-target discovery, T-6ac3f215 status desync) filed under a product epic. |
| Library Setup & Validation (E-a2efe648) | 6 | **ADR-001's core assumption turned out wrong.** NPOI's HWPF (.doc) and HSLF (.ppt) exist only in its unbuilt .NET Framework 2.0 `scratchpad/` tree. That led to two research tasks, ADR-004 (b2xtranslator for .doc/.ppt) and a fork of b2xtranslator. |
| CFB / Legacy Mime Detection (E-6728ee81) | 3 | Went smoothly. |
| OOXML Mime Detection Hardening (E-b54e8704) | 4 | Went smoothly. |
| Legacy OLE Extraction (E-a7ddf85b) | 13 | The longest wall-clock time. .doc/.ppt tasks sat `blocked-by-blocked-by-research` from 08-23 to 09-16, even though the research that blocked them finished on 08-23. Found and fixed real b2xtranslator bugs: the NUL-terminator bug under ICU, and the surrogate-pair emoji crash. |
| OOXML Extraction Completeness (E-46875bcf) | 13 | Unit-level work went fast. **The corpus integration test later found 4 real bugs**: xlsx embeds on WorksheetPart, pptx slide order, shared-part double-counting and missed layout/vmlDrawing media, and inconsistent subfile naming. |
| Public API Surface (E-bd43bc02) | 6 | Went smoothly. It was the join point of the dependency graph (T-3b4e70f4 had 7 dependencies). |
| Dataset Curation (E-c2846232) | 17 | The biggest and messiest epic. Acceptance criteria were cut several times (18 to 9 synthetic samples, 37 to 27, genuine xlsx revision history found to be impossible), then partly restored once ADR-005 was written. Parallel tasks conflicted on `samples/manifest.json`. |
| Test Suite (E-5717c1fe) | 8 | 2 of 8 tasks were closed as already delivered by other tasks. The integration suite paid off most of any piece of work in the project (see §3). |
| Time Profiling (E-3ffdc4d5) | 4 | Blocked until legacy extraction landed, then found it had no inputs (T-f0963fc3 was added to generate them). All P95 targets met. The slowest case is `xls` at 100MB, P95 about 2.3s against a 5s target. xls allocates about 39× its input size. |
| Stress Test Harness (E-c2544f52) | 4 | Worked, but its own test was flaky and failed the gate on PRs #118 to #120. Fixed at the root cause with a `Barrier(500)`, without retries (T-5080b00a). |

---

## 2. What went well (keep doing)

1. **Checking claims against real files.** Examples: subfile counts checked against independently parsed `.rels` (T-205594d7), expected detected format taken from running the real detector instead of from prose (T-71ab3bc0), and `EncryptionInfo`/`EncryptedPackage` confirmed by a raw byte scan (T-83b4a029). Every time an expectation came from prose rather than a run, it later turned out wrong (see §3.4).
2. **Not faking genuine data.** The project did not script "genuine" revision history. It recorded the xlsx gap in the manifest instead ("real Excel never populates `cp:revision`", 0 of 37 govdocs1 files). The manifest stayed trustworthy as a result.
3. **govdocs1 as a sample source.** It is public domain (U.S. federal works) and has real multi-user provenance. It supplied the revision-history docx/pptx/doc samples and the real .xls fixtures.
4. **ADR discipline.** Accepted ADRs were never rewritten. New decisions got a new ADR (ADR-005 for legacy authoring) or a dated note.
5. **Flagging decisions for a human inside the task.** `DECISION` / `FLAG FOR HUMAN` comments (T-009db62b naming, T-205594d7 subfile scope, T-270c32fe xlsx gap) got fast, clear answers. The naming decision was confirmed within 3 minutes.
6. **Fixing flaky tests at the root cause.** The concurrency flake was fixed with a barrier, not retries or a lower threshold, and verified 20/20 under synthetic CPU load.
7. **Keeping diffs surgical.** Agents repeatedly reverted incidental regeneration churn (`git checkout --` on unrelated samples) instead of committing it.

---

## 3. What went wrong

### 3.1 The library ADR checked licenses but not the APIs we would actually build against
ADR-001 picked NPOI for all three legacy formats after a careful license review (the paid EULA from 2.8.0 on). Nobody checked that `HWPFDocument` and `HSLFSlideShow` exist in the part of NPOI we would compile. They don't. That one gap caused 2 research tasks, ADR-004, a fork, a submodule, and a roughly 3-week stall of the Legacy OLE epic.
**Lesson:** a library-selection ADR must name the concrete entry-point type for each format and prove it builds for the target TFM with a 10-line spike, not only a license check.

### 3.2 The integration test came last but found the most bugs
T-7916a198 (the corpus integration test) ran against real third-party files and surfaced 6 issues in one pass: 4 real extractor bugs, 1 manifest typo (`'` vs `U+2019`), and 1 missing FileMetadata field set (T-fa8b3ed9). The unit tests missed all of them because they used synthetic fixtures built by our own generator. Our generator shares the extractor's assumptions: slides in relationship order, no shared parts, no layout-hosted media.
**Lesson:** start a thin corpus integration test, even with only a few samples, as soon as the first format extracts end to end. Don't schedule it as the final epic.

### 3.3 Tasks marked done without their output existing
T-4b624fef ("generate macro-enabled samples") was merged with the generator wired up, but it never ran the generator, so no sample files existed. T-f96fa73d found this later. Several tasks were also closed as "already satisfied" by earlier work: T-07188baf, T-a5f1d074, T-d7c81051, T-0834290d, T-f4ed5c8f and T-5e489191. That points to overlapping decomposition in Stage 02.
**Lesson:** if a task produces an artifact (a sample, a baseline, a report), a done task must include that artifact in its diff. When decomposing, check sibling tasks for "test for X" / "implement X" overlap.

### 3.4 Expectations written from prose instead of runs
The `expectedBehavior` for corrupt/truncated containers said "detected as docx, then rejected on full parse". In reality the detector returns `Unknown` first (T-71ab3bc0, fixed by T-48110fcb). The pptx subfile counts in the manifest were raw `ppt/media` part counts that nobody had checked against the extractor. The xlsx image names were guesses.
**Lesson:** a manifest field must either be observed from a run or be explicitly marked as a guess (`detectable: false`, with a note). Most entries already followed this; the drift came from entries that didn't.

### 3.5 Parallel tasks editing the same shared files
Three adversarial-sample tasks (T-83b4a029, T-7cdaeceb, T-3bddbe75) ran in parallel and all edited `samples/manifest.json` and `activity.md`. The merge conflicts were foreseeable and were flagged in the PR rather than prevented.
**Lesson:** when a single file is a shared registry (`samples/manifest.json`), either put the tasks that touch it in sequence in the batch plan, or have each task write a fragment that gets merged in.

### 3.6 Sample generation was not deterministic
Running `SampleGenerator` without flags regenerates every sample, with random GUIDs and timestamps in the zip containers. At least 3 tasks had to revert unrelated byte changes. This was partly fixed later with `--adversarial` / `--benchmark` flags.

---

## 4. Process and tooling findings (YDK)

These are about YDK itself, not the product. The biggest cost was bookkeeping.

### 1. Nothing signals that an epic has ended
- No YDK command path ever writes epic or story status. Only tasks get `update_status`, and `ydk task sync`/`close` touch tasks only. Nothing in `ydk task done` notices "this was the last open task in story S / epic E".
- So Stage 04 never triggered. The skill says "Sprint done: enter Stage 04", but "sprint done" was never computed.
- There was also split-brain state: QD-2c2d6d marked all epics and stories `done` in `manifest.yaml`, but every `.ydk/epics/*.md` and `.ydk/stories/*.md` still said `open`. This retro fixes the `.md` files.
- **Proposed YDK change:** when `task done`/`close`/`sync` closes the last open task of a story or epic, close the story/epic in both stores and print "Epic E-… complete: run Stage 04 retrospective (`ydk memory retrospective`)".

### 2. Status-sync churn
- 22 of 33 quick tasks were pure bookkeeping: status syncs, proof commits and graph refreshes. `project-rules.md` counts 37 of the first 228 commits as status-sync churn.
- Fixed mid-project by `.github/workflows/task-sync.yml` together with `ydk task sync`/`close`.
- The remaining gap: commits pushed straight to main, which bypass PRs, never get synced (§6).

### 3. Acceptance-criteria checkboxes don't mean anything
- Across the 89 done `T-` tasks, only 73 of 253 criteria are ticked. 62 done tasks have zero criteria ticked.
- `ydk task done` neither ticks criteria nor requires them to be ticked. The real record of what was done lives in the activity-log prose.
- Either enforce ticking at `done` time or stop presenting the checkboxes as state.

### 4. Blocked tasks never unblock
- T-c4123b53 and T-517f89c5 stayed `blocked-by-blocked-by-research` for 3 weeks after the research merged.
- T-07571b32's `blocked-by-ADR-001` label went stale once ADR-001/004 were accepted.
- A block with a named blocking task should clear itself when that task closes. (Note the doubled `blocked-by-blocked-by-` prefix: `block --reason` evidently prepends `blocked-by-` to a reason that already contains it.)

### 5. Verification plugins cost the most time
- **`pr-body-validation` failed on every task:**
  - It ran before the PR body existed (T-0c83ff28 has the full diagnosis).
  - Later it gave false positives for `screenshot_for_ui` on pure C# changes (T-40fc15b4, T-517f89c5, T-a8b6b4ec, T-fa8b3ed9, still happening on 09-25).
- **Irrelevant plugins:** `fastapi-*` and `nextjs-*` ran on a .NET repo. One `nextjs-no-direct-env` timeout failed a .NET task (T-30f2c1b8). Fixed by the dotnet allowlist (QD-b63f06).
- **Build-target discovery:** `dotnet-build`/`dotnet-format` picked the alphabetically first `.csproj` (T-ce2e3cc1).
- **Other failures:**
  - Stale verification cache (T-f96fa73d).
  - 120s timeouts on `dotnet-format`/`dotnet-build`.
  - `dotnet-test` failing whenever several agents ran in parallel on one machine: the flaky load test plus shared CPU.

### 6. The permission classifier and external repos
Forking b2xtranslator and pushing branches of the fork were denied mid-task several times (T-b70522d2, T-517f89c5, T-a8b6b4ec), which forced human handoffs. Then a submodule was pinned to a fork branch that was later deleted, which broke fresh clones (QD-334b3d).
**Lesson:** pin submodules only to commits reachable from the fork's `master`/tags, never to feature branches.

### 7. No CI for build or test
- `.github/workflows/` holds only `task-sync.yml` and `delete-merged-branch.yml`.
- Every build/test/format gate ran locally via YDK plugins on one Windows dev box.
- `docs/benchmarks/README.md` describes a "CI regression rule" (P95 over 20%) that no job enforces yet.

### 8. `activity.md` was only partly maintained
It has about 10 entries for 89 tasks. The real history is in the per-task activity logs.

---

## 5. Abandoned approaches (negative knowledge)

| Approach | Why it was dropped | Where |
|---|---|---|
| NPOI HWPF/HSLF for .doc/.ppt | Exists only in the unbuilt net2.0 scratchpad; `HSLFSlideShow` doesn't exist in the C# port | T-d6de875f, ADR-004 |
| Porting NPOI scratchpad ourselves | Feasible (IllidanS4/npoi precedent), but kept only as a .doc fallback | ADR-004 |
| modeverv/dotnet-poi | Rejected on provenance (about 48h of commit history) | ADR-004 |
| b2xtranslator for .xls too (full NPOI retirement) | Writes no docProps metadata for Xls; NPOI HPSF/POIFS still needed anyway | T-b70522d2, ADR-004 note |
| b2xtranslator / NPOI to *author* .doc/.ppt | Both are readers only; now hand-writing minimal [MS-DOC]/[MS-PPT] binaries on POIFS | T-b856b14e, ADR-005 |
| MS Office COM / LibreOffice for genuine edit history | COM broken on host (`CO_E_SERVER_EXEC_FAILURE`), no LibreOffice; used govdocs1 instead | T-270c32fe |
| Scripted "realistic" revision history as genuine | Tagged `synthetic` instead; genuine samples sourced separately | T-6f033869 |
| `Thread.Sleep` to force concurrency overlap in tests | Timing-dependent; replaced with `Barrier` | T-5080b00a |
| Counting subfiles per relationship | Double-counts shared parts; now counted per distinct part URI | T-205594d7 |
| Hand-creating QD "sync status" tasks | Replaced by `task-sync.yml` + `ydk task sync` | project-rules |

---

## 6. Loose ends found during this retro

- **Quick tasks merged but still `in-progress`:** QD-b63f06, QD-18cb51, QD-2c2d6d and QD-34384f. They were pushed straight to main (commits `5231ff6`, `232c609`, `c4141b0`, `db50894`) without PRs, so `task-sync` never saw them. Close them with `ydk task close <id> --reason "pushed directly to main"`.
- **Residual product gaps (accepted, documented):**
  - No genuine xlsx revision-history sample (a corpus limitation).
  - `slide_image_N` is a poor name for xlsx images (product decision, T-009db62b).
  - Nobody has checked that real Office can open the generated legacy samples (ADR-005).
  - `.doc` single-byte code-page runs beyond the fixture are untested.
- **Benchmark CI job** described in `docs/benchmarks/README.md` doesn't exist.

## 7. Actions

**Done in this retro:**
- [x] `docs/project-rules.md` updated. It dropped stale ".doc/.ppt blocked" and ".ppt fidelity unverified" statements, fixed the library-stack table (b2xtranslator per ADR-004), and added new gotchas from §3 to §5.
- [x] All 11 `.ydk/epics/*.md` and 22 `.ydk/stories/*.md` set to `status: done`, matching `manifest.yaml`.

**Follow-ups: this repo**
- [x] Close the 4 stale QD tasks (§6).
- [x] Add CI: `ci.yml` (PR gate: build, format, test, vulnerable packages on ubuntu + windows); `benchmarks.yml` and `stress.yml` on demand only.
- [ ] Run `/graphify --update` after this lands (per CLAUDE.md).

**Follow-ups: YDK (separate repo)**
- [ ] Close stories/epics automatically when their last task closes, and prompt Stage 04 (§4.1).
- [ ] Clear blocks automatically when the blocking task closes; fix the doubled `blocked-by-` prefix (§4.4).
- [ ] Either tick/enforce acceptance criteria in `task done` or stop showing them as state (§4.3).
- [ ] Stop `pr-body-validation` from treating `.cs` files as UI files (§4.5).

**For the next project's Stage 01/02:**
- Library ADRs must include a per-format entry-point spike on the target TFM (§3.1).
- Schedule a thin real-corpus integration test early, alongside the first extractor (§3.2).
- Serialize tasks that write to shared registries like `samples/manifest.json`, or give each its own fragment file (§3.5).
- Tasks that produce artifacts must include the artifact in their diff (§3.3).
