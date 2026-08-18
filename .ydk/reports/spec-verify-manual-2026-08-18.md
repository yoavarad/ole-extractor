# Spec Quality Verification — Manual Review (Re-run 4)

**Project:** ole-extractor
**Date:** 2026-08-18
**Method:** Manual review standing in for AWS Bedrock (unavailable). Rubrics applied by hand from `.ydk/spec-reviewers/n01.yaml`–`n10.yaml`, plus deterministic scans replicating the embedded `scan_filler_phrases`, `scan_url_paths`, `scan_type_annotations`, and `scan_unlinked_mentions` tools.
**Artifacts graded:** `docs/specs/*.md` (6 files), `.ydk/components/**/*.yaml` (26 files), `docs/project-rules.md`, `docs/adrs/*.md` (2 files).
**Supersedes:** all previous runs of this report (re-run 1 FAIL 4/10, re-run 2 FAIL 8/10, re-run 3 FAIL 9/10).

**Re-run 4 scope:** N04 re-graded against the current file state after the third fix round (a full synonym/naming sweep of **all** of `docs/specs/*.md`, `.ydk/components/**/*.yaml`, `docs/adrs/*.md`, and `docs/project-rules.md` — not only the files just edited), plus a coherence sanity check on the `testing-strategy.md` C5 rewrite. N01–N03 and N05–N10 were **not** re-graded; their scores are carried forward.

---

## Threshold Ambiguity — RESOLVED

The previous report flagged an open question: the harness printed a threshold of `8` for all ten reviewers, while `n07`–`n10` appeared to declare `7`. I read the `threshold:` field directly out of each YAML file this time. The files are authoritative and they are **not** uniform:

| File | Declared `threshold` (line 4) |
|---|---|
| `n01.yaml` | 8 |
| `n02.yaml` | 8 |
| `n03.yaml` | 8 |
| `n04.yaml` | 8 |
| `n05.yaml` | 8 |
| `n06.yaml` | 8 |
| `n07.yaml` | **7** |
| `n08.yaml` | **7** |
| `n09.yaml` | **7** |
| `n10.yaml` | **7** |

The harness printing `8` for all ten is a **harness display/default bug**, not the real configuration. Every grade below uses the per-file threshold.

*(Re-run 4 note: the distinction still matters. N04 now scores exactly 8 and clears its threshold with no margin; N05 and N08 likewise pass at exactly their thresholds. Were the harness's uniform `8` the real configuration, N08 would fail.)*

---

## Verdict

# PASS — 10 / 10 passed

Up from 9/10 in re-run 3, 8/10 in re-run 2, and 4/10 in re-run 1. **N04 now clears its threshold**, which was the last blocker.

- **N04 (Terminology Consistency) — score 8, threshold 8. PASS.** All four re-run-3 findings are fixed: `extract.yaml:12-13` now says "the matching open component and text-extraction component", `extract.yaml:15` now says "per-format-kind output shape", `overview.md:28` now uses the canonical label "Detection component", and `overview.md:30` is now hyphenated as "Text-extraction component". A full-corpus sweep (below) confirms **every** `DO NOT USE` term in `glossary.md` now has zero hits anywhere in the graded set outside the glossary's own DO NOT USE lines. What remains is three shorthand-drift residues, none of them a competing name for a glossary-governed role. Passes at threshold with no margin.
- **C5 (`testing-strategy.md` coverage rule) — coherent.** Sanity-checked, not re-graded. The three-mechanism categorization is internally consistent and now covers the two scenarios the previous enumeration dropped (deeply-nested embedding, file-too-large). One residual, C5b, noted below.

No required fixes remain. Seven optional margin-restoring edits are listed under Recommended — worth taking, since N04, N05, and N08 all pass at exactly their thresholds with zero headroom.

---

## Scorecard

| ID | Name | Group | Threshold | Score | Result | Δ vs. prev |
|---|---|---|---|---|---|---|
| N01 | Problem Statement | completeness | 8 | **9** | PASS | = PASS (carried forward) |
| N02 | Success Criteria | completeness | 8 | **9** | PASS | = PASS (carried forward) |
| N03 | Scope Boundaries | completeness | 8 | **9** | PASS | = PASS (carried forward) |
| N04 | Terminology Consistency | clarity | 8 | **8** | **PASS** | ▲ from 7 (meets exactly) |
| N05 | Ambiguity | clarity | 8 | **8** | PASS | = PASS (carried forward) |
| N06 | Flow Completeness | clarity | 8 | **8** | PASS | = PASS (carried forward) |
| N07 | Information Density | quality | 7 | **9** | PASS | = PASS (carried forward) |
| N08 | No Technical Specs in Prose | quality | 7 | **7** | PASS | = PASS (carried forward) |
| N09 | Component References | quality | 7 | **9** | PASS | = PASS (carried forward) |
| N10 | YAGNI | quality | 7 | **8** | PASS | = PASS (carried forward) |

---

## Deterministic Scan Results

Replicating the tools embedded in the rubric YAMLs, run over `docs/specs/`:

| Scan | Source rubric | Result |
|---|---|---|
| `scan_url_paths` (HTTP method + path, `/api/…`) | N08 | **0 findings** |
| `scan_type_annotations` (`int`/`str`/`bool`/`UUID`/`Decimal(…)`/`List[`/`datetime`…) | N08 | **0 findings** |
| `scan_filler_phrases` (20-phrase list) | N07 | **0 findings** |
| `scan_unlinked_mentions` — unlinked routes | N09 | **0 findings** |
| `scan_unlinked_mentions` — unlinked PascalCase entities | N09 | **2 findings** (`LibreOffice`, `FormatFamily`) |
| Placeholder scan (`TBD`, `TODO`, `FIXME`, `XXX`, `???`, `lorem`, `<insert`, `[fill`) | Step 19b | **0 findings** |
| Component-reference resolution (all `ydk:` refs in narratives → manifest `id:`) | Step 19b | **0 broken, 0 orphaned** |

The two PascalCase hits are addressed under N09 and N08 respectively — one is an external product name, the other is a dotted field path.

---

## Per-Criterion Detail

### N01 — Problem Statement · threshold 8 · **score 9 · PASS**

The rewrite lands all four required elements.

- **WHO** — `overview.md:5`: "Systems that ingest arbitrary Office files from untrusted sources — content pipelines, document management systems, search indexing, security scanning". Concrete persona set, previously absent.
- **WHAT** — `overview.md:5`: "cannot rely on the filename to say what a file actually is, and cannot always adopt a paid SDK just to open it".
- **WHY** — `overview.md:5`: "a renamed or mislabeled file either gets silently misprocessed or has to be rejected outright, and without structured extraction, every downstream consumer re-implements its own fragile parsing". Consequence is now explicit for both halves of the problem.
- **SUCCESS** — a dedicated `## Success Criteria` section at `overview.md:9-15` with five measurable criteria.

**Why 9 and not 10:** the WHY carries no baseline number. The rubric's 10-band examples all quantify the pain ("2+ hours daily", "3% revenue leakage", "62% bounce rate"); this one quantifies only the target state, not the current cost. The success side is fully measurable, so this is comfortably above the 8-band ("all four elements present, some could be more specific").

**Not blocking.** Optional polish: attach a figure to the consequence, e.g. what fraction of ingested files carry a misleading extension in the motivating use case.

---

### N02 — Success Criteria · threshold 8 · **score 9 · PASS**

Five criteria at `overview.md:11-15`, quantified and verifiable, spanning four distinct dimensions:

| Dimension | Criterion | Quantified? |
|---|---|---|
| Classification correctness | "100% of samples in the curated corpus … classify to their exact expected format — no false positives, no false negatives, filename never a factor" (`:11`) | Yes — 100% |
| Extraction correctness | "100% of curated corpus samples produce extraction output matching the corpus manifest's exact expected text/metadata content — not merely non-empty" (`:12`) | Yes — 100%, and the "not merely non-empty" clause kills the weakest possible reading |
| Robustness | "Every adversarial corpus sample (corrupt, truncated, oversized, password-protected, zero-byte, wrong-extension) fails with the specific error its scenario calls for — never an unhandled exception, never a hang, never a silent empty result" (`:13`) | Yes — universally quantified over an enumerated set |
| Performance | Delegated to `ydk:nfr:extraction/extraction-latency` (P95 < 500ms @ 10MB, P95 < 5s @ 100MB), `memory-ceiling` (10× input), `nesting-depth-guard` (1 GB/entry, 2 GB/container) (`:14`) | Yes — by reference to numeric NFR targets |
| Extensibility | "A hypothetical 7th format can be added by implementing and registering one new detection/open/text-extraction component set, without changing the two public function signatures" (`:15`) | Yes — binary and design-review-verifiable |

**Timebound dimension** is satisfied indirectly rather than by an explicit date: criteria are scoped to v1 throughout, the memory-ceiling NFR now carries a hard revision deadline ("MUST be revised to an empirically-validated number no later than the end of that epic" — `memory-ceiling.yaml:7-9`), and latency carries a CI regression gate ("CI fails the profiling task if P95 regresses more than 20% against the last committed baseline" — `extraction-latency.yaml:7`). That is enough to clear the 8-band's "missing one dimension (e.g., no timebound aspect)" deduction.

**Why 9 and not 10:** "met on the profiling corpus" (`:14`) delegates rather than restating the numbers inline, so a reader of the success section alone gets no figures without following three references.

**Note:** the "component pair" arithmetic slip flagged here in re-run 2 (**C4**) is fixed — criterion `:15` now reads "component set" and names all three roles. Score unchanged at 9.

---

### N03 — Scope Boundaries · threshold 8 · **score 9 · PASS**

`overview.md:40-57` provides all four rubric elements as an explicitly labelled structure:

- **IN scope (v1)** — 5 bullets, enumerating the six formats, macro-enabled variant handling, the four output categories, i18n fidelity, and the corpus/profiling/stress work (`:42-47`).
- **OUT of scope (v1)** — 4 bullets, **each with rationale and each with a pointer to the component that enforces the boundary**: password decryption (→ `password-protected`, "fails cleanly instead"), recursive subfile unpacking (→ `subfile-scope`), formats beyond the six (→ `format-extensibility`, with the "architecture must not preclude" caveat), and a hosted service/HTTP API ("it's a library; monitoring NFR fields are marked 'not applicable' accordingly") (`:49-53`).
- **DEFERRED** — 2 bullets with deferral rationale: additional formats "candidates for a future increment once the 6-format baseline is stable", and recursive unpacking "if a real use case emerges" (`:55-57`).
- **BOUNDARY definitions** — present but distributed rather than under a single label: `overview.md:36-38` ("in-process library with no network surface, no users, no auth"), `overview.md:34`'s forbidden-dependency rule ("format-specific parsing MUST NOT appear outside the open/text-extraction component layer"), and `testing-strategy.md:13` ("no network calls, no filesystem dependency inside the extraction path itself — callers supply bytes").

**Why 9 and not 10:** the boundary statements are correct and complete but are not gathered under a `BOUNDARY:` label the way the rubric's model answers are, so a skimmer reading only the Scope section will not find them.

---

### N04 — Terminology Consistency · threshold 8 · **score 8 · PASS (meets threshold exactly)** *(re-graded, re-run 4)*

#### Method — full-corpus sweep

Prior passes graded the files that were the fix round's primary edit target and each missed instances elsewhere. This pass swept every file in the graded set — `docs/specs/*.md` (6), `.ydk/components/**/*.yaml` (26), `docs/adrs/*.md` (2), `docs/project-rules.md` — for each of the 12 glossary terms plus each of their 15 `DO NOT USE` synonyms, and separately for undeclared shorthand drift (informal names for components/artifacts that have a canonical id).

Result on the declared bans: **zero hits across the entire graded set**, outside the `DO NOT USE` lines in `glossary.md` themselves. Specifically —

| Banned term | Hits in graded set |
|---|---|
| "OLE2" alone, "compound document" | 0 — `detect-mime-type.yaml:14`'s "CFB/OLE2 structure" is the sanctioned CFB+OLE pairing, not "OLE2" alone |
| "Open XML" without prefix, "docx format" for the family | 0 |
| "attachment", "embedded file" alone | 0 |
| "file type" (banned for both format family and exact mime type) | 0 |
| "format family" in the format-kind sense | 0 |
| "top-level", "shallow" (for embedding depth) | 0 |
| "sniffer", "mime detector" | 0 |
| "open strategy" | 0 |
| "text handler", "text extractor" | 0 |
| "fixture", "test file" alone | 0 |

Verb and action-noun forms of "sniff" (`detect-mime-type.yaml:12,23`, `corrupt-file.yaml:17`, `extraction-latency.yaml:3`) remain permitted — `glossary.md:27` bans the agent-noun "sniffer" as a name for the component, not the action. `docs/project-rules.md:12,13,20` ("Strategy (open) + Handler (text) pattern", "strategies, extractors, detectors") is still uncounted for the same reason as re-run 3: it describes existing C# class names in the scaffold, not the domain vocabulary. `docs/research/*` is not in the graded set.

#### What the fix round resolved

**N04-5 — RESOLVED.** `contract/extraction/extract.yaml` was swept, and both defects in it are gone:
> `extract.yaml:12-13` — "Dispatches on DetectedMimeType to the matching **open component and text-extraction component** (OOXML via DocumentFormat.OpenXml, legacy OLE via NPOI…"
> `extract.yaml:14-15` — "see [ydk:entity:extraction/WordFormatMetadata] and its siblings for the **per-format-kind** output shape"

The fix is better than F10 asked for on both counts: naming *both* roles rather than just "open component" is accurate (the method dispatches to both), and "and its siblings" reads more cleanly than the original "and family", which would itself have been a mild echo of the banned "format family" phrasing.

**N04-3 — RESOLVED.** `overview.md:28` now reads "- **Detection component** — one per format family (structural signature for OOXML, directory-entry inspection for legacy)…". Canonical label, and the redundant "one detection component" was correctly dropped from the sentence body.

**N04-4 — RESOLVED.** `overview.md:30` now reads "- **Text-extraction component** (one per format)…", matching `glossary.md:32` and `testing-strategy.md:5`.

**N04-1 and N04-2 — remain resolved.** Re-verified, not just carried forward. Every surviving "format family" occurrence in the graded set — `glossary.md:3,12,13,15,16,26`, `overview.md:28`, `extraction.md:13`, `ExtractionResult.yaml:18`, `adrs/001:7,17,23` — is the container-technology sense. Every "format kind" occurrence — `glossary.md:13,15`, `extraction.md:13,19`, `ExtractionResult.yaml:17`, `extract.yaml:15` — is the Word/Excel/PowerPoint sense. The two senses no longer cross anywhere.

#### Residual drift — three instances, all shorthand, none glossary-governed

These are what keeps the score at 8 rather than 9 or 10. None is a competing name for a defined role, and none would leave a reader unsure what refers to what.

**N04-6 (minor) — the nesting-depth-guard NFR has four informal shorthands.** One component id, four names in running prose:
> `overview.md:64` — "within the **bomb-guard's** absolute caps"
> `extraction.md:40` — "declared internal size exceeds the **nesting guard**"
> `oversized-nested-content.yaml:24` — "Embedded-object nesting exceeds the **depth guard**"
> `memory-ceiling.yaml:3` — "distinct from the **oversized-nested-content guard**"

Three of the four sit in a sentence that also carries the full `ydk:nfr:extraction/nesting-depth-guard` id, so the referent is never actually ambiguous; `extraction.md:40` is the exception — it names no id, so "the nesting guard" is the reader's only handle there. This is the closest thing to a real N04 finding left in the set, and it is the reason this scores 8 and not 9.

**N04-7 (trivial) — "container family" for "format family".**
> `FileMetadata.yaml:25` — "description: Which **container family** the file belongs to."

This is the description of the `FormatFamily` field itself, i.e. the exact field `glossary.md:12` points at for the canonical definition. "Container family" is not a defined term; the canonical phrasing is "format family" (or `glossary.md:12`'s own gloss, "which container technology a detected format uses").

**N04-8 (trivial) — two names for the corpus manifest, introduced by the C5 rewrite.**
> `testing-strategy.md:15` — "all tagged in the **dataset-curation manifest**"

against "the **corpus manifest**" at `testing-strategy.md:6`, `dataset-curation.md:17` (section heading), `dataset-curation.md:19`, and `unicode-fidelity.yaml:26`. Same artifact (`samples/manifest.json`), two names, one of them in the same file as the other.

#### Considered and not counted

- **"format-handling components"** (`testing-strategy.md:13`, `dependency-injection.yaml:7`) — an umbrella for all three roles, not a fourth name for any one of them, and glossed inline at `dependency-injection.yaml:7` ("detection components, open components, text-extraction components"). Legitimate collective noun.
- **"entry points" / "public functions" / "public facade"** (`overview.md:19,15,34`, `dependency-injection.yaml:12,13`, `testing-strategy.md:6,8`) — ordinary descriptive English for the two-function surface, not a glossary term; "facade" additionally denotes a specific distinct thing (the non-DI-aware wrapper), not a synonym.
- **"embedded item" / "embedded object"** (`extraction.md:29`, `SubfileItem.yaml:4,13`) vs. **"subfile"** — deliberate, and exactly the distinction `glossary.md:10` draws between the abstract in-document thing and the concrete extracted item.
- **`# MIME Detection`** (title case) vs. "mime detection" elsewhere — heading capitalization, not a term change.

**Scoring.** Every glossary-governed term is now clean across all 35 graded files — that is what the 10-band's "no synonyms" asks for on the axis the glossary actually governs. Holding it to 8 rather than higher: the guard shorthand (N04-6) is genuinely four names for one component, and N04-7 and N04-8 are two more small naming drifts, one of them newly introduced by this round's C5 edit. The 8-band is "mostly consistent, one or two minor slips"; counting N04-6 as the one real slip and N04-7/N04-8 together as the second lands squarely there, and nothing approaches the 5-band's "several term inconsistencies that could confuse a reader". **Score 8 — PASS at threshold, no margin.** As with N05 and N08, one more drift reopens this; see Recommended fixes.

---

### N05 — Ambiguity · threshold 8 · **score 8 · PASS (meets threshold exactly)** *(re-graded, re-run 3)*

All four re-run-2 findings are verified fixed against the current files.

**N05-1 ("etc.") — RESOLVED.** `extraction.md:9` now enumerates the set: "carries the fields common across all six formats — title, author, last-modified-by, subject, keywords, comments, created/modified timestamps, and application name — sourced from each format's native document-properties store." Nine fields, matching `FileMetadata.yaml`'s nine native-property-sourced fields (`Title`, `Author`, `LastModifiedBy`, `Subject`, `Keywords`, `Comments`, `CreatedAt`, `ModifiedAt`, `ApplicationName`) exactly. No open terminator.

**N05-2 ("and similar") — RESOLVED.** `extraction.md:27` now reads "Internal package structure that isn't an embedding or media item (the exhaustive exclusion list is in [ydk:req:extraction/subfile-scope]) is never listed as a subfile." The hedge is replaced by a pointer to the closed set at `subfile-scope.yaml:10-16`, which is genuinely exhaustive and is mirrored in acceptance criterion 1 (`:23`). Pointing at the manifest is the better fix — it removes the duplication that let the two drift apart in the first place.

**N05-3 ("many concurrent calls") — RESOLVED for the load target.** `testing-strategy.md:8` now reads "exercising at least 50 concurrent calls to both public functions (configurable higher for extended runs)". The harness now has an implementable floor, which was the substance of the finding.

**N05-4 (curated-sample rule) — RESOLVED, and well.** `dataset-curation.md:12` replaces "unusual property sets that are hard to synthesize convincingly" with two structurally testable requirements: "at least one with 2+ levels of nested embedding (an embedded object that itself contains an embedded object) and at least one with a real (not scripted) authorship/revision-history property chain from having been edited by multiple real users across multiple real save operations." A curator can now decide whether a candidate qualifies. This also has a useful side effect on **C5** — see below.

Deterministic re-scan over `docs/specs/`: **zero** instances of `etc.`, `and so on`, `and more`, `some`, `many`, `few`, `several`, `various`, `typically`, `usually`, `often`, `generally`, `probably`, `reasonable`, `appropriate`, `adequate`, `significant`, `substantial`, `as needed`, `if necessary`, `in practice`, `TBD`, or `to be determined`.

#### Residual soft language — four instances, all non-critical

None is in a requirement, success criterion, NFR target, or acceptance rule; all four are in explanatory or motivating prose whose precise form lives in a manifest.

1. `testing-strategy.md:8` — "run manually or as a **longer** CI job", and "(configurable **higher** for **extended** runs)". Unquantified comparatives. This is the harness's *cadence*, not its target; the target itself is now 50. (Partial residue of N05-3.)
2. `overview.md:7` — "backed by a test corpus **large enough to trust** the correctness claims". Subjective, but the Success Criteria section immediately below quantifies what the corpus must prove, and `dataset-curation.md:15` fixes its size at ≥30 + adversarial.
3. `mime-detection.md:19` — "a structurally-tiny file claiming an **implausibly large** internal size". Subjective gloss on a check whose actual numbers are pinned in `nesting-depth-guard.yaml:5` (1 GB/entry, 2 GB/container).
4. `dataset-curation.md:13` — "not per-format, since these scenarios are format-agnostic **where possible**". A hedge inside a parenthetical justification; the rule itself ("each tagged with which format(s) it targets") is precise.

Items 2–4 are pre-existing prose that neither this round nor re-run 2 flagged; they are listed for completeness rather than as new regressions.

**Scoring.** The 8-band is "1-3 minor instances of imprecise language in non-critical sections"; the 5-band requires vague terms "scattered throughout, **some in critical sections**". Zero remain in critical sections — every requirement, NFR target, success criterion, and acceptance rule in the set is now quantified — which rules out the 5-band decisively. Four minor non-critical instances rather than three keeps it off 9. **Score 8 — PASS at threshold, no margin.** One more soft construction in narrative prose drops this below the bar; recommend clearing item 1 (a stated cadence, e.g. "nightly") to restore headroom.

---

### N06 — Flow Completeness · threshold 8 · **score 8 · PASS**

The three behaviors from the previous finding are defined and cross-referenced exactly as claimed, each in all the places it needs to be:

**Per-subfile failure** — defined in the narrative, restated in the overview's adversarial list, and pinned by a test in the requirement manifest:
- `extraction.md:29` — "If an individual embedded item cannot be read … it is omitted from the subfiles list and the omission is logged — this does not fail the overall extraction call."
- `overview.md:70` — same behavior as an adversarial-review entry.
- `subfile-scope.yaml:24` — "Test: a document with one corrupt embedded object among otherwise-valid embeddings extracts successfully, omits only the corrupt one from Subfiles, and logs the omission — the overall Extract() call does not throw."

**Zero-byte input** — the detection/extraction asymmetry is stated identically in four places, including the error manifest's trigger list, which is where it is easiest to get wrong:
- `mime-detection.md:19` — "A zero-length input is never rejected by detection itself — it simply produces the Unknown result".
- `overview.md:66` — "Detection returns the Unknown result (not an error…); extraction always fails with [ydk:error:extraction/truncated-container]".
- `extraction.md:38` — truncated-container covers "or the input is zero-length".
- `truncated-container.yaml:19` — "detection itself never throws this, a zero-length input simply detects as Unknown; this trigger fires only if Extract() is called directly with a zero-length input".

**Null request / null required field** — consistently framed as boundary validation with an explicit justification for having no manifest:
- `overview.md:69` and `extraction.md:42` — both say it throws immediately via standard argument validation "before any … logic runs — this is boundary validation, not a domain error, so it has no dedicated error manifest."

Beyond the three new behaviors, coverage is strong: 11 adversarial scenarios enumerated with resolutions at `overview.md:61-72`; all six error manifests carry `triggers` lists; happy paths for both entry points are described (`mime-detection.md:7-11`, `extraction.md:7-29`); empty states are pinned to a value rather than left open ("empty, never null" — `extraction.md:23`; "Empty list (not null)" — `ExtractionResult.yaml:34`); and concurrency is addressed rather than ignored (`overview.md:72`).

**Deductions from 10:**
1. Rubric element 4 asks error handling to specify "WHAT error, WHO sees it, and WHAT they can do about it". WHAT and WHO are covered; remediation is thin — `password-protected.yaml` tells the caller the file is encrypted but not what recourse exists, and no narrative addresses caller remediation for any error.
2. The mismatch between the adversarial scenarios and the adversarial corpus (see **C5**) meant two scenarios had a stated behavior but no stated sample. *(Re-run 3: resolved — the corrupt-embedded-object sample was added and the coverage rule rescoped. Re-run 4: C5a closed too — every scenario with defined v1 behavior now maps to a named test mechanism. Not re-graded; the score would not move below 8 either way.)*
3. The "caller passes a DetectedMimeType inconsistent with the actual bytes" flow is specified only in `extract.yaml:15-19` ("gets undefined per-format-field behavior, not a guaranteed error"); no narrative mentions it, so a reader of the specs alone will not learn that this input has undefined behavior.

Score **8** — "most flows are complete, 1-2 minor edge cases not addressed". PASS.

---

### N07 — Information Density · threshold 7 · **score 9 · PASS**

The `scan_filler_phrases` tool's full 20-phrase list returns **zero** matches across all six narratives. Manual reading confirms it: no "It should be noted that", no "in order to", no "the fact that", no "in terms of", no hedging preambles, no restated points.

(A first pass of my own regex appeared to flag ~14 lines. All were false positives from `very` matching inside `every` — "Every synthetic sample…", "every occurrence logged", "Every adversarial-review scenario". The other apparent hits were `rather` inside "rather than", a contrastive construction, not the intensifier the rubric targets. No genuine filler exists.)

Prose is dense to the point of terseness — `overview.md:64` packs the bomb-guard behavior, the enforcement ordering, and the guard-vs-target distinction into two sentences; `mime-detection.md:9` states the entire detection strategy in one.

**Why 9 and not 10:** `dataset-curation.md:5` — "this gets its own epic and its own narrative, not a folded-in afterthought of 'testing'" — is commentary about how the documentation is organized rather than about the system, and the Purpose section would carry the same information without it.

---

### N08 — No Technical Specs in Prose · threshold 7 · **score 7 · PASS (meets threshold exactly)**

The claimed cleanup is verified. A full-text scan of `docs/specs/` returns **zero** occurrences of `IOpenStrategy`, `ITextExtractor`, `ExtractionResult.`, `Parallel.ForEachAsync`, `SemaphoreSlim`, `BenchmarkDotNet`, `HdrHistogram`, `WordDocument`, or `Workbook`. Both deterministic scans (`scan_url_paths`, `scan_type_annotations`) return zero. The narratives now reference tooling conceptually — `testing-strategy.md:7` says "(tooling choice recorded in ADR-002)" and `:8` says "(tooling choice and rationale recorded in ADR-002)" — with the actual library names confined to `docs/adrs/002-profiling-and-stress-testing.md:10-11` and `docs/project-rules.md`, exactly as intended. Literal CFB stream names now live only in `detect-mime-type.yaml:14-16`, which is a manifest.

Three leaks remain, all in `glossary.md`, all in the 7-band's "1-3 minor leaks (field names mentioned in passing)":

1. **Dotted field path** — `glossary.md:12`: "per [ydk:entity:extraction/FileMetadata]**.FormatFamily**". The bracketed reference is correct; the `.FormatFamily` suffix appended to it is exactly the dotted field-path pattern the previous round removed elsewhere. (This is also the second of the two `scan_unlinked_mentions` PascalCase hits.)
2. **Literal enum values** — `glossary.md:12`: "one of `LegacyOle` or `Ooxml`". These values are defined in `FileMetadata.yaml:23`.
3. **Literal mime type strings** — `glossary.md:15`: "(e.g. `application/msword`, not a generic `application/octet-stream`)". These are defined in `MimeDetectionResult.yaml:18-19` and `FileMetadata.yaml:18-20`.

All three are defensible — a glossary entry that defines a term arguably has to name the canonical values the term ranges over, and the rubric's `WHAT IS ALLOWED` list does not cover the glossary case. But the rubric opens with "This check has ZERO TOLERANCE", so they cannot be scored away entirely. Three minor leaks is the 7-band precisely.

`dataset-curation.md:19`'s `samples/manifest.json` is a repo path, not an API path or a type — not counted.

**PASS, but with no margin.** One more leak drops this below threshold. Recommend fixing the `.FormatFamily` suffix (item 1) to restore headroom; items 2 and 3 can stay.

---

### N09 — Component References · threshold 7 · **score 9 · PASS**

Strongest criterion in the set. A programmatic cross-check of every `ydk:` reference in `docs/specs/` against the `id:` field of all 26 manifests in `.ydk/components/`:

- **0 broken references** — every `ydk:` id cited in a narrative resolves to a real manifest.
- **0 orphaned components** — all 26 manifests (2 contracts, 8 entities, 6 errors, 4 NFRs, 5 requirements + 1) are cited by at least one narrative. Nothing was specified and then left unreferenced.
- **0 unlinked routes** (no routes exist — library, not a service).
- **2 unlinked PascalCase mentions**, neither a real miss: `LibreOffice` (`dataset-curation.md:12`) is an external authoring tool used to produce samples, not a system integration requiring an `[ydk:ext:…]` component; `FormatFamily` (`glossary.md:12`) is a field name, already scored as an N08 leak above.

The new cross-references added in this round all resolve correctly, including the bidirectional pair between `nesting-depth-guard.yaml:11` and `memory-ceiling.yaml:14`.

**Why 9 and not 10:** reference *style* is inconsistent on one line. `overview.md:14` cites three NFRs in backticks — `` `ydk:nfr:extraction/extraction-latency` ``, `` `ydk:nfr:extraction/memory-ceiling` ``, `` `ydk:nfr:extraction/nesting-depth-guard` `` — where every other citation in every other narrative uses the `[ydk:…]` bracket form. The ids are correct and resolve; only the delimiter differs. Tooling that matches on `\[ydk:[^\]]+\]` will not see these three.

---

### N10 — YAGNI · threshold 7 · **score 8 · PASS**

The spec is well-disciplined about not building what was not asked for. The explicit `OUT of scope` and `DEFERRED` lists (`overview.md:49-57`) actively fence off the obvious gold-plating candidates — recursive unpacking, additional formats, a hosted service — rather than leaving them ambiguous. `testing-strategy.md:15` refuses to invent infrastructure: "DB strategy / external API strategy: not applicable — this library has neither." `overview.md:32` confirms the dead-code folder is not being carried forward.

The one structure that *looks* like a YAGNI violation is not one. The registry-based `format-extensibility` requirement would normally read as "plugin architecture for one known use case" — the rubric's example violation. But it is a **stated user requirement**, recorded independently in `project-rules.md:21`: "doc/xls/ppt/docx/xlsx/pptx are the minimum required formats, not the ceiling. Design the mime-detection and extraction contracts so a new format … can be added without reshaping the public API." It is also scoped honestly — `format-extensibility.yaml:5` marks it `priority: medium`, the only non-critical requirement in the set, and it reuses the scaffold's existing dispatch pattern rather than inventing a new one.

**Deductions from 10** — two mild speculative-generality instances, both self-aware:

1. `MimeDetectionRequest.yaml:4-6` justifies the request-class shape as "so fields can be added later (e.g. size hints, partial-read options) without changing the function signature" — naming hypothetical future fields. The class-input shape itself is a stated user requirement; only the speculative field examples are surplus.
2. `oversized-nested-content.yaml:21` lists a trigger for "Embedded-object nesting exceeds the depth guard (relevant if a future version recurses into subfiles; v1 only extracts first-layer, so this mainly guards against a maliciously deep single-layer directory/FAT structure)" — a guard partially aimed at a version that does not exist. The parenthetical rescues it by naming a genuine v1 threat the same check covers.

Score **8** — "mostly well-scoped, 1-2 minor over-engineering instances". PASS.

---

## Step 19b Self-Review Checklist

### 1. Placeholder scan — **PASS**

Zero occurrences of `TBD`, `TO BE DETERMINED`, `TODO`, `FIXME`, `XXX`, `???`, `lorem ipsum`, `<insert`, `[fill`, or `coming soon` across `docs/specs/`, `docs/adrs/`, `docs/project-rules.md`, and `.ydk/components/`.

The single scan hit is a legitimate use of the word in running prose — `extraction.md:9`: "A field the source format doesn't provide is null, never a placeholder string." Not a placeholder.

The one genuinely provisional value in the spec set is now *declared* provisional with a binding deadline rather than left silent — `memory-ceiling.yaml:7-9`: "PROVISIONAL ratio, pending real allocation data from the Time Profiling epic — MUST be revised to an empirically-validated number no later than the end of that epic, and MUST NOT be treated as final before then." This is the correct handling.

### 2. Internal consistency — **C1–C3 resolved (re-run 2); C4, C5, C6 resolved (re-run 3); C5a resolved (re-run 4); one trivial residual, C5b**

**Previously blocking — all verified fixed:**

**C1 (macro-variant contradiction) — RESOLVED.** The two documents now say the same thing, and say it at the level of *why*, not just *what*:
- `mime-detection.md:13-15` — a dedicated "Macro-Enabled Variants" section: "structurally distinguishable … detection can and does see the difference internally. The detected-format value nonetheless stays the same as the base format (a docm detects as Docx, not as a separate enum value) so the six-format contract doesn't grow a parallel macro axis. The distinguishing information isn't discarded: it surfaces as an explicit macro-presence flag on the format-specific metadata produced by extraction, not on the detection result itself."
- `overview.md:44` and `overview.md:63` now match this exactly, with `:63` pointing at `mime-detection.md` for the reasoning.
- `MimeDetectionResult.yaml:7` keeps the enum at `[Doc, Xls, Ppt, Docx, Xlsx, Pptx, Unknown]` — no macro values — consistent with the above.

**C2 (WordFormatMetadata missing HasMacros) — RESOLVED.** `HasMacros` is present on all three format-metadata entities, all `required: true`, all with legacy-format handling described:
- `WordFormatMetadata.yaml:42-45` — "True if the document contains VBA macro content (docm, or legacy doc with a macro storage)."
- `ExcelFormatMetadata.yaml:19-22` and `PowerPointFormatMetadata.yaml:16-19` — parallel wording.
- `extraction.md:15-17` lists the macro-presence flag for all three, and `:19` states the invariant: "Every format family carries its own macro-presence flag."

**C3 (bomb-guard vs memory-ceiling relationship) — RESOLVED in the primary locations.** The two NFRs now cross-reference each other and each states which role it plays:
- `nesting-depth-guard.yaml:10-14` — "This is a hard, always-enforced ceiling, independent of ydk:nfr:extraction/memory-ceiling (which is a measured target over realistic files, not a runtime check) — this guard is what actually stands between the library and a memory-exhaustion attack."
- `memory-ceiling.yaml:11-15` — "it is a measured performance characteristic, not a runtime-enforced limit; adversarial/degenerate inputs are bounded instead by ydk:nfr:extraction/nesting-depth-guard, which IS enforced at runtime and takes precedence for those cases."
- `overview.md:64` states the same distinction in narrative form, including the edge case: "a maliciously small file within the bomb-guard's absolute caps is still rejected by the guard even if it would exceed the performance target's ratio."

See **C6** below for one residual leak of the old framing.

**New / residual issues:**

**C4 — RESOLVED** *(verified re-run 3)*. Both sites now count all three roles:
- `overview.md:15` — "can be added by implementing and registering one new **detection/open/text-extraction component set**, without changing the two public function signatures"
- `overview.md:31` — "adding a format later means adding a **detection/open/text-extraction component set** and registry entries, not touching the public API"

`format-extensibility.yaml` agrees in both the rule and the acceptance criteria: `:10-14` — "MUST be achievable by adding a new detection component, open component, and text-extraction component for that format plus registry entries for each"; `:22` — "'implement a detection component, an open component, and a text-extraction component for it, and register all three'". The arithmetic is now consistent with `overview.md:28-30` and `mime-detection.md:23`.

**C5 — RESOLVED** *(verified re-run 3; the quoted `testing-strategy.md:14` wording below has since been superseded by the re-run-4 rewrite described under C5a)*. The rule at `testing-strategy.md:14` was correctly scoped rather than universally quantified: file-dependent scenarios need a tagged corpus sample and are named explicitly ("corrupt, truncated, zero-byte, oversized, password-protected, wrong-extension, and corrupt-embedded-object are covered by the shared adversarial set, and the macro-variant-misclassification scenario is covered by each format's macro-enabled synthetic sample"), while "Scenarios that are pure argument/boundary validation (null request/field) or concurrency behavior (parallel calls) are covered by unit tests and the stress harness respectively". `dataset-curation.md:13` gained the matching sample — "one sample with a single corrupt embedded object alongside otherwise-valid content (exercises the per-subfile-failure rule in `extraction.md`)" — bringing the shared adversarial set to **7** (not 8; the round's summary miscounted, the composition list itself is correct and internally consistent). `subfile-scope.yaml:24`'s required test is now satisfiable.

**C5a — RESOLVED** *(verified re-run 4)*. The rule at `testing-strategy.md:14-17` was rewritten into an explicit three-way categorization, and both previously-orphaned scenarios are now named:
- `:15` (corpus-sample-backed) now ends "…; **deeply-nested embedding (the curated 2+-level-nesting sample)** — all tagged in the dataset-curation manifest", closing the `subfile-scope.yaml:26` gap.
- `:16` (unit-test-backed) now reads "null request/field validation; **file-too-large (a synthetic oversized byte-array length, per [ydk:nfr:extraction/max-file-size]'s test method)**".
- `:17` (stress-harness-backed) carries the concurrency scenario.

Checked scenario-by-scenario against `overview.md:61-72`: 11 of the 12 adversarial-review bullets now map to exactly one named mechanism, with no scenario claimed by two. The categorization is coherent and the two mechanisms that overlap conceptually (the zip/CFB-bomb "oversized" corpus sample vs. the "file-too-large" unit test) are correctly kept distinct — they exercise different guards. Note this rewrite is also where **N04-8** ("dataset-curation manifest" vs. "corpus manifest") entered.

Residual **C5b (trivial, informational)**: the 12th bullet, "Decimal/locale precision in numeric metadata" (`overview.md:71`), has no test under any of the three mechanisms — correctly, since it resolves to "out of scope for v1 … no numeric reinterpretation is performed, so there is no locale-conversion bug surface to defend", i.e. there is no behavior to test. But `testing-strategy.md:14` states the rule as universally quantified ("**Every** adversarial-review scenario … MUST have a corresponding test"), so as literally written this scenario is uncovered. A four-word carve-out ("every scenario with defined behavior") makes the rule true as stated. Not blocking, and not an N04 concern.

**C6 — RESOLVED** *(verified re-run 3)*. `oversized-nested-content.yaml:3-12` is rewritten and now matches the C3 framing exactly: the declared structure "exceeds the **hard, always-enforced caps** in [ydk:nfr:extraction/nesting-depth-guard]", checks happen "BEFORE allocating buffers for them", and memory-ceiling is explicitly demoted — "This is the mechanism that actually bounds memory use against malicious input; [ydk:nfr:extraction/memory-ceiling] is a separate, measured performance target over realistic files, **not something this check tests against**." The "allow" verb is gone. Memory-ceiling remains in `related:` (`:28`), which is correct — it is related context, not an enforced bound.

(Minor, uncounted: trigger `:23` reads "implies a file size **wildly** larger than FileBytes.Length" — soft language in a manifest trigger, where the enforced numbers come from `nesting-depth-guard.yaml:5`. Not scored under N05, which grades narrative prose.)

**C7 (informational) — subfile naming fallback is under-specified.** `ExtractionRequest.yaml:14` says `FileName` is "used verbatim as a fallback source for subfile naming when a container provides none", while `SubfileItem.yaml:17-20` says "legacy OLE storages are named by their CFB storage/stream name, which is used as-is when no better name is recoverable." These are not contradictory, but the precedence between the two fallbacks (CFB storage name vs. request filename) is not stated. Not a rubric violation; noting for the implementation planner.

**NFR schema formatting — verified fixed.** All four NFR `target` fields now lead with a number and unit, with prose moved to `measurement`/`test_method`:
- `extraction-latency.yaml:5` — "P95 < 500ms @ 10MB input; P95 < 5s @ 100MB input …"
- `max-file-size.yaml:5` — "104,857,600 bytes (100 MB)"
- `memory-ceiling.yaml:5` — "10x input file size (peak allocated bytes ≤ 10 * input byte length) …"
- `nesting-depth-guard.yaml:5` — "1,073,741,824 bytes (1 GB) per entry/stream; 2,147,483,648 bytes (2 GB) total per container"

Numeric coherence checks out: memory-ceiling's worst case (10 × 100 MB = 1 GB) sits inside nesting-depth-guard's 2 GB container cap, and `extraction-latency.yaml:5` correctly cites 100MB as "the configured max, per ydk:nfr:extraction/max-file-size".

Corpus arithmetic checks out: 3 synthetic + 2 curated = 5 per format × 6 formats = 30, matching `dataset-curation.md:15`'s "at least 30 per-format samples (6 formats x 5 synthetic+curated each) plus the adversarial set" (6 more → 36 total).

Adversarial-sample types listed in the success criteria (`overview.md:13`: corrupt, truncated, oversized, password-protected, zero-byte, wrong-extension) match `dataset-curation.md:13`'s six exactly.

### 3. Scope — **PASS**

Nothing in the spec exceeds the user's stated scope in `docs/project-rules.md:7`. The six formats match; content-only MIME detection matches; the four output categories (general metadata, format-specific metadata, flat text, first-layer subfiles) match; corpus + time profiling + stress harness match; the OSS-only constraint is honored and enforced through both ADRs (NPOI self-compiled to avoid the paid EULA, NBomber rejected for its Commercial Subscription requirement). The i18n requirement in `project-rules.md:22` is fully carried into `unicode-fidelity.yaml` including the surrogate-pair and normalization clauses.

No unrequested capability appears anywhere. The `DEFERRED` list correctly parks rtf/odt/pdf rather than smuggling them in.

### 4. Ambiguity — **PASS** (all 4 prior findings fixed; 4 minor residuals, none critical)

See N05 above. Fixed: `extraction.md:9` (now an exhaustive nine-field list), `extraction.md:27` (now a pointer to `subfile-scope`'s closed list), `testing-strategy.md:8` (now "at least 50 concurrent calls"), `dataset-curation.md:12` (now two structurally testable curated-sample requirements). Residual soft language — `testing-strategy.md:8` ("a longer CI job"), `overview.md:7` ("large enough to trust"), `mime-detection.md:19` ("implausibly large"), `dataset-curation.md:13` ("where possible") — is confined to explanatory prose; no requirement, NFR target, success criterion, or acceptance rule contains a vague term.

### 5. YAGNI — **PASS**

See N10 above. Two mild speculative-generality instances (`MimeDetectionRequest.yaml:4-6` naming hypothetical future fields; `oversized-nested-content.yaml:21` guarding a future recursion mode), both self-limiting and neither driving structure that would not otherwise exist. The registry/extensibility pattern is a stated user requirement, not gold-plating.

### 6. Component reference check — **PASS**

26/26 manifests referenced from narratives; 0 broken references; 0 orphans; all newly-added cross-references resolve. One style inconsistency (`overview.md:14` backticks three ydk ids instead of bracketing them). Full detail under N09.

---

## Required Fixes to Reach PASS

**None.** F1–F6 and F9–F10 and F12 from earlier rounds are all applied and verified. Every criterion clears its threshold.

### Recommended — margin restoration

Three criteria (N04, N05, N08) now pass at **exactly** their thresholds with zero margin, so any future prose addition can reopen them without warning. These edits are cheap insurance, not corrections.

**F13** *(N04, N04-6)* — pick one shorthand for `ydk:nfr:extraction/nesting-depth-guard` and use it everywhere. "Bomb guard" is the most descriptive candidate. At minimum fix `extraction.md:40` — "declared internal size exceeds the **nesting guard**" — which is the only one of the four that names no `ydk:` id, so the shorthand is a reader's sole handle there.
**F14** *(N04, N04-7)* — `FileMetadata.yaml:25`: "Which **container family** the file belongs to." → "Which **format family** the file belongs to." One word; this is the description of the very field `glossary.md:12` cites as canonical.
**F15** *(N04, N04-8)* — `testing-strategy.md:15`: "all tagged in the **dataset-curation manifest**" → "**corpus manifest**", matching `:6` in the same file plus `dataset-curation.md:17,19` and `unicode-fidelity.yaml:26`.
**F11** *(N05)* — replace "run manually or as a **longer** CI job" (`testing-strategy.md:8`) with a stated cadence (e.g. "run manually or nightly in CI").
**F7** *(N08)* — remove the `.FormatFamily` suffix at `glossary.md:12`.
**F8** *(N09, style)* — bracket the three ydk references at `overview.md:14` instead of backticking them, so `\[ydk:[^\]]+\]` tooling sees them.
**F16** *(C5b, trivial)* — `testing-strategy.md:14`: qualify "Every adversarial-review scenario" as "every adversarial-review scenario with defined v1 behavior", so the out-of-scope locale bullet at `overview.md:71` doesn't fall outside the rule as literally written.

---

## Summary

**The spec passes.** 10 of 10 criteria clear their thresholds. The third fix round applied all three required N04 edits and closed C5a as well, and in one place improved on what was asked: `extract.yaml:12-13` names *both* the open component and the text-extraction component rather than just the open component, which is the more accurate description of what `Extract` actually dispatches to.

This pass swept the full graded set rather than only the edited files — all 12 glossary terms and all 15 of their `DO NOT USE` synonyms, across `docs/specs/`, `.ydk/components/`, `docs/adrs/`, and `docs/project-rules.md`. Banned terms now return **zero** hits outside the glossary's own DO NOT USE lines, and the format-family / format-kind senses no longer cross anywhere. That closes the class of miss that the two prior passes each hit.

Three small naming drifts remain, all shorthand rather than competing role names: four informal names for the nesting-depth-guard NFR (N04-6), "container family" at `FileMetadata.yaml:25` (N04-7), and "dataset-curation manifest" vs. "corpus manifest" (N04-8, newly introduced by the C5 rewrite). They hold N04 at 8 rather than higher but do not block it.

**The one thing worth acting on before the spec is treated as stable:** N04, N05, and N08 all now pass at *exactly* their thresholds with no margin. Any of the three can be reopened by a single added sentence. F13–F15, F11, and F7 are one-line edits that would put all three back above the bar rather than on it.
