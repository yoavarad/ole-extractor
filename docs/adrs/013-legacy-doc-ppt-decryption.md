# ADR-013: Decrypt password-protected legacy doc and ppt into a plain compound file

## Status
Proposed (2026-11-10). Amends ADR-012: moves encrypted doc (RC4 and RC4 CryptoAPI) and encrypted
ppt (RC4 CryptoAPI) from "deferred" to "decrypted". XOR-obfuscated doc stays deferred.

## Context
ADR-012 deferred doc/ppt decryption ("Custom RC4/CryptoAPI decryptor for doc/ppt. Deferred") and
assumed that encrypted doc/ppt would fail as `PASSWORD_PROTECTED`. Two research spikes
(2026-11-10, prototypes run against the libraries pinned at npoi `72ff7c7` and b2xtranslator
`930217b`) show that both assumptions need revisiting.

**Today's behaviour is not `PASSWORD_PROTECTED`.** Run through the unmodified library:
- An encrypted doc (BinaryRC4 or CryptoAPI) returns a *success* result: empty text, the plain
  summary metadata, and one `UnexpectedFieldFailure` at `ExtractedText`. b2xtranslator parses
  ciphertext FIB fields as offsets and throws `ByteParseException`. Nothing in the library or in
  b2xtranslator reads the FIB's `fEncrypted` flag.
- An encrypted ppt returns a *partial* result with three `UnexpectedFieldFailure` entries
  (SlideCount, NotesSlideCount, ExtractedText), caused by b2xtranslator's `NotSupportedException`
  on the encrypted Current User token. Where the file has pictures it also returns the
  **ciphertext** `Pictures` stream as a subfile.
- `ParseTimeErrorGuard.CheckCfbNotEncrypted` only recognises the OOXML `EncryptionInfo` /
  `EncryptedPackage` pair, so neither format is caught before parsing.

**Decryption is feasible with what we already reference.** In both formats the compound-file
directory stays plain, and only some stream contents are encrypted:
- **doc** (MS-DOC 2.2.6). The FIB base (first 32 bytes of `WordDocument`) carries `fEncrypted`,
  `fObfuscated` and `lKey`. For RC4 schemes `lKey` is the size of an encryption header at the
  start of the table stream: version 1.1 is BinaryRC4, versions 2.2/3.2/4.2 are RC4 CryptoAPI.
  Encrypted are `WordDocument` after its first 68 bytes, the table stream after its first `lKey`
  bytes, and the whole `Data` stream, in 512-byte re-keyed blocks. Decryption preserves lengths.
- **ppt** (MS-PPT 2.3.7). Only RC4 CryptoAPI is allowed. The Current User token is `0xF3D1C4DF`
  for an encrypted file, and the newest `UserEditAtom` points (through the persist directory) at a
  `CryptSession10Container` that holds the encryption header. Each persist object is decrypted as
  one RC4 run keyed by its persist id. The `Pictures` stream is decrypted field by field, and the
  document properties may be moved into an `EncryptedSummary` stream.
- NPOI `main` already has working `EncryptionInfo`, `BinaryRC4Decryptor` and `CryptoAPIDecryptor`
  classes. The prototypes decrypted every available fixture with them (3 doc, 5 ppt), and the
  output was byte-identical to an independent decryptor (msoffcrypto-tool) and to a hand-written
  RC4 implementation. No NPOI or b2xtranslator change is required.

Defects found in NPOI's helpers, all avoidable by validating first:
- NPOI ignores `EncryptionHeader.headerSize`, reads the CSP name until a NUL, and does not
  validate `keySize`. A corrupt header therefore shows up as a failed verification, which looks
  like a wrong password.
- `CryptoAPIDecryptor.GetDataStream(DirectoryNode)` hard-codes the stream name `EncryptedSummary`
  (Word and Excel use `encryption`, yoavarad/npoi#199). It also uses an unvalidated descriptor
  count as an array size: a crafted count gives `OutOfMemoryException`, which escapes per-field
  isolation.
- NPOI's XOR support was broken for xls Method 1 (fixed by yoavarad/npoi#197, pinned at npoi
  `41873b800`) and is missing for Word Method 2 (yoavarad/npoi#198).

**How the plain compound file is written decides whether b2xtranslator can read it.** Rebuild
matrix from the prototypes:
- OpenMcdf `CopyTo` drops root and child CLSIDs and reorders embedded objects.
- OpenMcdf output writes the root entry's start sector as `0xFFFFFFFF` when there is no mini
  stream. b2xtranslator then fails (`InvalidSectorInChainException`) on `cryptoapi-proc2356.ppt`.
- Editing streams in place in an opened NPOI file system shrinks the mini stream but leaves its
  FAT chain one sector too long. b2xtranslator then fails (`ChainSizeMismatchException`) on
  `Password_Protected-hello.ppt`.
- A **fresh NPOI `NPOIFSFileSystem`**, with the root CLSID copied, untouched entries copied with
  `EntryUtils.CopyNodeRecursively` and the replaced streams written new, worked on every fixture.
  It also preserved CLSIDs and embedded-object order on plain docs with Macros and ObjectPool.

## Decision
1. **Encrypted doc and ppt are decrypted in this library, into an in-memory plain compound file,
   which then goes through the existing pipeline unchanged.** Decryptable schemes: doc with RC4
   (BinaryRC4, version 1.1) or RC4 CryptoAPI (versions 2.2/3.2/4.2, key sizes 40 to 128 bits), and
   ppt with RC4 CryptoAPI. With the correct password they are classified and extracted like their
   unencrypted versions. The ADR-012 outcomes apply unchanged: no password ->
   `PASSWORD_PROTECTED`, wrong password -> `INVALID_PASSWORD`, password on an unencrypted file
   ignored. Rules: `ydk:req:extraction/legacy-decryption`.
2. **Detection before parsing.** An encryption guard runs after the compound-file preflight and
   before any open strategy or b2xtranslator call. It reads the doc FIB base, or the ppt Current
   User stream and edit chain. An encrypted doc/ppt with no password fails as
   `PASSWORD_PROTECTED` here, so the empty-text and partial results described above can no longer
   be returned. `DetectMimeType` is unchanged: encrypted doc/ppt still classify from their plain
   directory and the password is not used there (ADR-012 decision 5).
3. **Crypto reuses NPOI `main`; parsing of encryption structures is ours.**
   - Password verification and per-block ciphers come from NPOI `EncryptionInfo`, `Decryptor`
     and `CryptoAPIDecryptor`. The header bytes are wrapped as
     `LittleEndianInputStream(ByteArrayInputStream)`, because the CryptoAPI header constructor
     casts its input to `InputStream`.
   - For doc, the streams are read through `GetDataStream(stream, size, 0)` with
     `SetChunkSize(512)` and `ReadPlain` for the clear prefix.
   - For ppt, `InitCipherForBlock(null, persistId)` gives one cipher per persist object, and a
     fresh block-0 cipher is used for each Pictures field.
   - The host validates every encryption-header field itself before NPOI sees it
     (`ydk:req:extraction/legacy-encryption-header-bounds`), so a malformed header is reported as
     `CORRUPT_FILE` and never as a wrong password or a raw NPOI exception.
   - The encrypted-summary parser (`EncryptedSummary`, MS-OFFCRYPTO 2.3.5.4) is our own short,
     bounded parser. NPOI `GetDataStream(DirectoryNode)` is never called.
   - The Pictures field walker is ported from Apache POI's `HSLFSlideShowEncrypted.decryptPicture`
     (Apache-2.0). It keeps the Apache-2.0 header and a source note, and gets a
     `THIRD-PARTY-NOTICES` entry, like the Agile port in ADR-012.
4. **Rebuild with a fresh NPOI `NPOIFSFileSystem`** (`ydk:req:extraction/legacy-plaintext-rebuild`).
   The rebuilt file holds:
   - the root CLSID, copied;
   - every untouched entry, copied recursively;
   - every decrypted stream, written as a new entry.

   No in-place edit is made and OpenMcdf is not used as the writer.
   - **doc.** Decrypt `WordDocument`, the table stream and `Data`. Clear `fEncrypted` (0x0100) and
     `fObfuscated` (0x8000) in the FIB flags word and set `lKey` to 0, so the rebuilt file is
     self-consistent and a later encryption check does not fire on it again. b2xtranslator would
     parse it without this step, but the oracle output clears the flags too.
   - **ppt.** Decrypt every persist object except the `CryptSession10Container` in place in a copy
     of `PowerPoint Document`. Rewrite only the Current User `headerToken` from `0xF3D1C4DF` to
     `0xE391C05F`. The `UserEditAtom` (record length 0x20), its persist entry and the
     `CryptSession10Container` record stay as they are: b2xtranslator reads the optional field
     and ignores the leftover record, so no offsets move.
   - **Pictures (ppt).** Decrypted whole, so the opaque `Pictures` subfile matches the unencrypted
     equivalent.
   - **Encrypted summary (ppt).** `EncryptedSummary` is decrypted and replaced by real
     `\x05SummaryInformation` / `\x05DocumentSummaryInformation` streams.
5. **The plain bytes replace the input for the rest of the call**: for the open, and for every
   fallback that re-reads `input.Bytes`. `FileMetadata.FileSizeBytes` and telemetry keep the size
   of the supplied (encrypted) input.
6. **b2xtranslator needs no change for decryption.** A typed "encrypted document" exception in
   b2xtranslator (yoavarad/b2xtranslator#156) is optional defence in depth, for the case where the
   guard is bypassed.
7. **XOR obfuscation.**
   - **xls (Method 1)** stays in scope under ADR-012 and works since the yoavarad/npoi#197
     re-pin (npoi `41873b800`). Before it, at npoi `72ff7c7`, the correct password failed with
     `RecordFormatException`, reported as `CORRUPT_FILE`.
   - **doc (Method 2)** is **not in scope for this increment**. An encrypted doc with
     `fObfuscated = 1` fails as `PASSWORD_PROTECTED` with or without a password. It is deferred
     until yoavarad/npoi#198 lands and a Word-authored XOR fixture exists.
   - **ppt and OOXML** have no XOR scheme.
8. **Known gaps are stated, not hidden.** These have no fixture or are documented limitations, and
   `ydk:req:extraction/legacy-decryption` lists each one with its chosen behaviour:
   - 40-bit CryptoAPI doc;
   - CryptoAPI doc with encrypted document properties (`encryption` stream);
   - CryptoAPI doc whose OLE objects are relocated into the `Data` stream;
   - ppt whose Current User token disagrees with its edit-chain structure;
   - storage entries inside `EncryptedSummary`.

## Alternatives Considered
- **Keep doc/ppt deferred (ADR-012 as written).** Rejected. The library does not even reject these
  files today: it returns empty or partial results and leaks ciphertext. A detection guard is
  needed anyway, and the prototypes show that the decryption beyond it costs about 400 lines per
  format group on NPOI primitives we already ship.
- **Hand-written RC4 + MD5/SHA-1 instead of NPOI's decryptors.** Proven equally (byte-identical
  output, and it avoids NPOI's generic exceptions), but rejected to stay with ADR-012's "reuse NPOI
  main's primitives" and keep crypto code we own to a minimum. NPOI's weak spots are covered by
  host-side header validation.
- **OpenMcdf as the rebuild writer, or an in-place NPOI edit.** Rejected. Each one produced files
  that b2xtranslator's strict reader rejects (no-mini-stream root sector; over-long mini-stream
  chain). OpenMcdf `CopyTo` also loses CLSIDs and reorders embedded objects.
- **POI-style "compact" ppt rewrite** (drop the `CryptSession10Container`, shrink the
  `UserEditAtom`, rewrite the persist directory). Rejected. It gave the same result in the
  prototype but needs about 25 more lines and moves offsets. The token rewrite alone is enough.
- **Teach b2xtranslator to decrypt.** Rejected. It would put format crypto in a fork with no
  upstream to track, and NPOI already has the primitives.
- **Use NPOI `GetDataStream(DirectoryNode)` for `EncryptedSummary`.** Rejected. A crafted
  descriptor count gives `OutOfMemoryException`/`OverflowException`, the stream name is hard-coded,
  and it throws when `fDocProps` is set and the stream is absent.
- **Implement XOR doc now from the spec alone.** Rejected for this increment. No public fixture
  exists, no trustworthy reference implementation exists (POI's HWPF XOR path misreads `lKey`),
  and one spec detail (the order of the two 16-bit halves inside `lKey`) is unverified.

## Consequences
- **Corrections to ADR-012.**
  - Decision 3: `PASSWORD_PROTECTED` for "encryption this library does not decrypt" now covers
    XOR-obfuscated doc and unsupported doc schemes, but no longer RC4/CryptoAPI doc or ppt.
  - The "Custom RC4/CryptoAPI decryptor" alternative is taken up here.
  - The consequence that called today's doc/ppt behaviour "unverified" is replaced by the measured
    behaviour above.
- **More crypto-adjacent code we own:**
  - FIB and encryption-header validation;
  - the ppt edit-chain and persist-directory walker;
  - the encrypted-summary parser;
  - the ported Pictures field walker;
  - the rebuild helper.

  It is validated against the Apache POI and msoffcrypto-tool fixtures, against
  msoffcrypto-tool's independent plaintext, and against crafted header mutations.
- **Memory.** Encrypted doc/ppt must be fully buffered, including stream input (ADR-008). Peak
  memory is about 3 to 6 times the input (decrypted streams plus the rebuilt file). That is still
  bounded by `FILE_TOO_LARGE` on the input. Decryption never grows a stream.
- **Fixtures.** Encrypted doc/ppt samples join `samples/encrypted/` with `password` and, where one
  exists, `unencryptedTwin`. Two ppt fixtures (`cryptoapi-proc2356.ppt`,
  `ppt_with_png_encrypted.ppt`) decrypt correctly but convert to empty text today because of
  b2xtranslator defects that have nothing to do with encryption:
  - `FillMapping.cs:265` and `:558`: missing `fillShadeType`;
  - 16-byte `ClientAnchor`, leading to an NRE at `ShapeTreeMapping.cs:2302`;
  - `TextMasterStyleMapping.writepPr`.

  Their text-parity assertions are gated on those separate fixes.
- **Unverified items** stay as explicit test-gap statements in `ydk:req:extraction/legacy-decryption`,
  together with the dependencies that would close them (fixtures, yoavarad/npoi#198,
  yoavarad/npoi#199).
