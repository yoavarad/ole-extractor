# Testing Strategy

## Test Types

- **Unit tests** — per-component correctness (a given open component or text-extraction component against a specific known input), independent of the DI container.
- **Integration tests** — both public functions end-to-end against every sample in the curated corpus (see `dataset-curation.md`), asserting exact expected format, exact expected text/metadata content (not just non-empty), and exact expected subfile list against the corpus manifest.
- **Time profiling** — a dedicated benchmark project measuring [ydk:nfr:extraction/extraction-latency] and [ydk:nfr:extraction/memory-ceiling] per format against fixed corpus samples (tooling choice recorded in ADR-002), run separately from the unit/integration suite.
- **Stress testing** — a concurrent-load harness (tooling choice and rationale recorded in ADR-002) exercising at least 50 concurrent calls to both public functions (configurable higher for extended runs), reporting throughput/latency/memory under load. Not part of the standard test suite gate — run manually or as a longer CI job.

## Rules

- **NEVER assert only "text is non-empty"** for a corpus sample that has an exact expected string recorded in the manifest — this is how truncation/mojibake bugs hide (see [ydk:req:extraction/unicode-fidelity]'s acceptance criteria).
- **NEVER assert a thrown error for a per-field failure case**, and never assert a no-value leaf without also asserting its field-error entry. Under [ydk:req:extraction/per-field-error-isolation] the adversarial cases that occur after the container opens — post-open corruption, a corrupt embedded object, an oversized nested item — assert a populated result plus a specific [ydk:entity:extraction/FieldError] at the exact field path, and assert that the document's other leaves are still populated. An empty extracted text with no field-error entry, or a short subfile list with no field-error entry, is a test failure: that silent-pass shape is the behavior this requirement removes, so a test that tolerates it removes the guard. The container-open cases (file-too-large, password-protected, unsupported-format, truncated-container, zero-byte, null request/field, and a container that cannot be opened at all) still assert the throw, and also assert that no result is returned, so the fatal set cannot drift into the per-field channel unnoticed.
- **NEVER mock the format-handling components** in integration tests — the whole point is exercising the real parsing path against real file bytes. Mocking is only appropriate at true external boundaries, and this library has none (no network calls, no filesystem dependency inside the extraction path itself — callers supply bytes).
- **Every adversarial-review scenario in `overview.md` that isn't explicitly marked out of scope MUST have a corresponding test** (the decimal/locale-precision bullet is explicitly out of scope and needs no test), via one of three mechanisms — a scenario not covered by one of these doesn't count as covered:
  - Corpus-sample-backed: corrupt, truncated, zero-byte, oversized, password-protected, wrong-extension, and corrupt-embedded-object (shared adversarial set); macro-variant-misclassification (each format's macro-enabled synthetic sample); deeply-nested embedding (the curated 2+-level-nesting sample) — all tagged in the corpus manifest.
  - Unit-test-backed (no checked-in sample needed): null request/field validation; file-too-large (a synthetic oversized byte-array length, per [ydk:nfr:extraction/max-file-size]'s test method).
  - Stress-harness-backed: concurrent calls on shared/different files.
- Database and external-API test strategy: not applicable — this library has neither.
