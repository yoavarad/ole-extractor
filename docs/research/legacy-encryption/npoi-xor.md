# NPOI fork XOR obfuscation defects, summary-stream item B, b2xtranslator item C

Status: COMPLETE (sections appended in the order evidence completed: 1, 2, 3, 5, 4, 6, 7, 8). Issue drafts (same folder): issue-npoi-xor-xls.md, issue-npoi-xor-doc.md, issue-npoi-summary-stream.md, issue-b2x-encrypted-exception.md.

Paths used throughout:
- REPO = `C:\Users\yoava\Projects\ole-extractor`
- NPOI = `REPO\third_party\npoi` (HEAD 72ff7c756), sources under `NPOI\main\...`
- B2X = `REPO\third_party\b2xtranslator` (HEAD 930217b)
- TD = `NPOI\testcases\test-data`
- SCRATCH = `C:\Users\yoava\.claude\jobs\e469dc21\tmp\research-xor`
- Spec URL pattern: `https://learn.microsoft.com/en-us/openspecs/office_file_formats/<ms-xxx>/<guid>`

Tags: `[RUN]` = executed here, `[SPEC]` = Microsoft Open Specification, `[CODE path:line]`, `[UNVERIFIED]`.

## 0. Prototypes and scratch artefacts

- `SCRATCH\scripts\scan_filepass.py` - prints FilePass scheme per .xls (run: `python -I scripts/scan_filepass.py <dir>`).
- `SCRATCH\proto\repro\` - console harness (`repro.csproj`, `Program.cs`); args are pairs `<file> <password|->`; sets `Biff8EncryptionKey.CurrentUserPassword`, opens `new HSSFWorkbook(FileStream)`, prints cells + SummaryInformation or the exception chain. `out-orig\repro.exe` is linked against the host's unpatched `REPO\ExtractorOle\ExtractorOLE\bin\Release\net10.0\NPOI.Core.dll`.
- `SCRATCH\npoi-work\` - `git archive` export of NPOI HEAD 72ff7c756 with its own git repo (baseline commit 936443e); patched copy lives here; `git -C npoi-work diff` is the patch.
- `SCRATCH\downloads\poi\` - Apache POI trunk reference sources; `SCRATCH\downloads\spec\` - Microsoft spec pages (HTML + text).

## 1. Reproduction on unpatched NPOI.Core.dll

### 1.1 Fixture schemes (FilePass scan)

[RUN] `python -I SCRATCH/scripts/scan_filepass.py TD/spreadsheet` (olefile; reads the Workbook stream, finds record 0x002F):

| Fixture (TD\spreadsheet\) | Scheme | Decisive output |
|---|---|---|
| `xor-encryption-abc.xls` | XOR obfuscation (wEncryptionType=0), BIFF8 | `XOR key=0x514A verifier=0xCC1A`, FilePass at offset 20, size 6 |
| `60284.xls` | XOR, but BIFF5 (BOF 0x0500), 4-byte FilePass | `key=B359 ver=9A0A`; NPOI rejects earlier with `OldExcelFormatException` |
| `password.xls`, `50833.xls`, `51832.xls` | RC4 (Office binary RC4, version 1.1) | `BinaryRC4 1.1` |
| `35897-type4.xls`, `97_password.xls`, `97_rc4cryptoapi_password.xls` | RC4 CryptoAPI (version x.2), FilePass size 200 | `CryptoAPI 4.2` |

`xor-encryption-abc.xls` is the only BIFF8 XOR fixture in TD. No fixture contains an "Encrypted*" stream in its OLE directory.

### 1.2 XOR fixture, unpatched

[RUN] `SCRATCH\proto\repro\out-orig\repro.exe TD\spreadsheet\xor-encryption-abc.xls abc`

- Correct password `abc`:
  `NPOI.Util.RecordFormatException: Failed to initialise decrypting stream: this decryptor doesn't support changing the chunk size`
  - at `NPOI.HSSF.Record.Crypto.Biff8DecryptingStream..ctor` (Biff8DecryptingStream.cs:66)
  - at `NPOI.HSSF.Record.RecordInputStream..ctor` (RecordInputStream.cs:148)
  - at `RecordFactoryInputStream.StreamEncryptionInfo.CreateDecryptingStream` (RecordFactoryInputStream.cs:142)
  - at `RecordFactoryInputStream..ctor` (:220), `RecordFactory.CreateRecords` (RecordFactory.cs:464), `HSSFWorkbook..ctor(DirectoryNode, Boolean)` (HSSFWorkbook.cs:319)
  - inner: `NPOI.EncryptedDocumentException: this decryptor doesn't support changing the chunk size` at `Decryptor.SetChunkSize` (Decryptor.cs:97), called from Biff8DecryptingStream.cs:55.
- Wrong password (`wrong`): `NPOI.EncryptedDocumentException: Supplied password is invalid for salt/verifier/verifierHash` (RecordFactoryInputStream.cs:132).
- No password (`-`): `NPOI.EncryptedDocumentException: Default password is invalid for salt/verifier/verifierHash` (RecordFactoryInputStream.cs:132).

Conclusion: password verification for XOR already works (verifier check precedes stream creation); the correct password fails one step later because `XORDecryptor` does not override `SetChunkSize`. The exception surfaced to callers is `RecordFormatException`, not `EncryptedDocumentException`.

### 1.3 Controls, unpatched

[RUN] same harness:
- `password.xls` / `password` (RC4): opens, 3 sheets, SummaryInformation author `pavel`, company `Jet Infosystem`. Wrong: `Supplied password is invalid...`. None: `Default password is invalid...`.
- `35897-type4.xls` / `freedom` (CryptoAPI): opens, Sheet1 A1 = `hello there!`, author `Jeff Stevens`, lastAuthor `nstaats`. Wrong/none: same two messages.
- `50833.xls` (RC4), no password: opens (default `VelvetSweatshop` path), A1 = `test cell value`.
- `51832.xls` (RC4), no password: `Default password is invalid...` (expected by `TestBugs.Test51832`).
- `97_password.xls`, `97_rc4cryptoapi_password.xls`: password is `Password1234_` (see section 2 for rerun).

## WORK LOG (appended)

## 2. Fix proven by run (scratch copy)

Patch (unified diff, generated with `git -C SCRATCH/npoi-work diff`): `SCRATCH\npoi-xor-fix.patch`. Apply script: `SCRATCH\scripts\apply_patch.py` + `SCRATCH\scripts\XORDecryptor.patched.cs`.

[RUN] `dotnet build SCRATCH\npoi-work\testcases\main\NPOI.TestCases.Core.csproj -c Release -f net10.0` after patch: `0 Error(s)` (TreatWarningsAsErrors + analyzers on).

[RUN] `SCRATCH\proto\repro\out-patched\repro.exe TD\spreadsheet\xor-encryption-abc.xls abc` (patched `NPOI.Core.dll`, md5 52f06e6e...):
```
=== xor-encryption-abc.xls password=abc
OK sheets=1
  sheet[0] 'Sheet1' rows 0..2
    A1 [Numeric] = 1
    A2 [Numeric] = 2
    A3 [Numeric] = 3
```
This equals POI's expectation `validateContent("xor-encryption-abc.xls", "abc", "Sheet1\n1\n2\n3\n")` (POI trunk `poi/src/test/java/org/apache/poi/hssf/usermodel/TestCryptoAPI.java`, copy in `SCRATCH\downloads\poi\TestCryptoAPI.java`) and the NPOI test `TestXorEncryption.TestUserFile` (A1=1.0, A2=2.0, A3=3.0).
- wrong password: `NPOI.EncryptedDocumentException: Supplied password is invalid for salt/verifier/verifierHash` (unchanged).
- no password: `NPOI.EncryptedDocumentException: Default password is invalid for salt/verifier/verifierHash` (unchanged).

[RUN] Controls, original vs patched DLL (output files `SCRATCH\proto\controls-1.txt` = original, `controls-2.txt` = patched; `diff` prints nothing = IDENTICAL):
- `password.xls`/`password` OK (author `pavel`), wrong rejected; `35897-type4.xls`/`freedom` OK (A1 `hello there!`), none rejected;
- `97_password.xls`/`Password1234_` OK (A1 `lorem`); `97_rc4cryptoapi_password.xls`/`Password1234_` OK (A1 `lorem ipsum`, author `Microsoft Office User`), wrong rejected;
- `50833.xls` no password OK (default password path); `51832.xls` no password rejected.

### 2.1 NUnit results (scratch copy, net10.0)

[RUN] `dotnet test SCRATCH\npoi-work\testcases\main\NPOI.TestCases.Core.csproj -c Release -f net10.0 --no-build`
- Baseline (unpatched 936443e): see `SCRATCH\proto\full-main-baseline.txt` (last line quoted below).
- Patched: `Passed! - Failed: 0, Passed: 3033, Skipped: 56, Total: 3089` (`SCRATCH\proto\full-main-patched.txt`). The only delta is `TestXorEncryption.TestUserFile` moving from Skipped to Passed.
- Filter `FullyQualifiedName~TestCases.HSSF.Record.Crypto|FullyQualifiedName~TestCases.POIFS.Crypt` (patched): `Total tests: 11, Passed: 11` - `TestXorEncryption1`, `TestUserFile`, `TestBiff8DecryptingStream` (`TestReadsAlignedWithBoundary`, `TestReadsSpanningBoundary`, `TestReadHeaderUshort`, `TestReadByteArrays`), `TestBiff8EncryptionKey` (`TestCreateKeyDigest`, `TestValidateWithDefaultPassword`, `TestValidateWithSuppliedPassword`), `TestRC4`, `TestCipherAlgorithm`.

[RUN] `dotnet test SCRATCH\npoi-work\testcases\ooxml\NPOI.OOXML.TestCases.Core.csproj -c Release -f net10.0 --filter "FullyQualifiedName~POIFS.Crypt"` (patched): `Passed! - Failed: 0, Passed: 64, Skipped: 3, Total: 67` (TestDecryptor, TestEncryptor, TestCryptoFixtures, TestPasswordEncryption, TestEncryptionInfo, TestAgileEncryptionParameters, TestCryptoGaps, TestSecureTempZip, TestSignatureInfo, TestCertificateEncryption; skipped: `TestCertificateEncryption1`, `TestAgileEncryptionModes`, `InPlaceReWrite`, all pre-existing `[Ignore]`).

- Baseline last line: `Passed!  - Failed:     0, Passed:  3032, Skipped:    57, Total:  3089, Duration: 14 s - NPOI.TestCases.dll (net10.0)`


### 2.2 Second, larger XOR fixture (independent oracle)

- Fixture: `SCRATCH\downloads\msoffcrypto-fixtures\tests_inputs_xor_password_123456789012345.xls` (91136 B, md5 4578b1f33c9600c1c2d91b126e2ace20), from msoffcrypto-tool `https://raw.githubusercontent.com/nolze/msoffcrypto-tool/master/tests/inputs/xor_password_123456789012345.xls`, password `123456789012345` (15 chars, the maximum XOR password length).
- [RUN] `python -I scripts/scan_filepass.py downloads/msoffcrypto-fixtures` -> `XOR key=0x8265 verifier=0x9EB1 @off 20 size 6 BOFvers=0x0600` (BIFF8 XOR).
- [RUN] unpatched `out-orig\repro.exe <file> 123456789012345` -> `NPOI.Util.RecordFormatException: Failed to initialise decrypting stream: this decryptor doesn't support changing the chunk size` (same as 1.2).
- [RUN] patched `REPRO_DUMP=bn.tsv out-patched\repro.exe <file> 123456789012345` -> `OK sheets=1`, sheet `420`, rows 0..419, 10920 cells; `cmp bn.tsv proto\big-xlrd.tsv` -> identical, where `big-xlrd.tsv` is produced by `python -I scripts/xlrd_dump.py <file> 123456789012345 big-xlrd.tsv` (msoffcrypto-tool decrypt + xlrd read, independent implementation). This stream spans many 1024-byte chunks and CONTINUE/record boundaries, so it exercises the chunk-boundary paths that the 3-cell `xor-encryption-abc.xls` does not.

## 3. NPOI defect list for XOR obfuscation (BIFF8 Method 1), one entry per patch hunk

Line numbers are for unpatched NPOI 72ff7c756 (`NPOI\main\...`). Java references are POI trunk copies in `SCRATCH\downloads\poi\`. Spec: MS-OFFCRYPTO 2.3.7.3 `https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-offcrypto/75d2b548-18ad-4e26-8aa3-bc5fc37b89af` (text copy `SCRATCH\downloads\spec\offcrypto-2.3.7.3.txt`).

| # | Defect | Where | Effect | Correct code (patch) | Reference |
|---|---|---|---|---|---|
| D1 | `SetChunkSize` not overridden; base throws | `POIFS/Crypt/Decryptor.cs:95-97` (base), missing in `POIFS/Crypt/XOR/XORDecryptor.cs`; caller `HSSF/Record/Crypto/Biff8DecryptingStream.cs:55` | THE observed failure: `EncryptedDocumentException: this decryptor doesn't support changing the chunk size`, wrapped by `Biff8DecryptingStream.cs:66` into `RecordFormatException` [RUN 1.2] | `public override void SetChunkSize(int chunkSize) { this.chunkSize = chunkSize; }` | POI `XORDecryptor.java:97-100` `@Override setChunkSize` |
| D2 | `GetDataStream(InputStream,int,int)` declared without `override` (method hiding, CS0114) | `XOR/XORDecryptor.cs:69`; base virtual `Decryptor.cs:63-66` throws `this decryptor doesn't support reading from a stream`; caller `Biff8DecryptingStream.cs:56` calls through a `Decryptor` reference | Once D1 is fixed the call still lands on the base method and throws (masked today by D1) [CODE] | `public override InputStream GetDataStream(InputStream stream, int size, int initialPos)` | POI `XORDecryptor.java:81-85` `@Override` |
| D3 | `initialPos` passed as the stream `size` | `XOR/XORDecryptor.cs:71` -> ctor `(InputStream, long size, int chunkSize, XORDecryptor)` `:21` | `size` would be 0 (Biff8DecryptingStream passes `initialPos` 0), so `RemainingBytes()` = 0 and every read returns -1 [CODE ChunkedCipherInputStream.cs:160,212-215] | ctor `XORCipherInputStream(InputStream stream, int initialPos, int chunkSize, XORDecryptor decryptor) : base(stream, int.MaxValue, chunkSize, decryptor)` and store `initialPos` as `initialOffset` | POI `XORDecryptor.java` `super(stream, Integer.MAX_VALUE, chunkSize); this.initialOffset = initialPos;` |
| D4 | No record-relative transformation: `XORCipherInputStream` overrides only `InitCipherForBlock` | `XOR/XORDecryptor.cs:14-25` | Method 1 needs XorArrayIndex = (stream offset + record data length) % 16 per record and rotate, so a per-chunk `Cipher` cannot implement it | override `InvokeCipher(int totalBytes, bool doFinal)` (rotate left 3 = rotate right 5, XOR `xorArray[(initialOffset + recordEnd + (pos - recordStart) + i) & 0x0F]`, only for `pos + i < recordEnd`) and `SetNextRecordSize(int recordSize)` (sets `recordStart = pos`, `recordEnd = pos + recordSize`, transforms `min(recordSize, chunk.Length - (pos & mask))` bytes); full code in `SCRATCH\npoi-xor-fix.patch` hunk 3 | POI `XORDecryptor.java` `invokeCipher`, `setNextRecordSize`; MS-OFFCRYPTO 2.3.7.3 |
| D5 | Inner `XORCipher : Cipher` hides (does not override) `Update`/`DoFinal`, and its algorithm is wrong (no rotation, index = i % 16 from chunk start) | `XOR/XORDecryptor.cs:84-118`; base non-virtual `Cipher.Update(byte[],int,int,byte[])` `Cipher.cs:113` (returns int), `Cipher.DoFinal(byte[])` `Cipher.cs:136`, `DoFinal(byte[],int,int,byte[])` `Cipher.cs:146` | `ChunkedCipherInputStream.InvokeCipher` (`ChunkedCipherInputStream.cs:~262-275`) calls the 4/5-argument base overloads on `cipherImpl` (null for `XORCipher`) -> would throw `NullReferenceException`; even if dispatched, output is wrong | delete `XORCipher`; `InitCipherForBlock(...)` returns `null` (both the static helper and the stream override) | POI `XORDecryptor.java:64-73` both return `null` |
| D6 | `GetDataStream(DirectoryNode)` reads an `EncryptedPackage` stream | `XOR/XORDecryptor.cs:61-67` | XOR has no package stream (only BIFF record streams / Word streams); misleading behaviour | `throw new EncryptedDocumentException("not supported");` | POI `XORDecryptor.java:76-79` |
| D7 | `GetChunkMask()` is `private` | `POIFS/Crypt/ChunkedCipherInputStream.cs:205` | needed by the D4 subclass | `protected int GetChunkMask()` | POI `ChunkedCipherInputStream.java` `protected int getChunkMask()` |
| D8 | `ReadFully(buf, off, len)` passes `buf.Length` instead of `len` for never-encrypted records | `HSSF/Record/Crypto/Biff8DecryptingStream.cs:112` | over-reads (and desynchronises the cipher position) whenever a plain record (BOF 0x0809, InterfaceHdr 0x00E1, FilePass 0x002F; `IsNeverEncryptedRecord` `:190-211`) is read into a sub-range of a larger buffer; affects RC4/CryptoAPI too | `ccis.ReadPlain(buf, off, len);` | POI `Biff8DecryptingStream.java:112-118` |
| D9 | analyzer hygiene forced by D5/D6 removal | `XOR/XORDecryptor.cs:7` (`using System.IO` now unused), `:11` (`_length` never assigned) | build has `TreatWarningsAsErrors` | remove the using; `private readonly long _length = -1L;` | [RUN] build 0 errors |

Not defects for xls (keep as is):
- `CryptoFunctions.CreateXorArray1` hard-codes `int nRotateSize = 2` (`POIFS/Crypt/CryptoFunctions.cs:560`, comment `/* Excel = 2; Word = 7 */` `:559`). POI Java is identical (`CryptoFunctions.java:556`). For Excel this is correct together with the `rotl3` in D4: [RUN] `python -I` comparison script (section 4.2) shows `rotl2(x)` = `rotl3(spec array)` where spec array = `Ror(x)` (MS-OFFCRYPTO 2.3.7.2), so `rotl3(v) ^ rotl2(x)` = `rotl3(v ^ specArray)` = spec `DecryptData_Method1`. Word needs the spec array itself = `nRotateSize = 7` (section 4).
- `XOREncryptionInfoBuilder` is `internal` (`XOR/XOREncryptionInfoBuilder.cs:27`). The xls path instantiates it by reflection (`EncryptionInfo.GetBuilder`, `EncryptionInfo.cs:303-315`, `EncryptionMode.cs:39`), which works for internal types ([RUN] patched repro opens the file). No visibility change is needed for xls; the .doc work (section 4) adds a dedicated public API instead of exposing the builder.
- `Biff8EncryptionKey.CurrentUserPassword` and verifier logic (`XORDecryptor.VerifyPassword` `:31-48`, `CreateXorKey1`/`CreateXorVerifier1`) are already correct: wrong/no password give the expected `EncryptedDocumentException` before stream creation [RUN 1.2, 2].

Out-of-scope notes found while reading: `XOREncryptor` (`XOR/XOREncryptor.cs`) was not exercised (writing XOR files is not needed by the host).

## 5. Item B: CryptoAPI encrypted-summary stream name (verdict: NEEDED)

Spec: stream name MUST be `encryption` for xls [SPEC MS-XLS 2.1.7.6 `https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-xls/3ff73530-461c-4507-9b66-9e18aba3124e`] and doc [SPEC MS-DOC 2.1.8 `.../ms-doc/f43676cb-c68e-4839-9a3b-90b151282b6b`]; `EncryptedSummary` for ppt [SPEC MS-PPT 2.1.6 `.../ms-ppt/8faaf589-abed-4d79-a758-529ce59dfcfd`]. Text copies: `SCRATCH\downloads\spec\xls-2.1.7.6.txt`, `doc-2.1.8.txt`.

Code:
- `CryptoAPIDecryptor.GetDataStream(DirectoryNode)` hard-codes `dir.GetEntry("EncryptedSummary")` [CODE NPOI/main/POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs:209].
- `POIDocument.GetPropertySet` checks that `GetEncryptedPropertyStreamName()` (= `"encryption"`, `POIDocument.cs:535-538`) exists, then calls that hard-coded `GetDataStream(directory)` [CODE POIDocument.cs:229-238]; the exception is swallowed and logged as WARN [CODE POIDocument.cs:270-272], so `SummaryInformation` silently becomes `null`.
- The writer side is consistent with the spec: `CryptoAPIEncryptor.SetSummaryEntries(dir, GetEncryptedPropertyStreamName(), fs)` writes `encryption` [CODE POIDocument.cs:349, CryptoAPIEncryptor.cs:109]. So NPOI cannot read back its own output.
- The only unit test uses the name `EncryptedSummary` on both sides, masking the bug [CODE NPOI/testcases/ooxml/POIFS/Crypt/TestCryptoGaps.cs:18-38].
- POI Java has `public POIFSFileSystem getSummaryEntries(DirectoryNode root, String encryptedStream)` and `POIDocument` calls `dec.getSummaryEntries(dirNode, encryptedStream)` [CODE SCRATCH/downloads/poi/CryptoAPIDecryptor.java:180-243, POIDocument.java:224-229].

[RUN] `SCRATCH\proto\summary\out\summary.exe out\roundtrip.xls` (harness `SCRATCH\proto\summary\Program.cs`, unpatched host `NPOI.Core.dll`): writes an `HSSFWorkbook` with `Biff8EncryptionKey.CurrentUserPassword = "pass"` (CryptoAPI, `FilePassRecord(EncryptionMode.CryptoAPI)`, HSSFWorkbook.cs:~1628) and `SummaryInformation.Author = "Round Trip Author"`, then reopens it with the same password:
```
entries: encryption,DocumentSummaryInformation,Workbook
read cell: x
ei docPropsEncrypted: True
SummaryInformation is null: True
GetDataStream EX System.IO.FileNotFoundException: no such entry: "EncryptedSummary"
author:
```
No fixture in `TD` has an `encryption` stream ([RUN] olefile scan of all OLE files in TD: only `slideshow/Password_Protected-hello.ppt` and `Password_Protected-56-hello.ppt` contain `EncryptedSummary`; none contain `encryption`). The round-trip above is therefore the reproduction; for a third-party fixture see issue draft (spec-derived).

Unvalidated descriptor count [CODE CryptoAPIDecryptor.cs:221-222: `int encryptedStreamDescriptorCount = (int)leis.ReadUInt(); new StreamDescriptorEntry[encryptedStreamDescriptorCount]`]. [RUN] same harness, part 3: build a summary with `SetSummaryEntries`, flip ciphertext bits of the (RC4 stream-ciphered) count, call `GetDataStream`:
```
count 0x7FFFFFFF: EX System.OutOfMemoryException: Array dimensions exceeded supported range.
count 0xFFFFFFFF: EX System.OverflowException: Arithmetic operation resulted in an overflow.
count 0x000F4240: EX NPOI.Util.RuntimeException: Unexpected end-of-file
```
`OutOfMemoryException` is classified by the host as a resource fault, not a parse failure (`ExtractorOle/ExtractorOLE/Helpers/Guardrails/ParseFailure.cs:19-20`), so a 4-byte tampered field escapes per-field isolation. POI bounds it: `maxDescriptorCount = (summary.length - (streamDescriptorArrayOffset + 4)) / STREAM_DESCRIPTOR_MIN_SIZE` with `STREAM_DESCRIPTOR_MIN_SIZE = 18` (4+4+2+1+1+4+2 terminator, empty name), throws `IOException`, and collects into a `List` [CODE CryptoAPIDecryptor.java:58,195-210]. Also unvalidated: `streamDescriptorArrayOffset` (`:217-219`, negative/oversized seek) and `nameSize`/name read (`:233`).

Required API (spec in `issue-npoi-summary-stream.md`): `public NPOIFSFileSystem GetSummaryEntries(DirectoryNode root, string encryptedStream)`; `GetDataStream(DirectoryNode)` keeps `EncryptedSummary` by delegating; `POIDocument.GetPropertySet` uses `GetSummaryEntries(dirNode, encryptedStream)`.

## 4. XOR obfuscation per format

### 4.1 Applicability

| Format | XOR legal? | Method | Status in NPOI fork | Evidence |
|---|---|---|---|---|
| .xls (BIFF8) | yes | Method 1 (MS-OFFCRYPTO 2.3.7.1-2.3.7.3), FilePass `wEncryptionType=0` | broken today, fixed by the patch | [RUN] sections 1.2, 2, 2.2; [SPEC MS-XLS 2.2.10 `.../ms-xls/0f2ea0a1-9fc8-468d-97aa-9d333b72d106`, 2.5.343 XORObfuscation] |
| .xls BIFF5 (`60284.xls`) | yes (older format) | 4-byte FilePass | rejected earlier with `OldExcelFormatException`; out of scope | [RUN] 1.1 |
| .doc | yes | Method 2 (MS-OFFCRYPTO 2.3.7.4-2.3.7.6), FibBase `fEncrypted=1 && fObfuscated=1`, `lKey` = 32-bit verifier | not supported: NPOI HWPF is scratchpad-only and throws; nothing in `NPOI.Core` implements Method 2 | [SPEC MS-DOC 2.2.6.1 `.../ms-doc/79dea1e9-4dce-4fa0-8c6b-56ba37b68351`]; [CODE NPOI/scratchpad/HWPF/HWPFDocumentCore.cs:132-134] |
| .ppt | no | only RC4 CryptoAPI | n/a | [SPEC MS-PPT 2.3.7 `.../ms-ppt/b0963334-4408-4621-879a-ef9c54551fd8`: "Only the Office Binary Document RC4 CryptoAPI encryption method ... is supported"]; see `research\ppt-encryption.md` 1.1 |
| OOXML (.docx/.xlsx/.pptx) | no | Standard/Agile in `EncryptedPackage` | n/a | [SPEC MS-OFFCRYPTO 2.3.7: XOR "is supported for backward compatibility with older file formats"; OOXML uses 2.3.4.x Standard/Agile] |

### 4.2 Word Method 2, spec-derived and independently checked

Spec text copies: `SCRATCH\downloads\spec\offcrypto-2.3.7.1.txt` ... `offcrypto-2.3.7.7.txt`, `doc-2.2.6.1.txt`. URLs (prefix `https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-offcrypto/`): 2.3.7.1 `fb2d125c-1012-4999-b5ef-15a2bd4bec36`, 2.3.7.2 `72c141a7-5f27-4a60-8164-448bed90546f`, 2.3.7.4 `0752a14e-70ed-4965-b8fb-193223223445`, 2.3.7.5 `06494548-8c5c-4697-bce1-e2a9fe1c4de4`, 2.3.7.6 `5e09eb75-9135-4585-b7fb-031261c25920`, 2.3.7.7 `2831971d-6581-4d4a-a988-7f1c387b3b7d`.

Rules [SPEC]:
- Detection: FibBase flags word at `WordDocument` offset 0x0A: `fEncrypted` 0x0100 and `fObfuscated` 0x8000 both 1; `lKey` (offset 0x0E, uint32 LE) = `CreatePasswordVerifier_Method2(password)` = `(CreateXorKey_Method1 << 16) | CreatePasswordVerifier_Method1`.
- Password to bytes: for each UTF-16 char take the low byte, or the high byte if the low byte is 0; truncate to 15. (Reading MUST also try the LCID/ANSI conversion; not implemented by NPOI/POI; [UNVERIFIED] whether needed in practice.)
- XOR array: password bytes then `PadArray[0..]` up to 16 bytes; even index `Ror(b ^ KeyLow)`, odd index `Ror(b ^ KeyHigh)`, where KeyHigh/KeyLow = high/low byte of `CreateXorKey_Method1` and `Ror` = rotate right by 1 (= rotate left 7).
- Transform (encrypt == decrypt, an involution): for each byte at stream offset `o`: `k = xorArray[o % 16]`; if `b != 0 && (b ^ k) != 0` then `b ^= k`, else unchanged.
- Streams: `WordDocument` (offsets relative to stream start, bytes 0..67 stored untransformed), the table stream (`1Table` if FibBase flag 0x0200 else `0Table`) whole, and `Data` whole. All other streams/storages (summary info, ObjectPool, etc.) are NOT obfuscated.

Implementation evidence:
- [RUN] Python spec implementation `SCRATCH\scripts\make_xor_doc.py` (verifier/array/transform written from the pseudocode). For `abc` it gives verifier `0x514ACC1A`, for `123456789012345` `0x82659EB1`. These equal the BIFF8 FilePass key/verifier of the two Excel XOR fixtures (`0x514A/0xCC1A`, `0x8265/0x9EB1`, sections 1.1/2.2): independent check of 2.3.7.1/2.3.7.2/2.3.7.4.
- [RUN] Array comparison for 8 passwords (`a`, `ab`, `abc`, `password`, `Password1234_`, `123456789012345`, 14 x `x`, `tika`): 2.3.7.2 array == 2.3.7.5 array == NPOI-style construction with `nRotateSize = 7`; `nRotateSize = 2` (current `CreateXorArray1`) never matches. Vectors: `abc` -> `95999475da577857da7465a87a2f2577`; `123456789012345` -> `2a582b5b285a295d2e592a582b5b289c`.
- [RUN] Transform vector (`abc` array): input `0041994275001020` at stream offset 0 -> `00d80d37af006877`; same input at offset 100 -> `0016e115af007588`.
- [RUN] Microsoft Word (Office16, COM automation, read-only, `SCRATCH\scripts\word_open.ps1`) as oracle on files produced by `make_xor_doc.py`:
  - `xor-plain-data-abc.doc` (from `plain-data.doc` = POI `password_password_cryptoapi.doc` decrypted with msoffcrypto-tool; has `Data` stream; FibBase flags 0x93F0, lKey 0x514ACC1A; md5 ca3edb5a68327cb5faea2d7f3d6f8b6e) with `abc`: `OK chars=15 text=[This is a test | ]`, identical to `plain-data.doc` (md5 2bcbff47456e5843dc4fbcddf6fc9ee2) opened without password.
  - `xor-plain-text-abc.doc` (from msoffcrypto `rc4cryptoapi_password_plain.doc`; md5 7dd19e47eedb0ba96c86fdf1b4bdaba5) with `abc`: `OK chars=12 text=[lorem ipsum | ]`, identical to its plain twin.
  - `xor-m2-abc.doc` with `wrong`: `ERR ... The password is incorrect. Word cannot open the document.`
  - Negative control `xor-prefix0-abc.doc` (first 68 bytes also transformed): Word opens it but text is garbage (`chars=4097 text=[y<U OQqS ...`), confirming the 68-byte clear prefix.
  - Not checked by Word: `xor-plain-data-long.doc` (15-char password; the Word run hung on a password prompt for a later item). [UNVERIFIED] by oracle; its verifier is checked against the xls fixture.
  - Side effect: one hidden Word automation process (`WINWORD.EXE /Automation -Embedding`, started 18:49) could not be terminated by this agent (permission denied); the user may need to end it.
- Public fixture search: Apache POI test-data (only RC4/CryptoAPI .doc, see `research\doc-encryption.md`), msoffcrypto-tool (no XOR .doc; its `doc97.py` reads `fObfuscation` but does not decrypt it [CODE site-packages/msoffcrypto/format/doc97.py:310-312]), LibreOffice core `sw/qa/core/data/ww8/pass`, `ww6/pass`, `sw/qa/extras/ww8import/data`, `ww8export/data` ([RUN] gh api listings, no name containing pass/xor/encr/protect/crypt). GitHub code search `fObfuscated extension:doc` -> 0 results. No public XOR .doc found. LibreOffice is not installed; Word 2016+ has no XOR option on save. The fixture must be generated (script above) and is validated by Word.

### 4.3 POI Java and NPOI for .doc

- POI `HWPFDocumentCore.getEncryptionInfo()` reads `lKey` bytes of the table stream and builds `new EncryptionInfo(leis, EncryptionMode.xor)` when `fObfuscated` [CODE SCRATCH/downloads/poi/HWPFDocumentCore.java:345-371]. For XOR, `lKey` is the verifier, not a size, and `XOREncryptionVerifier(dis)` reads Excel's FilePass layout (key u16, verifier u16) from table-stream bytes [CODE XOREncryptionInfoBuilder.java:37-40, XOREncryptionVerifier.java:32]; decryption uses the Method 1 record-based `XORCipherInputStream`, whose transform only runs on `setNextRecordSize`, never called by HWPF. Conclusion from reading: POI Java does NOT correctly support XOR .doc ([UNVERIFIED] by run; no JVM installed).
- NPOI: `NPOI.Core` has no HWPF; scratchpad HWPF throws `EncryptedDocumentException("Cannot process encrypted word files!")` [CODE NPOI/scratchpad/HWPF/HWPFDocumentCore.cs:132-134]. Reusable in `NPOI.Core`: `CryptoFunctions.CreateXorVerifier2(string)` (public, matches 2.3.7.4) [CODE CryptoFunctions.cs:443-490], `CreateXorKey1` (:528), private `toAnsiPassword` (LSB-else-MSB rule) [CODE :581-598]. Missing: a Method 2 array (`CreateXorArray1` uses `Encoding.ASCII` and `nRotateSize = 2` [CODE :545-570]) and the Method 2 transform. A consumer today can verify the password (`CreateXorVerifier2(pw) == lKey`) but must re-implement 2.3.7.5/2.3.7.6 to de-obfuscate. API: `issue-npoi-xor-doc.md`.

## 6. Thread safety, default password, write protection, exception mapping

- `Biff8EncryptionKey.CurrentUserPassword` is `[ThreadStatic]` [CODE NPOI/main/HSSF/Record/Crypto/Biff8EncryptionKey.cs:151-169]. It is read once in `RecordFactoryInputStream.StreamEncryptionInfo.CreateDecryptingStream` [CODE RecordFactoryInputStream.cs:121-124], shared by all FilePass schemes, so XOR honours it exactly like RC4/CryptoAPI ([RUN] 1.2/2: wrong -> `Supplied password is invalid...`, none -> `Default password is invalid...`). Host rule already recorded: set immediately before the synchronous `HSSFWorkbook` open, clear in `finally`, no `await` in between (`REPO\docs\project-rules.md:46`).
- Default password: when the slot is null, `Decryptor.DEFAULT_PASSWORD = "VelvetSweatshop"` is used [CODE Decryptor.cs:28, RecordFactoryInputStream.cs:122-125]; same path for XOR. [RUN] RC4 `50833.xls` opens with no password. No XOR fixture uses `VelvetSweatshop`, so the XOR default-key case is [CODE] only. A supplied password on a default-key file is used instead of the default (no fallback), so "ignore a supplied password" (task #380 S-pw-xls#3) needs host logic (e.g. retry with null on `EncryptedDocumentException`) - [CODE] reading, [UNVERIFIED] by run.
- Write protection is not encryption: xls write-reservation uses the FileSharing record (0x005B, Method 1 verifier only) and the workbook stays plain; only FilePass (0x002F) is password-to-open [SPEC MS-XLS 2.2.10; MS-OFFCRYPTO 2.3.7.1 "used in XOR obfuscation as well as for document write protection"].
- Exception surface today (unpatched): XOR + correct password -> `NPOI.Util.RecordFormatException` (inner `EncryptedDocumentException`). The host's `ParseTimeErrorGuard` only catches a top-level `NPOI.EncryptedDocumentException` -> `PasswordProtectedException` [CODE ExtractorOle/ExtractorOLE/Helpers/Guardrails/ParseTimeErrorGuard.cs:104-107]; a `RecordFormatException` becomes `CorruptFileException` (`:108-111`; `FieldIsolation.cs:93` CorruptFile). So an XOR xls with the correct password is reported as corrupt today. Mapping rule for the host: top-level `EncryptedDocumentException` -> PASSWORD_PROTECTED when no password was supplied, INVALID_PASSWORD when one was; never map `RecordFormatException` to INVALID_PASSWORD (its inner `EncryptedDocumentException` means "scheme not supported", not "bad password"). After the fork fix the XOR file opens and no mapping change is needed.

## 7. Item C: b2xtranslator typed exception for encrypted input (verdict: NEEDED)

[RUN] `SCRATCH\proto\b2x\out\b2x.exe <files>` (harness references the host's built `b2xtranslator*.dll` from `REPO\ExtractorOle\ExtractorOLE\bin\Release\net10.0\`; `new WordDocument(new StructuredStorageReader(f))` + convert like `DocTextExtractor.ConvertToDocx`, or `new PowerpointDocument(reader)` like `PptToPptxConverter`):
```
=== PasswordProtected.doc              (TD/document, RC4)
EX b2xtranslator.DocFileFormat.ByteParseException: The Word document is corrupt or not a valid Word 97-2003 file.
EX System.IO.EndOfStreamException: Unable to read beyond the end of the stream.   (at StringTable.Parse, StringTable.cs:48)
=== password_tika_binaryrc4.doc / password_password_cryptoapi.doc / xor-plain-data-abc.doc   -> same two lines
=== plain-data.doc
PARSED doc fEncrypted=False chars=15
CONVERTED docx bytes=4696 text=[This is a test|]
=== Password_Protected-hello.ppt / Password_Protected-np-hello.ppt
EX System.NotSupportedException: Encryped PPT files aren't supported at this time   (at CurrentUserAtom..ctor, CurrentUserAtom.cs:114)
```
- .doc: the failure is incidental (garbage FIB/STTB offsets past the 68-byte clear prefix), wrapped as "corrupt" by `WordDocument.cs:187-190`; which exception (or none, with garbage output) depends on ciphertext, so it is not deterministic. The FIB already reads `fEncrypted` (0x0100), `fCrypto` (0x8000, spec name `fObfuscated`) and `lKey` [CODE b2x/Doc/DocFileFormat/FileInformationBlock.cs:28,35,37,451,458,460]; nothing consumes them. The full FIB is parsed from (possibly encrypted) bytes before any check could run at `WordDocument.cs:200`, so the check must peek FibBase before `new FileInformationBlock(...)` (`WordDocument.cs:198-199`).
- .ppt: `NotSupportedException` with typo "Encryped" [CODE b2x/Ppt/PptFileFormat/CurrentUserAtom.cs:113-114].
- Existing b2x exception types: `ByteParseException` (Doc), `InvalidStreamException` (`Ppt/PptFileFormat/Exceptions.cs:5`), `InvalidRecordException` (`Common/OfficeDrawing/Exceptions.cs:5`), StructuredStorage exceptions in `Common/StructuredStorage/Common/Exceptions.cs` (namespace `b2xtranslator.StructuredStorage.Common`, with `MalformedInput.IsParseFault` :157-170). None means "encrypted"; reusing them would keep the host's corrupt-file mapping.
- Host: .doc body via `DocTextExtractor.ConvertToDocx` [CODE ExtractorOle/ExtractorOLE/Handlers/DocTextExtractor.cs:50-66]; .ppt body via `PptToPptxConverter.Convert` [CODE .../FileTypeStrategy/PptToPptxConverter.cs:31-55], called lazily from `PptOpenStrategy.PopulateBodyContent` where failures become field errors (`FieldIsolation.IsIsolatable`); `StrategyIsolation.IsOpenFailure` excludes only `NPOI.EncryptedDocumentException` [CODE StrategyIsolation.cs:27-28]; `ParseTimeErrorGuard` maps only `NPOI.EncryptedDocumentException` [CODE ParseTimeErrorGuard.cs:104-107] and does not detect encrypted .doc/.ppt (`:226-242`). `DocOpenStrategy.cs:17-30` notes b2x is not yet wired for .doc metadata.
- Spec: `issue-b2x-encrypted-exception.md`.

## 8. Fork delivery conventions

- NPOI fork: PR branches must match `^(feat|fix|docs|chore|refactor|test|ci|perf|release|task)/[a-z0-9-]+$`, commits conventional `type(scope): desc` [CODE NPOI/.github/workflows/conventions.yml:10-30, NPOI/CLAUDE.md]; history uses `task/<n>-<slug>` / `chore/<n>-<slug>` branches merged by PR (`git -C third_party/npoi log --oneline -15`: `72ff7c756 Merge pull request #180 from yoavarad/task/179-...`). Tests: `dotnet test solution/NPOI.Core.Test.sln -p:NpoiDevTfm=net10.0` [CODE NPOI/AGENTS.md:92-98]; CI runs `./build.cmd Test Pack RemoveNpoiPackFromPackage` [CODE NPOI/.github/workflows/CI.yml:35]; a `Format.yml` workflow also gates PRs. Proven here: `dotnet test testcases/main/NPOI.TestCases.Core.csproj -c Release -f net10.0` (3033 passed / 0 failed / 56 skipped patched) and `testcases/ooxml/NPOI.OOXML.TestCases.Core.csproj --filter FullyQualifiedName~POIFS.Crypt` (64/0/3). Work is tracked as YDK tasks in the fork's own `.ydk/`.
- b2xtranslator fork: same conventions workflow (`b2x/.github/workflows/conventions.yml`); build/test `dotnet build b2xtranslator.sln`, `dotnet test UnitTests` [CODE b2x/CLAUDE.md:5]; CI builds `UnitTests -f net10.0 -p:CiWarningsAsErrors=true` then `dotnet test UnitTests -c Release --no-build` [CODE b2x/.github/workflows/ci.yml]; tests are NUnit 3.14 in `UnitTests/*.cs` (e.g. `MalformedInputTests.cs`, `DocOpenFile.cs`, `PptEndToEndTests.cs`), fixtures in `UnitTests/files/` copied via `<Content Include="files\...">` in `UnitTests/UnitTests.csproj:14-30`. History: `Task NNN: ... (#PR)` and conventional commits.
- Local patches so far: none kept as patch files; fixes land in the fork by PR and the host re-pins (`3ac316b chore(deps): re-pin npoi to fork main merge of npoi#177`, `54f9aeb chore(deps): bump npoi and b2xtranslator submodules to latest main`, `d359b76 chore: repin b2xtranslator to fork main d20ff458b ...`). Rule: pin only to commits reachable from the fork's main or a tag [CODE REPO/docs/project-rules.md:48]; `.gitmodules` has `branch = main` for both. Host bump: `git -C third_party/npoi fetch origin` + `git -C third_party/npoi checkout <merge-sha>`, commit the gitlink in the host as `chore(deps): re-pin npoi to fork main merge of npoi#<n>`, then build the host and run its tests.

### 5.1 Addendum: the EncryptedSummary header is enciphered

[RUN] `python -I` + olefile on `TD/slideshow/Password_Protected-hello.ppt` and `Password_Protected-56-hello.ppt`: the raw first two uint32 of `EncryptedSummary` (948 B) are `(695478643, 1862785955)` and `(1383508460, 2583146904)`. Neither is a plausible plaintext offset/size, so Office enciphers the 8-byte header (RC4 block 0 from stream offset 0), as NPOI's reader and writer do (`CryptoAPIEncryptor.cs:~170-175` writes it through the cipher stream). This contradicts the "(4, plain)" reading in `research\ppt-encryption.md` 1.1.
