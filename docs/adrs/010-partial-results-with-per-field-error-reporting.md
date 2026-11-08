# ADR-010: Partial extraction results with per-field error reporting, instead of fail-fast on the whole file

## Status
Accepted. 2026-11-08. Stage 01 (Major Feature) spec work on branch `spec/per-field-error-isolation`;
no implementation task cut yet.

## Context
Extraction was all-or-nothing. Each format's open path wrapped population in one broad catch, so a
single unparseable source record — one mangled property-store entry, one bad body-text record —
failed the entire call and the caller got nothing, not even the eleven metadata properties that
parsed fine. Real Office files from untrusted sources are routinely damaged in exactly this
localized way, which is the whole premise of `docs/specs/overview.md`'s problem statement.

Two places had already drifted away from fail-fast, accidentally rather than by design: text
extraction swallowed its own errors and returned an empty string, and an unreadable embedded item
was skipped with an unstructured console write. Neither reached the caller. Both were
indistinguishable from the legitimate case — a document with no text, a document with no
embeddings — so a caller could not tell "absent" from "unreadable" for any part of the result.
`docs/specs/extraction.md` documented the embedding case as intended behavior and the text case not
at all.

The open question was not whether to return partial results but what the unit of isolation is, and
how a caller learns which parts of the result it should not trust.

## Decision
The unit of isolation is the **leaf property**. Each of the twelve source-derived general-metadata
properties, each leaf property of the applicable format-metadata shape, the flat text, and each
individual embedded item succeed or fail independently of every other one.

Only container-open failures remain fatal: `file-too-large`, `password-protected`,
`unsupported-format`, `truncated-container`, `corrupt-file` in its container-level role, and
null-request / null-required-field argument validation. Nothing is extractable from a container
that will not open, so a partial result there would carry no information the thrown error does not
already carry more precisely. `corrupt-file` keeps both roles, split on whether the container
opened. `oversized-nested-content` and any undeclared exception raised while populating one leaf
become per-field.

A failed leaf is left at its no-value state — null, empty string, absent from the list, or the
type's default for a non-nullable count or flag — and never at a placeholder or sentinel. The
result carries an additive `FieldErrors` collection of field path + error code + human-readable
message, empty and never null on a clean extraction. That collection is the sole discriminator
between a value the document does not record and a value we could not read; for the non-nullable
counts and flags it is the *only* discriminator, since their defaults are indistinguishable from
genuine zeroes and falses. The originating exception type stays off the public surface and goes to
telemetry.

Field paths are dotted from the result root using the result's own public property names, with a
0-based bracket index for list elements, and the index is the source walk position rather than the
returned-list position so that it stays meaningful for an item that is absent from the list. The
convention is specified in `docs/project-rules.md` and `docs/specs/extraction.md`; the rule is
testable as `ydk:req:extraction/per-field-error-isolation`.

## Alternatives Considered
- **Four coarse output groups** (general metadata / format metadata / text / subfiles each
  succeed or fail as a unit). Rejected: one unreadable timestamp would still cost the caller all
  twelve general-metadata properties, and one bad embedding all of them. The grouping is an
  artifact of how the result is shaped, not of how files are damaged — damage is per record, so
  isolation has to be per leaf to actually buy anything.
- **A per-field `Result<T>` / `Field<T>` wrapper on every property.** Rejected: it changes the type
  of every field on the result, so every existing caller breaks and has to unwrap values it
  currently reads directly, and it reshapes the sample manifest that records expected extraction
  output for the whole corpus. The additive collection delivers the same information with no
  change to any existing field's type.
- **Log-only, with no public surface.** Rejected: this is what text extraction and subfile
  skipping already did, and it is precisely the failure mode being fixed — the caller cannot
  distinguish missing from failed, so it either trusts a wrong value or distrusts every value.
  A log line is not reachable by the code that has to make that decision.
- **Nothing fatal — always return a result.** Rejected: an unopenable container yields an all-null
  result plus a field error for every leaf, which is a verbose way of saying "the file is
  unreadable" and invites callers to process a result that has nothing in it. The fatal set is
  kept precisely where there is no useful partial answer.

## Breaking changes and migration
No type on the result changes and no field is removed, so callers compile unchanged. The behavioral
change is in which inputs throw.

| Before | After | Migration |
|---|---|---|
| Any parse failure after the container opened threw and returned no result | The call returns a result with the affected leaves at their no-value state and a `FieldErrors` entry for each | Callers relying on a throw to mean "this file is bad" must check whether `FieldErrors` is non-empty. Callers that only read values are unaffected, except that they now get the readable parts of a damaged file instead of an exception. |
| A declared-oversize embedded item failed the whole extraction | That one item is skipped with an `OVERSIZED_NESTED_CONTENT` field error; the rest of the document extracts | Stop catching this error around `Extract()` for extraction; it is still thrown by mime detection. |
| Text extraction returned an empty string on failure, silently | Empty string plus a `FieldErrors` entry at `ExtractedText` | A caller treating empty text as "no text" must now check `FieldErrors` to keep that reading correct. |
| An unreadable embedded item was omitted and written to the console | Omitted and recorded as a field error at its walk-order path | Callers comparing subfile counts against expectations can now account for the gap. |

Batch extraction is unchanged: a file with only field errors is a batch success carrying a
populated result, not a batch item error. The per-item error channel keeps exactly the fatal set
above as its causes.

## Consequences
- Every leaf's population needs its own failure boundary, rather than one catch per format. This is
  more code in the open and text-extraction components, and it is the cost of the decision.
- A non-nullable count or flag can now hold a default that means nothing. That ambiguity is real
  and is resolved only by the field-error collection, so every caller that branches on those
  values has to consult it. Widening them to nullable was rejected as a worse trade (it breaks the
  result shape for a signal already carried elsewhere), but the ambiguity has to be documented
  wherever those fields are described.
- Tests change shape: the adversarial corpus cases for post-open corruption and oversized nested
  content now assert a populated result plus a specific field-error entry, not a thrown exception.
  An empty text field with no field error, or a short subfile list with no field error, becomes a
  test failure — which is the regression guard against the silent behavior returning.
- `FieldErrors` is a new public vocabulary. Its error codes are closed and shared with the thrown
  errors, so adding a per-field cause is a spec change, not an ad-hoc string.
