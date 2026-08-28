---
task: T-9e6bfce8
title: Implement xls first-layer embedded-subfile walk
status: SPEC (planner output, not implemented)
---

# 1. File Manifest

| Path | New/Modified | Purpose |
|---|---|---|
| `ExtractorOle/ExtractorOLE/Helpers/IExtractionHelper.cs` | Modified | Add `void ExtractFirstLayerEmbedded(DocumentExtractionResult result, HSSFWorkbook workbook)` overload (mirrors the existing `ExtractFirstLayerEmbedded(DocumentExtractionResult, OpenXmlPart)` overload already on this interface). Requires new `using NPOI.HSSF.UserModel;`. |
| `ExtractorOle/ExtractorOLE/Helpers/ExtractionHelper.cs` | Modified | Implement the new overload + two new private helpers: `ExtractHssfObjectData(HSSFObjectData obj, List<EmbeddedFileItem> fileList, ref int index)` and `ExtractHssfPictureData(HSSFPictureData pic, List<EmbeddedFileItem> fileList, ref int index)`. Each embedded-object item gets its own try/catch (per-item corruption isolation — the acceptance-criteria requirement). New `using NPOI.HSSF.UserModel; using NPOI.POIFS.FileSystem;`. |
| `ExtractorOle/ExtractorOLE/Helpers/FileTypeStrategy/XlsOpenStrategy.cs` | Modified | Inside `Open()`, after populating `result.Metadata` from `SummaryInformation` and while `workbook`/`ms` are still in scope, call `_helper.ExtractFirstLayerEmbedded(result, workbook);`. No other change — `Open()`'s existing outer try/catch (returns `null` on any exception) is untouched and still the whole-file corruption boundary. |
| `ExtractorOle/ExtractorOLE.Tests/Excel/XlsOpenStrategyTests.cs` | Modified (existing file) | Add new fixture-building helpers + 3 new `[Fact]`s: happy-path embedded object (also proves the "opaque container" AC), happy-path inline picture, and the valid+corrupt pair required by `test_strategy`. |

Not touched: `Old/` (dead code), `SampleGenerator/Generators/XlsSampleGenerator.cs` (confirmed out of scope — it's a `NotSupportedException` placeholder unrelated to this task; wiring it up is a separate future task), `DocumentExtractionResult.cs` (DTO shape is reused as-is — no `SubfileItem` type exists or is needed; `EmbeddedFileItem`/`EmbeddedFiles` already satisfy the shape the task's `component_refs` describe conceptually), `ServiceRegistration.cs` (already resolves `IExtractionHelper`→`ExtractionHelper` as a singleton and injects it into `XlsOpenStrategy`; no DI wiring changes needed).

# 2. Production-code NPOI APIs (verified against `third_party/npoi` @ `2.7.6-rc1`)

All citations are against `third_party/npoi/main/...` (submodule not checked out inside the worktree at `T-9e6bfce8` — read from the main repo checkout instead, same pinned commit `a0f50a01a845aa4dd793cee0ac5b776745966cb6` in both, so content is identical).

## 2.1 Enumerating embedded OLE objects and inline pictures

- `HSSFWorkbook.GetAllEmbeddedObjects()` → `IList<HSSFObjectData>` — `main/HSSF/UserModel/HSSFWorkbook.cs:2282-2290`. Internally walks every sheet's `DrawingPatriarch` recursively (`HSSFWorkbook.cs:2297-2321`) collecting shapes that are `HSSFObjectData`. This call itself does not touch POIFS/the object's payload — it only walks in-memory shape/escher records, so it will not throw for the per-item corruption scenario this task targets (confirmed by reading the method body: no `.Directory`/`.ObjectData` access inside the walk).
- `HSSFWorkbook.GetAllPictures()` → `IList` of `HSSFPictureData` — `main/HSSF/UserModel/HSSFWorkbook.cs:2104-2117`. Workbook-wide, decodes each `AbstractEscherHolderRecord` and searches for picture blips. This is the "inline media" half of AC #1, distinct from `GetAllEmbeddedObjects()` (OLE objects only).

## 2.2 Reading a single embedded object

`main/HSSF/UserModel/HSSFObjectData.cs`:

- `bool HasDirectoryEntry()` (line 107-114): `return streamId != null && streamId != 0;` — **note this is a cheap field check, it does not touch POIFS at all**, so it cannot itself throw and cannot itself detect the "entry doesn't really exist / isn't a directory" corruption case.
- `DirectoryEntry Directory` getter (line 68-87): computes `streamName = "MBD" + HexDump.ToHex((int)streamId)`, does `_root.GetEntry(streamName)`, and **throws `IOException("Stream " + streamName + " was not an OLE2 directory")` if the resolved entry exists but is not a `DirectoryEntry`** (line 84). This is the exact corruption path named in the task's context brief and is what the corrupt-fixture test must trigger. (`_root.GetEntry` on a name that doesn't exist at all throws `FileNotFoundException` per POIFS convention — either exception must be caught per-item.)
- `byte[] ObjectData` getter (line 94-100): `FindObjectRecord().ObjectData` — used when `HasDirectoryEntry()` is false.
- `string OLE2ClassName` (line 53-59).

`main/HSSF/UserModel/HSSFPictureData.cs`: `byte[] Data` (line 59-76), `string SuggestFileExtension()` (line 92-111), `string MimeType` (line 115-137).

## 2.3 Serializing a container `DirectoryEntry` to an opaque byte blob (AC #2)

Verified concrete approach — creating a **new empty POIFS filesystem, copying the source directory's nodes into its root, then writing it out to bytes**:

```csharp
using NPOI.POIFS.FileSystem;

DirectoryEntry sourceDir = objData.Directory;              // HSSFObjectData.Directory
var target = new NPOIFSFileSystem();                        // main/POIFS/FileSystem/NPOIFSFileSystem.cs:101 — "Constructor, intended for writing"
EntryUtils.CopyNodes(sourceDir, target.Root);                // main/POIFS/FileSystem/EntryUtils.cs:63-70 — copies all of sourceDir's children (recursively, via CopyNodeRecursively, line 29-53) into target.Root
using var outStream = new MemoryStream();
target.WriteFileSystem(outStream);                           // main/POIFS/FileSystem/NPOIFSFileSystem.cs:796
byte[] opaqueBytes = outStream.ToArray();
```

- `EntryUtils.CopyNodes(DirectoryEntry sourceRoot, DirectoryEntry targetRoot)` — `main/POIFS/FileSystem/EntryUtils.cs:63-70`. Iterates `sourceRoot`'s children and calls `CopyNodeRecursively` for each, which recreates subdirectories (`target.CreateDirectory`, copying `StorageClsid`) and copies documents (`target.CreateDocument(name, new DocumentInputStream(entry))`) — `EntryUtils.cs:29-53`. This is a generic directory-tree copy, exactly what's needed to turn an in-place `DirectoryEntry` (still attached to the parent workbook's POIFS) into a standalone blob.
- `NPOIFSFileSystem()` parameterless constructor — `main/POIFS/FileSystem/NPOIFSFileSystem.cs:101-118`, explicitly documented "Constructor, intended for writing." `.Root` (`DirectoryNode`, implements `DirectoryEntry`) — `NPOIFSFileSystem.cs:854`.
- `NPOIFSFileSystem.WriteFileSystem(Stream stream)` — `NPOIFSFileSystem.cs:796`.

This is generic — it works whether the source `DirectoryEntry` is a "real" nested OLE2 document (e.g. an embedded workbook) or the simpler `MBDxxxxxxxx` wrapper produced by Excel's generic OLE-package mechanism (see §3) — both are `DirectoryEntry` instances with children, and `CopyNodes` doesn't care about their semantic contents. This satisfies the spec's "returned as one opaque item, not recursively unpacked" — we never inspect what's inside the copied directory beyond copying it as-is.

## 2.4 Logging convention for the omitted/corrupt item

No `ILogger` exists anywhere in `ExtractorOLE` (confirmed via repo-wide search — only `Console.WriteLine` is used, e.g. `ExtractionHelper.cs:59`, `MainExtractor.cs:72,109,114`). The corruption-omission log for this task should follow that existing convention: `Console.WriteLine($"Skipping corrupt embedded object at index {index}: {ex.Message}");` inside the per-item catch block. (If a future task introduces `ILogger`, this call site is a one-line swap — not blocking for this task.)

# 3. Test-fixture write-API (verified against NPOI's own test suite using these exact APIs)

Source: `third_party/npoi/testcases/main/HSSF/UserModel/TestOLE2Embeding.cs:73-115` and `third_party/npoi/testcases/ooxml/SS/UserModel/TestEmbedOLEPackage.cs:83-98` — both exercise the real "add an OLE object to an HSSF sheet" write path end-to-end (write, then read back and assert `IObjectData`).

## 3.1 Building a valid embedded object

```csharp
var wb = new HSSFWorkbook();
var sheet = wb.CreateSheet("Sheet1");
var patriarch = (HSSFPatriarch)sheet.CreateDrawingPatriarch();

int picIdx = wb.AddPicture(pngBytes, PictureType.PNG);       // HSSFWorkbook.cs:2008 — thumbnail/icon picture backing the OLE object shape
int storageId = wb.AddOlePackage(payloadBytes, "label", "file.bin", "file.bin");
                                                               // HSSFWorkbook.cs:2197-2232 — creates POIFS dir "MBD"+HexDump.ToHex(storageId)
                                                               //   containing "Ole" + "Ole10Native" (wraps payloadBytes via Ole10Native)
var anchor = (HSSFClientAnchor)wb.GetCreationHelper().CreateClientAnchor();
anchor.SetAnchor(1, 1, 0, 0, 2, 5, 0, 0);
patriarch.CreateObjectData(anchor, storageId, picIdx);        // HSSFPatriarch.cs:204 — binds the MBD directory + picture as one shape
```

- `HSSFWorkbook.AddOlePackage(byte[] oleData, string label, string fileName, string command)` — `main/HSSF/UserModel/HSSFWorkbook.cs:2197-2232`. Allocates the next free `"MBD" + HexDump.ToHex(storageId)` (8-hex-digit, matches `HSSFObjectData.Directory`'s own name-construction at `HSSFObjectData.cs:75`), creates that as a real `DirectoryEntry` (`Directory.CreateDirectory(storageStr)`, line 2213) with two document children. **This means every object built via `AddOlePackage` already has `HasDirectoryEntry() == true`** — i.e. this single write path is sufficient to cover both the "happy path embedded object" test and the "container/opaque" AC #2 test (no need to separately construct a `HasDirectoryEntry()==false` flat-blob fixture; that branch is a straightforward `FindObjectRecord().ObjectData` read not exercised by any AC, so it's out of scope for required tests — see §5 Open Questions).
- `HSSFPatriarch.CreateObjectData(IClientAnchor anchor, int storageId, int pictureIndex)` — `main/HSSF/UserModel/HSSFPatriarch.cs:204` — confirmed signature and usage from NPOI's own test at `TestOLE2Embeding.cs:95,101,107` and `TestEmbedOLEPackage.cs:52,91`.
- `HSSFWorkbook.AddPicture(byte[] pictureData, PictureType format)` — `HSSFWorkbook.cs:2008`.

## 3.2 Building the "corrupt" second embedded object

NPOI's normal write API cannot produce a corrupt object directly (by design — it always writes well-formed structures). The reliable, concrete approach is: **build a second valid object exactly as in §3.1, serialize the workbook, then reopen the raw bytes as a POIFS filesystem (not via `HSSFWorkbook`) and directly rename/replace that object's `MBD...` directory entry with a plain document of the same name** — this makes `HasDirectoryEntry()` still report `true` (the `ObjRecord`'s `streamId` is untouched — that lives in the BIFF/escher records, not in POIFS) while `.Directory` throws `IOException` at read time, exactly reproducing the named corruption case in §2.2.

```csharp
using NPOI.POIFS.FileSystem;
using NPOI.Util;

byte[] wbBytes;
using (var ms = new MemoryStream()) { wb.Write(ms); wbBytes = ms.ToArray(); }

string corruptName = "MBD" + HexDump.ToHex(storageIdB);       // HexDump.ToHex(int) → 8 hex digits, main/Util/HexDump.cs:397-400
                                                                // identical construction to HSSFObjectData.Directory (HSSFObjectData.cs:75)
using var raw = new NPOIFSFileSystem(new MemoryStream(wbBytes));
var entry = (Entry)raw.Root.GetEntry(corruptName);             // DirectoryNode.GetEntry — main/POIFS/FileSystem/DirectoryNode.cs:390
entry.RenameTo(corruptName + "_orig");                         // EntryNode.RenameTo — main/POIFS/FileSystem/EntryNode.cs:188 (frees up corruptName)
raw.Root.CreateDocument(corruptName, new MemoryStream(new byte[] { 0x00 }));
                                                                // DirectoryEntry.CreateDocument — main/POIFS/FileSystem/DirectoryEntry.cs:96
                                                                // corruptName now resolves to a DocumentEntry, not a DirectoryEntry
using var corruptOut = new MemoryStream();
raw.WriteFileSystem(corruptOut);
byte[] corruptWbBytes = corruptOut.ToArray();
```

- Why rename-then-recreate instead of delete-then-recreate: `EntryNode.Delete()` (`EntryNode.cs:166-175`) is documented "If this Entry Is a directory, it cannot be deleted unless it Is empty" (`EntryNode.cs:160-161`), and the `MBD...` directory has two children (`Ole`, `Ole10Native`) — deleting would require recursively emptying it first. `RenameTo` (`EntryNode.cs:188-...`, "fails if there Is a sibling Entry ... with the same name") has no such empty-directory restriction, so renaming the original directory out of the way and creating a fresh document under the freed name is the simpler, verified-available operation.
- `raw.Root.GetEntry(name)` returns the entry cast to `Entry`; casting the found `MBD...` entry to `DirectoryEntry` before calling `RenameTo` is unnecessary since `RenameTo`/`Delete` are declared on the base `Entry` interface (`Entry.cs:79` for `Delete`; `RenameTo` is on `EntryNode`, which both `DirectoryNode` and `DocumentNode` derive from) — either works, `Entry` is sufficient.
- `corruptWbBytes` is then read back exactly the way production code reads any `.xls`: `new HSSFWorkbook(new MemoryStream(corruptWbBytes))`. Object A's `MBD` directory is untouched and reads fine; Object B's `MBD` directory now resolves to a `DocumentEntry`, so `.Directory` throws `IOException` when the code under test reaches it.

This whole fixture-building sequence only uses NPOI's public, documented, verified-signature API — no raw byte-array bit-flipping, no undocumented internals. **No blocker found.**

# 4. Acceptance-criteria → test mapping

| Acceptance criterion | Test |
|---|---|
| AC #1: "First-layer embedded objects and inline media in an .xls fixture appear as correct SubfileItem entries" | New test `Open_XlsWithEmbeddedObjectAndPicture_PopulatesEmbeddedFiles` — builds one `AddOlePackage`+`CreateObjectData` object (§3.1) and one plain `patriarch.CreatePicture(anchor, wb.AddPicture(pngBytes, PictureType.PNG))` inline picture, asserts `result.EmbeddedFiles` has 2 items, correct `BinaryData`/`SizeInBytes` for each (object's bytes == the copied-POIFS blob's bytes contain the original `payloadBytes` when parsed back out; picture's `BinaryData` == `pngBytes` exactly via `HSSFPictureData.Data`). |
| AC #2: "A subfile that is itself a container is returned as one opaque SubfileItem, not recursively unpacked" | Same test as AC #1 (or a dedicated assertion within it) — assert exactly **one** `EmbeddedFileItem` is produced for the one `HSSFObjectData` with `HasDirectoryEntry()==true`, i.e. its two internal POIFS streams (`Ole`, `Ole10Native`) are never separately listed in `EmbeddedFiles`. |
| AC #3 / `test_strategy`: "A single corrupt embedded object is omitted from Subfiles and logged; Extract() still returns successfully" | New test `Open_OneValidOneCorruptEmbeddedObject_OmitsCorruptAndLogsWithoutThrowing` — builds two `AddOlePackage` objects (§3.1), corrupts the second one's POIFS directory entry (§3.2), calls `XlsOpenStrategy.Open(corruptWbBytes)`. Assert: (a) does not throw, (b) `result` is not null, (c) `result.EmbeddedFiles.Count == 1` and it's the valid object's data, (d) the corrupt one is logged — captured via `Console.SetOut(new StringWriter())` around the call, asserting the captured output contains an identifying substring (e.g. "corrupt" or the failing index), then restoring `Console.Out`. |

# 5. Open Questions / Risks

1. **No genuine blocker.** Every API used above (read-side: `GetAllEmbeddedObjects`, `GetAllPictures`, `HasDirectoryEntry`, `Directory`, `ObjectData`, `EntryUtils.CopyNodes`, `NPOIFSFileSystem`; write-side/fixture: `AddPicture`, `AddOlePackage`, `CreateObjectData`, `RenameTo`, `CreateDocument`) is present in the pinned `2.7.6-rc1` source under `main/` (not `scratchpad/`, so it's part of the already-referenced `NPOI.Core.csproj`) and is exercised by NPOI's own test suite with the same call shapes used here.
2. **`HasDirectoryEntry()==false` (flat `ObjectData`) branch is untested by this plan.** No acceptance criterion or `test_strategy` line requires it, and I did not find a documented NPOI write API that reliably produces a `HSSFObjectData` shape with `streamId == 0` short of hand-crafting raw `EmbeddedObjectRefSubRecord` bytes (fragile, undocumented). Recommendation: implement the `ObjectData` read-path in production code (it's a two-line `else` branch, essentially free), but don't block this task on fixture-testing it — flag as a follow-up if a real-world `.xls` corpus sample later surfaces one.
3. **Naming convention for `EmbeddedFileItem.FileName`/`PackagePath` on HSSF items is a genuine design choice, not dictated by any existing convention.** OOXML's `ExtractPartData` uses the OPC part's real `Uri` for `PackagePath` and a content-type-derived extension for `FileName` — HSSF has no equivalent per-item path. Proposed default (mirroring the `{prefix}_{index}{ext}` shape `ExtractPartData` already uses): `FileName = $"embedded_object_{index}.bin"` / `FileName = $"picture_{index}.{pic.SuggestFileExtension()}"`, `PackagePath = $"embedded_object_{index}"` / `$"picture_{index}"`. Not spec-mandated — reviewer should confirm this is acceptable or adjust; it doesn't affect any acceptance criterion (which only requires "binary, filename, and size" to exist and be correct-for-that-item, not a specific naming scheme).
4. **Where exactly to call the new embedded-extraction from `XlsOpenStrategy.Open()` matters for stream lifetime.** `HSSFWorkbook`'s constructor reads the whole POIFS structure into memory eagerly (unlike a lazy stream-backed reader), so calling `_helper.ExtractFirstLayerEmbedded(result, workbook)` after the `using (var ms = ...)` block closes should be safe in principle — but to avoid any risk of NPOI lazily re-reading from the now-disposed `MemoryStream` (unverified for every internal code path), the plan places the call **inside** the existing `using` block, before `ms` is disposed, which is strictly safer and costs nothing.
5. **Interface footprint.** Adding an HSSF-specific overload to `IExtractionHelper` (shared across Word/Excel/PowerPoint OOXML strategies too) slightly widens that interface's surface for an XLS-only concern. Alternative considered: a standalone static helper class colocated with `XlsOpenStrategy.cs`, called directly without going through DI. Rejected in favor of the interface-overload approach because (a) the task's context brief explicitly says to mirror `ExtractFirstLayerEmbedded`/`ExtractPartData`'s "general shape/spirit," (b) the project's own convention explicitly says new extractors should go through DI rather than static/manual instantiation, and (c) `ExtractionHelper` already mixes OOXML- and CFB-specific concerns (see its existing `_cfbMimeDetector` field and `ExcelLegacy` mime handling), so this isn't a new precedent. Flagging in case the reviewer prefers the standalone-class alternative instead.
