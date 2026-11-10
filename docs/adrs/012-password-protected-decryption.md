# ADR-012: Decrypt password-protected OOXML and xls with a caller-supplied password

## Status
Proposed (2026-11-10). Spec branch `spec/password-decryption`.

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
lives only in NPOI's `ooxml` project (`NPOI.OOXML.Core`), which we do not reference today. doc/ppt
bodies go through b2xtranslator, which has no decryption at all.

## Decision
1. **Optional `Password` on both `MimeDetectionRequest` and `ExtractionRequest`.** Null or empty
   means no password. Batch APIs keep their signatures; each request carries its own password.
2. **Decrypt encrypted OOXML (Agile + Standard) and encrypted xls in memory.** After decryption the
   file is classified and extracted exactly like its unencrypted version. Add a `ProjectReference` to
   `third_party/npoi/ooxml/NPOI.OOXML.Core.csproj` for the Agile decryptor. Use NPOI's
   `EncryptionInfo`/`Decryptor` for both OOXML schemes, and HSSF's own `FILEPASS` handling for xls.
3. **New error `INVALID_PASSWORD`** (`ydk:error:extraction/invalid-password`) for a supplied
   password that fails the scheme's verifier. It is fatal only. `PASSWORD_PROTECTED` keeps meaning
   "no password supplied", and also "encryption this library does not decrypt": encrypted doc/ppt,
   Extensible encryption and IRM.
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
- **Hand-port Agile decryption into this repo** instead of referencing NPOI `ooxml`. Rejected for
  now: it duplicates audited crypto code we already vendor (Apache-2.0, same submodule pin), and
  crypto is the worst place for a bespoke port. The cost of the reference is its transitive
  dependencies; see Consequences. Revisit if those prove unacceptable. The fallback is a narrow port
  of `AgileDecryptor` and its helpers only.
- **Custom RC4/CryptoAPI decryptor for doc/ppt.** Deferred. b2xtranslator cannot read encrypted
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
- `ExtractorOLE` gains a `ProjectReference` to `NPOI.OOXML.Core`. That brings in
  `NPOI.OpenXml4Net.Core`, `NPOI.OpenXmlFormats.Core`, `System.Security.Cryptography.Xml` and
  **SkiaSharp**, a native-asset package. The implementing task must confirm that the build, CI on
  ubuntu + windows, and the vulnerable-package check stay green. It must also record any license or
  package-size impact in ADR-001's stack table.
- One new public request field on each of the two DTOs, which is additive and non-breaking. There
  is also one new error code and exception type.
- Encrypted test samples are flagged by folder alone. They all live under `samples/encrypted/` and are never mixed into curated/adversarial/synthetic, and their unencrypted twins stay where they are. Their manifest entries carry `password` and `unencryptedTwin` as test data. There is no filename prefix, no manifest flag and no test trait. Stage 02 moves the existing `samples/adversarial/password-protected.docx` to `samples/encrypted/` and updates its manifest path.
- `samples/adversarial/password-protected.docx` is a Word-written wrapper that carries a
  `WordDocument` stub. Without a password it will now detect as Unknown, not Doc, so its manifest
  expectation must be re-observed.
- Encrypted doc/ppt must fail as `PASSWORD_PROTECTED` rather than with whatever the conversion
  library happens to throw. Today's behavior for them is unverified, because the corpus has no
  encrypted doc/ppt sample. The implementing task observes it and, if needed, adds an explicit
  encrypted-flag check.
- Agile key derivation costs about 100,000 SHA-512 rounds per verification, and a detect-then-extract
  pipeline pays it twice. The cost is accepted. Encrypted samples are excluded from the latency
  baseline (`ydk:nfr:extraction/extraction-latency`), which measures unencrypted extraction.
