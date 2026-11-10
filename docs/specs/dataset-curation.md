# Dataset Curation

## Purpose

Test sample quality determines whether every other epic's correctness claims mean anything — this gets its own epic and its own narrative, not a folded-in afterthought of "testing".

## Corpus Composition

For each of the 6 supported formats, the corpus contains at minimum:

- **3 synthetic complex samples**, generated programmatically: (1) a file with at least 3 first-layer embedded objects/media of at least 2 different kinds, (2) a file with a text body exceeding 100,000 words, and (3) a macro-enabled variant with at least one embedding. Every synthetic sample includes multilingual/mixed-script text and emoji in both body text and at least 2 metadata fields, per [ydk:req:extraction/unicode-fidelity].
- **2 curated real-world-style samples**: self-authored in Office/LibreOffice, or drawn from public-domain/OSS sample collections with checked, recorded licensing — at least one with 2+ levels of nested embedding (an embedded object that itself contains an embedded object) and at least one with a real (not scripted) authorship/revision-history property chain from having been edited by multiple real users across multiple real save operations.
- **The shared adversarial set** (not per-format, since these scenarios are format-agnostic where possible): one corrupt sample, one truncated sample, one zero-byte sample, one oversized-declared-size sample (zip/CFB bomb per [ydk:nfr:extraction/nesting-depth-guard]), one password-protected sample, one wrong-extension sample (correct content, misleading or missing filename extension), and one sample with a single corrupt embedded object alongside otherwise-valid content (exercises the per-subfile-failure rule in `extraction.md`) — each tagged with which format(s) it targets.

- **The encrypted set**, each paired with an unencrypted twin whose expectations it must match after decryption ([ydk:req:extraction/password-decryption]): Agile-encrypted docx, xlsx and pptx; a Standard-encrypted OOXML sample; an RC4 xls and a CryptoAPI xls (one of them with encrypted document properties); an xls encrypted only with Excel's default key; encrypted doc and ppt samples per [ydk:req:extraction/legacy-decryption] (listed below); an encrypted package embedded as a subfile in an unencrypted document (must come back opaque); and an Agile sample whose encrypted bytes were altered after encryption (must fail the integrity check). Passwords are test data, not secrets: they are recorded in plain text in the manifest and include at least one non-ASCII password. Office-authored samples are preferred; where encryption is scripted, the sample is tagged synthetic and the tool is recorded. The existing `samples/adversarial/password-protected.docx` (password known from its upstream source) counts as the Agile docx once its decrypted expectations are observed and recorded.

**Encrypted samples are flagged by folder.** Every sample of the encrypted set lives under `samples/encrypted/` and nowhere else, including the document that embeds an encrypted subfile, the encrypted doc/ppt samples and the existing password-protected docx, which moves there from `samples/adversarial/` (a Stage 02 task, together with its manifest path). It is never mixed into the curated, adversarial or synthetic folders. Each unencrypted twin stays in its usual folder. The folder is the only flag: there is no filename prefix and no manifest marker for encryption. Each encrypted sample's manifest entry carries two data fields that the tests consume: `password`, the open password in plain text (omitted only for the default-key xls and the embedding document, which have none), and `unencryptedTwin`, the manifest path of the twin whose expectations it must match (omitted for the embedding document, for negative-only samples whose password is unknown, and for third-party samples that have no Office-saved twin, see below).

**Encrypted doc and ppt samples** ([ydk:req:extraction/legacy-decryption], ADR-013). All are third-party test files with a known password:

| Sample | Scheme | Password | Source and licence | Twin |
|---|---|---|---|---|
| `password_tika_binaryrc4.doc` | BinaryRC4 | `tika` | Apache POI test-data, Apache-2.0 | none |
| `password_password_cryptoapi.doc` | RC4 CryptoAPI, 128-bit, has a Data stream | `password` | Apache POI test-data, Apache-2.0 | none |
| `rc4cryptoapi_password.doc` | RC4 CryptoAPI, 128-bit | `Password1234_` | msoffcrypto-tool tests, MIT | upstream `rc4cryptoapi_password_plain.doc` (same licence), added as the unencrypted twin |
| `PasswordProtected.doc` | BinaryRC4 | unknown | Apache POI test-data, Apache-2.0 | none; negative-only (no password, wrong password) |
| `Password_Protected-hello.ppt` | RC4 CryptoAPI, 40-bit, encrypted properties | `hello` | Apache POI test-data, Apache-2.0 | none |
| `Password_Protected-56-hello.ppt` | RC4 CryptoAPI, 56-bit, encrypted properties | `hello` | Apache POI test-data, Apache-2.0 | none |
| `Password_Protected-np-hello.ppt` | RC4 CryptoAPI, 40-bit, plain properties | `hello` | Apache POI test-data, Apache-2.0 | none |
| `cryptoapi-proc2356.ppt` | RC4 CryptoAPI, keySize 0 (40-bit), no mini stream | `crypto` | Apache POI test-data, Apache-2.0 | none |
| `ppt_with_png_encrypted.ppt` | RC4 CryptoAPI, 40-bit, pictures | `password` | Apache POI test-data, Apache-2.0 | none (`ppt_with_png.ppt` is a different save, not a twin) |

Where no Office-saved twin exists, the sample's manifest entry records its own exact expectations. They are observed by running an independent decryptor's plaintext (msoffcrypto-tool, version recorded in the entry) through the unencrypted pipeline, never written from prose. The decrypted pictures' SHA-256 is recorded the same way.

Two ppt samples need care: `cryptoapi-proc2356.ppt` and `ppt_with_png_encrypted.ppt`. They decrypt correctly, but their plaintext converts to empty text because of b2xtranslator conversion defects that have nothing to do with encryption:

- a missing `fillShadeType` lookup in `FillMapping`;
- 16-byte client anchors read as 8-byte, leading to a null reference in `ShapeTreeMapping`;
- a nullable value in `TextMasterStyleMapping.writepPr`.

Their recorded expectation is whatever the unencrypted-equivalent pipeline produces for their plaintext. Text-parity assertions for them are gated on those separate b2xtranslator fixes; decrypted pictures and metadata are asserted now.

Not yet available, and listed as test gaps in [ydk:req:extraction/legacy-decryption]:

- a 40-bit CryptoAPI doc;
- a CryptoAPI doc with encrypted document properties;
- a CryptoAPI doc with OLE objects;
- an XOR-obfuscated doc (deferred);
- an XOR-obfuscated xls. `xor-encryption-abc.xls`, password `abc`, Apache POI test-data, Apache-2.0, is a candidate, gated on yoavarad/npoi#197.

This totals at least 30 per-format samples (6 formats x 5 synthetic+curated each) plus the adversarial set and the encrypted set.

## Corpus Manifest

Every sample is recorded in `samples/manifest.json` with: format, which composition rule above it satisfies, expected detected format, and — for the text/metadata content — the exact expected Unicode string per field being asserted (not a description of it). For subfile counts: every synthetic and curated sample records an exact expected subfile count and the exact expected filename for each entry, since these are fully under our control at generation/curation time; there is no case in this corpus where subfile count is left unspecified. This manifest is the ground truth the test suite asserts against — a sample without a manifest entry does not count as coverage.

## Provenance & Licensing

Every sample file is tagged self-generated vs. third-party, with license recorded for anything third-party, so the corpus is safe to commit and redistribute. Each third-party encrypted sample also records its upstream URL and licence in its manifest entry, and has a line in `THIRD-PARTY-NOTICES` (Apache POI test-data: Apache-2.0; msoffcrypto-tool tests: MIT).
