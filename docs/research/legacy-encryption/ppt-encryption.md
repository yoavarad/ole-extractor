# Decrypting password-protected legacy .ppt in ole-extractor: research report

Status: COMPLETE for 2.1-2.10 (resumed 2026-11-10; written incrementally).

Assemblies used (deviation, deliberate): `proto\libbin\` holds the Release build from `.ydk\worktrees\291` (commit 27fcf14 = #291 CA1031, the tip of main's code before the docs-only spec PR). The repo's own `ExtractorOle\ExtractorOLE\bin\Release\net10.0\` is stale (Nov 9, 79 KB `ExtractorOLE.dll`, b2xtranslator built Debug) [RUN: `sha1sum`, `ls -la`, `strings ... | grep pdb`]. Nothing was rebuilt from the repo.
Date: 2026-11-10. Repo: `C:\Users\yoava\Projects\ole-extractor` (read only, nothing modified).

Tags: `[RUN]` executed here (command + decisive output), `[SPEC §x]` Microsoft Open Specifications,
`[CODE path:line]` source read, `[UNVERIFIED]` not confirmed.

Scratch and prototype:
- Prototype source: `C:\Users\yoava\.claude\jobs\e469dc21\tmp\research-ppt\proto\PptDec\` (`PptDec.csproj`, `Layout.cs`, `Crypto.cs`, `Program.cs`)
- Downloads (untrusted data, separate from scripts): `C:\Users\yoava\.claude\jobs\e469dc21\tmp\research-ppt\downloads\{poi-fixtures,poi-java,poi-java2,specs}`
- Spec text copies: `downloads\specs\ms-ppt-<section>.txt`, `ms-offcrypto-<section>.txt` (first line of each file is its URL)

Spec URL base: `https://learn.microsoft.com/en-us/openspecs/office_file_formats/`
- MS-PPT §2.1.1 Current User Stream `ms-ppt/76cfa657-07a6-464b-81ab-4c017c611f64`
- MS-PPT §2.1.3 Pictures Stream `ms-ppt/150a72bc-487f-467e-994e-01270dfaf9bf`
- MS-PPT §2.1.6 Encrypted Summary Information Stream `ms-ppt/8faaf589-abed-4d79-a758-529ce59dfcfd`
- MS-PPT §2.3.3 UserEditAtom `ms-ppt/3ffb3fab-95de-4873-98aa-d508fbbac981`
- MS-PPT §2.3.4 PersistDirectoryAtom `ms-ppt/d10a093d-860f-409c-b065-aeb24b830505`
- MS-PPT §2.3.5 PersistDirectoryEntry `ms-ppt/6214b5a6-7ca2-4a86-8a0e-5fd3d3eff1c9`
- MS-PPT §2.3.7 CryptSession10Container `ms-ppt/b0963334-4408-4621-879a-ef9c54551fd8`
- MS-OFFCRYPTO §2.3.5 Office Binary Document RC4 CryptoAPI Encryption `ms-offcrypto/071a449b-e453-49f3-b0d1-4738dca899e3`
- MS-OFFCRYPTO §2.3.5.1 Encryption Header `ms-offcrypto/36cfb17f-9b15-4a9b-911a-f401f60b3991`
- MS-OFFCRYPTO §2.3.5.2 Encryption Key Generation `ms-offcrypto/12ec1195-af2d-44e6-8c73-003e79e635d5`
- MS-OFFCRYPTO §2.3.5.3 Encrypted Stream Descriptor `ms-offcrypto/b13d4d28-69b2-4ba0-884e-44449be17884`
- MS-OFFCRYPTO §2.3.5.4 Encrypted Summary Stream `ms-offcrypto/032c21fb-1dd9-42d6-8440-f7d74baa21e7`
- MS-OFFCRYPTO §2.3.5.6 Password Verification `ms-offcrypto/fbfe41db-ca02-413a-a3bb-609fa0b25cd3`

---

## 0. Fixtures

All are Apache-2.0 test data of Apache POI (`https://raw.githubusercontent.com/apache/poi/trunk/test-data/slideshow/<name>`).
Passwords come from POI's own test code and its `test-data/poi-integration-exceptions.csv`
(`slideshow/Password_Protected-hello.ppt,,,hello`, `...-56-hello.ppt,,,hello`, `...-np-hello.ppt,,,hello`,
`slideshow/cryptoapi-proc2356.ppt,,,crypto`, `slideshow/ppt_with_png_encrypted.ppt,,,password`) [RUN: `curl -sL .../test-data/poi-integration-exceptions.csv | grep -i ppt`].

| Fixture | Location | Size | Password | Password source | Confirmed by running |
|---|---|---|---|---|---|
| `Password_Protected-hello.ppt` | repo `third_party\npoi\testcases\test-data\slideshow\` (sha256 identical to POI trunk copy) | 10752 | `hello` | POI `TestDocumentEncryption.java` + CSV | YES (own and NPOI verifier true; `wrongpw` false) |
| `Password_Protected-56-hello.ppt` | same folder | 10752 | `hello` | same | YES |
| `Password_Protected-np-hello.ppt` | same folder | 10752 | `hello` | same | YES |
| `cryptoapi-proc2356.ppt` | `downloads\poi-fixtures\` (POI trunk URL above) | 1341952 | `crypto` | POI `TestDocumentEncryption.cryptoAPIDecryption`, `TestHxxFEncryption.java:75` | YES (wrongpw false) |
| `ppt_with_png_encrypted.ppt` | `downloads\poi-fixtures\` (POI trunk URL above) | 26112 | `password` | POI `TestPictures.java:400-403` + CSV | YES (wrongpw false) |
| `ppt_with_png.ppt` (unencrypted twin candidate) | `downloads\poi-fixtures\`, also in repo npoi test-data | 39424 | none | n/a | n/a. NOT a byte twin: different save (3 vs 4 persist objects, 8- vs 16-byte ClientAnchors, 1 vs 2 pictures). Its PNG is byte-identical to the 2nd picture of the decrypted encrypted file (see 2.5). |

Scheme / key size per fixture [RUN: `PptDec.exe dec <fixture> <pw>`, line `DETECT:`]. All are RC4 CryptoAPI (EncryptionInfo AlgID 0x6801 RC4, AlgIDHash 0x8004 SHA-1, flags fCryptoAPI 0x04, CSP `Microsoft Base Cryptographic Provider v1.0`, ProviderType 1, headerSize 118):

| Fixture | EncryptionInfo version | flags | KeySize field -> effective | fDocProps | persist id of CryptSession10Container |
|---|---|---|---|---|---|
| Password_Protected-hello | 2.2 | 0x04 | 40 | no (EncryptedSummary present) | 6 |
| Password_Protected-56-hello | 2.2 | 0x04 | 56 | no | 7 |
| Password_Protected-np-hello | 2.2 | 0x0C | 40 | YES (plain `\x05SummaryInformation`, no EncryptedSummary) | 7 |
| cryptoapi-proc2356 | 3.2 | 0x04 | 0 -> 40 (spec: 0 means 40) | no | 13 |
| ppt_with_png_encrypted | 4.2 | 0x04 | 40 | no | 5 |

All five have exactly one UserEditAtom (recLen 0x20) and one PersistDirectoryAtom.

---

## 1. Established so far (reading phase)

### 1.1 Format rules (spec)

- Only RC4 CryptoAPI is legal for .ppt: "Only the Office Binary Document RC4 CryptoAPI encryption method ... is supported" [SPEC MS-PPT §2.3.7]. No XOR obfuscation and no "BinaryRC4" (40-bit, version 1.1) for .ppt.
- Current User stream is never encrypted; `headerToken` SHOULD be `0xF3D1C4DF` for an encrypted document, `0xE391C05F` otherwise [SPEC MS-PPT §2.1.1 / §2.3.2].
- In the PowerPoint Document stream of an encrypted file: `UserEditAtom` and `PersistDirectoryAtom` MUST NOT be encrypted; the `CryptSession10Container` record header (recVer 0xF, recType 0x2F14) is not encrypted and its data is the RC4 CryptoAPI EncryptionHeader structure; everything else is encrypted; the stream MUST contain exactly one `UserEditAtom`; `encryptSessionPersistIdRef` MUST exist (UserEditAtom recLen 0x20 instead of 0x1C) [SPEC MS-PPT §2.3.7, §2.3.3].
- Persist object decryption: block number = persist object identifier; bytes = the whole record (8-byte header + recLen) as one continuous RC4 stream [SPEC MS-PPT §2.3.7].
- Pictures stream MUST be encrypted: each field of each `OfficeArtBStoreContainerFileBlock` is decrypted separately with block number 0 (cipher restarted per field) [SPEC MS-PPT §2.3.7].
- Summary: if `fDocProps` (EncryptionInfo flags bit 0x08) is not set, `\x05SummaryInformation` MUST NOT exist, `EncryptedSummary` MUST exist, `\x05DocumentSummaryInformation` SHOULD exist but empty; if `fDocProps` is set the property streams are stored unencrypted as usual [SPEC MS-PPT §2.3.7, §2.1.6].
- Key derivation: `H0 = SHA1(salt ‖ password UTF-16LE)`, `Hfinal = SHA1(H0 ‖ block as LE32)`, key = first KeySize bits; KeySize 40 means 5 key bytes followed by 11 zero bytes (a 128-bit RC4 key); KeySize 0 MUST be read as 40 [SPEC MS-OFFCRYPTO §2.3.5.2, §2.3.5.1].
- Password verification: block 0, decrypt 16-byte EncryptedVerifier then 20-byte EncryptedVerifierHash with the same cipher state, compare SHA1(verifier) [SPEC MS-OFFCRYPTO §2.3.5.6].
- Encrypted summary stream: `StreamDescriptorArrayOffset` (4), `StreamDescriptorArraySize` (4) -- CORRECTION after running: these 8 bytes are themselves RC4-encrypted with a fresh block-0 cipher (spec step 8 "Initialize the encryption key ... block number 0x00000000"; NPOI `SeekableMemoryStream` ctor starts a block-0 cipher before reading them, `CryptoAPIDecryptor.cs:89-93,215-216`; reading them as plain fails on every fixture) -- encrypted stream data, then at that offset `EncryptedStreamDescriptorCount` + descriptors encrypted with block 0; descriptor = StreamOffset(4) StreamSize(4) Block(2) NameSize(1) flags(1, bit0 fStream) Reserved2(4) StreamName UTF-16 null terminated; each stream decrypted with its own Block [SPEC MS-OFFCRYPTO §2.3.5.3, §2.3.5.4].

### 1.2 Library facts

- `PptOpenStrategy.OpenFrom` opens the CFB with NPOI, reads metadata through `HPSFPropertiesOnlyDocument(fs).SummaryInformation`, sets `HasMacros` from a root storage named `Macros`, calls `ExtractFirstLayerEmbedded(result, fs)`, then converts with b2xtranslator (best effort; failures become field errors) [CODE ExtractorOle/ExtractorOLE/Helpers/FileTypeStrategy/PptOpenStrategy.cs].
- `EmbeddedFileExtractor.ExtractFirstLayerEmbedded(result, NPOIFSFileSystem)` returns every top-level root storage as one opaque subfile and the root `Pictures` stream as one opaque subfile (raw bytes). It does not read persist objects [CODE ExtractorOle/ExtractorOLE/Helpers/EmbeddedFileExtractor.cs:~205]. Consequence: for parity with the unencrypted twin the whole Pictures stream must be decrypted, and the summary must be rebuilt as real `\x05SummaryInformation` / `\x05DocumentSummaryInformation` streams.
- `ParseTimeErrorGuard.CheckCfbNotEncrypted` only recognises root `EncryptionInfo`/`EncryptedPackage` (OOXML wrapper) [CODE ExtractorOle/ExtractorOLE/Helpers/Guardrails/ParseTimeErrorGuard.cs:226-242]. It does not detect encrypted .ppt.
- b2xtranslator rejects the encrypted token: `case 0xF3D1C4DF: throw new NotSupportedException("Encryped PPT files aren't supported at this time");` [CODE third_party/b2xtranslator/Ppt/PptFileFormat/CurrentUserAtom.cs:113-114].
- b2xtranslator `UserEditAtom` reads the optional `EncryptSessionPersistIdRef` when bytes remain and never uses it [CODE third_party/b2xtranslator/Ppt/PptFileFormat/UserEditAtom.cs:81-101].
- NPOI (C#, `third_party/npoi/main`): `CryptoAPIDecryptor.VerifyPassword` :113, public `InitCipherForBlock(Cipher, int)` :150, `GetDataStream(DirectoryNode)` :206 with the stream name `"EncryptedSummary"` hard-coded :209 and an unvalidated descriptor count used as an array size :222 [CODE third_party/npoi/main/POIFS/Crypt/CryptoAPI/CryptoAPIDecryptor.cs]. NPOI's HSLF (`scratchpad/HSLF`) is not compiled, so NPOI has no .ppt reader that drives these classes.
- `StandardEncryptionHeader(ILittleEndianInput)` casts its input to `InputStream` for Mark/Reset when it is not a `RecordInputStream` [CODE third_party/npoi/main/POIFS/Crypt/Standard/StandardEncryptionHeader.cs:49-65]; `LittleEndianByteArrayInputStream` is not an `InputStream` [CODE third_party/npoi/main/Util/LittleEndianByteArrayInputStream.cs:26], so the EncryptionInfo payload must be wrapped as `new LittleEndianInputStream(new ByteArrayInputStream(payload))`. Runtime confirmation: CONFIRMED, see 2.4.

### 1.3 Reference implementation (Apache POI, Java)

- POI detects encryption by the UserEditAtom `encryptSessionPersistIdRef`, not by the Current User token; when the persist directory has no entry for that id it treats the file as not encrypted [CODE poi-scratchpad/.../hslf/usermodel/HSLFSlideShowEncrypted.java, downloaded copy in `downloads\poi-java\`].
- `decryptRecord`: `setChunkSize(-1)`, cipher for block = persistId, decrypt 8-byte header, read recLen, decrypt recLen more bytes with the same cipher [CODE same file].
- `decryptPicture`: fresh block-0 cipher per field: 8-byte header; for FBSE (0xF007) parts 1,1,16,2,4,4,4,1,1,1,1 then name; then rgbUid 16 bytes once or twice (twice for recInstance 0x217, 0x3D5, 0x46B, 0x543, 0x6E1, 0x6E3, 0x6E5, 0x7A9); then 34-byte metafile header for 0xF01A/0xF01B/0xF01C or 1 tag byte otherwise; then the blip data [CODE same file:233-303].
- `removeEncryptionRecord` drops the CryptSession10Container record, shrinks the UserEditAtom (ref = -1), removes the persist entry [CODE same file].

---

## 2. Answers to the ten questions

Prototype commands below run from `C:\Users\yoava\.claude\jobs\e469dc21\tmp\research-ppt\` with
`EXE=proto/PptDec/bin/Release/net10.0/PptDec.exe`, fixtures in `downloads/poi-fixtures/`. Raw outputs:
`out/runs2/*.txt` (writer/variant matrix, q3, q4), `out/final/*.txt` (final per-fixture runs), `out/today.txt` (unmodified library).

### 2.1 Detection

**Where the bytes are.** `Current User` stream (never encrypted): `CurrentUserAtom` = rh (8 bytes, recType 0x0FF6), `size` u32 (MUST be 0x14), `headerToken` u32 at offset 12, `offsetToCurrentEdit` u32 at offset 16, then name/version fields [SPEC MS-PPT §2.1.1, §2.3.2]. In `PowerPoint Document`: at `offsetToCurrentEdit` a `UserEditAtom` (rh recType 0x0FF5, recLen 0x1C or 0x20); fields at offsets from the record start: +16 `offsetLastEdit`, +20 `offsetPersistDirectory`, +24 `docPersistIdRef`, +28 `persistIdSeed`, +36 `encryptSessionPersistIdRef` (only when recLen 0x20) [SPEC MS-PPT §2.3.3; CODE proto/PptDec/Layout.cs:96-104]. `PersistDirectoryAtom` (recType 0x1772) body = entries of u32 (`persistId` low 20 bits, `cPersist` high 12 bits) each followed by `cPersist` u32 offsets [SPEC MS-PPT §2.3.4, §2.3.5; CODE Layout.cs:132-150].

**Cheapest correct algorithm** (implemented in `Layout.cs:89-128`, proven on all 5 fixtures):
1. Read `Current User` whole (fixtures: 36 to 4096 bytes). Check recType 0x0FF6 and length >= 20; take token and `offsetToCurrentEdit`.
2. Read `PowerPoint Document` whole (it has to be buffered anyway for in-place decryption).
3. Walk the edit chain: at `off`, require recType 0x0FF5 and recLen in {0x1C, 0x20}; read its persist directory; merge entries "first seen wins" (the newest edit is visited first, so later saves override earlier ones); continue at `offsetLastEdit` until 0. A visited-offset set stops cycles.
4. Take `encryptSessionPersistIdRef` from the **newest** UserEditAtom only (the one `offsetToCurrentEdit` points at). Resolve it through the merged map; the record there must be recVer/recInstance 0x000F, recType 0x2F14 (`CryptSession10Container`); payload = its recLen bytes (EncryptionInfo: version, flags, header, verifier) [SPEC MS-PPT §2.3.7].
5. Pre-validate the payload: version major 2..4, minor 2; AlgID 0x6801; AlgIDHash 0x8004 or 0; KeySize 0 -> 40, else 40..128 step 8; SaltSize 16; VerifierHashSize 20; HeaderSize fits [CODE proto/PptDec/Crypto.cs:23-46; SPEC MS-OFFCRYPTO §2.3.5.1].
Cost [RUN `dec`, `REBUILD:` line]: detect+verify+decrypt+rebuild 66-108 ms per file, cold-JIT dominated; detection itself is a handful of u32 reads.

Note: spec says an encrypted file MUST have exactly one UserEditAtom [SPEC MS-PPT §2.3.7]; all fixtures have one. Walking the chain anyway costs nothing and matches what b2x does when it builds its own persist directory, so both see the same effective (id -> offset) map.

**Authoritative signal: the structure, not the token.** POI keys only on `encryptSessionPersistIdRef` and treats "ref without persist entry" as not encrypted [CODE downloads/poi-java/HSLFSlideShowEncrypted.java, constructor]. b2xtranslator keys only on the token and throws for 0xF3D1C4DF [CODE third_party/b2xtranslator/Ppt/PptFileFormat/CurrentUserAtom.cs:113-114]. All five fixtures agree (token 0xF3D1C4DF and ref resolving to 0x2F14) [RUN `dec`, `DETECT:`]. No disagreeing sample exists, so this decision table is a design choice [UNVERIFIED against real-world files]:

| token | newest UserEditAtom ref -> 0x2F14 record | Decision |
|---|---|---|
| 0xE391C05F | absent (recLen 0x1C) | not encrypted; today's path unchanged |
| 0xE391C05F | present, resolves to 0x2F14 | encrypted (structure wins) |
| 0xE391C05F | present, id not in persist map | not encrypted (POI behaviour; b2x ignores the field, `UserEditAtom.cs:81-101`) |
| 0xF3D1C4DF | resolves to 0x2F14 | encrypted |
| 0xF3D1C4DF | absent / unresolvable / other record type / structure unreadable | no password: `PASSWORD_PROTECTED`; with password: `CORRUPT_FILE` (cannot decrypt; b2x would refuse the token anyway) |
| any other token | n/a | not touched; today b2x throws `InvalidStreamException` (CurrentUserAtom.cs:116-118) |

Rule for failures while detecting: if the token is 0xE391C05F and a structural read fails (bad recType, offset beyond stream, cycle), detection answers "not encrypted" and the existing pipeline reports whatever it reports today, so unencrypted files see no behaviour change. Once a file is known to be encrypted, failures map as in 2.8.

**No password**: once "encrypted" is decided, throw `PASSWORD_PROTECTED` before any key derivation.

**Only CryptoAPI for ppt; XOR not relevant.** "Only the Office Binary Document RC4 CryptoAPI encryption method ... is supported" [SPEC MS-PPT §2.3.7]. All five fixtures parse as RC4 CryptoAPI v2.2/3.2/4.2 [RUN `dec`, `DETECT:`]. XOR obfuscation exists only for xls (FILEPASS) and doc (FIB); no ppt structure carries it, so the ppt decryptor needs no XOR branch. Any payload failing step 5 is `CORRUPT_FILE` (spec-illegal for ppt) [design].

### 2.2 Decryption rules

- **Persist objects.** For every (id, offset) in the merged persist map except the CryptSession10Container id: new RC4 cipher keyed with block = persist id; decrypt the 8-byte header; read recLen from the *decrypted* header; continue the *same* cipher over recLen bytes [SPEC MS-PPT §2.3.7; CODE Crypto.cs:150-171]. No 512-byte re-keying (POI `setChunkSize(-1)` [CODE downloads/poi-java/HSLFSlideShowEncrypted.java decryptRecord]). [RUN `walk` on decrypted cryptoapi-proc2356]: `persistObjects=12 records=1566 structuralErrors=0`; persist objects up to 39060 bytes decrypt into exact child-length trees, which a 512-byte re-key would break. Own and NPOI ciphers give identical bytes: `DOC: own == npoi: True` on all 5.
- **Never encrypted** (left as is): the whole `Current User` stream; every `UserEditAtom`; every `PersistDirectoryAtom`; the CryptSession10Container record, header and body [SPEC MS-PPT §2.3.7]. Bytes not covered by any persist entry stay untouched [RUN `dec`: hello `decrypted bytes=6288 of doc 6566`; remainder = UserEditAtom + PersistDirectoryAtom + CryptSession10Container].
- **Same offset referenced by two ids**: reject as `CORRUPT_FILE` [CODE Crypto.cs:159]; decrypting twice would corrupt it.
- **Pictures stream: full decryption needed for parity.** The library returns the root `Pictures` stream as one opaque subfile [CODE ExtractorOle/ExtractorOLE/Helpers/EmbeddedFileExtractor.cs:226-230], and today it returns the *ciphertext* for encrypted files [RUN `today`: cryptoapi `'Pictures' size=1026524 sha256=9225FA94...`; after decryption `EB85DE0E...`]. Per record, a fresh block-0 cipher for each field: 8-byte rh; for 0xF007 (FBSE) fields 1,1,16,2,4,4,4,1,1,1,1, then the name (cbName bytes), then the embedded blip rh (8); then rgbUid 16 bytes (twice for recInstance 0x217, 0x3D5, 0x46B, 0x543, 0x6E1, 0x6E3, 0x6E5, 0x7A9); then a 34-byte metafile header for 0xF01A/0xF01B/0xF01C, else 1 tag byte; then the rest of the record as one run [CODE Crypto.cs:173-225, ported from POI decryptPicture]. Stop, leaving the remainder, when a decrypted rh type is not 0xF007 or 0xF018..0xF117 (POI behaviour). [RUN] own == NPOI on both fixtures with Pictures; decrypted blips start `89504e470d0a1a0a` (PNG); picture #2 of `ppt_with_png_encrypted` is byte-identical to the only picture of `ppt_with_png.ppt` (sha1 prefix `711f23417e83917f`, `python -I proto/scripts/piccmp.py`).
- **Encrypted summary (fDocProps clear).** `EncryptedSummary` = [8-byte header, encrypted with a fresh block-0 cipher: arrOff, arrSize] [stream data] [at arrOff, arrSize bytes encrypted with a fresh block-0 cipher: count u32, then descriptors: StreamOffset u32, StreamSize u32, Block u16, NameSize u8 (chars), flags u8, Reserved u32, name UTF-16 NameSize chars, u16 terminator]; each stream is decrypted with a fresh cipher for its own Block, one run, no re-key [SPEC MS-OFFCRYPTO §2.3.5.3-2.3.5.4; CODE Crypto.cs:228-257]. Fixtures: `\x05SummaryInformation` block 0, `\x05DocumentSummaryInformation` block 1, sizes 72..4096 [RUN `dec`]. Rebuild: delete `EncryptedSummary`; write each descriptor as a root stream under its name (the `\x05` prefix is part of the stored name), replacing the empty `\x05DocumentSummaryInformation` placeholder. [RUN] own streams are byte-identical to NPOI `GetDataStream(DirectoryNode)` output (sha1 F1FEF6B5..., D03E04BB...), and the library reads Title/Creator/dates from them (2.5). The library reads metadata only through NPOI `HPSFPropertiesOnlyDocument` [CODE PptOpenStrategy.cs:111-120], so nothing else is needed. Descriptors flagged as storage (flags bit0 = 0) occur in no fixture; recommend skipping them without error [UNVERIFIED].
- **fDocProps set ("np" fixture).** No `EncryptedSummary`; property streams are plain and are copied untouched. [RUN] np: metadata present even today (`out/today.txt`) and after decryption. NPOI `GetDataStream` throws `FileNotFoundException: no such entry: "EncryptedSummary"` here [RUN `npoisummary` np], so the code must branch on the stream's presence rather than call NPOI blindly. Recommend: decide by presence of `EncryptedSummary`; a flag/presence mismatch is tolerated (no fixture shows one) [design].

### 2.3 What to rewrite for b2xtranslator (experiment)

Two variants, both run through b2x reader + converter and the library [RUN, `out/runs2/q3-*.txt`, `out/final/*`]:
- **token** (in place): decrypt persist objects in place and set `headerToken` = 0xE391C05F in a copy of `Current User`. UserEditAtom (recLen 0x20), the persist entry and the CryptSession10Container record stay as they are. Result: b2x parses (`persist records enumerated=6 [..., UnknownRecordx1]`; the 0x2F14 record becomes an `UnknownRecord` the converter ignores); library text/metadata/counts identical to compact on all 5 fixtures (hello 147 chars, np 177 chars, 1 slide, 1 notes slide).
- **compact** (POI `removeEncryptionRecord`): drop the record, rewrite one PersistDirectoryAtom, shrink UserEditAtom to 0x1C, fix `offsetToCurrentEdit` [CODE Program.cs `Compact`]. Same library outcome, about 25 more lines, and it rewrites offsets.
Conclusion: **only the token must be rewritten.** A 32-byte UserEditAtom is tolerated (b2x reads the optional field and never uses it, `UserEditAtom.cs:81-101`), and so are the persist entry and the record. Use the in-place token variant: persist offsets do not move, so nothing else needs fixing. A third variant, zeroing the record's recVer ("neutral"), is unnecessary and gives the same result [RUN q3-neutral].

### 2.4 NPOI API reality [RUN `info`, `npoisummary`, `fuzzsum`; output `out/runs2/q4.txt`]

- `new EncryptionInfo(new LittleEndianByteArrayInputStream(payload), EncryptionMode.CryptoAPI)` throws `InvalidCastException: Unable to cast ... LittleEndianByteArrayInputStream to NPOI.Util.InputStream` (from `StandardEncryptionHeader` ctor [CODE third_party/npoi/main/POIFS/Crypt/Standard/StandardEncryptionHeader.cs:49-65]). It works with `new LittleEndianInputStream(new ByteArrayInputStream(payload))`: `v2.2 flags=0x4 keySize=40 cipher=RC4 hash=SHA-1 decryptor=CryptoAPIDecryptor`. Optional NPOI fix: in that ctor, do the Mark/Reset only when the input `is InputStream`. Not blocking.
- `Decryptor.VerifyPassword(pw)`: true for the right password and false for `wrongpw` on all 5 [RUN].
- `CryptoAPIDecryptor.InitCipherForBlock(null, block)` is public and returns a usable `Cipher`; `cipher.Update(buf, off, len, buf, off)` works in place. Each persist object decrypted with block = persist id is byte-identical to the hand-written RC4 [RUN `DOC: own == npoi: True`].
- `GetDataStream(DirectoryNode)` works for ppt as is when `EncryptedSummary` exists (returns a serialized CFB holding the two property streams, identical to own output) but: (a) the hard-coded `"EncryptedSummary"` gives `FileNotFoundException` when fDocProps is set [CODE CryptoAPIDecryptor.cs:209; RUN np]; (b) the descriptor count is unvalidated: count 0x7FFFFFFF gives `OutOfMemoryException: Array dimensions exceeded supported range` at :222, 0xFFFFFFFF gives `OverflowException` at :222, 1000 gives `NPOI.Util.RuntimeException: Unexpected end-of-file` at :227 [RUN `fuzzsum --count`]. `OutOfMemoryException` is not a parse fault in this library and would escape field isolation [design risk]. (c) It serializes a whole extra CFB. Recommendation: do **not** use `GetDataStream`; parse the summary with ~30 own lines (count bounded by `(arrSize-4)/14`, offsets checked), using NPOI only for `InitCipherForBlock`. If NPOI is patched instead: validate `count` against `streamDescriptorArraySize` before `new StreamDescriptorEntry[count]` (:222), and `streamOffset` >= 8 and `+ streamSize <= streamDescriptorArrayOffset` (:241-245).
- NPOI cipher vs hand-written: both proven equal. The NPOI path owns ~0 crypto code (matches ADR-012 "reuse NPOI main's primitives"); hand-written is ~60 lines (RC4 + SHA-1 derivation, `Crypto.cs:49-121`) [RUN own == npoi everywhere]. Recommend NPOI `EncryptionInfo` + `CryptoAPIDecryptor` (`VerifyPassword`, `InitCipherForBlock`) with the payload wrapped as above, plus own payload pre-validation (2.1 step 5) so malformed payloads map to `CORRUPT_FILE` rather than to NPOI exceptions.

### 2.5 Prototype end to end

Pipeline in `proto/PptDec/Program.cs` `Dec`: detect -> verify (own and NPOI) -> decrypt doc / Pictures / summary (own and NPOI, compared) -> token rewrite (or compact) -> rebuild CFB in memory -> b2x reader + converter probe -> `MainExtractor.Extract(new ExtractionRequest { FileBytes, FileName = "fixture.ppt", DetectedMimeType = "application/vnd.ms-powerpoint" })` through `ServiceRegistration.Register` [CODE Program.cs `RunLibrary`]. Recommended configuration: `--variant token --writer npoicopy` (`out/final/*-token-npoicopy.txt`).

| Fixture (password) | Today: unmodified library on the encrypted file [RUN `today`, `out/today.txt`] | Decrypted rebuild through `MainExtractor.Extract` [RUN `out/final`] | Root cause of any gap |
|---|---|---|---|
| Password_Protected-hello (`hello`) | no throw; text 0; metadata empty; 0 subfiles; 3 FieldErrors `UnexpectedFieldFailure` (SlideCount, NotesSlideCount, ExtractedText), caused by b2x `NotSupportedException` | text 147 chars ("This is a protected document / It only has the one slide / It does have notes / The password is "hello" / Just for the hell of it, we have / some notes"); Title, Creator, LastModifiedBy, Created, Modified, App, Rev present; SlideCount 1, NotesSlideCount 1; 0 subfiles; 0 FieldErrors | none |
| Password_Protected-56-hello (`hello`) | as above | same 147 chars, metadata, counts; 0 FieldErrors | none |
| Password_Protected-np-hello (`hello`) | as above, **but metadata present** (property streams unencrypted) | 177 chars (adds "Properties are not protected"); metadata; 1/1; 0 FieldErrors | none |
| cryptoapi-proc2356 (`crypto`) | 3 FieldErrors; `Pictures` subfile is **ciphertext** (1026524 bytes) | metadata Title 'RC4 CryptoAPI Encryption', Creator 'Dominic Salemno'; Pictures subfile decrypted (7 valid blips); **text 0, 3 FieldErrors** | b2xtranslator defect unrelated to encryption: `KeyNotFoundException 'fillShadeType'` at `FillMapping.cs:265` (OPT has `fillShadeColors` without `fillShadeType`; 4 shapes, `walk`). With that fix emulated on the parsed model (`--patch`) conversion succeeds: 1 slide, slide text "Dominic Salemno". |
| ppt_with_png_encrypted (`password`) | 3 FieldErrors; `Pictures` subfile is ciphertext (16588 bytes) | Pictures decrypted (2 PNGs; #2 byte-identical to the twin's PNG); metadata empty, which is correct (the decrypted property sets hold only the codepage: `olefile getproperties` -> `{1: 0}`); **text 0, 3 FieldErrors** | b2xtranslator defects unrelated to encryption, both triggered by POI-written records: (1) 16-byte `ClientAnchor` (RectStruct) read as an 8-byte SmallRect -> invalid anchor -> null `ChildAnchor` -> `NullReferenceException` at `ShapeTreeMapping.cs:2302`; (2) with (1) emulated, `InvalidOperationException: Nullable object must have a value` in `TextMasterStyleMapping.writepPr` (which nullable [UNVERIFIED]). The decrypted document is structurally perfect (`walk`: 187 records, 0 errors; text atoms include "Here is a PNG image:"). |
| ppt_with_png.ppt (unencrypted, not a true twin) | text 22 chars "H / ere is a PNG image:"; Title 'Slide 1', Creator 'Daniel Noll'; 1 slide; Pictures 1385 bytes; 0 FieldErrors | n/a | see §0 |

Wrong password (`wrongpw`) on hello, cryptoapi, png: `VERIFY: own=False npoi=False`, nothing decrypted -> `INVALID_PASSWORD` [RUN]. Encrypted file with no password: must become `PASSWORD_PROTECTED`; today it is *not*: it returns a partial result with FieldErrors, leaks ciphertext as a `Pictures` subfile (cryptoapi, png), and for "np" returns real metadata.

All gaps above are encryption-independent: each reproduces on the decrypted, structurally valid plaintext and disappears when the defect is emulated (b2x `FillMapping.cs:265` and `:558` missing key; `ClientAnchor.cs:20-47` ignores 16-byte anchors; `TextMasterStyleMapping.writepPr` nullable). Two further failures were CFB-writer interop defects, see 2.6.

### 2.6 Compound-file rebuild writer

Matrix [RUN `dec ... --variant {token,compact} --writer {npoi,npoicopy,openmcdf}`, `out/runs2/*`, `out/final/*`]:

| Writer | How | Result through b2x `StructuredStorageReader` | Root cause |
|---|---|---|---|
| OpenMcdf 3.2.0 (`RootStorage.Create` + copy) | `Program.cs RebuildOpenMcdf` | **fails** on cryptoapi-proc2356: `InvalidSectorInChainException` at `MiniFat.ReadSectorsUsedByMiniStream` | When no stream is below 4096 bytes there is no mini stream; OpenMcdf writes the root entry's start sector as 0xFFFFFFFF (`root.start 0xffffffff root.size 0`), NPOI and Office write 0xFFFFFFFE [RUN `python -I proto/scripts/cfbhdr.py`]. b2x treats only ENDOFCHAIN as "no mini stream" [CODE third_party/b2xtranslator/Common/StructuredStorage/Reader/MiniFat.cs:117] and walks a chain from 0xFFFFFFFF. Works on the 4 small fixtures. |
| NPOI in place (`CreateOrUpdateDocument` on the opened source fs, `WriteFileSystem`) | `RebuildNpoiInPlace` | **fails** on Password_Protected-hello: `ChainSizeMismatchException: The number of sectors used by MiniStream does not match the specified size` | Replacing streams shrinks the root mini stream size (1152 -> 896 bytes) but leaves its FAT chain at 3 sectors (needs 2) [RUN olefile chain count `root chain sectors 3 needed 2`]; b2x checks this strictly. |
| **NPOI copy** (new `NPOIFSFileSystem`, root CLSID copied, `EntryUtils.CopyNodeRecursively` for every untouched entry, `CreateDocument` for replaced streams, `WriteFileSystem`) | `RebuildNpoiCopy` | **works on all 5**, both variants | same technique the library already uses for embedded storages [CODE EmbeddedFileExtractor.cs:330-339] |

Preservation: root CLSID is copied explicitly (`{64818D10-4F9B-11CF-86EA-00AA00B929E8}` survives [RUN `REBUILT root:`]); `CopyNodeRecursively` copies child storage CLSIDs [CODE third_party/npoi/main/POIFS/FileSystem/EntryUtils.cs:37-38] and all streams byte for byte. Not preserved: entry timestamps and state bits (irrelevant to every reader in the pipeline) [UNVERIFIED that NPOI drops them; not needed]. Entry order changes (NPOI sorts); harmless. The rebuilt file can be smaller (10752 -> 10240 bytes) because the small streams move into the mini stream.

Memory [RUN `REBUILD:` line, `GC.GetTotalAllocatedBytes`]: npoicopy + token on cryptoapi-proc2356 (1.34 MB) allocated 19.5 MB (14.9x input); on 10 KB files ~790 KB (fixed overhead). The prototype does every step twice (own and NPOI decryption, two NPOI opens of the source), so a lean implementation should allocate well under that: about input + doc copy + Pictures copy + new filesystem + serialized output, i.e. ~4-6x input [UNVERIFIED estimate]. Peak live memory is bounded by the CFB guard already applied to the input (`FILE_TOO_LARGE`); decryption never grows any stream (output streams have exactly the input sizes, except `EncryptedSummary` -> two smaller streams).

### 2.7 Embedded objects, pictures, macros: decryption coverage

- **Pictures**: root `Pictures` stream returned whole as one subfile [CODE EmbeddedFileExtractor.cs:226-230]. Needs full decryption (2.2) for parity; proven [RUN piccmp].
- **Embedded objects**: the library returns every top-level root **storage** as an opaque subfile [CODE EmbeddedFileExtractor.cs:213-220]. In .ppt, OLE objects live inside `PowerPoint Document` as `ExOleObjStg` persist objects (zlib-compressed), not as root storages, so the library does not extract them today (no fixture has one) [UNVERIFIED that a real ppt never carries root storages; MS-PPT puts OLE data in persist objects]. After the persist-object decryption they are plaintext like everything else; no extra coverage. Any root storages of an encrypted file are not encrypted by the scheme and are copied verbatim by the NPOI-copy rebuild.
- **Macros**: `HasMacros` = root storage named `Macros` exists [CODE PptOpenStrategy.cs:30,87-88]. The directory is plaintext, so the decision is the same on the encrypted file and on the rebuild; no decryption coverage needed. (Side observation, not encryption related: in .ppt the VBA project is a persist object referenced by `VBAInfoAtom`, so this storage check may never be true for real .ppt files [UNVERIFIED, no macro .ppt sample].)
- **Text, slide counts**: come from the b2x conversion of the rebuilt plaintext; covered by the persist-object decryption.
- **Metadata**: from `\x05SummaryInformation` via NPOI HPSF; covered by the summary rebuild.

### 2.8 Limits, guards and error mapping

All reads below are inside streams already loaded and bounded by the CFB guards (`FILE_TOO_LARGE`, CFB `TRUNCATED_CONTAINER`, nesting caps) that run before the strategy. Decryption allocates nothing larger than the streams it already holds, so no new `OVERSIZED_NESTED_CONTENT` trigger is needed; the 1 GB per-stream cap of `nesting-depth-guard` applies unchanged to the input [CODE .ydk/components/nfr/extraction/nesting-depth-guard.yaml:5].

| Check (encrypted file, password supplied) | Bound | Error |
|---|---|---|
| no password | n/a | `PASSWORD_PROTECTED` |
| `Current User` missing, < 20 bytes, recType != 0x0FF6 | must hold 20 bytes | `CORRUPT_FILE` |
| `offsetToCurrentEdit` / `offsetLastEdit` / `offsetPersistDirectory` + 8 + recLen beyond `PowerPoint Document` length | stream length | `CORRUPT_FILE` (the CFB is intact; a stream-internal offset is not container truncation) |
| UserEditAtom recType != 0x0FF5 or recLen not 0x1C/0x20 | exact | `CORRUPT_FILE` |
| edit chain cycle (offset seen twice) | visited set | `CORRUPT_FILE` |
| edit chain length | at most stream length / 36 (each atom >= 36 bytes) holds automatically with the visited set; an explicit cap (e.g. 10,000 as in `Layout.cs:93`) is cheap | `CORRUPT_FILE` |
| PersistDirectoryAtom recType != 0x1772; entry `4 + 4*cPersist` past atom end | atom length; cPersist <= 4095 by encoding | `CORRUPT_FILE` |
| persist id range | 20-bit by encoding (max 0xFFFFF) | none needed |
| persist offset + 8 beyond stream, or decrypted recLen runs past stream end | stream length | `CORRUPT_FILE` |
| two ids share one offset | set | `CORRUPT_FILE` |
| encrypt ref missing / not 0x2F14 / payload past stream | see 2.1 | `CORRUPT_FILE` |
| EncryptionInfo: version, AlgID, AlgIDHash, KeySize, HeaderSize, SaltSize 16, VerifierHashSize 20 | 2.1 step 5 | `CORRUPT_FILE` |
| verifier mismatch | n/a | `INVALID_PASSWORD` (before any content is decrypted) |
| `EncryptedSummary` < 8 bytes; arrOff < 8 or arrOff + arrSize > length; arrSize < 4 | stream length | `CORRUPT_FILE` |
| descriptor count > (arrSize - 4) / 14; descriptor or name past array end; stream offset < 8 or offset + size > arrOff | array/stream bounds | `CORRUPT_FILE`. Never use NPOI `GetDataStream` (unvalidated count at `CryptoAPIDecryptor.cs:222` -> `OutOfMemoryException`/`OverflowException`, RUN 2.4) |
| Pictures: rh recLen past stream end; FBSE name or field past record end | stream length | `CORRUPT_FILE`, or (recommended, since Pictures is a separate leaf) `CORRUPT_FILE` FieldError on the Pictures subfile only, keeping text/metadata. Design choice [UNVERIFIED which the project prefers]; the prototype throws. |
| Pictures: unknown record type | n/a | stop decrypting and keep the rest as is (POI behaviour), no error |

Truncated CFB itself (sector count beyond file) is already `TRUNCATED_CONTAINER` from the existing preflight [CODE ParseTimeErrorGuard.cs:57-59]. All new errors must avoid the password, derived keys and decrypted bytes in messages (ADR-012 decision 8).

### 2.9 Design risks and task breakdown

**Design check: "decrypt to a plain CFB here, feed the unchanged pipeline" holds.** Evidence: on all 5 fixtures the rebuilt bytes go through the unchanged `MainExtractor.Extract` and give the same kind of result as an unencrypted file, and every remaining gap is an encryption-independent b2x defect (2.5). Risks the prototype exposed:
1. **CFB writer choice is load-bearing.** OpenMcdf output and NPOI in-place output both break b2x's strict reader (2.6). Use the NPOI copy writer, and keep a regression test on cryptoapi-proc2356 (no mini stream) and hello (mini stream shrinks).
2. **Input substitution point.** `MainExtractor.Extract` uses `input` for open (`MainExtractor.cs:111`) and for the text fallback `input.Bytes` (`:131`, `:324`). The plaintext must replace the input for both, else the fallback re-converts the encrypted bytes. Telemetry size and `FileSizeBytes` should keep the original input [design].
3. **Stream input.** Decryption needs the whole `PowerPoint Document` (and Pictures) in memory; stream input (ADR-008) must be buffered for encrypted ppt only. The Pictures stream is the large one (1 MB of the 1.3 MB fixture).
4. **Today's behaviour is a silent partial result plus ciphertext leak**, not `PASSWORD_PROTECTED` (2.5). Even with no decryption work, detection alone fixes the spec violation of `.ydk/components/req/extraction/password-decryption.yaml:108`.
5. **b2x conversion gaps** mean 2 of 5 fixtures yield no text after correct decryption. Acceptance tests for decryption must not assert text on those two unless the b2x tasks (2.10) land first; assert decrypted Pictures/metadata instead.
6. **Spec is currently "deferred"** for ppt (ADR-012 decision 3, `password-protected.yaml` trigger "whether or not a password was supplied"). Implementing decryption requires amending ADR-012, `password-decryption.yaml` (line 108 test), `password-protected.yaml` and `invalid-password.yaml` triggers first.

**Ordered tasks** (files under `ExtractorOle/ExtractorOLE/` unless stated):

| # | Task | Files | Input -> output | Tests (fixture, password -> expected) |
|---|---|---|---|---|
| 0 | Spec amendment: ppt moves from "deferred" to "decrypted" | `docs/adrs/012-...md`, `.ydk/components/req/extraction/password-decryption.yaml`, `error/extraction/password-protected.yaml`, `invalid-password.yaml` | n/a | spec-verify passes |
| 1 | Add fixtures | `samples/encrypted/` (+ manifest entries with `password`; twin only where real) | 5 POI files (Apache-2.0, attribution in THIRD-PARTY-NOTICES) | manifest loads |
| 2 | `PptEncryptionInfo` reader: detect per 2.1 (pure function over `Current User` + `PowerPoint Document` bytes) | new `Helpers/Decryption/PptEncryptionLayout.cs` (~150 lines) | bytes -> {NotEncrypted, Encrypted(payload, persist map, encId), Malformed(reason)} | each fixture -> Encrypted with encId 6/7/7/13/5; `ppt_with_png.ppt` -> NotEncrypted; synthetic: cycle, offset past end, recLen 0x1D, ref to non-0x2F14 -> Malformed |
| 3 | Fail fast: encrypted ppt + no password -> `PASSWORD_PROTECTED` (before b2x) | `ParseTimeErrorGuard.cs` or `PptOpenStrategy.cs` | | 5 fixtures, no password -> `PASSWORD_PROTECTED`; `ppt_with_png.ppt` unchanged result; `DetectMimeType` still `Ppt` for all |
| 4 | `PptDecryptor`: verify (NPOI `EncryptionInfo` over `LittleEndianInputStream(ByteArrayInputStream)`, `VerifyPassword`), decrypt persist objects in place (`InitCipherForBlock(null, id)`), Pictures (per-field block 0), EncryptedSummary (own parser) | new `Helpers/Decryption/PptDecryptor.cs` (~200 lines) | layout + password -> decrypted `Current User` (token rewritten), doc, Pictures, summary streams | wrongpw on hello/cryptoapi/png -> `INVALID_PASSWORD`; decrypted doc of hello walks with 0 structural errors; png picture #2 == twin PNG; hello summary streams sha1 F1FEF6B5.../D03E04BB...; fuzz: descriptor count 0x7FFFFFFF -> `CORRUPT_FILE` (no OOM) |
| 5 | Rebuild plain CFB (NPOI copy writer) | same file or `PptPlainCompoundFile.cs` (~50 lines) | source fs + replacements -> byte[] | rebuilt cryptoapi parses with b2x `StructuredStorageReader` (no mini stream case); rebuilt hello parses (mini stream shrink case); root CLSID preserved |
| 6 | Wire into `MainExtractor`: after preflight, for format Ppt with encrypted layout and a password, swap `input` to plaintext bytes for open and text fallback | `MainExtractor.cs`, `Helpers/Input/ExtractionInput.cs` | | hello `hello` -> text 147 chars, Title 'This is a protected document', SlideCount 1, NotesSlideCount 1, 0 FieldErrors; 56-hello same; np-hello -> 177 chars; cryptoapi `crypto` -> Title 'RC4 CryptoAPI Encryption', Pictures subfile sha256 prefix EB85DE0E, (text only after b2x task B); png `password` -> 1 Pictures subfile 16588 bytes whose second blip equals the twin PNG; password on unencrypted `ppt_with_png.ppt` -> identical to no-password result; stream input variant of hello gives the same |
| 7 | Confidentiality and error hygiene | tests only | | no password/key bytes in exception messages/FieldErrors (search for "hello" in all messages) |

**Size estimate** (honest): production code ~400-450 lines (layout 150, decryptor 200, rebuild 50, wiring 30-50) + tests ~300 lines + fixtures. About 2-3 builder days including review, plus task 0. The b2x tasks in 2.10 are separate, ~0.5-1 day each.

### 2.10 b2xtranslator edits

Required for this design: **none**. The token rewrite makes b2x see an ordinary file (2.3).

Optional / recommended (each encryption-independent; found on decrypted fixtures):

| # | File:line | Current | Proposed | Acceptance test |
|---|---|---|---|---|
| A | `Ppt/PptFileFormat/CurrentUserAtom.cs:113-114` | `throw new NotSupportedException("Encryped PPT files aren't supported at this time")`; not a parse fault, so the library reports `UnexpectedFieldFailure` FieldErrors instead of a typed error [RUN today] | throw a typed `EncryptedDocumentException`-style exception (e.g. new `b2xtranslator.StructuredStorage.Common.EncryptedFileException : InvalidStreamException` or a dedicated type the library maps to `PASSWORD_PROTECTED`) | b2x unit test: `new PowerpointDocument(reader)` on Password_Protected-hello throws that type; library: defence in depth if detection (task 3) is bypassed |
| B | `Ppt/PresentationMLMapping/FillMapping.cs:265` (and `:558`) | `so.OptionsByID[fillShadeType]` indexer -> `KeyNotFoundException` when only `fillShadeColors` is present | `TryGetValue` with the MS-ODRAW default for fillShadeType [UNVERIFIED default value; spec MS-ODRAW fillShadeType] | decrypted cryptoapi-proc2356 converts; library text non-empty (contains "Dominic Salemno") [RUN with emulated fix] |
| C | `Common/OfficeDrawing/ClientAnchor.cs:20-47` | always reads four Int16 (SmallRectStruct) | if `Bytes.Length == 16` read four Int32 (RectStruct) [SPEC MS-PPT PptOfficeArtClientAnchor] | decrypted ppt_with_png_encrypted no longer hits the NRE at `ShapeTreeMapping.cs:2302` [RUN with emulated fix] |
| D | `Ppt/PresentationMLMapping/ShapeTreeMapping.cs:2297-2302` | dereferences `chanchor` without a null check | skip `a:xfrm` when neither anchor is valid | same fixture; no NRE |
| E | `Ppt/PresentationMLMapping/TextMasterStyleMapping.cs` `writepPr` | `Nullable.Value` on a missing paragraph property (POI-written master) | guard with `HasValue` | after C, decrypted ppt_with_png_encrypted converts; text contains "Here is a PNG image:" [UNVERIFIED: not run, the exact field was not isolated] |
| F | `Common/StructuredStorage/Reader/MiniFat.cs:117` | only ENDOFCHAIN means "no mini stream" | also treat FREESECT/NOSTREAM 0xFFFFFFFF (or `_sizeOfMiniStream == 0`) as none | the OpenMcdf-written rebuild of cryptoapi-proc2356 (`out/cryptoapi-proc2356-compact-openmcdf.ppt`) opens. Only needed if a writer other than NPOI copy is chosen. |


