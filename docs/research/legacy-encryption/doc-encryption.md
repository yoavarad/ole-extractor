# MS-DOC password decryption research (legacy Word .doc, caller-supplied open password)

Status: COMPLETE (session 2 answered Q4-Q10; see "Session 2" and "Remaining UNVERIFIED").
Evidence tags: `[RUN]` = executed here, command + decisive output; `[SPEC §]` = Microsoft Open Specifications; `[CODE path:line]` = source read; `[UNVERIFIED]` = not proven.

Paths
- Repo: `C:\Users\yoava\Projects\ole-extractor` (not modified). Submodule heads: npoi `72ff7c7`, b2xtranslator `930217b` [RUN `git -C third_party/<x> log -1`].
- Scratch: `C:\Users\yoava\.claude\jobs\e469dc21\tmp\research-doc\`
  - `proto\inspect.py` (olefile dumper), `proto\oracle.py` (msoffcrypto-tool 6.0.0 independent decryption oracle), `proto\DocDecryptProto\` (C# prototype, in progress)
  - `downloads\poi-fixtures\`, `downloads\msoffcrypto-fixtures\`, `downloads\tika-fixtures\`, `downloads\poi-src\` (POI trunk Java reference sources), `oracle-out\` (oracle plaintext)

Spec URL base: `https://learn.microsoft.com/en-us/openspecs/office_file_formats/`
- MS-DOC 2.2.6 `ms-doc/37639397-6451-427b-9cf2-01d56e927f25`; 2.2.6.1 XOR `ms-doc/79dea1e9-4dce-4fa0-8c6b-56ba37b68351`; 2.2.6.2 RC4 `ms-doc/3e9a4e92-bdfe-4379-8144-74d033892ede`; 2.2.6.3 RC4 CryptoAPI `ms-doc/55d29d8e-fd80-4c2d-a258-ffdb7ed9e236`; Encryption Stream `ms-doc/f43676cb-c68e-4839-9a3b-90b151282b6b`; FibBase `ms-doc/26fb6c06-4e5c-4778-ab4e-edbf26a545bb`; Security considerations `ms-doc/424cbebc-43ac-4c66-850b-d6320fa9a6a6`
- MS-OFFCRYPTO 2.3.1 flags `ms-offcrypto/200a3d61-1ab4-4402-ae11-0290b28ab9cb`; 2.3.5.1 CryptoAPI header `ms-offcrypto/36cfb17f-9b15-4a9b-911a-f401f60b3991`; 2.3.5.2 key `ms-offcrypto/12ec1195-af2d-44e6-8c73-003e79e635d5`; 2.3.5.3 descriptor `ms-offcrypto/b13d4d28-69b2-4ba0-884e-44449be17884`; 2.3.5.4 encrypted summary `ms-offcrypto/032c21fb-1dd9-42d6-8440-f7d74baa21e7`; 2.3.6.1 RC4 header `ms-offcrypto/76aeedb0-4d59-487f-8bd8-fb6860a60df7`; 2.3.6.2 RC4 key `ms-offcrypto/09a537cc-44ba-4ffe-af8a-e1c866ed4221`; 2.3.7.4 XOR verifier method 2 `ms-offcrypto/0752a14e-70ed-4965-b8fb-193223223445`; 2.3.7.6 XOR transform method 2 `ms-offcrypto/5e09eb75-9135-4585-b7fb-031261c25920`; Appendix A `ms-offcrypto/6b4a08cb-195a-442e-b31c-7c94624a8c29`

## Fixtures

| File (scratch `downloads\...`) | Source URL | Scheme (observed) | Password | How confirmed |
|---|---|---|---|---|
| `poi-fixtures\password_tika_binaryrc4.doc` (22016 B, sha256 9231e724...) | `https://raw.githubusercontent.com/apache/poi/trunk/test-data/document/password_tika_binaryrc4.doc` (byte-identical to Tika `testWORD_protected_passtika.doc`) | BinaryRC4 v1.1, lKey=52 | `tika` | [RUN] `python -I proto/oracle.py ... wrong tika` -> `FAIL ... 'wrong' InvalidKeyError`, `OK ... 'tika' 22016` |
| `poi-fixtures\password_password_cryptoapi.doc` (27136 B, sha256 f2d0dc59...) | `.../apache/poi/trunk/test-data/document/password_password_cryptoapi.doc` | RC4 CryptoAPI v4.2, flags 0x0C (fDocProps=1 = props NOT encrypted), keySize 128, lKey=198, has `Data` stream | `password` | [RUN] oracle `OK ... 'password' 27136` |
| `msoffcrypto-fixtures\rc4cryptoapi_password.doc` (23040 B, sha256 9c5217be...) + upstream plaintext twin `rc4cryptoapi_password_plain.doc` and `plain.doc` | `https://raw.githubusercontent.com/nolze/msoffcrypto-tool/master/tests/inputs/rc4cryptoapi_password.doc` (`tests/outputs/...plain.doc`) | RC4 CryptoAPI v4.2, flags 0x0C, keySize 128, lKey=198 | `Password1234_` | [RUN] oracle `OK ... 'Password1234_' 23040 65eb884b...`, `cmp` with upstream `rc4cryptoapi_password_plain.doc` -> identical |
| `poi-fixtures\PasswordProtected.doc` (19968 B, sha256 863d2216..., identical to `third_party/npoi/testcases/test-data/document/PasswordProtected.doc`) | `.../apache/poi/trunk/test-data/document/PasswordProtected.doc` | BinaryRC4 v1.1, lKey=52 | unknown (upstream uses it only as "encrypted, must throw": `third_party/npoi/testcases/scratchpad/HWPF/UserModel/TestProblems.cs:201`) | not confirmed; usable for no-password and wrong-password cases only |

Not found so far: XOR-obfuscated .doc; CryptoAPI .doc with encrypted document properties (`encryption` stream); CryptoAPI .doc with 40-bit key; CryptoAPI .doc with embedded OLE objects.

## Established so far (to be reorganised under Q1..Q10)

### Detection (Q1)
- FibBase is the first 32 bytes of `WordDocument`: flags word at 0x0A (0x0100 fEncrypted, 0x0200 fWhichTblStm, 0x8000 fObfuscated), lKey at 0x0E (4 bytes LE) [SPEC MS-DOC FibBase].
- fEncrypted=0 -> plain. fEncrypted=1,fObfuscated=1 -> XOR, lKey = 32-bit verifier. fEncrypted=1,fObfuscated=0 -> lKey = byte size of the EncryptionHeader at offset 0 of the table stream (`1Table` if fWhichTblStm else `0Table`); EncryptionVersionInfo 1.1 -> BinaryRC4; vMajor 2/3/4 + vMinor 2 -> RC4 CryptoAPI [SPEC MS-DOC 2.2.6, 2.2.6.2, 2.2.6.3; MS-OFFCRYPTO 2.3.5.1, 2.3.6.1].
- [RUN] `python -I proto/inspect.py <fixtures>`: both BinaryRC4 files `flags 13F0 fEncrypted 1 fObfuscated 0 lKey 52 version 1.1` (52 = 4 + 16 salt + 16 verifier + 16 verifierHash); both CryptoAPI files `lKey 198 version 4.2 flags 0000000C headerSize 126 algId 00006801 algIdHash 00008004 keySize 128 provType 1 csp 'Microsoft Enhanced Cryptographic Provider v1.0'` (198 = 12 + 126 + 60).
- The compound-file directory is fully plain for every fixture (stream names/sizes/CLSID readable) [RUN inspect.py]. Today's `ParseTimeErrorGuard.CheckCfbNotEncrypted` only looks for `EncryptionInfo`/`EncryptedPackage` entries, so it does not see an encrypted .doc [CODE ExtractorOle/ExtractorOLE/Helpers/Guardrails/ParseTimeErrorGuard.cs:226-242].

### Stream rules (Q2)
- Encrypted: `WordDocument` except its first 68 bytes, table stream except its first lKey bytes, whole `Data` stream. 512-byte blocks, block number = stream offset / 512, RC4 re-keyed per block. The keystream position counts from stream offset 0, i.e. the clear prefix still consumes keystream [SPEC MS-DOC 2.2.6.2, 2.2.6.3]. POI does exactly this: `FIB_BASE_LEN = 68`, `RC4_REKEYING_INTERVAL = 512`, `readPlain` for the prefix [CODE downloads/poi-src/HWPFDocumentCore.java].
- BinaryRC4: all other streams/storages are not encrypted [SPEC MS-DOC 2.2.6.2].
- CryptoAPI: fDocProps flag (0x08) SET means document properties are NOT encrypted (MS-OFFCRYPTO 2.3.5.1 wording; MS-DOC 2.2.6.3 words it the other way round - spec inconsistency). All CryptoAPI fixtures have flags 0x0C, plain `\x05SummaryInformation` streams and no `encryption` stream, matching MS-OFFCRYPTO; POI and NPOI both implement `IsDocPropsEncrypted = !flagDocProps` [CODE third_party/npoi/main/POIFS/Crypt/EncryptionInfo.cs:370-373; poi-src/EncryptionInfo.java:276-277].
- When properties are encrypted, Word's stream name is `encryption` [SPEC MS-DOC Encryption Stream], layout per MS-OFFCRYPTO 2.3.5.4. No fixture yet -> [UNVERIFIED] by run.

### Independent oracle output (shape of a correct decrypted file)
- [RUN] inspect.py on msoffcrypto's upstream plaintext twin: same stream sizes as the encrypted file (`1Table` 7246, `WordDocument` 4096), `flags 12F0 fEncrypted 0 lKey 0`. So decryption is length-preserving, FIB offsets into the table stream are unchanged (they already include the lKey prefix), and the tool clears fEncrypted and lKey.

### NPOI reality (Q4) - source reading done, run pending
- `EncryptionInfo(ILittleEndianInput, EncryptionMode)` is public [CODE third_party/npoi/main/POIFS/Crypt/EncryptionInfo.cs:95]; `Decryptor.SetChunkSize`, `GetDataStream(InputStream,int,int)`, `VerifyPassword` public [CODE .../Decryptor.cs:63,68,95]; `ChunkedCipherInputStream.ReadPlain` public [CODE .../ChunkedCipherInputStream.cs:126].
- NPOI HWPF (scratchpad, not in NPOI.Core.dll) has no decryption: `throw new EncryptedDocumentException("Cannot process encrypted word files!")` [CODE third_party/npoi/scratchpad/HWPF/HWPFDocumentCore.cs:132-134].
- `CryptoAPIDecryptor.GetDataStream(DirectoryNode)` hard-codes the stream name `"EncryptedSummary"` [CODE .../CryptoAPI/CryptoAPIDecryptor.cs:209]; Word needs `"encryption"`. POI has `getSummaryEntries(root, encryptedStream)` instead.
- XOR in NPOI main is not usable for .doc: `XORDecryptor.GetDataStream(InputStream,int,int)` does not override the base method and passes `initialPos` as the size [CODE .../XOR/XORDecryptor.cs:69-72]; inner `XORCipher` hides, not overrides, `Cipher.Update/DoFinal` [CODE .../XOR/XORDecryptor.cs:85-117]; `CreateXorArray1` hard-codes `nRotateSize = 2` (Excel; Word is 7) [CODE .../CryptoFunctions.cs:566]. Run pending.

### b2xtranslator (Q3/Q4)
- FIB parser reads `fEncrypted` (0x0100), names 0x8000 `fCrypto`, reads `lKey`; nothing in Doc/ or Common/ consumes them (grep), so an encrypted file is parsed as if plain [CODE third_party/b2xtranslator/Doc/DocFileFormat/FileInformationBlock.cs:451,458,460].

## PENDING
All answered in Session 2 below; only the UNVERIFIED list at the end remains.

---
## Session 2 (continuation) - results appended in order answered

### Build note
- The repo's prebuilt `ExtractorOle/ExtractorOLE/bin/Release/net10.0/ExtractorOLE.dll` is STALE (2026-11-09 08:56; lacks `DocumentExtractionResult.FieldErrors`, prototype failed with CS1061). Commits after it touch the library (f88fbe2, d8191e4, 282cdca, 1b2a78d, a276603, dc7ce12) [RUN `git log --since=...`].
- So the prototype now references a HEAD build made from a scratch copy: `research-doc\head-build\` (sources), output `research-doc\head-out\`. Copy verified identical to repo sources: [RUN] `diff -rq -x bin -x obj head-build/ExtractorOle/ExtractorOLE <repo>/ExtractorOle/ExtractorOLE` (and npoi/main, b2xtranslator/Doc) -> no output. Build: `dotnet build head-build/.../ExtractorOLE.csproj -c Release -o head-out -p:TreatWarningsAsErrors=false` -> `0 Error(s)`. The repo itself was not built or touched.

### Q5a. What today's UNMODIFIED library returns for encrypted .doc
[RUN] `proto/DocDecryptProto/out/DocDecryptProto.exe baseline <fixtures>` (full output `research-doc\baseline.out`), request = `ExtractionRequest{FileBytes, FileName, DetectedMimeType=application/msword}`:

| Fixture | DetectMimeType | Extract result |
|---|---|---|
| password_tika_binaryrc4.doc | Doc, supported=True | NO exception. textLength=0; metadata from plain SummaryInformation (creator 'Antoni', app 'Microsoft Office Word', revision 2, words=5); embeddedFiles=0; FieldErrors=1 `ExtractedText:UnexpectedFieldFailure` |
| password_password_cryptoapi.doc | same | same shape: text empty, creator 'kiwiwings', 1 FieldError `ExtractedText:UnexpectedFieldFailure` |
| rc4cryptoapi_password.doc | same | same shape: text empty, created 2018-08-15T12:35, words=1, 1 FieldError `ExtractedText:UnexpectedFieldFailure` |
| PasswordProtected.doc | same | same shape: creator 'Zsolt Koppany', company 'Intland GmbH', 1 FieldError `ExtractedText:UnexpectedFieldFailure` |
| control: rc4cryptoapi_password_plain.doc (upstream plaintext twin) | same | textLength=11 `lorem ipsum`, FieldErrors=0 |

Conclusion: today an encrypted .doc is NOT rejected; it "succeeds" with empty text and a generic field error, and leaks unencrypted metadata. No `PASSWORD_PROTECTED` error. (Root cause of the field error: see next item.)

Root cause of the field error [RUN `DocDecryptProto.exe b2x <fixture>` = `new StructuredStorageReader(ms)` + `new b2xtranslator.DocFileFormat.WordDocument(reader)`]:
- All four encrypted fixtures: `b2xtranslator.DocFileFormat.ByteParseException: The Word document is corrupt or not a valid Word 97-2003 file. <- inner System.IO.EndOfStreamException` thrown from `WordDocument..ctor` [CODE third_party/b2xtranslator/Doc/DocFileFormat/WordDocument.cs:180-190]. b2x parses ciphertext FIB fields (beyond byte 68) as offsets and runs off the stream.
- Plain twin: parses, `fEncrypted=False lKey=0 textLen=12`.
- `FieldIsolation.Classify` has no `ByteParseException` case, so it lands in `UnexpectedFieldFailure` [CODE ExtractorOle/ExtractorOLE/Helpers/FieldIsolation.cs:83-95]. The body is converted in `DocTextExtractor.ConvertToDocx` [CODE ExtractorOle/ExtractorOLE/Handlers/DocTextExtractor.cs:50-71]; metadata/embedded come from `DocOpenStrategy.OpenFrom` via NPOI POIFS + HPSF, which never looks at the FIB [CODE .../Helpers/FileTypeStrategy/DocOpenStrategy.cs:60-86].
- Relation to ADR-012 (`docs/adrs/012-password-protected-decryption.md`, status Proposed): ADR currently DEFERS doc decryption ("Custom RC4/CryptoAPI decryptor for doc/ppt. Deferred ... stays PASSWORD_PROTECTED") and says today's doc behaviour is "unverified". This run verifies it: today an encrypted .doc is NOT `PASSWORD_PROTECTED`. Doc decryption needs an ADR-012 amendment (decision 3 wording, "Alternatives" entry) - flag for the spec owner. Also `ExtractionRequest` has no `Password` property yet and no `INVALID_PASSWORD` exception exists at HEAD [RUN grep `Password|INVALID_PASSWORD` in ExtractorOle/ExtractorOLE -> only PasswordProtectedException + ParseTimeErrorGuard]; doc work depends on the ADR-012 DTO/error task landing first.

### Q4. NPOI API reality (run)
Harness: `proto\DocDecryptProto\NpoiPath.cs` (OpenInfo/Decrypt), `Malformed.cs`; outputs `research-doc\run-*.out`, `research-doc\malformed.out`. NPOI.Core.dll = HEAD build in `head-out\`.
- Working call sequence [RUN, all three fixtures, `[3]`/`[4]` lines]:
  1. `var ei = new EncryptionInfo(new LittleEndianInputStream(new ByteArrayInputStream(tableStream[0..lKey])), null);` (`null` preferred mode; BinaryRC4 -> `BinaryRC4Decryptor`, 4.2 -> `CryptoAPIDecryptor`, keySize 40/128).
  2. `bool ok = ei.Decryptor.VerifyPassword(password);` -> correct=True, wrong=False, NO exception on wrong password (returns false). Each wrong attempt needs nothing special; the same `EncryptionInfo` can be retried (prototype built a fresh one for the wrong probe).
  3. `ei.Decryptor.SetChunkSize(512);` then `var s = (ChunkedCipherInputStream)ei.Decryptor.GetDataStream(new ByteArrayInputStream(streamBytes), streamBytes.Length, 0);` then `s.ReadPlain(buf, 0, clearPrefix)` (68 for WordDocument, lKey for table, 0 for Data) and `s.Read(...)` for the rest.
  Result: NPOI output byte-IDENTICAL to the BCL-only implementation for WordDocument, 1Table and Data on all three fixtures (`[4] ... npoi=IDENTICAL`), and both identical to msoffcrypto-tool from the clear prefix on (`[5] oracle ... IDENTICAL`).
- Wrapper rule: CryptoAPI header MUST be wrapped in an `InputStream`-derived `ILittleEndianInput` (`LittleEndianInputStream`). [RUN] `new EncryptionInfo(new LittleEndianByteArrayInputStream(t,0,lKey), null)` -> CryptoAPI: `System.InvalidCastException: Unable to cast ... LittleEndianByteArrayInputStream to NPOI.Util.InputStream`; BinaryRC4: OK. Cause: `((InputStream) is1).Mark/Reset` [CODE third_party/npoi/main/POIFS/Crypt/Standard/StandardEncryptionHeader.cs:55,64] (`CryptoAPIEncryptionHeader : StandardEncryptionHeader` [CODE .../CryptoAPI/CryptoAPIEncryptionHeader.cs:25-28]). Not a blocker; use `LittleEndianInputStream`.
- No NPOI change is REQUIRED for BinaryRC4/CryptoAPI .doc (no non-public member needed). Robustness gaps found by the mutation matrix (callers must validate first; see Q9):
  - NPOI ignores `EncryptionHeader.headerSize` (reads `SizeExtra` and never uses it) and reads the CSP name until a NUL [CODE .../Standard/StandardEncryptionHeader.cs:28-29,73-80]. [RUN] headerSize=0 / 0xFFFFFFFF -> ctor OK, verify=True; CSP unterminated -> `System.Exception: Salt size != 16 !?` (generic `System.Exception`).
  - NPOI does not validate keySize: [RUN] keySize 41 / 256 / 0x7FFFFFFF -> ctor OK, verify=False (no crash, but a corrupt header is reported as wrong password).
  - fExternal (0x10) accepted -> verify=True; AES flag/algId -> `RuntimeException: Unexpected end-of-file`; unknown algId -> `EncryptedDocumentException: cipher algorithm 4660 not found`; version 4.4 -> `IOException: Not found type ...AgileEncryptionInfoBuilder`; 9.9/3.3 -> `EncryptedDocumentException: Unknown encryption`; too few bytes -> `NPOI.Util.RuntimeException: Unexpected end-of-file`.
  - NPOI never looks at lKey (caller passes the bytes): given the whole table stream it parses fine, so lKey must be validated by the caller.
- Recommendation: either path works; the BCL-only implementation (~150 lines: RC4 + MD5/SHA-1 key derivation, `DocCrypto.cs`) is equally proven and avoids NPOI's generic exceptions and InputStream plumbing. Using NPOI is acceptable if every header field is validated first (Q9) and NPOI exceptions are mapped. Builder may pick either; the prototype proves both. Suggest BCL path for doc (no `System.Exception` mapping, span-based, no per-block `Cipher` objects) - judgement call, not a defect.

### Q5. PROTOTYPE end-to-end results (the decisive run)
[RUN] `DocDecryptProto.exe run <fixture> <password> <oracle>`; full logs `research-doc\run-tika.out`, `run-cryptoapi-poi.out`, `run-cryptoapi-mso.out`. Steps: detect from FIB + table header, verify (own and NPOI), decrypt WordDocument (68 clear), table (lKey clear), Data (0 clear), clear fEncrypted (0x0100) + fObfuscated (0x8000) and set lKey=0, rebuild CFB (option A: copy of input bytes, OpenMcdf overwrite of the three streams), then `MainExtractor.Extract(new ExtractionRequest{FileBytes, FileName, DetectedMimeType="application/msword"})`.

| Fixture / password | Detect | Verify correct / wrong / empty | Decrypted text (len, first chars) | Metadata | Subfiles | FieldErrors | = oracle via same API |
|---|---|---|---|---|---|---|---|
| password_tika_binaryrc4.doc / `tika` | BinaryRc4, lKey 52, 1Table | True / False / False | 36, `This is an encrypted Word 2007 File.` | creator 'Antoni', app 'Microsoft Office Word', rev 2, words 5 | 0 | 0 | YES (textSha E57468AF3A5BFA79, identical metadata lines) |
| password_password_cryptoapi.doc / `password` | CryptoApi 4.2, flags 0C, 128-bit, lKey 198, has Data | True / False / False | 14, `This is a test` | creator 'kiwiwings', rev 1, words 2 | 0 | 0 | YES (C7BE1ED902FB8DD4) |
| rc4cryptoapi_password.doc / `Password1234_` | CryptoApi 4.2, flags 0C, 128-bit, lKey 198 | True / False / False | 11, `lorem ipsum` | created 2018-08-15T12:35, rev 1, words 1 | 0 | 0 | YES (5E2BF57D3F40C4B6 = also the upstream plaintext twin's result in Q5a) |
| PasswordProtected.doc / `wrong`, `` (empty), `password`,`Password`,`test`,`1234`,`secret` | BinaryRc4, lKey 52 | all False (own and NPOI) | n/a -> would be `INVALID_PASSWORD` (with password) / `PASSWORD_PROTECTED` (none) | - | - | - | - |

Other results from the same runs:
- Untouched streams (`\x01CompObj`, `\x05SummaryInformation`, `\x05DocumentSummaryInformation`) are byte-identical between encrypted input and oracle output -> copy unchanged [RUN `[5]`].
- FIB markers: clearing is NOT needed for b2xtranslator ([8a] markers left set -> same text sha) and zeroing the table header bytes is not needed either ([8b] same). Still clear them (oracle does: flags 13F0 -> 12F0, lKey 0) so the plain CFB is self-consistent and any later encryption check (e.g. a new FIB guard) does not re-trigger on it.
- msoffcrypto oracle decrypts the table header region too (its `1Table[0:8]` = `4a5d7c07...` garbage, our output keeps the clear header `04000200 0c000000`) [RUN olefile probe]; irrelevant to parsing because the lowest FibRgFcLcb fc with lcb>0 equals lKey (52 / 198) in every fixture: no structure overlaps the header [RUN `[6]`].
- Re-keying: per-512-byte block, block index from stream offset 0, clear prefix consumes keystream; implemented in `DocCrypto.DecryptStream` and equals NPOI `ChunkedCipherInputStream` with `SetChunkSize(512)` + `ReadPlain`.
- 40-bit CryptoAPI: own code pads the 5-byte block key with 11 zero bytes to 16, matching POI/NPOI [CODE downloads/poi-src/CryptoAPIDecryptor.java:126-129; third_party/npoi/main/POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs:166-169]. No 40-bit fixture -> [UNVERIFIED by run].

### Q6. Compound-file rebuild writer
Decryption is length-preserving (Q5), so only the content of 2-3 root streams changes; the directory, other storages and CLSIDs stay as they are.
[RUN] encrypted fixtures, `[7]` lines of `run-*.out`, and [RUN] `DocDecryptProto.exe rebuildcheck <plain docs>` (`research-doc\rebuildcheck.out`; writes WordDocument/table/Data back through each writer, diffs the full tree incl. CLSIDs, and compares `Extract` text sha, creator, EmbeddedFiles list, HasMacros, FieldErrors):

| Option | Encrypted fixtures | macro-embed/sample.doc (Macros+ObjectPool) | multi-embedding (4 objects) | Bug50936_3.doc (624 KB, Macros, ObjectPool CLSID, Data) | adversarial/unnamed-embedded-storage.doc (directory not a valid BST) | Alloc vs input |
|---|---|---|---|---|---|---|
| A OpenMcdf 3.2.0: clone input bytes, `RootStorage.Open(ms)`, `OpenStream(name)`, overwrite, `Flush` | IDENTICAL, extract matches | IDENTICAL, SAME | IDENTICAL, SAME | IDENTICAL, SAME | output fine (extract SAME via NPOI compare); OpenMcdf cannot enumerate this file at all but name lookup of root streams worked | 1.0-1.7x |
| B OpenMcdf new root + `Storage.CopyTo` | root CLSID LOST | IDENTICAL | EmbeddedFiles ORDER changed | root + ObjectPool child CLSIDs LOST | THREW `FileFormatException: Directory entries are not in a valid binary search tree` | 5.6-14x |
| C NPOI new `NPOIFSFileSystem` + recursive copy | IDENTICAL | IDENTICAL | IDENTICAL | IDENTICAL | IDENTICAL | 9.9-22x |
| C2 NPOI open + `CreateOrUpdateDocument` + `WriteFileSystem` | IDENTICAL | IDENTICAL | IDENTICAL | IDENTICAL | IDENTICAL | 3.5-15x |
| D b2xtranslator `StructuredStorageWriter` (fed from a snapshot) | IDENTICAL | IDENTICAL | EmbeddedFiles ORDER changed | IDENTICAL | failed (prototype feeds it from OpenMcdf snapshot) | 11-19x |

Decision: **C2 (NPOI `NPOIFSFileSystem` + `CreateOrUpdateDocument(stream, name)` for each decrypted root stream + `WriteFileSystem`)** is the robust choice: preserves everything, reads the malformed-BST file, and NPOI is already the reader the doc pipeline uses (`CfbFileSystems.Open`). Option A is the cheapest (one copy of the input; peak ~ input + copy + one decrypted stream) but depends on OpenMcdf, which rejects some files NPOI and b2x accept (directory BST validation) - acceptable only with a fallback, so not worth two paths. B and D are rejected (CLSID loss / order change / no gain).
Memory with C2: input bytes (caller's) + NPOI's in-memory FS view + 3 decrypted stream arrays (sum <= input size) + output MemoryStream (~input) -> about 3-4x input live at peak; measured allocation 3.5x on the 624 KB file. Release the decrypted arrays and the source FS before handing the rebuilt bytes on. Rebuilt bytes then flow through the unchanged pipeline (guards, DocOpenStrategy, DocTextExtractor).

### Q7. CryptoAPI side effects: OLE objects, ObjectPool, macros
- Spec: for RC4 CryptoAPI "The ObjectPool storage MUST NOT be present and if the file contains OLE objects, the storage objects for the OLE objects MUST be stored in the Data stream as specified in sprmCPicLocation" ... "All other streams and storages MUST NOT be encrypted" [SPEC MS-DOC 2.2.6.3 `ms-doc/55d29d8e-fd80-4c2d-a258-ffdb7ed9e236`]. sprmCPicLocation: for an encrypted (CryptoAPI) file "the value specifies an offset in the data stream which contains an FOBJH followed by an OLE object storage" [SPEC MS-DOC Character Properties `ms-doc/7022285b-9621-42e9-ad4d-4e02c115ef18`]. FOBJH = 8 bytes: `cbHeader` int16 (=8), bit0 `fCompressed` (zlib RFC1950 when 1), `cbObj` int32 = size of FOBJH + storage [SPEC MS-DOC FOBJH `ms-doc/16b5bf72-3acd-4a4c-b602-23ba525caa99`]. BinaryRC4 (2.2.6.2) has no such rule: its ObjectPool stays a normal, unencrypted storage.
- Consequences for this library:
  - Subfiles: `EmbeddedFileExtractor` only walks root `ObjectPool` [CODE ExtractorOle/ExtractorOLE/Helpers/EmbeddedFileExtractor.cs:401-433]. A decrypted CryptoAPI .doc with OLE objects therefore yields 0 EmbeddedFiles with no error. Getting them requires a NEW Data-stream walker (find sprmCPicLocation on U+0014 runs with sprmCFOle2 -> FOBJH at Data offset -> optional zlib -> CFB bytes). Recommend: out of scope for the first doc-decryption task; document the gap (embedded objects of CryptoAPI-encrypted .doc not returned) and file a follow-up. [UNVERIFIED by run: no CryptoAPI .doc with an OLE object exists in any fixture source searched.]
  - Body text: b2xtranslator builds OLE paths as `\ObjectPool\_<value>\` [CODE third_party/b2xtranslator/Doc/DocFileFormat/OleObject.cs:52-58] and treats missing `\x01Ole`/`\x01CompObj` streams as best-effort skips [CODE .../OleObject.cs:127-153]; so text conversion should survive, but `OleObjectMapping.Convert` with an empty stream set is [UNVERIFIED].
  - HasMacros: `root.HasEntryCaseInsensitive("Macros")` [CODE .../Helpers/FileTypeStrategy/DocOpenStrategy.cs:138]. Macros storage is "another storage" -> never encrypted, never relocated, so HasMacros is correct for encrypted and decrypted files alike (already correct today because the directory is plain). [SPEC 2.2.6.2/2.2.6.3 "All other streams and storages MUST NOT be encrypted"]; no encrypted+macro fixture -> run not possible.
  - Encrypted document properties (CryptoAPI with fDocProps clear in MS-OFFCRYPTO terms): `\x05SummaryInformation` absent, placeholder DocumentSummaryInformation, real properties inside the `encryption` stream (MS-OFFCRYPTO 2.3.5.4). Today `DocOpenStrategy.ReadSummaryInformation` would just return null (absent stream) -> metadata silently empty. Reading them needs CryptoAPI decryption of the `encryption` stream (NPOI `CryptoAPIDecryptor.GetDataStream(DirectoryNode)` hard-codes `"EncryptedSummary"` [CODE third_party/npoi/main/POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs:209] - cannot be used for Word without a change). Recommend: separate follow-up task; first task returns body text and leaves metadata null in this case. No fixture -> [UNVERIFIED by run].

### Q8. XOR-obfuscated .doc (doc-specific rules only; NPOI XOR defects are in `research\npoi-xor.md`)
Spec rules [SPEC MS-DOC 2.2.6.1 `ms-doc/79dea1e9-4dce-4fa0-8c6b-56ba37b68351`]:
- Detect: `fEncrypted=1 AND fObfuscated=1`. `FibBase.lKey` (offset 0x0E, 4 bytes LE) holds the 32-bit verifier from MS-OFFCRYPTO 2.3.7.4 Method 2 = `(CreateXorKey_Method1 << 16) | CreatePasswordVerifier_Method1` [SPEC `ms-offcrypto/0752a14e-...`]. lKey is NOT a header length here; there is no header in the table stream.
- Obfuscated: WordDocument (transform position relative to stream start, first 68 bytes written untransformed), the WHOLE table stream (no clear prefix), the WHOLE Data stream. All other streams/storages untouched.
- Transform Method 2: `out = in ^ XorArray[offset % 16]` unless `in == 0` or `in ^ key == 0` (byte left unchanged); array restarts every 16 bytes [SPEC `ms-offcrypto/5e09eb75-...`]. XorArray = CreateXorArray_Method1 using `XorRor` = XOR then rotate right 1 bit (equivalent to rotate-left 7, which is the "Word = 7" in POI/LibreOffice comments) [SPEC `ms-offcrypto/72c141a7-...`; CODE downloads/poi-src/CryptoFunctions.java:555-561]. NPOI's `CreateXorArray1` rotates left 2 (Excel), so it is wrong for Word (already recorded).
- Password -> single bytes: try BOTH (a) current-LCID ANSI conversion and (b) per UTF-16 unit take LSB unless 0 then MSB; truncate to 15; accept if either matches [SPEC 2.3.7.4]. (a) is environment dependent; recommend implementing (b) plus Windows-1252 as the deterministic stand-in for (a) [judgement].
- [RUN] `DocDecryptProto.exe xor` (`proto\DocDecryptProto\XorCheck.cs`): spec XorKey/Verifier1 implementations reproduce the xls FILEPASS values: `'abc': XorKey=514A MATCH; Verifier1=CC1A MATCH`; `'123456789012345': XorKey=8265 MATCH; Verifier1=9EB1 MATCH` (xls values from npoi-xor.md §1.1 and its msoffcrypto fixture). Word lKey for 'abc' would be `514ACC1A` [UNVERIFIED by Word fixture: word order of the two halves inside lKey]. Transform is an involution with the zero-skip rule: [RUN] `transform involution ... True`.
- POI is NOT a reference here: its HWPF XOR path reads `lKey` bytes of the table stream as an EncryptionInfo (`getDocumentEntryBytes(tableStrmName, -1, fibBase.getLKey())`, `EncryptionMode.xor`) [CODE downloads/poi-src/HWPFDocumentCore.java:349-352] although for XOR lKey is the verifier, and its `XORCipherInputStream` implements the Excel record-based scheme [CODE downloads/poi-src/XORDecryptor.java:125-139]. msoffcrypto-tool does not decrypt XOR .doc either [UNVERIFIED: not run].
- Fixtures: none found (POI test-data, Tika, msoffcrypto inputs searched in session 1). Word 97+ only writes XOR when the "compatible with Word 6/95" encryption type is chosen. Whole XOR-for-.doc path is [UNVERIFIED-by-fixture]. Recommendation: first release treats XOR .doc as `PASSWORD_PROTECTED` (unsupported scheme) even with a password; implement only once a real Word-authored XOR fixture exists (ask the user to produce one with Word: File > Info > Protect > Encrypt, Advanced/97-2003 "Weak encryption (XOR)" option, if their Word build still offers it).

### Q9. Limits and guards (what to validate, in order, and which error)
Rules derived from the prototype's detector (`DocCrypto.Detect`) and the mutation matrix [RUN `research-doc\malformed.out`]:
1. WordDocument absent or < 68 bytes, or `wIdent != 0xA5EC` -> not this path (let the existing pipeline report it; today that is `CORRUPT_FILE`). Read only the first 68 bytes (no full-stream allocation) for detection.
2. `fEncrypted=0` -> plain, unchanged pipeline, password ignored (ADR-012 decision 4).
3. `fEncrypted=1`, no password -> `PASSWORD_PROTECTED` (before any table-stream read). Applies to all schemes incl. XOR.
4. `fObfuscated=1` -> XOR: `PASSWORD_PROTECTED` (unsupported) in v1 (Q8).
5. Table stream (`1Table` if fWhichTblStm else `0Table`) missing -> `CORRUPT_FILE`.
6. lKey bounds: `lKey < 8` or `lKey > tableStream.Length` -> `CORRUPT_FILE`; plus a hard cap (prototype: 4096; real values 52 and 198; CryptoAPI max = 12 + headerSize + 60 where headerSize includes a CSP name of at most 255 UTF-16 chars, so <= ~600) - lKey > cap -> `CORRUPT_FILE`. This must be checked BEFORE allocating `lKey` bytes: [RUN] NPOI happily parses with lKey 0xFFFFFFFF if given the bytes; POI's `getDocumentEntryBytes(table, -1, lKey)` style allocation would be attacker-sized. Note lKey > table length is better reported as `CORRUPT_FILE` than `TRUNCATED_CONTAINER` (the CFB itself is intact; the FIB field is wrong).
7. Version: 1.1 -> BinaryRC4 (need lKey >= 52); {2,3,4}.2 -> CryptoAPI; 4.4 (Agile) / anything else -> `PASSWORD_PROTECTED` (encryption this library does not decrypt, ADR-012 decision 3 wording). [RUN] NPOI would throw `IOException "Not found type ...AgileEncryptionInfoBuilder"` / `EncryptedDocumentException "Unknown encryption"`.
8. CryptoAPI header: `headerSize` must satisfy `32 <= headerSize` and `12 + headerSize + 60 <= lKey` else `CORRUPT_FILE` (NPOI ignores headerSize). Flags: need fCryptoAPI (0x04); fExternal (0x10) or fAES (0x20) -> `PASSWORD_PROTECTED` (unsupported; NPOI accepts fExternal silently). AlgID must be 0x6801 (RC4), AlgIDHash 0x8004 or 0 -> otherwise `PASSWORD_PROTECTED`. keySize 0 -> 40; then must be 40..128 and a multiple of 8, else `CORRUPT_FILE` (NPOI reports 41/256/0x7FFFFFFF as wrong password, misleading). saltSize must be 16, verifierHashSize 20 -> else `CORRUPT_FILE`. CSP name parsed only within headerSize (never "read until NUL" across the boundary).
9. Verify password (one MD5/SHA-1 derivation, cheap; no spin count in these schemes) -> mismatch = `INVALID_PASSWORD`. Password > 255 UTF-16 units truncated to 255 (POI/NPOI behaviour [CODE downloads/poi-src/CryptoAPIDecryptor.java:144-146]).
10. Decrypt: lengths are the existing stream lengths (already bounded by `FILE_TOO_LARGE` on the input and by CFB parsing); no size field in the encryption header drives an allocation, so `OVERSIZED_NESTED_CONTENT` does not apply to the decrypt step itself. Loop is `ceil(len/512)` blocks, each one hash + one RC4 init (256-byte KSA) -> linear, ~0.1 ms for these files [RUN `[7]` timings]. No attacker-controlled loop counts.
11. After rebuild, the plain CFB goes through every existing guard (`ParseTimeErrorGuard` etc.) and the unchanged DocOpenStrategy/DocTextExtractor; any b2x failure keeps today's FieldError behaviour.
12. Cancellation: call `ExtractionCancellation.ThrowIfCancellationRequested()` between streams (pattern in DocTextExtractor.cs:53,60,68).
13. Confidentiality: password, derived keys and decrypted arrays never logged; clear key arrays (`CryptographicOperations.ZeroMemory`) after use (ADR-012 decision 8).

### Q10. Design check and ordered task breakdown
Design under test: "decrypt to a plain CFB inside this repo, feed the unchanged pipeline". Prototype verdict: WORKS for BinaryRC4 and CryptoAPI on every available fixture; output equals an independent decryptor (msoffcrypto-tool) through the same public API. Risks / corrections found:
1. Today's behaviour is not `PASSWORD_PROTECTED` but a silent "success" with empty text + `UnexpectedFieldFailure` and leaked plain metadata (Q5a). ADR-012's statement that encrypted doc stays `PASSWORD_PROTECTED` is therefore not true at HEAD; the detection guard (T1 below) is needed even if decryption were never built.
2. ADR-012 currently DEFERS doc decryption; it needs an amendment (decision 3 and "Alternatives Considered") before implementation tasks are opened. Depends on ADR-012's `Password` DTO field + `INVALID_PASSWORD` exception landing first (not present at HEAD).
3. Rebuild writer: do NOT use OpenMcdf `CopyTo` (drops CLSIDs, reorders) nor OpenMcdf as the only reader (rejects some real files: `samples/adversarial/unnamed-embedded-storage.doc` -> `Directory entries are not in a valid binary search tree`). Use NPOI (Q6, option C2).
4. CryptoAPI + OLE objects: embedded objects live in the Data stream behind FOBJH, not in ObjectPool -> 0 EmbeddedFiles after decryption, silently. Encrypted document properties (`encryption` stream) -> metadata null. Both are spec-derived gaps with no fixture; scope them out of v1 explicitly (documented limitation) or as follow-ups.
5. XOR .doc: no fixture, no trustworthy reference implementation -> keep `PASSWORD_PROTECTED` in v1.
6. Memory: decrypting needs the whole input in memory (stream/path input is buffered via `ExtractionInput.Bytes` [CODE ExtractorOle/ExtractorOLE/Helpers/Input/ExtractionInput.cs:85]) plus about 3x for the rebuild (Q6). Within the existing `FILE_TOO_LARGE` bound; note it in the spec.
7. Stale build trap for whoever verifies: the repo's `bin\Release\net10.0` is older than HEAD (Build note above); tests must run against a fresh build.

Hook point [CODE ExtractorOle/ExtractorOLE/MainExtractor.cs:81-111]: after `ParseTimeErrorGuard.Preflight` and before `ParseTimeErrorGuard.OpenOrThrow`, when `format == OfficeMimeType.WordLegacy`, replace `input` with an `ExtractionInput` over the decrypted bytes (or throw). Detection (`DetectMimeType`) needs no change: encrypted .doc already detects as `Doc` [RUN baseline] and ADR-012 decision 5 says the password is unused for doc detection.

Ordered tasks (each independently testable):

T1. Encrypted-.doc detection guard (no decryption yet).
- Files: new `ExtractorOle/ExtractorOLE/Helpers/Crypto/DocEncryption.cs` (FIB probe: first 68 bytes of WordDocument via NPOI `CfbFileSystems`/`DirectoryNode`; classify None/Xor/BinaryRc4/CryptoApi/Unsupported; validations of Q9 items 1-8); call site in `MainExtractor.Extract` (WordLegacy only); tests `ExtractorOLE.Tests/.../DocEncryptionTests.cs`; samples under `samples/encrypted/` + manifest entries (ADR-012 consequences).
- Input: CFB bytes. Output: scheme record (scheme, table name, lKey, parsed header) or exception.
- Tests: `PasswordProtected.doc`, no password -> `PASSWORD_PROTECTED`; same for the three other fixtures without password; `rc4cryptoapi_password_plain.doc` -> unchanged (`lorem ipsum`, 0 FieldErrors); every existing plain .doc sample -> unchanged; malformed matrix (mutate a copy in the test, as `Malformed.cs` does): lKey 0 / 20 / 0xFFFFFFFF / tableLen+1 -> `CORRUPT_FILE`; version 9.9 and 4.4 -> `PASSWORD_PROTECTED`; headerSize 0 / 0xFFFFFFFF, keySize 41 / 256, saltSize != 16, verifierHashSize != 20 -> `CORRUPT_FILE`; fAES / fExternal / algId != 0x6801 -> `PASSWORD_PROTECTED`; fObfuscated set -> `PASSWORD_PROTECTED`.
T2. Password verification (BinaryRC4 + CryptoAPI).
- Files: `Helpers/Crypto/DocEncryption.cs` (key derivation MS-OFFCRYPTO 2.3.6.2 / 2.3.5.2; verifier check with `CryptographicOperations.FixedTimeEquals`); BCL `MD5`/`SHA1` + ~30-line RC4, or NPOI `EncryptionInfo` over `LittleEndianInputStream(ByteArrayInputStream)` - both proven (Q4).
- Tests: `password_tika_binaryrc4.doc`+`wrong` -> `INVALID_PASSWORD`; `password_password_cryptoapi.doc`+`passwordx` -> `INVALID_PASSWORD`; `rc4cryptoapi_password.doc`+`password1234_` (wrong case) -> `INVALID_PASSWORD`; `PasswordProtected.doc`+`wrong` -> `INVALID_PASSWORD`; empty-string password == no password -> `PASSWORD_PROTECTED` (ADR-012 decision 1).
T3. Stream decryption + FIB clear + CFB rebuild + pipeline.
- Files: `Helpers/Crypto/DocEncryption.cs` (decrypt WordDocument [68 clear], table [lKey clear], Data [all]; 512-byte re-keying counted from offset 0; clear 0x0100|0x8000 at 0x0A and zero lKey at 0x0E); rebuild with NPOI `NPOIFSFileSystem` + `CreateOrUpdateDocument` + `WriteFileSystem`; `MainExtractor.cs` swaps the input; zero key material afterwards.
- Tests (expected values from [RUN] Q5): `password_tika_binaryrc4.doc`/`tika` -> text `This is an encrypted Word 2007 File.` (36 chars), creator `Antoni`, 0 FieldErrors, 0 EmbeddedFiles; `password_password_cryptoapi.doc`/`password` -> `This is a test` (14), creator `kiwiwings`; `rc4cryptoapi_password.doc`/`Password1234_` -> result equal to extracting the twin `rc4cryptoapi_password_plain.doc` (text `lorem ipsum`, same metadata); password supplied for a plain .doc -> identical to the no-password result; unit: decrypted stream SHA-256 from the clear prefix on equals the oracle's (record hashes, do not ship oracle files); rebuild helper round-trips a plain doc with Macros + ObjectPool + CLSIDs (e.g. `samples/synthetic/macro-embed/sample.doc`) with identical tree, EmbeddedFiles order and HasMacros.
T4. Docs + limitations.
- Files: `docs/adrs/012-password-protected-decryption.md` (amend), `docs/specs/extraction.md` (doc: BinaryRC4/CryptoAPI supported; XOR, CryptoAPI OLE objects, encrypted properties not), `CHANGELOG.md`, third-party notices for added fixtures (POI test-data Apache-2.0; msoffcrypto-tool fixtures MIT).
T5 (follow-up). CryptoAPI OLE objects from the Data stream (FOBJH + optional zlib) into EmbeddedFiles - blocked on a fixture.
T6 (follow-up). CryptoAPI encrypted document properties (`encryption` stream, MS-OFFCRYPTO 2.3.5.4) - blocked on a fixture.
T7 (follow-up). XOR .doc (Q8) - blocked on a Word-authored fixture.
Separate small fix found here (not decryption): `FieldIsolation.Classify` maps b2x `ByteParseException` to `UnexpectedFieldFailure`; it signals malformed input and arguably belongs in the `CorruptFile` set [CODE ExtractorOle/ExtractorOLE/Helpers/FieldIsolation.cs:83-95].

### b2xtranslator: edit required?
NOT required. [RUN] unmodified b2xtranslator (HEAD 930217b build) converts every decrypted file, with FIB markers cleared or not ([7], [8a], [8b]). Optional hardening only: make `WordDocument` refuse `FIB.fEncrypted` with a dedicated exception instead of the current `ByteParseException <- EndOfStreamException` [CODE third_party/b2xtranslator/Doc/DocFileFormat/WordDocument.cs:180-190; FileInformationBlock.cs:451-460]. Useful for b2x's own callers; this library never reaches it once T1 lands.

### NPOI: edit required?
NOT required for BinaryRC4/CryptoAPI .doc (Q4). Known defects, none blocking: CryptoAPI header ctor needs an `InputStream` (InvalidCastException otherwise, StandardEncryptionHeader.cs:55,64); ignores headerSize and reads the CSP name until NUL (StandardEncryptionHeader.cs:28-29,73-80); no keySize validation; `CryptoAPIDecryptor.GetDataStream(DirectoryNode)` hard-codes `EncryptedSummary` (CryptoAPIDecryptor.cs:209; matters only for T6); XOR array rotation is Excel-only (CryptoFunctions.cs:566; matters only for T7).

## Remaining UNVERIFIED (no fixture available)
- 40-bit CryptoAPI .doc (key padding implemented per POI/NPOI, not run).
- CryptoAPI .doc with encrypted properties (`encryption` stream), with OLE objects (FOBJH in Data), with macros.
- Any XOR .doc (spec + key/verifier math checked against xls values only; order of the two 16-bit halves in lKey unverified).
- `OleObjectMapping.Convert` behaviour when ObjectPool is absent but the text references an OLE object.
