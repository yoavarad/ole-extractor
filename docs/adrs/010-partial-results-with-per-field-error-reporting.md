# ADR-010: Partial extraction results with per-field error reporting, instead of fail-fast on the whole file

## Status
Accepted. 2026-11-08. Stage 01 (Major Feature) spec work on branch `spec/per-field-error-isolation`;
no implementation task cut yet.

Amended 2026-11-09 (#288): the flat text of a workbook or presentation keeps its readable
sheets' or slides' text when only some fail; see "Amendment" at the end.

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

The fatal set is exactly: `file-too-large`, `password-protected`, `unsupported-format`,
`truncated-container`, `corrupt-file` in its container-level role, `oversized-nested-content` in
its open-time role only, null-request / null-required-field argument validation, and any undeclared
exception raised while the container is still being opened. Nothing is extractable from a container
that will not open, so a partial result there would carry no information the thrown error does not
already carry more precisely — and there is no result object to attach a field error to in the
first place. `corrupt-file` and `oversized-nested-content` each keep two roles, split on the same
question: had the container finished opening? After a successful open, a `corrupt-file` source
record behind one leaf, an `oversized-nested-content` breach on one leaf, and any undeclared
exception raised while populating one leaf all become per-field.

A failed leaf is left at its no-value state — null, empty string, absent from the list, or the
type's default for a non-nullable count or flag — and never at a placeholder or sentinel. The
result carries an additive `FieldErrors` collection of field path + error code + human-readable
message, empty and never null on a clean extraction. That collection is the sole discriminator
between a value the document does not record and a value we could not read; for the non-nullable
counts and flags it is the *only* discriminator, since their defaults are indistinguishable from
genuine zeroes and falses. The originating exception type stays off the public surface and goes to
telemetry.

Field paths are dotted from the result root using the result's own public C# property names rather
than this spec's entity names — `Metadata.Created`, not `GeneralMetadata.CreatedAt` — because a
path has to match character-for-character the property the caller just read as null, or every
caller translates before it can act. A failed embedded item is keyed by its package path, as
`EmbeddedFiles[word/embeddings/oleObject1.bin]`, not by a position: the item is absent from the
returned list, so no position in that list identifies it, and the package path is the one
identifier both sides can see. The convention is specified in `docs/project-rules.md` and
`docs/specs/extraction.md`; the rule is testable as
`ydk:req:extraction/per-field-error-isolation`.

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
| A declared-oversize embedded item failed the whole extraction | That one item is skipped with an `OVERSIZED_NESTED_CONTENT` field error; the rest of the document extracts | Keep catching this error around `Extract()` only for the open-time case (the container's own declared structure breaches a cap, which is still fatal); a breach on a single item after a successful open no longer throws. It is still always thrown by mime detection. |
| Text extraction returned an empty string on failure, silently | Empty string plus a `FieldErrors` entry at `ExtractedText` | A caller treating empty text as "no text" must now check `FieldErrors` to keep that reading correct. |
| An unreadable embedded item was omitted and written to the console | Omitted and recorded as a field error keyed by its package path | Callers comparing subfile counts against expectations can now account for the gap, and can match the gap to a place in the container. |

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
- `FieldErrors` is a new public vocabulary of exactly three codes, closed, so adding a per-field
  cause is a spec change rather than an ad-hoc string. Two of the three — `CORRUPT_FILE` and
  `OVERSIZED_NESTED_CONTENT` — are also the codes of thrown errors, in their field-level roles.
  The third, `UNEXPECTED_FIELD_FAILURE`, has no error manifest and is never thrown out of the
  call, so the vocabulary overlaps the thrown set without being drawn from it.
- One item can fail without the caller learning how many others were never looked at. If the
  enumeration of the container's items faults part-way, the fault gets one bare `EmbeddedFiles`
  entry and the items past it are simply absent. That blind spot is accepted: the alternative is
  reporting items whose existence is not known, which would be a fabrication.

## Amendment (2026-11-09, #288)
The flat text of a workbook (xls, xlsx) or a presentation (ppt, pptx) is read sheet by sheet or slide by
slide, and is the one exception to "a failed leaf is left at its no-value state". When some sheets or
slides fail, the flat text keeps the text of the readable ones and carries one `ExtractedText` field
error; for that leaf an entry means "incomplete", not "no value". Discarding every healthy sheet's
text because one sheet was damaged would contradict this ADR's own premise (damage is local, so
isolation should be). A failure not confined to one sheet or slide still leaves the empty string. The
migration row for text extraction reads accordingly: empty string (or the readable sheets' or slides'
text) plus a `FieldErrors` entry at `ExtractedText`.
