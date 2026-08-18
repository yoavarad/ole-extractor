# Dataset Curation

## Purpose

Test sample quality determines whether every other epic's correctness claims mean anything — this gets its own epic and its own narrative, not a folded-in afterthought of "testing".

## Corpus Composition

For each of the 6 supported formats, the corpus contains at minimum:

- **3 synthetic complex samples**, generated programmatically: (1) a file with at least 3 first-layer embedded objects/media of at least 2 different kinds, (2) a file with a text body exceeding 100,000 words, and (3) a macro-enabled variant with at least one embedding. Every synthetic sample includes multilingual/mixed-script text and emoji in both body text and at least 2 metadata fields, per [ydk:req:extraction/unicode-fidelity].
- **2 curated real-world-style samples**: self-authored in Office/LibreOffice, or drawn from public-domain/OSS sample collections with checked, recorded licensing — at least one with 2+ levels of nested embedding (an embedded object that itself contains an embedded object) and at least one with a real (not scripted) authorship/revision-history property chain from having been edited by multiple real users across multiple real save operations.
- **The shared adversarial set** (not per-format, since these scenarios are format-agnostic where possible): one corrupt sample, one truncated sample, one zero-byte sample, one oversized-declared-size sample (zip/CFB bomb per [ydk:nfr:extraction/nesting-depth-guard]), one password-protected sample, one wrong-extension sample (correct content, misleading or missing filename extension), and one sample with a single corrupt embedded object alongside otherwise-valid content (exercises the per-subfile-failure rule in `extraction.md`) — each tagged with which format(s) it targets.

This totals at least 30 per-format samples (6 formats x 5 synthetic+curated each) plus the adversarial set.

## Corpus Manifest

Every sample is recorded in `samples/manifest.json` with: format, which composition rule above it satisfies, expected detected format, and — for the text/metadata content — the exact expected Unicode string per field being asserted (not a description of it). For subfile counts: every synthetic and curated sample records an exact expected subfile count and the exact expected filename for each entry, since these are fully under our control at generation/curation time; there is no case in this corpus where subfile count is left unspecified. This manifest is the ground truth the test suite asserts against — a sample without a manifest entry does not count as coverage.

## Provenance & Licensing

Every sample file is tagged self-generated vs. third-party, with license recorded for anything third-party, so the corpus is safe to commit and redistribute.
