# ADR-012: Decrypt password-protected OOXML and xls with a caller-supplied password

## Status
Proposed (2026-11-10). Spec branch `spec/password-decryption`.
Amended 2026-11-10 by [ADR-013](013-legacy-doc-ppt-decryption.md). Encrypted doc (RC4, RC4 CryptoAPI)
and ppt (RC4 CryptoAPI) are now decrypted, XOR-obfuscated doc stays deferred, and the statements
below about today's doc/ppt behaviour are corrected. See "Amendment" at the end.

## Context
Until now, decryption was out of scope. Every encrypted file failed as `PASSWORD_PROTECTED`, even
when the caller held the password. Callers that do hold open passwords (archives with a known
password, documents their own users protected) had no way to extract those files.

Encrypted Office files come in three shapes:

- **OOXML (docx/xlsx/pptx).** The ZIP package is encrypted whole and stored as an `EncryptedPackage`
  stream, with an `EncryptionInfo` stream beside it, inside a CFB wrapper (MS-OFFCRYPTO). Office
  2010 and later write *Agile* encryption; Office 2007 wrote *Standard* encryption. Word may also add
  a legacy `WordDocument` stub to the wrapper.
- **xls.** The CFB directory stays in plain text. The `Workbook` stream carries a `FILEPASS` record,
  and the records after it are encrypted with XOR obfuscation, RC4 or CryptoAPI RC4.
- **doc / ppt.** The CFB directory stays in plain text. The streams are RC4/CryptoAPI-encrypted and
  flagged in the FIB (doc) or by a `CryptSession10Container` (ppt).

Our stack (ADR-001, ADR-004): NPOI `main` (`NPOI.Core`) is referenced from source. It contains the
Standard, CryptoAPI, BinaryRC4 and XOR decryptors, and HSSF decrypts xls through
`Biff8EncryptionKey.CurrentUserPassword`, which is a **thread-static** slot. The Agile decryptor
lives only in NPOI's `ooxml` project (`NPOI.OOXML.Core`), which we do not reference. Its XML
descriptor is bound through `NPOI.OpenXmlFormats` (`NPOI.OpenXmlFormats.Encryption`). NPOI main's
`EncryptionInfo` finds the Agile builder by reflection on a type name that is absent unless the
`ooxml` assembly is loaded. NPOI's Agile decryptor reads the data-integrity HMAC key and value but
never checks them. doc/ppt bodies go through b2xtranslator, which has no decryption at all.

## Decision
1. **Optional `Password` on both `MimeDetectionRequest` and `ExtractionRequest`.** Null or empty
   means no password. Batch APIs keep their signatures; each request carries its own password.
2. **Decrypt encrypted OOXML (Agile + Standard) and encrypted xls in memory.** After decryption the
   file is classified and extracted exactly like its unencrypted version. **ExtractorOLE keeps
   referencing NPOI `main` only.** Standard encryption uses NPOI main's `EncryptionInfo`/`Decryptor`,
   and xls uses HSSF's own `FILEPASS` handling.
   **Agile decryption is ported into this library** from NPOI's `ooxml/POIFS/Crypt/Agile`
   (Apache-2.0). Only the decrypt side is ported: `AgileDecryptor`, `AgileEncryptionHeader`,
   `AgileEncryptionVerifier` and `KeyPair`, which cover SHA-512 (any hash the descriptor names)
   spin-count key derivation, password verification against the verifier hash, the intermediate
   key, and segment-wise AES-CBC decryption of `EncryptedPackage`. `AgileEncryptor` and
   `AgileEncryptionInfoBuilder` are not ported. The ported code:
   - reads the EncryptionInfo XML descriptor with `System.Xml`, not `NPOI.OpenXmlFormats` binding
     classes;
   - reuses NPOI main's `CryptoFunctions`/cipher helpers where they already exist, rather than
     copying them;
   - is routed by our own EncryptionInfo version check (4.4 = Agile), never through NPOI main's
     reflection lookup;
   - **adds the data-integrity check** that NPOI omits. After the password verifies, the HMAC
     (RFC 2104, descriptor hash) over the `EncryptedPackage` stream is compared with the decrypted
     `encryptedHmacValue`. A mismatch is `CORRUPT_FILE` (container-level): the password was right,
     so it is not `INVALID_PASSWORD`, but the ciphertext was altered. Standard encryption carries no
     integrity data, so it is not checked.
   Attribution: every ported file keeps the Apache License 2.0 header from its source and adds a
   line naming the NPOI source path and the submodule commit it was ported from. A
   `THIRD-PARTY-NOTICES` entry records the Apache-2.0 origin (ASF / nissl-lab NPOI).
3. **New error `INVALID_PASSWORD`** (`ydk:error:extraction/invalid-password`) for a supplied
   password that fails the scheme's verifier. It is fatal only. `PASSWORD_PROTECTED` keeps meaning
   "no password supplied", and also "encryption this library does not decrypt": XOR-obfuscated
   doc and other doc schemes outside ADR-013, Extensible encryption and IRM. (Amended by ADR-013:
   this list used to include all encrypted doc/ppt.)
4. **A password supplied for an unencrypted file is ignored**, for all six formats including doc
   and ppt. There is no error and no warning `FieldError`, and the result is identical to the
   no-password call. A password is something the caller *has*, not a claim that the file is
   encrypted. Callers that pass one password across a mixed batch must not see spurious failures.
   An xls encrypted only with Excel's default `VelvetSweatshop` key counts as having no open
   password, so the same rule applies to it.
5. **Detection.** A CFB holding `EncryptionInfo` + `EncryptedPackage` is an encrypted OOXML package,
   whatever stubs sit beside it. With no password it returns Unknown: the inner format cannot be
   known, and detection's contract is lenient. With the correct password the package is decrypted
   and classified by the OOXML structural check. A wrong password throws `INVALID_PASSWORD`, because
   the caller asked for decryption and Unknown would hide their mistake. Encrypted doc/xls/ppt are
   classified from their plain-text directory, as today, and the password is not used for them.
6. **Guards stay authoritative.** `FILE_TOO_LARGE` applies to the encrypted input. Before any buffer
   is allocated, the declared decrypted size is checked against the nesting-depth guard
   (`OVERSIZED_NESTED_CONTENT`) and against the stream length (`TRUNCATED_CONTAINER`). An Agile spin
   count over 10,000,000 (the MS-OFFCRYPTO maximum) is `CORRUPT_FILE` before any key derivation. The
   decrypted package then passes through every guard an unencrypted one would.
7. **Thread-static HSSF password.** Set it immediately before the synchronous HSSF open and clear it
   in `finally` on the same thread, with no `await` in between. Also clear it before every
   password-less xls open. Nothing else is shared across calls.
8. **Confidentiality** (`ydk:req:extraction/password-confidentiality`). The password, derived keys and
   decrypted bytes never reach logs, exceptions, `FieldError`s or results, and are not retained after
   the call.
9. **The password applies to the supplied container only.** Encrypted embedded subfiles are
   returned opaque, as today.

## Alternatives Considered
- **Reference NPOI's `ooxml` project (`NPOI.OOXML.Core`) for the Agile decryptor.** Rejected
  because of its dependencies. To use one decryptor class we would take on `NPOI.OpenXml4Net.Core`,
  the very large generated `NPOI.OpenXmlFormats.Core` schema assembly, and
  `System.Security.Cryptography.Xml`, and the reference would also carry `ooxml`'s own SkiaSharp
  reference. (Note: NPOI main already references SkiaSharp and its Linux native assets, so SkiaSharp
  itself is not new to our build. The new weight is OpenXml4Net, OpenXmlFormats and
  Cryptography.Xml, plus a second, parallel OOXML object model next to DocumentFormat.OpenXml.) It
  would also not give us the integrity check, which NPOI does not perform.
- **Custom RC4/CryptoAPI decryptor for doc/ppt.** Deferred here; taken up by ADR-013, which
  decrypts with NPOI main's decryptors and rebuilds a plain compound file. Original text: b2xtranslator cannot read encrypted
  streams, so we would have to decrypt each stream to a plain CFB ourselves (FIB/table/data streams
  for doc; the persist-object directory and `CryptSession10Container` handling for ppt) and then feed
  b2xtranslator. That is a substantial, format-specific body of crypto code with no upstream to track.
  It stays `PASSWORD_PROTECTED` until a real use case justifies it.
- **Return Unknown for a wrong password at detection.** Rejected. It hides a caller error behind a
  "not an Office file" answer.
- **Throw `PASSWORD_PROTECTED` from detection when no password is given.** Rejected. It changes
  detection's lenient no-throw contract for a case extraction already reports.
- **Fail (or warn via `FieldError`) when a password is supplied for an unencrypted file.** Rejected.
  It punishes callers who pass a known password for a mixed batch, and a warning entry would break
  the rule that a `FieldError` means "this leaf could not be read".
- **Treat a wrong password as `CORRUPT_FILE`.** Rejected. The verifier can tell a wrong password
  apart from damage, and callers need to know which one happened in order to retry.

## Consequences
- **We now own a small piece of crypto code.** It covers about four classes of decrypt-side Agile
  logic plus the integrity check, and nothing tracks upstream fixes to it. The mitigations are as
  follows. Only the decrypt path is ported, and it reuses NPOI main's primitives. It is validated
  against the Office-authored encrypted samples (Agile docx/xlsx/pptx, which must decrypt to their
  unencrypted twins), against wrong-password cases, and against a tampered-ciphertext sample that
  must fail the integrity check. When the NPOI submodule is re-pinned, the ported files are diffed
  against their `ooxml` source. The library stack table in `docs/project-rules.md` records the port.
- No new project or package reference. The dependency set of `ExtractorOLE` is unchanged.
- One new public request field on each of the two DTOs, which is additive and non-breaking. There
  is also one new error code and exception type.
- Encrypted test samples are flagged by folder alone. They all live under `samples/encrypted/` and are never mixed into curated/adversarial/synthetic, and their unencrypted twins stay where they are. Their manifest entries carry `password` and `unencryptedTwin` as test data. There is no filename prefix, no manifest flag and no test trait. Stage 02 moves the existing `samples/adversarial/password-protected.docx` to `samples/encrypted/` and updates its manifest path.
- `samples/adversarial/password-protected.docx` is a Word-written wrapper that carries a
  `WordDocument` stub. Without a password it will now detect as Unknown, not Doc, so its manifest
  expectation must be re-observed.
- Encrypted doc/ppt must fail as `PASSWORD_PROTECTED` (no password) rather than with whatever the
  conversion library happens to throw. *Corrected by ADR-013:* this was written as "unverified",
  and running it showed the library does not fail at all. An encrypted doc returns a success
  result with empty text and one `UnexpectedFieldFailure`. An encrypted ppt returns a partial
  result with three `UnexpectedFieldFailure` entries and hands back its encrypted `Pictures`
  stream as a subfile. An explicit encryption guard before parsing is therefore required
  (ADR-013 decision 2).
- Agile key derivation costs about 100,000 SHA-512 rounds per verification, and a detect-then-extract
  pipeline pays it twice. The cost is accepted. Encrypted samples are excluded from the latency
  baseline (`ydk:nfr:extraction/extraction-latency`), which measures unencrypted extraction.

## Amendment (2026-11-10, ADR-013)
- Encrypted doc (BinaryRC4, RC4 CryptoAPI) and encrypted ppt (RC4 CryptoAPI) move from deferred to
  decrypted. With the correct password they extract like their unencrypted versions, and the
  outcomes of decisions 1, 3 and 4 apply to them unchanged. Design and rules: ADR-013,
  `ydk:req:extraction/legacy-decryption`.
- XOR-obfuscated doc stays `PASSWORD_PROTECTED` with or without a password, deferred until
  yoavarad/npoi#198 and a fixture exist.
- XOR-obfuscated xls (decision 2, via HSSF) remains in scope, but it is blocked on
  yoavarad/npoi#197. Until that re-pin, the correct password fails as `CORRUPT_FILE`.
- Decision 5 is unchanged: detection still classifies encrypted doc/ppt from the plain directory
  without using the password.
