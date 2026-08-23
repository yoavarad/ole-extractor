# Stage 02 Planning Report — 2026-08-19

## Summary

Executed Stage 02 (Task Management & Planning) in two passes using `ydk task create-batch`.
Stage remains `"02"` in `.ydk/state.json` (untouched, per instructions).

- **Pass 1**: full backlog at epic+story granularity — all 11 epics, 22 stories.
- **Pass 2**: Sprint 1 (Epic 1 Library Setup & Validation + Epic 2 Project Restructure) decomposed into 9 full tasks with dependencies, component/spec refs, acceptance, and test strategy.

Batch YAML sources live at `.ydk/batches/pass1-epics-stories.yaml` and
`.ydk/batches/pass2-sprint1-tasks.yaml` (kept under `.ydk/`, not repo root, per instructions —
the authoritative state now lives in ydk's own task store, these are the batch inputs that
produced it).

## Pass 1 — Full Backlog (Epics + Stories)

Command: `ydk task create-batch --from .ydk/batches/pass1-epics-stories.yaml [--dry-run]`

Dry run passed with zero validation errors on the first attempt (schema matched
`_validate_batch_yaml`/`create_batch` in `task_cmd.py` as read: epics take
`{id, title, description, spec_refs}` with no `release` field; stories add
`epic`/`component_refs`/`acceptance`; `spec_refs` resolved fine against repo-root-relative
paths like `docs/specs/overview.md` and `docs/adrs/001-extraction-library-stack.md`).

Real run created **33/33 items successfully** (11 epics, 22 stories), zero failures.

### Epics created

| Placeholder | Real ID | Title |
|---|---|---|
| epic-01-library-setup | E-a2efe648 | Library Setup & Validation |
| epic-02-project-restructure | E-d4ac3268 | Project Restructure |
| epic-03-cfb-mime-detection | E-6728ee81 | CFB / Legacy Mime Detection |
| epic-04-legacy-ole-extraction | E-a7ddf85b | Legacy OLE Extraction |
| epic-05-ooxml-mime-detection | E-b54e8704 | OOXML Mime Detection Hardening |
| epic-06-ooxml-extraction | E-46875bcf | OOXML Extraction Completeness |
| epic-07-public-api | E-bd43bc02 | Public API Surface |
| epic-08-dataset-curation | E-c2846232 | Dataset Curation |
| epic-09-test-suite | E-5717c1fe | Test Suite |
| epic-10-time-profiling | E-3ffdc4d5 | Time Profiling |
| epic-11-stress-harness | E-c2544f52 | Stress Test Harness |

### Stories created (22)

Story breakdown per epic: Epic 1 → 2 stories (build wiring, i18n validation); Epic 2 → 3
stories (net8+enum fix, DI registry+embedded walk, Program.cs wiring); Epic 3 → 1 story;
Epic 4 → 3 stories (doc/xls/ppt); Epic 5 → 1 story; Epic 6 → 3 stories (docx/xlsx/pptx);
Epic 7 → 2 stories (detect facade, extract facade+errors); Epic 8 → 3 stories (synthetic,
curated, adversarial+manifest); Epic 9 → 2 stories (integration, unit); Epic 10 → 1 story;
Epic 11 → 1 story.

Real IDs (relevant ones referenced again in Pass 2):
- `story-01-npoi-openmcdf-build` → S-e496f356
- `story-01-npoi-i18n-validation` → S-418e5f1b
- `story-02-net8-and-enum-fix` → S-d01346bc
- `story-02-di-registry-and-embedded-walk` → S-43c41d4a
- `story-02-program-cs-wiring` → S-fe24ecff

(Full 22-story list and titles are in the batch YAML source and visible via `ydk task list`.)

## Sanity checks after Pass 1

- `ydk task coverage`: **5/6 spec sections have stories.** Only uncovered section:
  `docs/specs/glossary.md` — expected and correct, since glossary.md is term definitions
  (no requirements/behavior to cover with a story).
- `ydk task component-coverage`: **26/26 components not yet referenced by any task** — expected
  at this point, since component-coverage checks *tasks*, not stories, and Pass 1 only created
  epics/stories. Story-level `component_refs` were set throughout, but coverage won't close
  until each epic's stories are decomposed into tasks (Sprint 2+ will close most of the
  remainder; some NFR/req components will only get task coverage once the corresponding
  format-specific epics are decomposed).

## Pass 2 — Sprint 1 Tasks (Epic 1 + Epic 2)

Command: `ydk task create-batch --from .ydk/batches/pass2-sprint1-tasks.yaml [--dry-run]`

Dry run passed cleanly (story references to the real `S-...` IDs from Pass 1 resolved with no
errors — confirms cross-batch-file story references work by using the real ID directly, since
placeholder resolution only happens within a single batch file's own pass).

Real run created **9/9 tasks successfully**, zero failures.

| Placeholder | Real ID | Title | Story |
|---|---|---|---|
| task-net8-downgrade | T-eeb6684b | Downgrade ExtractorOLE.csproj to net8.0 | S-d01346bc |
| task-fix-enum-typo | T-2ac2f140 | Fix OfficeMimeTypeEnum typo (OpenXmlUnkown) | S-d01346bc |
| task-openmcdf-package-ref | T-d7c81051 | Add OpenMcdf >=3.1.3 package reference | S-e496f356 |
| task-npoi-self-compile | T-d6de875f | Self-compile NPOI from Apache-2.0 source and reference it | S-e496f356 |
| task-verify-library-build | T-a5f1d074 | Verify combined NPOI + OpenMcdf build | S-e496f356 |
| task-npoi-i18n-validation | T-94570c2b | Empirically validate NPOI Unicode/i18n fidelity on the legacy OLE path | S-418e5f1b |
| task-generalize-embedded-walk | T-05bd14c7 | Generalize embedded-subfile walk beyond PowerPoint special-casing | S-43c41d4a |
| task-wire-di-registry | T-90924b6d | Wire DI registry pattern for extensible format dispatch | S-43c41d4a |
| task-program-cs-invoke | T-07188baf | Wire Program.cs to invoke both public functions on a real file | S-fe24ecff |

### Dependency graph modeled

```
T-eeb6684b (net8 downgrade)
  ├─> T-2ac2f140 (fix enum typo)
  │     └─> T-05bd14c7 (generalize embedded walk)
  │           └─> T-90924b6d (wire DI registry)
  │                 └─> T-07188baf (Program.cs wiring)
  ├─> T-d7c81051 (OpenMcdf package ref) ─┐
  └─> T-d6de875f (NPOI self-compile) ────┴─> T-a5f1d074 (verify combined build)
                                                 └─> T-94570c2b (NPOI i18n validation)
```

All edges are `:blocks` type. `task-generalize-embedded-walk` depends on both
`task-net8-downgrade` and `task-fix-enum-typo` (needs the corrected enum and net8 target before
refactoring the walk). `task-wire-di-registry` depends on the generalized walk. `task-program-cs-invoke`
depends on the registry being wired, since it should exercise real dispatch, not a stub.
Library-wiring tasks (OpenMcdf ref, NPOI self-compile) both depend on the net8 downgrade landing
first, and both converge into a single "verify combined build" task before the i18n-validation
task (which needs NPOI actually compiled and referenceable) proceeds.

## DAG Validation

`ydk task validate-dag`: **DAG is valid.**
- Parallel waves: 5
- Critical path length (dependency-only): 5
- Critical path: `T-eeb6684b -> T-2ac2f140 -> T-05bd14c7 -> T-90924b6d -> T-07188baf`

## Task Complexity Analysis

`ydk task analyze-complexity` was attempted per instructions ("if it exists, to sanity-check task
sizing"). It exists, but **failed** — the command calls out to AWS Bedrock via `botocore`, and
this environment has no AWS credentials configured (`NoCredentialsError: Unable to locate
credentials`). This is consistent with `docs/adrs/003-manual-spec-review-in-place-of-bedrock.md`,
which already documents that Bedrock isn't available/usable in this project's environment and
substitutes manual review instead. No further action taken — task sizing was instead
sanity-checked manually: all 9 Sprint 1 tasks are single-concern, file-scoped changes (one
`.csproj` edit, one enum rename, two package/build-wiring tasks, one build-verification task, one
empirical-validation task, one refactor, one DI-wiring task, one `Program.cs` edit) — none look
oversized for a single task.

## Schema Notes / Surprises

- Confirmed via reading `_normalize_refs`, `_validate_batch_yaml`, and `create_batch` in
  `task_cmd.py` before trusting the secondhand summary: epics do **not** accept `component_refs`
  (only `title`, `description`, `spec_refs`) — only stories and tasks do. This matched the
  briefing exactly.
- `spec_refs` validation checks `project_root / ref` first, then `project_root / spec_location / ref`
  (spec_location from config, default `docs/specs`) — so both `docs/specs/overview.md` and a
  bare `overview.md` would have resolved; repo-root-relative paths were used throughout for
  clarity.
- Cross-batch-file story references (Pass 2 tasks referencing Pass 1 stories) work correctly by
  using the real `S-...` ID directly — placeholder-to-real-ID resolution (`id_map`) is scoped to
  a single `create-batch` invocation, confirmed by reading pass 1's `Pass 2: Update task
  dependencies` code, which only touches IDs defined in the same file.
- No validation errors were hit in either pass — both dry runs passed on the first attempt.

## Open Questions / Risks for Stage 03

1. **NPOI i18n fidelity is still unverified** at planning time — `task-npoi-i18n-validation`
   (T-94570c2b) is the first real empirical test of this ADR-001-flagged risk. If it fails for any
   script family, Epic 4 (Legacy OLE Extraction) scope may need to change (e.g. a fallback
   encoding-correction layer) before its stories/tasks are decomposed in a later sprint.
2. **Component coverage is 0/26 after Pass 1** and will only partially close after Sprint 1 (Epic
   1/2 tasks reference ~6-8 of the 26 components). Full component coverage requires decomposing
   the remaining 9 epics into tasks in later sprints — flagging this now so it isn't mistaken for
   a planning gap versus the expected two-pass structure.
3. **`ydk task analyze-complexity` is non-functional in this environment** (no AWS Bedrock
   credentials) — if automated complexity scoring is wanted for future sprints, either Bedrock
   credentials need to be provisioned, or this becomes another manual-review substitution
   alongside ADR-003's existing precedent.
4. **Sprint 2+ story decomposition not started** — Epics 3-11 (19 stories) still need task-level
   decomposition before Stage 03 execution can proceed past Sprint 1. This was explicitly out of
   scope for this planning pass per the task brief.
5. Stage 04 (learning capture) tooling was intentionally **not** set up or scoped into any Sprint
   1 task, per the instruction that it's a Stage 03/04 process step, not a Stage 02 planning
   concern.

## Command Log (all invocations used, in order)

```
ydk task --help
ydk task create-batch --from .ydk/batches/pass1-epics-stories.yaml --dry-run
ydk task create-batch --from .ydk/batches/pass1-epics-stories.yaml
ydk task coverage
ydk task component-coverage
ydk task create-batch --from .ydk/batches/pass2-sprint1-tasks.yaml --dry-run
ydk task create-batch --from .ydk/batches/pass2-sprint1-tasks.yaml
ydk task validate-dag
ydk task analyze-complexity   # failed: no AWS Bedrock credentials (NoCredentialsError)
```

All invocations used `PYTHONIOENCODING=utf-8` and bounded timeouts (60-180s); every command
returned promptly, no hangs encountered.
