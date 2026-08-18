# Overview

## Problem Statement

Systems that ingest arbitrary Office files from untrusted sources — content pipelines, document management systems, search indexing, security scanning — cannot rely on the filename to say what a file actually is, and cannot always adopt a paid SDK just to open it. Without exact, content-verified classification, a renamed or mislabeled file either gets silently misprocessed or has to be rejected outright, and without structured extraction, every downstream consumer re-implements its own fragile parsing.

We are building a C# library that classifies the exact Office format of a file from its bytes alone, then extracts general metadata, format-specific metadata, flat text, and first-layer embedded content into one structured result — covering doc/xls/ppt (legacy) and docx/xlsx/pptx (OOXML) as a minimum, using only open-source libraries, correct on exotic scripts and emoji, and backed by a test corpus large enough to trust the correctness claims.

## Success Criteria

- 100% of samples in the curated corpus (see `dataset-curation.md`) classify to their exact expected format — no false positives, no false negatives, filename never a factor.
- 100% of curated corpus samples produce extraction output matching the corpus manifest's exact expected text/metadata content — not merely non-empty.
- Every adversarial corpus sample (corrupt, truncated, oversized, password-protected, zero-byte, wrong-extension) fails with the specific error its scenario calls for — never an unhandled exception, never a hang, never a silent empty result.
- Latency and resource targets in `ydk:nfr:extraction/extraction-latency`, `ydk:nfr:extraction/memory-ceiling`, and `ydk:nfr:extraction/nesting-depth-guard` are met on the profiling corpus.
- A hypothetical 7th format can be added by implementing and registering one new detection/open/text-extraction component set, without changing the two public function signatures.

## Public API

Two entry points, both taking class inputs rather than bare parameters (see [ydk:entity:extraction/MimeDetectionRequest] and [ydk:entity:extraction/ExtractionRequest] — fields can be added later without breaking the signature):

- [ydk:contract:extraction/detect-mime-type] — bytes + filename in, exact format out. Content-only per [ydk:req:extraction/content-only-mime-detection].
- [ydk:contract:extraction/extract] — bytes + filename + previously-detected format in, full result out per [ydk:entity:extraction/ExtractionResult].

## Component Architecture

Continues the existing scaffold's dependency-injection convention ([ydk:req:extraction/dependency-injection]):

- **Detection component** — one per format family (structural signature for OOXML, directory-entry inspection for legacy), registered behind a single dispatch point. Never performs a full parse.
- **Open component** (one per format) — owns opening the file with the correct backing library (per [ydk:req:extraction/format-extensibility]'s registry pattern) and producing metadata + subfiles.
- **Text-extraction component** (one per format) — owns flattening body text for a given open document.
- Format dispatch is registry-based, not a hardcoded enumeration, per [ydk:req:extraction/format-extensibility] — adding a format later means adding a detection/open/text-extraction component set and registry entries, not touching the public API.
- The pre-existing scaffold's dead-code folder is not reused — confirmed to have zero live edges into the active path.

Forbidden dependency: format-specific parsing MUST NOT appear outside the open/text-extraction component layer — the public facade and orchestration code depend only on those components' interfaces, never on a specific backing library directly.

## Auth & Security

Not applicable — this is an in-process library with no network surface, no users, no auth. The only security-relevant concerns are resource-exhaustion inputs, covered under Adversarial Review below and [ydk:nfr:extraction/nesting-depth-guard] / [ydk:nfr:extraction/memory-ceiling].

## Scope

**IN scope (v1):**
- Format detection and extraction for doc, xls, ppt, docx, xlsx, pptx
- Macro-enabled variants (docm/xlsm/pptm) are detected and extracted as their base format — see `mime-detection.md` for exactly how macro presence is surfaced without splitting the detected-format enum
- General metadata, format-specific metadata ([ydk:entity:extraction/WordFormatMetadata], [ydk:entity:extraction/ExcelFormatMetadata], [ydk:entity:extraction/PowerPointFormatMetadata]), flat text, first-layer subfiles ([ydk:req:extraction/subfile-scope])
- Multilingual/emoji text and metadata fidelity ([ydk:req:extraction/unicode-fidelity])
- Curated + generated test corpus, time profiling, stress testing

**OUT of scope (v1):**
- Decrypting or opening password-protected files ([ydk:error:extraction/password-protected] — fails cleanly instead)
- Recursive unpacking of subfiles nested inside other subfiles (only first-layer embeddings, per [ydk:req:extraction/subfile-scope])
- Any format beyond the 6 listed (though the architecture must not preclude adding them — [ydk:req:extraction/format-extensibility])
- A hosted service / HTTP API around the library (it's a library; monitoring NFR fields are marked "not applicable" accordingly)

**DEFERRED:**
- Additional format support (rtf, opendocument formats, pdf) — candidates for a future increment once the 6-format baseline is stable
- Recursive subfile unpacking, if a real use case emerges

## Adversarial Review

- **Encrypted/password-protected file?** → [ydk:error:extraction/password-protected], detected before a full parse is attempted, never a silent empty result.
- **Deeply nested embedded objects (doc-in-xls-in-ppt)?** → Only first-layer extraction in v1 ([ydk:req:extraction/subfile-scope]); a nested subfile is returned as one opaque item, not recursively unpacked, so nesting depth cannot cause unbounded work by itself.
- **Macro-enabled variant misclassified as its non-macro sibling, or vice versa?** → Detection reads the actual internal structure, which does differ for macro-enabled packages, but the detected-format value intentionally stays the same as the base format (see `mime-detection.md`); macro presence is a separate, explicit flag on format-specific metadata so no information is lost.
- **Zip bomb / CFB bomb (small file, huge declared internal size)?** → [ydk:error:extraction/oversized-nested-content], declared sizes checked against [ydk:nfr:extraction/nesting-depth-guard] before any buffer is allocated. This guard is a hard, always-enforced ceiling independent of [ydk:nfr:extraction/memory-ceiling], which is a measured performance target over realistic corpus files, not a runtime-enforced limit — a maliciously small file within the bomb-guard's absolute caps is still rejected by the guard even if it would exceed the performance target's ratio.
- **Truncated/corrupt file?** → [ydk:error:extraction/truncated-container] (declared-vs-actual byte mismatch) or [ydk:error:extraction/corrupt-file] (structurally present but unparseable), never a hang or a silently-empty result.
- **Zero-byte file?** → Detection returns the Unknown result (not an error — see `mime-detection.md`); extraction always fails with [ydk:error:extraction/truncated-container], since zero bytes can never satisfy any supported format's minimum container structure.
- **Wrong extension vs. real content (e.g. a docx renamed to .txt)?** → Must still classify correctly — content-only detection is a hard requirement ([ydk:req:extraction/content-only-mime-detection]), tested explicitly.
- **File larger than the configured limit?** → [ydk:error:extraction/file-too-large], rejected before parsing, every occurrence logged ([ydk:nfr:extraction/max-file-size]).
- **Null request or null required field on a request?** → Both public functions throw immediately on a null request object or a null required field (standard argument-validation convention), before any detection/extraction logic runs — this is boundary validation, not a domain error, so it has no dedicated error manifest.
- **One embedded subfile is itself corrupt while the rest of the document is fine?** → That single subfile is omitted from the result and the omission is logged; it does not fail the overall extraction call — one bad embedding shouldn't deny access to an otherwise-good document.
- **Decimal/locale precision in numeric metadata (e.g. Excel cell values pulled into flat text)?** → Out of scope for v1 — flat text extraction preserves the document's own text representation of numbers as-is; no numeric reinterpretation is performed, so there is no locale-conversion bug surface to defend.
- **Concurrent extraction calls on the same/different files?** → Each call is independent and stateless given its own request object; no shared mutable state is introduced by the component registry (DI-registered instances must be stateless or thread-safe — enforced by code review under [ydk:req:extraction/dependency-injection]). This is also directly exercised by the stress-test harness (see `testing-strategy.md`).
