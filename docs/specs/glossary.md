# Glossary

**OLE / CFB** — Compound File Binary format (also called OLE2), the binary container format used by legacy doc/xls/ppt. One canonical term pair; use "CFB" when referring to the file structure specifically (directory, storages, streams) and "OLE" when referring to the format family conversationally.
DO NOT USE: "OLE2" alone, "compound document" (ambiguous with OOXML).

**OOXML** — Office Open XML, the ZIP-based container format used by docx/xlsx/pptx (and their macro-enabled -m siblings).
DO NOT USE: "Open XML" without "Office" prefix (ambiguous), "docx format" as a stand-in for the whole family.

**Subfile** — a first-layer OLE-embedded object or inline media item extracted from a document, per [ydk:req:extraction/subfile-scope]. Represented by [ydk:entity:extraction/SubfileItem].
DO NOT USE: "attachment" (implies email), "embedded file" alone (ambiguous with the abstract concept vs. the concrete extracted item).

**Format family** — one of `LegacyOle` or `Ooxml`, per [ydk:entity:extraction/FileMetadata].FormatFamily — which container technology a detected format uses.
DO NOT USE: "file type" (too vague — could mean format family, format kind, or exact format).

**Format kind** — one of Word, Excel, or PowerPoint — which of the three format-specific metadata shapes ([ydk:entity:extraction/WordFormatMetadata], [ydk:entity:extraction/ExcelFormatMetadata], [ydk:entity:extraction/PowerPointFormatMetadata]) applies to a document. Orthogonal to format family: format family is container technology (legacy vs. OOXML), format kind is application (Word vs. Excel vs. PowerPoint) — a docx and a doc share a format kind (Word) but differ in format family.
DO NOT USE: "format family" for this meaning — the two terms are not interchangeable.

**Exact mime type** — the specific IANA mime type string for one of the six supported formats (e.g. `application/msword`, not a generic `application/octet-stream`), or the `Unknown` sentinel. Output of [ydk:contract:extraction/detect-mime-type].
DO NOT USE: "file type" for this either — "exact mime type" and "detected format" are the two canonical terms, used interchangeably only for the same underlying value.

**Content-only detection** — the rule that mime detection reads only file bytes, never the filename. See [ydk:req:extraction/content-only-mime-detection].

**First-layer** — describes subfile extraction depth: subfiles directly embedded in the original document are extracted; content nested inside a subfile's own binary is not (v1 scope).
DO NOT USE: "top-level" (ambiguous with document structure, not embedding depth), "shallow" (not a defined term).

**Detection component** — the component that performs a structural, non-parsing check to classify a file's format family and format. One per format family.
DO NOT USE: "sniffer", "mime detector" (as a distinct noun from "detection component" — same thing, one name).

**Open component** — the component that opens a file with the correct backing library and produces metadata + subfiles for one format.
DO NOT USE: "open strategy" (implementation-pattern name, not the conceptual role).

**Text-extraction component** — the component that flattens a given open document's body to plain text for one format.
DO NOT USE: "text handler", "text extractor" as a bare noun (use the full term).

**Corpus / sample** — a single test file in the curated dataset (`dataset-curation.md`). "Corpus" refers to the whole collection; "sample" to one file within it.
DO NOT USE: "fixture" (overloaded testing-framework term), "test file" alone (ambiguous with unit test source files).

**Leaf property** — a single value on the extraction result that succeeds or fails independently of every other value (see [ydk:req:extraction/per-field-error-isolation]). Includes each source-derived metadata property, the flat text, and each subfile.
DO NOT USE: "field" alone (ambiguous with request fields and component manifest fields), "attribute".

**Field path** — the dotted address of a leaf property from the extraction result root; a failed subfile is addressed by its package path in brackets (for legacy containers, the storage path inside the container). Grammar and examples are on [ydk:entity:extraction/FieldError].
DO NOT USE: "field name", "property path".

**Field error** — one record of a leaf property that could not be populated while the call as a whole succeeded: a field path, a code and a message ([ydk:entity:extraction/FieldError]).
DO NOT USE: "partial error", "warning".

**Open password** — the password a caller supplies to decrypt an encrypted document, carried optionally on [ydk:entity:extraction/MimeDetectionRequest] and [ydk:entity:extraction/ExtractionRequest] ([ydk:req:extraction/password-decryption]). It is the only kind of password this library uses.
DO NOT USE: "password" for modify/write-protection or sheet-protection passwords (those are not open passwords and are out of scope); "key" (the key is derived from the password, it is not the password).

**Encrypted package** — an OOXML document stored encrypted inside a CFB wrapper, per the Office encryption standard. It is classified and extracted as its inner OOXML format once decrypted.
DO NOT USE: "password-protected docx" as a format name; "encrypted CFB" alone (encrypted doc/xls/ppt are also encrypted CFB files, but they are not encrypted packages).
