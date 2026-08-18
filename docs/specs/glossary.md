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
