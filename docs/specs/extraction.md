# Extraction

## Purpose

[ydk:contract:extraction/extract] turns a file already classified by mime detection into a full result: general metadata, format-specific metadata, flat text, and first-layer subfiles. It trusts the caller-supplied detected format rather than re-detecting — detection and extraction are separate, sequential steps by design.

## General Metadata

[ydk:entity:extraction/FileMetadata] carries the fields common across all six formats — title, author, last-modified-by, subject, keywords, comments, created/modified timestamps, application name, revision number, cumulative editing duration, and last-printed timestamp — sourced from each format's native document-properties store. A field the source format doesn't provide is null, never a placeholder string.

## Format-Specific Metadata

Exactly one metadata shape populates the result, chosen by format kind (Word, Excel, or PowerPoint — distinct from format family, which is the container technology; see `glossary.md`):

- [ydk:entity:extraction/WordFormatMetadata] — doc/docx (page/word/character/paragraph counts, company/manager/template fields, macro-presence flag)
- [ydk:entity:extraction/ExcelFormatMetadata] — xls/xlsx (sheet count/names, active sheet, macro-presence flag)
- [ydk:entity:extraction/PowerPointFormatMetadata] — ppt/pptx (slide count, notes-slide count, macro-presence flag)

This is a discriminated union at the API level, not a bag of nullable fields for every format on one type. Every format kind carries its own macro-presence flag (see `mime-detection.md` for why detection itself doesn't split macro variants into separate detected formats).

## Flat Text

The flattened plain-text body is empty, never null, when there's nothing to extract. Must satisfy [ydk:req:extraction/unicode-fidelity]: Hebrew, Arabic, Persian, Russian, Latin, and CJK text, plus emoji (including multi-codepoint sequences), must round-trip without mojibake or truncation. This is explicitly harder on the legacy path (per the library research spike, exact text-decoding behavior for mixed-encoding legacy fields is unverified) than the OOXML path (already Unicode-native internally) — empirical testing against the multilingual corpus is required before this requirement is considered met for legacy formats.

## Subfiles

The subfiles list holds one entry per first-layer OLE-embedded object and inline media item, per [ydk:req:extraction/subfile-scope] — binary, filename, and size for each. Internal package structure that isn't an embedding or media item (the exhaustive exclusion list is in [ydk:req:extraction/subfile-scope]) is never listed as a subfile. A subfile that is itself a container (e.g. an embedded spreadsheet inside a document) is returned as one opaque item — its own contents are not recursively unpacked in v1.

OOXML (docx/xlsx/pptx and macro variants) subfile `FileName` is `<prefix>_<N><ext>`, where `<N>` is the 1-based position in the returned list and `<ext>` the item's file extension: the prefix is `slide_image` for an image part hosted one hop below the main part (a slide or worksheet, in pptx and xlsx) and `embedded_object` for every other item (OLE/package embeddings at any level, and image parts related directly to the main part, as in docx) — the prefix follows where the part sits in the package, not the document kind, so an xlsx image is `slide_image_N`; `samples/manifest.json` records these exact names.

If an individual embedded item cannot be read (its own bytes are corrupt while the surrounding document is fine), it is omitted from the subfiles list and the omission is logged — this does not fail the overall extraction call. A single bad embedding shouldn't deny access to an otherwise-good document.

## Error Handling

Extraction fails explicitly and specifically rather than returning a partial or silently-empty result:

- [ydk:error:extraction/file-too-large] — rejected before parsing
- [ydk:error:extraction/password-protected] — encrypted content detected before a full parse is attempted
- [ydk:error:extraction/unsupported-format] — the supplied detected-format value isn't one of the six supported formats
- [ydk:error:extraction/truncated-container] — declared container size exceeds actual byte length, or the input is zero-length
- [ydk:error:extraction/corrupt-file] — structure present but unparseable by the backing library
- [ydk:error:extraction/oversized-nested-content] — declared internal size exceeds [ydk:nfr:extraction/nesting-depth-guard]

A null request object, or a null value on a required request field, is rejected immediately via standard argument validation before any of the above logic runs — this is boundary validation, not a domain error, and has no dedicated error manifest.

## Extensibility & DI

Same registry-based dispatch principle as detection ([ydk:req:extraction/format-extensibility]); every component is resolved through dependency injection, continuing the existing scaffold's convention ([ydk:req:extraction/dependency-injection]), not constructed inline.
