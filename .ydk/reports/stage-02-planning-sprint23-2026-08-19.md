# Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19

## Summary

Decomposed the remaining 9 epics (19 stories: Epic 3–11, excluding Epic 1/2 which
were done in the prior pass) into full tasks, in two sequential
`ydk task create-batch` invocations, run one at a time with dry-run validation
first. Stage remains `"02"` in `.ydk/state.json` (untouched).

- **Sprint 2** (`.ydk/batches/pass3-sprint2-tasks.yaml`): Epics 3–7 (CFB detection,
  Legacy OLE extraction, OOXML detection hardening, OOXML extraction completeness,
  Public API surface) — **34 tasks** (33 from the batch + 1 corrective task added
  afterward via `ydk task create`, see Validation Errors / Gaps below).
- **Sprint 3** (`.ydk/batches/pass4-sprint3-tasks.yaml`): Epics 8–11 (Dataset
  curation, Test suite, Time profiling, Stress harness) — **25 tasks**.
- **Total across all three sprints: 68 tasks** (9 Sprint 1 + 34 Sprint 2 + 25 Sprint 3).

## Task Counts Per Story

| Epic | Story (real ID) | Tasks |
|---|---|---|
| 3 CFB/Legacy Mime Detection | S-3bc6b929 | 3 |
| 4 Legacy OLE Extraction | S-32677db6 (doc) | 4 |
| 4 | S-0602557d (xls) | 4 |
| 4 | S-23b8b734 (ppt) | 4 |
| 5 OOXML Mime Detection Hardening | S-7d040ba7 | 4 (3 in batch + 1 corrective) |
| 6 OOXML Extraction Completeness | S-156ec71a (docx) | 3 |
| 6 | S-8d6146f0 (xlsx) | 3 |
| 6 | S-37c18545 (pptx) | 3 |
| 7 Public API Surface | S-072bfdf6 (detect facade) | 2 |
| 7 | S-51d277f1 (extract facade) | 4 |
| **Sprint 2 total** | | **34** |
| 8 Dataset Curation | S-41ebb945 (synthetic) | 5 |
| 8 | S-da936361 (curated) | 4 |
| 8 | S-6bc8566f (adversarial+manifest) | 4 |
| 9 Test Suite | S-f6d7124a (integration) | 3 |
| 9 | S-814fa928 (unit) | 3 |
| 10 Time Profiling | S-709fe2de | 3 |
| 11 Stress Harness | S-d88f514b | 3 |
| **Sprint 3 total** | | **25** |

Full placeholder→real-ID mapping for all 59 batch-created tasks is in the two
batch-create console outputs (not reproduced here for length); the notable IDs
referenced by cross-sprint edges are listed below.

## Cross-Epic/Cross-Sprint Dependency Edges Modeled

The single biggest modeling decision: **`depends_on` in `ydk task create-batch`
only resolves IDs defined within that same batch file** (confirmed by the
CLI's own help text: "Each entity has an `id` field used for cross-referencing
within the file"). Pre-existing real `T-...` IDs from an earlier batch are
rejected as "undefined ID" during dry-run — this is a stricter behavior than
Pass 2's `story:` field (which *does* resolve real cross-batch `S-...` IDs,
since it's validated against the persisted store rather than the batch's
local id_map). See **Validation Errors / Fixes** below for how this was
worked around.

Modeled dependency structure (edges within a batch use placeholders resolved
by the CLI; cross-sprint edges to already-existing real IDs were added by
directly editing the new task's frontmatter `dependencies:` list after
creation, then re-validated with `ydk task validate-dag`, which reads from
the repository rather than a batch file):

- **Epic 3 (CFB detection)** chains: core detection → guardrails → DI registry
  wiring. `task-cfb-detect-core` depends on Sprint 1's `T-d7c81051` (OpenMcdf
  package ref) and `T-2ac2f140` (enum typo fix); the registry task depends on
  `T-90924b6d` (Sprint 1's DI registry).
- **Epic 4 (Legacy OLE extraction, doc/xls/ppt)** each split into
  open-component → {metadata, text, subfiles} (metadata/subfiles are siblings
  off open; text additionally depends on Sprint 1's `T-94570c2b`, the NPOI
  i18n validation task, since legacy text-decoding fidelity was flagged
  unverified there). Open components depend on `T-a5f1d074` (verified
  combined NPOI+OpenMcdf build) and `T-90924b6d` (DI registry).
- **Epic 5 (OOXML detection hardening)**: harden → {macro-variants,
  zero-byte, and the corrective guardrails task}, harden depends on
  `T-2ac2f140`.
- **Epic 6 (OOXML extraction completeness, docx/xlsx/pptx)**: metadata and
  text tasks depend on `T-90924b6d`; subfiles tasks depend on Sprint 1's
  `T-05bd14c7` (the generalized embedded-subfile walk) directly, since Epic 6
  explicitly builds on that generalization rather than the DI registry.
- **Epic 7 (Public API)**: `task-detect-facade-wire` depends on Epic 3's
  registry task, all three Epic 5 detection tasks, and `T-90924b6d`.
  `task-extract-facade-wire` depends on **all six format-completion tasks**
  (doc/xls/ppt/docx/xlsx/pptx subfile tasks) plus `T-90924b6d` — this is the
  key "facade needs every format pipeline done" edge the brief called for.
  The three Extract error-handling tasks (guardrail, parse-time,
  unsupported+validation) all fan out from `task-extract-facade-wire`.
- **Epic 8 (Dataset curation)**: synthetic-generator's scaffold task depends
  on Sprint 1's `T-a5f1d074` (need working NPOI/OpenXml write capability);
  the three composition-rule generation tasks depend on the scaffold; the
  manifest task depends on all three. Adversarial-set tasks depend on the
  synthetic manifest task (crafting adversarial variants from valid base
  samples). `task-corpus-manifest-finalize` depends on all of synthetic,
  curated, and adversarial manifest work — this is the corpus ground truth
  Epic 9 needs.
- **Epic 9 (Test suite)**: integration-detect depends on
  `task-corpus-manifest-finalize` (in-batch) and Sprint 2's `T-fb89c6f3`
  (DetectMimeType facade complete); integration-extract depends on the same
  manifest task plus Sprint 2's three Extract-facade error-handling leaves
  (`T-3335d397`, `T-61477563`, `T-a2b91dc6`); the adversarial-scenario task
  depends on both. Unit-test tasks depend on the corresponding Sprint 2
  facade validation/error tasks (not the corpus, since they use synthetic
  inputs, not checked-in samples).
- **Epic 10/11 (Profiling/Stress)**: both scaffold tasks depend on Sprint 2's
  full facade completion set (all four Epic 7 leaf tasks — both public
  functions must exist and handle errors before benchmarking/stress-testing
  them); the benchmark/harness "exercise" tasks additionally depend on
  `task-corpus-manifest-finalize` for fixed-size corpus samples to run
  against.

## DAG Validation

`ydk task validate-dag` (run once at the end, across all three sprints):

```
DAG is valid.
Parallel waves: 11
Critical path length (dependency-only): 11
Critical path: T-eeb6684b -> T-2ac2f140 -> T-05bd14c7 -> T-90924b6d ->
  T-517f89c5 -> T-12dfd045 -> T-3b4e70f4 -> T-3335d397 -> T-0055ec7c ->
  T-96b5181b -> T-113fe4b2
```

(net8 downgrade → enum fix → generalized walk → DI registry → ppt open →
ppt subfiles → Extract facade wire → Extract guardrail errors → stress
harness scaffold → concurrent-call exercise → stress reporting.) The
critical path routes through the stress harness rather than the benchmark
or test-suite branches because the stress harness scaffold, like the
benchmark scaffold, depends on **all four** Epic 7 leaf tasks, and the
concurrent-call/reporting chain adds two more sequential steps after that —
slightly longer than the test-suite or benchmark branches from the same
join point.

## Component Coverage

`ydk task component-coverage`: **All 26/26 components are now referenced by
at least one task.** (Sprint 1 already covered ~8; Sprint 2 closed the
format-specific entities/errors/most NFRs; Sprint 3 closed
`extraction-latency` and `memory-ceiling`, the two components only
addressable once the profiling epic itself was decomposed.)

## Validation Errors Hit and Fixes

1. **`depends_on` cross-batch-file real-ID rejection.** Both Sprint 2 and
   Sprint 3 dry-runs initially failed with `Task 'X' depends on undefined ID
   'T-...'` for every edge pointing at an already-existing real task ID from
   an earlier batch (Sprint 1 IDs in the Sprint 2 batch; Sprint 1 *and*
   Sprint 2 IDs in the Sprint 3 batch). Root cause: `create-batch`'s two-pass
   dependency resolution only builds its id_map from IDs defined in the
   *current* file, unlike the `story:`/`epic:` foreign-key fields, which
   validate against the persisted store. Fix: stripped those specific
   cross-sprint edges from each batch YAML's `depends_on` lists (left an
   inline comment noting which real IDs they'd need), ran `create-batch` for
   real, then added the edges by editing each new task's `.ydk/tasks/T-*.md`
   frontmatter `dependencies:` list directly with a small one-off Python
   script (PyYAML round-trip), and re-validated with `ydk task validate-dag`,
   which reads from the repository and confirmed every edge landed correctly.
   Both helper scripts were deleted after use; only the batch YAML files and
   the resulting task/story/epic markdown remain.
2. **`--component-refs`/`--acceptance` on `ydk task create` don't accept
   comma-joined strings** — the CLI treats one `--component-refs "a,b,c"` as
   a single (invalid) component ID rather than splitting on commas. Fix:
   passed the flag once per value (`--component-refs a --component-refs b
   ...`), confirmed via `--dry-run` before the real call.
3. **Gap found and fixed proactively**: while writing Sprint 3, re-reading
   `ydk:nfr:extraction/max-file-size` and `ydk:nfr:extraction/nesting-depth-guard`
   (`applies_to: [detect-mime-type, extract]`) revealed that Sprint 2's Epic
   5 (OOXML detection hardening) never got a guardrail task — only Epic 3's
   CFB path did, even though the NFRs apply to *both* structural checks per
   `mime-detection.md`'s Guardrails section. Since Sprint 2 was already
   committed, this was closed with one surgical `ydk task create` (not a
   whole new batch) — `T-33a58c59`, "Enforce max-file-size and
   nesting-depth-guard checks in the OOXML detection path" — wired as a
   dependency of `task-ooxml-detect-harden` and added as an extra dependency
   of `task-detect-facade-wire`'s frontmatter. Re-validated DAG afterward;
   still valid, wave count unchanged (8 waves for Sprint 1+2 at that point).
   This is reflected in Sprint 2's 34-task total (33 batch + 1 corrective).

No other validation errors were hit; both batch dry-runs passed cleanly on
the first attempt after the `depends_on` fix above.

## Open Questions / Risks for Stage 03

1. **Adversarial-set item count discrepancy (7 vs. 8).** Epic 8's own
   description and story-08-adversarial-set's title both say "8-item"
   adversarial set, but `dataset-curation.md`'s Corpus Composition section
   and the story's own `acceptance` bullets enumerate exactly **7** distinct
   items (corrupt, truncated, zero-byte, oversized/bomb, password-protected,
   wrong-extension, corrupt-embedded-object). Sprint 3's three
   adversarial-sample tasks were built around the 7 explicitly-enumerated
   items (testable acceptance criteria take precedence over an unqualified
   epic-description number), and `task-corpus-manifest-finalize`'s acceptance
   says "7 adversarial" accordingly. Stage 03 should resolve whether an 8th
   scenario was intended (macro-variant-misclassification is already covered
   separately via the synthetic macro samples, not the adversarial set, so
   it's not an obvious 8th candidate) or whether the epic/story text is
   simply off by one.
2. **NPOI i18n fidelity is still the single biggest risk gating Epic 4/6
   text-extraction tasks** (unchanged from the Sprint 1 report) —
   `T-94570c2b` hasn't run yet. All three Epic 4 `*-text` tasks and all three
   Epic 6 `*-text` tasks reference its findings in their acceptance criteria;
   if it surfaces a real fidelity gap for a script family, those six tasks'
   scope (and possibly their acceptance criteria) will need revisiting before
   Stage 03 execution reaches them.
3. **Dataset Curation (Epic 8) tasks are unusually "no test" by design** —
   13 of 68 tasks are `test_strategy: "No test — infra/tooling task"` because
   producing a sample or a manifest entry isn't itself a testable unit; their
   correctness is validated transitively by Epic 9's integration tests
   consuming the manifest. This is expected given testing-strategy.md's
   three-mechanism rule is about *scenario* coverage, not *task* coverage,
   but flagging it so it isn't mistaken for a planning gap.
4. **The Epic 5 OOXML-guardrail gap (see Validation Errors #3) is a good
   signal that a second pass specifically re-checking every NFR's
   `applies_to` list against the tasks that actually reference it would be
   worthwhile before Stage 03 execution starts** — this pass caught one
   instance but wasn't a systematic audit; there may be other
   `applies_to`-vs-task-coverage mismatches that `component-coverage`
   (which only checks "referenced by ≥1 task," not "referenced by every path
   it should be") won't surface.
5. **Format-specific "open component" work is unevenly distributed between
   Epic 4 and Epic 6** — Epic 4 (legacy) got a dedicated open-component task
   per format because the scaffold has no legacy open logic yet; Epic 6
   (OOXML) did not, on the assumption that the existing scaffold already
   opens OOXML documents (Epic 2's generalized-walk work touched
   `ExtractionHelper.ExtractFirstLayerEmbedded`, which presupposes an already
   -open document). If that assumption is wrong once Stage 03 starts reading
   the actual `ExtractionHelper`/`MainExtractor` code, Epic 6 may need an
   additional "open component" task added per format, similar to Epic 4's.

## Command Log

```
ydk task list
ydk task --help
ydk task list --help
ydk task create-batch --from .ydk/batches/pass3-sprint2-tasks.yaml --dry-run   # failed: undefined cross-sprint IDs
ydk task create-batch --from .ydk/batches/pass3-sprint2-tasks.yaml --dry-run   # clean, after stripping cross-sprint edges
ydk task create-batch --from .ydk/batches/pass3-sprint2-tasks.yaml            # 33/33 created
python .ydk/batches/_add_cross_sprint_deps.py                                  # added 20 cross-sprint edges via frontmatter, then deleted
ydk task validate-dag                                                          # valid, 8 waves
ydk task create --title "Enforce max-file-size..." --dry-run                   # component-refs comma-splitting failed first attempt
ydk task create --title "Enforce max-file-size..."                              # T-33a58c59 created (corrective Epic 5 task)
ydk task validate-dag                                                          # valid, 8 waves (unchanged)
ydk task create-batch --from .ydk/batches/pass4-sprint3-tasks.yaml --dry-run   # clean
ydk task create-batch --from .ydk/batches/pass4-sprint3-tasks.yaml            # 25/25 created
python .ydk/batches/_add_cross_sprint_deps2.py                                 # added 8 cross-sprint edge sets, then deleted
ydk task validate-dag                                                          # valid, 11 waves
ydk task component-coverage                                                    # 26/26
```

All invocations used `PYTHONIOENCODING=utf-8` and bounded timeouts (60-180s);
every command returned promptly, no hangs encountered.
