# Extraction

## Purpose

[ydk:contract:extraction/extract] turns a file already classified by mime detection into a full result: general metadata, format-specific metadata, flat text, and first-layer subfiles. It trusts the caller-supplied detected format rather than re-detecting — detection and extraction are separate, sequential steps by design.

## General Metadata

[ydk:entity:extraction/FileMetadata] carries the fields common across all six formats — title, author, last-modified-by, subject, keywords, comments, created/modified timestamps, application name, revision number, cumulative editing duration, and last-printed timestamp — sourced from each format's native document-properties store. A field the source format doesn't provide is null, never a placeholder string.

Implementation naming: the C# `FileMetadata` DTO uses `Creator` (entity `Author`), `Created` (entity `CreatedAt`) and `Modified` (entity `ModifiedAt`); the other fields keep the entity names, and `EditingDurationMinutes` is whole minutes for both OLE EditTime and OOXML TotalTime. The sample manifest uses the entity name `Author` and maps it to `Creator`. A [ydk:entity:extraction/FieldError] field path is spelled with the result's public property names rather than the entity names; the mapping is recorded in the manifests.

## Format-Specific Metadata

Exactly one metadata shape populates the result, chosen by format kind (Word, Excel, or PowerPoint — distinct from format family, which is the container technology; see `glossary.md`):

- [ydk:entity:extraction/WordFormatMetadata] — doc/docx (page/word/character/paragraph counts, company/manager/template fields, macro-presence flag)
- [ydk:entity:extraction/ExcelFormatMetadata] — xls/xlsx (sheet count/names, active sheet, macro-presence flag)
- [ydk:entity:extraction/PowerPointFormatMetadata] — ppt/pptx (slide count, notes-slide count, macro-presence flag)

This is a discriminated union at the API level, not a bag of nullable fields for every format on one type. Every format kind carries its own macro-presence flag (see `mime-detection.md` for why detection itself doesn't split macro variants into separate detected formats).

## Flat Text

The flattened plain-text body is empty, never null, when there's nothing to extract — and also when flattening failed, which is why the empty string alone is not an answer: the two cases are told apart by whether the result carries a [ydk:entity:extraction/FieldError] for the text (see Per-Field Error Isolation), not by inspecting the string. Must satisfy [ydk:req:extraction/unicode-fidelity]: Hebrew, Arabic, Persian, Russian, Latin, and CJK text, plus emoji (including multi-codepoint sequences), must round-trip without mojibake or truncation. This is explicitly harder on the legacy path (per the library research spike, exact text-decoding behavior for mixed-encoding legacy fields is unverified) than the OOXML path (already Unicode-native internally) — empirical testing against the multilingual corpus is required before this requirement is considered met for legacy formats.

Docx body text (and the legacy .doc path, which is converted to docx first) takes each top-level paragraph once: a paragraph nested in a text box is part of its enclosing paragraph and is not emitted a second time. Product decision (2026-11-08): text inside tracked deletions (`w:delText`) is excluded and tracked insertions are kept, so the text matches the document with all edits accepted. b2xtranslator writes legacy deleted runs as `w:del`/`w:delText`, so .doc follows the same rule.

## Subfiles

The subfiles list holds one entry per first-layer OLE-embedded object and inline media item, per [ydk:req:extraction/subfile-scope] — binary, filename, and size for each. Internal package structure that isn't an embedding or media item (the exhaustive exclusion list is in [ydk:req:extraction/subfile-scope]) is never listed as a subfile. A subfile that is itself a container (e.g. an embedded spreadsheet inside a document) is returned as one opaque item — its own contents are not recursively unpacked in v1.

OOXML (docx/xlsx/pptx and macro variants) subfile `FileName` is `<prefix>_<N><ext>`, where `<N>` is the 1-based position in the returned list and `<ext>` the item's file extension: the prefix is `slide_image` for an image part hosted one hop below the main part (a slide or worksheet, in pptx and xlsx) and `embedded_object` for every other item (OLE/package embeddings at any level, and image parts related directly to the main part, as in docx) — the prefix follows where the part sits in the package, not the document kind, so an xlsx image is `slide_image_N`; `samples/manifest.json` records these exact names.

**Counting rule.** A subfile is a distinct package part (OOXML) or top-level/pool storage or picture record (legacy), not a relationship, so the subfile count is not always the literal number of embeddings a sample was authored with. Any embedding or media part reachable from the main part counts exactly once, wherever it is hosted (slide, worksheet, layout, master, or OLE-preview/vmlDrawing image; a part shared by several hosts is listed once, per PR #125), but the enumeration never descends into a subfile's own contents. Consequences recorded in `samples/manifest.json`: (a) the OLE-preview icon picture that a legacy xls object needs is itself a picture and counts, so `multi-embedding/sample.xls` returns 5 subfiles for 4 authored embeddings; (b) the placeholder VBA project of OOXML macro variants (docm/xlsm/pptm) and of legacy doc/xls is a macro flag, not a subfile, whereas legacy ppt reports every top-level storage as an embedded object, so `macro-embed/sample.ppt` returns 2 (Embedding1 plus the Macros storage). (b) is a known legacy-ppt asymmetry, accepted and recorded rather than special-cased; the manifest `expectedSubfileCount` for every sample follows this rule rather than the authored embedding count.

If an individual subfile cannot be read (its own bytes are corrupt while the surrounding document is fine), it is omitted from the subfiles list and reported as a [ydk:entity:extraction/FieldError] keyed by its own package path, not merely logged; this does not fail the overall extraction call. The counting rule above describes the document; the returned list describes what was readable, and the field errors account for the gap.

## Error Handling

Extraction fails the whole call for the fixed set below and for nothing else. The set is enumerated rather than characterised: argument validation runs before a byte is read, unsupported-format is decided from the supplied detected-format value alone, and the remainder are failures of the container open itself.

- [ydk:error:extraction/file-too-large] — rejected before parsing
- [ydk:error:extraction/password-protected] — encrypted content detected before a full parse is attempted
- [ydk:error:extraction/unsupported-format] — the supplied detected-format value isn't one of the six supported formats
- [ydk:error:extraction/truncated-container] — declared container size exceeds actual byte length, or the input is zero-length
- [ydk:error:extraction/corrupt-file] — in its container-level role only: the container's own structure is unparseable, so no document is ever opened
- [ydk:error:extraction/oversized-nested-content] — in its open-time role only: a [ydk:nfr:extraction/nesting-depth-guard] cap is breached by the container's own declared structure, while the container is still being opened

A null request object, or a null value on a required request field, is rejected immediately via standard argument validation before any of the above logic runs — this is boundary validation, not a domain error, and has no dedicated error manifest. An exception no component declared, raised while the container is still being opened, is fatal too and propagates unwrapped; no [ydk:entity:extraction/FieldError] is possible for it because there is no result object yet.

Every failure after the container opens is isolated to the leaf it affects and reported through the per-field channel below instead of being thrown. Two of the errors above therefore carry two roles each, split by one question — had the container finished opening? For [ydk:error:extraction/corrupt-file], unparseable container structure is fatal and an unparseable source record behind one leaf is not. For [ydk:error:extraction/oversized-nested-content], a guard breach in the container's own declared structure during the open is fatal, while a breach on one subfile after a successful open costs the caller that subfile only. The [ydk:nfr:extraction/nesting-depth-guard] caps are unchanged in both roles. The error stays wholly fatal on [ydk:contract:extraction/detect-mime-type], which has no per-field channel.

## Per-Field Error Isolation

[ydk:req:extraction/per-field-error-isolation] makes the leaf property the unit of failure — not the result, and not an output group. Each of the twelve source-derived properties on [ydk:entity:extraction/FileMetadata], each leaf property of whichever format-metadata shape applies ([ydk:entity:extraction/WordFormatMetadata], [ydk:entity:extraction/ExcelFormatMetadata], [ydk:entity:extraction/PowerPointFormatMetadata]), the flat text, and each individual subfile ([ydk:entity:extraction/SubfileItem]) succeed or fail independently of every other one. A document whose creation timestamp is stored in a mangled property-store record still yields its title, its author, its word count, its body text, and its subfiles; the caller loses one timestamp.

A failed leaf is left at its no-value state and is never given a placeholder, a sentinel, or a guess: a nullable property is null, the flat text is the empty string, a failed subfile is absent from the list. The non-nullable counts and flags — the Excel sheet count and sheet names, the PowerPoint slide count, every format kind's macro-presence flag — carry their type's default when they fail (zero, false, or an empty list), and that default is not a claim about the document. The [ydk:entity:extraction/FieldError] entry for that leaf is the only discriminator, in both directions: no entry means the value is the document's; an entry means the value is unknown. Such a leaf is not reconciled against its siblings either — a sheet count of zero beside a populated list of sheet names is the expected shape of one failed leaf next to a succeeding one, and neither value is recomputed or cleared to match.

Three required properties on [ydk:entity:extraction/FileMetadata] — file size, exact mime type and format family — sit outside the isolated set. They derive from the request and the caller-supplied detection verdict, not from a parsed source record, so they cannot fail once the container has opened and never appear in a field path.

[ydk:entity:extraction/FieldError] is additive: a new collection on [ydk:entity:extraction/ExtractionResult] carrying a field path, an error code from a closed three-value vocabulary, and a human-readable message. Nothing existing changes type, so a caller that ignores the collection sees exactly the result it saw before. The originating exception is not on the public surface; its type and stack go to the log/telemetry sink, so the result never pins callers to a backing library's exception taxonomy.

The code is chosen by cause, not by leaf. An unreadable or malformed source record behind the leaf takes the corrupt-source code; a nesting-guard breach on a subfile takes the oversized-content code; anything else takes the residual unexpected-failure code, which is never used where either of the other two fits. The first two are the codes of [ydk:error:extraction/corrupt-file] and [ydk:error:extraction/oversized-nested-content] in their field-level roles; the residual code has no error manifest and is never thrown. The enum is recorded on [ydk:entity:extraction/FieldError].

A field path is dotted from the result root and uses the result's public property names. A failed subfile is keyed in brackets by its package path — for legacy containers, its storage path inside the container — never by a number, because list positions shift when items are omitted. When a failing subfile's package path is unknown or empty, the bare collection name is used. The same bare name covers a fault in the enumeration of the container's subfiles: one entry for the whole fault, with the subfiles never reached absent from the list and not individually reported. That is the one accepted blind spot — a caller learns the list is incomplete, but not by how much. The grammar, examples and name mapping are on [ydk:entity:extraction/FieldError].

The collection is empty, never null, when every leaf was populated. Entries come in population order: general metadata, then format metadata, then the flat text, then subfiles in the order the container enumerates them. There is at most one entry per distinct field path; only the bare collection name may repeat, once per failing subfile with no recoverable package path plus at most once more for an enumeration fault.

Two previously accidental behaviors are now specified: failed flat-text extraction returns the empty string and a field-error entry, and an unreadable subfile is omitted and recorded rather than reaching only a log reader.

Batch extraction (ADR-007) is unaffected in shape: a file that produced only field errors is a batch success carrying a populated result with a non-empty field-error collection, never a batch item error. The batch item error channel means "this file could not be extracted at all" and keeps exactly the fatal set above as its causes. A caller scanning a batch therefore looks in two places: item errors for failed files, field errors for partially successful ones.

## Extensibility & DI

Same registry-based dispatch principle as detection ([ydk:req:extraction/format-extensibility]); every component is resolved through dependency injection, continuing the existing scaffold's convention ([ydk:req:extraction/dependency-injection]), not constructed inline.
