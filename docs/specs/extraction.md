# Extraction

## Purpose

[ydk:contract:extraction/extract] turns a file already classified by mime detection into a full result: general metadata, format-specific metadata, flat text, and first-layer subfiles. It trusts the caller-supplied detected format rather than re-detecting — detection and extraction are separate, sequential steps by design.

## General Metadata

[ydk:entity:extraction/FileMetadata] carries the fields common across all six formats — title, author, last-modified-by, subject, keywords, comments, created/modified timestamps, application name, revision number, cumulative editing duration, and last-printed timestamp — sourced from each format's native document-properties store. A field the source format doesn't provide is null, never a placeholder string.

Implementation naming: the C# `FileMetadata` DTO uses `Creator` (entity `Author`), `Created` (entity `CreatedAt`) and `Modified` (entity `ModifiedAt`); the other fields keep the entity names, and `EditingDurationMinutes` is whole minutes for both OLE EditTime and OOXML TotalTime. The sample manifest uses the entity name `Author` and maps it to `Creator`.

## Format-Specific Metadata

Exactly one metadata shape populates the result, chosen by format kind (Word, Excel, or PowerPoint — distinct from format family, which is the container technology; see `glossary.md`):

- [ydk:entity:extraction/WordFormatMetadata] — doc/docx (page/word/character/paragraph counts, company/manager/template fields, macro-presence flag)
- [ydk:entity:extraction/ExcelFormatMetadata] — xls/xlsx (sheet count/names, active sheet, macro-presence flag)
- [ydk:entity:extraction/PowerPointFormatMetadata] — ppt/pptx (slide count, notes-slide count, macro-presence flag)

This is a discriminated union at the API level, not a bag of nullable fields for every format on one type. Every format kind carries its own macro-presence flag (see `mime-detection.md` for why detection itself doesn't split macro variants into separate detected formats).

## Flat Text

The flattened plain-text body is empty, never null, when there's nothing to extract — and also when flattening failed, which is why the empty string alone is not an answer: the two cases are told apart by whether the result carries a [ydk:entity:extraction/FieldError] for the text (see Per-Field Error Isolation), not by inspecting the string. Must satisfy [ydk:req:extraction/unicode-fidelity]: Hebrew, Arabic, Persian, Russian, Latin, and CJK text, plus emoji (including multi-codepoint sequences), must round-trip without mojibake or truncation. This is explicitly harder on the legacy path (per the library research spike, exact text-decoding behavior for mixed-encoding legacy fields is unverified) than the OOXML path (already Unicode-native internally) — empirical testing against the multilingual corpus is required before this requirement is considered met for legacy formats.

## Subfiles

The subfiles list holds one entry per first-layer OLE-embedded object and inline media item, per [ydk:req:extraction/subfile-scope] — binary, filename, and size for each. Internal package structure that isn't an embedding or media item (the exhaustive exclusion list is in [ydk:req:extraction/subfile-scope]) is never listed as a subfile. A subfile that is itself a container (e.g. an embedded spreadsheet inside a document) is returned as one opaque item — its own contents are not recursively unpacked in v1.

OOXML (docx/xlsx/pptx and macro variants) subfile `FileName` is `<prefix>_<N><ext>`, where `<N>` is the 1-based position in the returned list and `<ext>` the item's file extension: the prefix is `slide_image` for an image part hosted one hop below the main part (a slide or worksheet, in pptx and xlsx) and `embedded_object` for every other item (OLE/package embeddings at any level, and image parts related directly to the main part, as in docx) — the prefix follows where the part sits in the package, not the document kind, so an xlsx image is `slide_image_N`; `samples/manifest.json` records these exact names.

**Counting rule.** A subfile is a distinct package part (OOXML) or top-level/pool storage or picture record (legacy), not a relationship, so the subfile count is not always the literal number of embeddings a sample was authored with. Any embedding or media part reachable from the main part counts exactly once, wherever it is hosted (slide, worksheet, layout, master, or OLE-preview/vmlDrawing image; a part shared by several hosts is listed once, per PR #125), but the walk never descends into a subfile's own contents. Consequences recorded in `samples/manifest.json`: (a) the OLE-preview icon picture that a legacy xls object needs is itself a picture and counts, so `multi-embedding/sample.xls` returns 5 subfiles for 4 authored embeddings; (b) the placeholder VBA project of OOXML macro variants (docm/xlsm/pptm) and of legacy doc/xls is a macro flag, not a subfile, whereas legacy ppt reports every top-level storage as an embedded object, so `macro-embed/sample.ppt` returns 2 (Embedding1 plus the Macros storage). (b) is a known legacy-ppt asymmetry, accepted and recorded rather than special-cased; the manifest `expectedSubfileCount` for every sample follows this rule rather than the authored embedding count.

If an individual embedded item cannot be read (its own bytes are corrupt while the surrounding document is fine), it is omitted from the subfiles list and the omission is reported as a [ydk:entity:extraction/FieldError] against that item's own path — not merely logged — and this does not fail the overall extraction call. A single bad embedding shouldn't deny access to an otherwise-good document, but a caller still has to be able to tell that the list it received is shorter than the document's, which a log line it never sees cannot do. The counting rule above describes the document; the returned list describes what was readable, and the gap between the two is exactly what the field-error entries account for.

## Error Handling

Extraction fails the whole call only when the container cannot be opened, because then nothing is extractable and a partial result would say nothing except that the file was unreadable — which is what the thrown error already says, more precisely. That set is fixed:

- [ydk:error:extraction/file-too-large] — rejected before parsing
- [ydk:error:extraction/password-protected] — encrypted content detected before a full parse is attempted
- [ydk:error:extraction/unsupported-format] — the supplied detected-format value isn't one of the six supported formats
- [ydk:error:extraction/truncated-container] — declared container size exceeds actual byte length, or the input is zero-length
- [ydk:error:extraction/corrupt-file] — in its container-level role only: the container's own structure is unparseable, so no document is ever opened

A null request object, or a null value on a required request field, is rejected immediately via standard argument validation before any of the above logic runs — this is boundary validation, not a domain error, and has no dedicated error manifest.

Every failure after the container opens is isolated to the leaf it affects and reported through the per-field channel below instead of being thrown. [ydk:error:extraction/corrupt-file] therefore carries both roles, and the dividing line is whether the container opened — not the exception's type, not the backing library that raised it, not how deep in the document the bad record sits: unparseable container structure is fatal, an unparseable source record behind one leaf is not. [ydk:error:extraction/oversized-nested-content] stops being fatal for extraction entirely: the [ydk:nfr:extraction/nesting-depth-guard] caps are still hard ceilings enforced before any buffer is allocated, but the breach now costs the caller the one item that breached them rather than the document. It remains fatal on [ydk:contract:extraction/detect-mime-type], which produces a single verdict and has no per-field channel to report into.

## Per-Field Error Isolation

[ydk:req:extraction/per-field-error-isolation] makes the leaf property the unit of failure — not the result, and not an output group. Each of the twelve source-derived properties on [ydk:entity:extraction/FileMetadata], each leaf property of whichever format-metadata shape applies ([ydk:entity:extraction/WordFormatMetadata], [ydk:entity:extraction/ExcelFormatMetadata], [ydk:entity:extraction/PowerPointFormatMetadata]), the flat text, and each individual subfile ([ydk:entity:extraction/SubfileItem]) succeed or fail independently of every other one. A document whose creation timestamp is stored in a mangled property-store record still yields its title, its author, its word count, its body text, and its embeddings; the caller loses one timestamp.

A failed leaf is left at its no-value state and is never given a placeholder, a sentinel, or a guess: a nullable property is null, the flat text is the empty string, a failed subfile is absent from the list. The non-nullable counts and flags — the Excel sheet count and sheet names, the PowerPoint slide count, every format kind's macro-presence flag — carry their type's default when they fail, meaning zero, false, or an empty list, and that default is explicitly *not* a claim about the document. A caller cannot distinguish "this workbook genuinely has no macros" from "we could not determine whether it has macros" by reading the flag, and must not try: the [ydk:entity:extraction/FieldError] entry for that leaf's path is the only discriminator, in both directions. No entry means the value is the document's; an entry means the value is a placeholder the caller should treat as unknown. This is deliberate — widening those leaves to nullable would change the result shape every existing caller already compiles against, for a signal the field-error collection carries anyway.

[ydk:entity:extraction/FieldError] is additive: a new collection on [ydk:entity:extraction/ExtractionResult] carrying a field path, an error code from a closed vocabulary shared with the thrown errors, and a human-readable message. Nothing existing changes type, so a caller that ignores the collection sees exactly the result it saw before, with the pre-change behavior it already handles. The originating exception is deliberately not on the public surface — code and message only, with the exception type and stack going to the log/telemetry sink, so the public result never pins callers to a backing library's exception taxonomy.

The field path is a dotted path rooted at the result and spelled with the result's own public property names, with a 0-based bracket index for an element of a list: `Metadata.Created` for a general-metadata property, `FormatMetadata.PageCount` for a format-metadata property, `ExtractedText` for the flat text, `EmbeddedFiles[2]` for the third embedded item. The bracket index is the item's position in the source container's embedded-item walk order, which is *not* its position in the returned list — a failed item is absent from that list, so returned positions shift — and *not* the 1-based `<N>` baked into the item's generated `FileName` by the naming rule above. Three separate numberings, deliberately: the path has to stay stable for an item that isn't in the list, so it cannot be derived from the list. Callers must not use a bracket index to look up into the returned subfiles. An item that fails before its walk position is determined, because the container's own embedded-item enumeration faults mid-walk, is reported against the bare collection path `EmbeddedFiles` with no index.

The collection is empty, never null, when every leaf was populated — the signal for a clean extraction is an empty collection, not a null one, so a caller never has to null-check before iterating. Entries come in population order: general metadata, then format metadata, then the flat text, then embedded items in walk order. There is at most one entry per distinct field path, because each leaf is populated exactly once, with the single exception of the index-less `EmbeddedFiles` path, which repeats once per item whose position could not be determined.

Two behaviors that were previously accidental become specified by this rule, and "silently" is exactly what the change removes. Flat-text extraction that fails must now return the empty string *and* a field-error entry, where it previously returned the empty string alone and was indistinguishable from a genuinely empty document. An unreadable embedded item must now be omitted *and* recorded, where the omission previously reached nobody but a log reader.

Batch extraction (ADR-007) is unaffected in shape and explicit in meaning: a file that produced only field errors is a batch success, carrying a populated result whose field-error collection is non-empty, never a batch item error. The batch wrapper's per-item error channel means "this file could not be extracted at all" and keeps exactly the fatal set above as its causes. A caller scanning a batch for problems therefore has to look in two places — item errors for files that failed, field errors for files that partially succeeded — and that is the point: the two are different outcomes and collapsing them loses the partial result.

## Extensibility & DI

Same registry-based dispatch principle as detection ([ydk:req:extraction/format-extensibility]); every component is resolved through dependency injection, continuing the existing scaffold's convention ([ydk:req:extraction/dependency-injection]), not constructed inline.
