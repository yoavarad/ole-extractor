# OpenMcdf — Research Spike Notes

Research date: 2026-08-18. Repo: [ironfede/openmcdf](https://github.com/ironfede/openmcdf).

## Library name

**OpenMcdf** — a pure C#/.NET library implementing the Microsoft Compound File Binary (CFB) format, a.k.a. OLE2 Structured Storage. This is the container format underlying legacy `.doc`/`.xls`/`.ppt`, `.msg` (Outlook), `.msi`, and `.suo` files.

Package split (NuGet):
- `OpenMcdf` — core CFB reader/writer (the one we need).
- `OpenMcdf.Ole` — separate package for OLE Properties (SummaryInformation/DocumentSummaryInformation streams). README marks this **experimental and subject to change**. Not required for our core sniffing/extraction job but potentially useful later for format-specific metadata (author, title, etc. from legacy property streams).
- Note: there is also a third-party fork package `OpenMcdf-2` on NuGet (separate from the official `OpenMcdf` package) — not the one to use; stick with the official `OpenMcdf` id maintained by ironfede.

## License

**MPL-2.0** (Mozilla Public License 2.0). OSS, file-level copyleft only — safe for use in a proprietary/commercial product without requiring the whole product to be open-sourced (only modifications to MPL-licensed files themselves would need to be shared). No commercial-license conflict with the project's OSS-only constraint.

## Maintenance signal

- **Stars:** 388 (as of research date).
- **Last commit / push:** 2026-08-17 (i.e., yesterday relative to research date) — repo is actively pushed to.
- **Last release:** `v3.2.0`, published 2026-07-30. Release cadence has been roughly monthly-to-bimonthly through 2025-2026 (v3.0.0 on 2025-06-03, then 3.0.1 → 3.2.0 through mid-2026, ~10 releases in ~14 months).
- **Downloads:** ~20.8M total across all versions on NuGet, 24K on the latest version alone — widely used. 83 NuGet packages depend on it, notably `MsgReader` (9.2M downloads), a popular Outlook `.msg` parser — a strong real-world validation signal that OpenMcdf handles genuine, messy Office/Outlook files in production.
- **Issues/PRs:** ~9 open issues, ~8 open PRs at time of research — small, manageable backlog, not abandoned/overwhelmed.
- **Security response:** CVE-2026-41511 (infinite-loop DoS from a crafted cyclic directory tree) was reported and patched within the same release cycle (fixed in v3.1.3, released 2026-04-20) — indicates the maintainer responds to security reports.
- Overall: **healthy, actively maintained** project with a real, non-trivial user base. Not a one-person weekend toy — it's the de facto standard OSS CFB library for .NET.

## API surface relevant to our use case

Core types (namespace `OpenMcdf`): `RootStorage`, `Storage`, `CfbStream`, `EntryInfo`, `EntryType`.

**Opening a file** — critically, it accepts an arbitrary `Stream`, not just a file path, so a `MemoryStream` wrapping our in-memory `byte[]` works directly (no temp file needed):

```csharp
public static RootStorage Open(Stream stream, StorageModeFlags flags = StorageModeFlags.None);
public static RootStorage OpenRead(string fileName, StorageModeFlags flags = StorageModeFlags.None);
public static RootStorage Open(string fileName, FileMode mode, FileAccess access, StorageModeFlags flags = StorageModeFlags.None);
```

Example from README:
```csharp
using var root = RootStorage.OpenRead("report.xls");
using CfbStream workbookStream = root.OpenStream("Workbook");
```

Or from bytes directly:
```csharp
using var ms = new MemoryStream(fileBytes);
using var root = RootStorage.Open(ms);
```

`RootStorage` derives from `Storage`, so all storage operations apply at the root too.

**Enumerating entries** (this is the key API for mime-sniffing and for embedded-object discovery):

```csharp
public class Storage : ContextBase
{
    public EntryInfo EntryInfo { get; }
    public IEnumerable<EntryInfo> EnumerateEntries();          // list children of this storage
    public bool ContainsEntry(string name);
    public bool TryGetEntryInfo(string name, out EntryInfo entryInfo);
    public Storage OpenStorage(string name);                   // recurse into nested storage
    public bool TryOpenStorage(string name, out Storage? storage);
    public CfbStream OpenStream(string name);                  // open a stream for reading
    public bool TryOpenStream(string name, out CfbStream? stream);
}

public readonly record struct EntryInfo(
    EntryType Type,      // Storage or Stream
    string Path,
    string Name,
    long Length,
    Guid CLSID,
    DateTime CreationTime,
    DateTime ModifiedTime);
```

Recursive tree walk (needed for embedded-object discovery) is trivial and not a special/hidden API — just recurse manually:

```csharp
void Walk(Storage storage, string prefix)
{
    foreach (EntryInfo entry in storage.EnumerateEntries())
    {
        Console.WriteLine($"{prefix}{entry.Name} ({entry.Type}, {entry.Length} bytes)");
        if (entry.Type == EntryType.Storage)
        {
            using Storage child = storage.OpenStorage(entry.Name);
            Walk(child, prefix + entry.Name + "/");
        }
        else
        {
            using CfbStream stream = storage.OpenStream(entry.Name);
            byte[] buf = new byte[stream.Length];
            stream.ReadExactly(buf); // or stream.Read/CopyTo a MemoryStream
        }
    }
}
```

`CfbStream` is a standard `System.IO.Stream`, so ordinary `.Read`/`.CopyTo(memoryStream)`/`.ToArray()` patterns work to pull raw bytes out — no special extraction API needed.

**Mime-sniffing recipe this enables** (matches the task description exactly): check the 8-byte CFB magic (`D0 CF 11 E0 A1 B1 1A E1`, internally `Header.Signature`) first as a cheap pre-check without invoking OpenMcdf at all, then `RootStorage.Open(stream)` and call `root.EnumerateEntries()` once — no need to open/read any stream content — and inspect top-level names (`"WordDocument"` → doc, `"Workbook"`/`"Book"` → xls, `"PowerPoint Document"` → ppt, etc.). This is a true directory-only operation.

## Strengths

- **Lightweight for directory-only reads.** `RootStorage.Open` + `EnumerateEntries()` only needs to parse the CFB header, FAT/MiniFAT sector chains, and the directory sector chain — it does not decode any document-format-specific content (no OOXML/binary-record parsing). This is exactly the "open just enough to read names" sniffing behavior we want.
- **Not a full in-memory load.** The library is a sector-chain/stream-based reader built around `System.IO.Stream` — it reads sectors on demand via enumerators (`FatSectorEnumerator`, `DirectoryTreeEnumerator`, `MiniFatChainEnumerator`, etc.) rather than slurping the entire file into a byte array up front. Internally it does use a `SectorDataCache` for sector data, so repeated small reads are efficient, but the initial cost of just walking the directory is proportional to directory-entry count and FAT/DIFAT chain length, not file size. Opening a multi-hundred-MB CFB file to just list root entries should be fast; there is no published microbenchmark specifically for "open + list root entries" but the API design (streaming, chain-walking, on-demand sector reads) supports this claim. There's also an `OpenMcdf.Benchmarks` project in the repo (BenchmarkDotNet-based) if we want to run our own numbers later.
- **Accepts arbitrary `Stream`**, so no temp files needed when working from `byte[]` (wrap in `MemoryStream`).
- **Generic, format-agnostic byte extraction.** Because `CfbStream` is a plain `Stream`, extracting an embedded OLE object (whether under a nested storage like `"\x01Ole10Native"`/`"Ole10Native"`, a `"\x01Ole"` stream, or an arbitrary nested storage representing a `Package`/embedded object) is just: enumerate → find matching storage/stream name or CLSID → `OpenStream`/`OpenStorage` → read bytes. No document-model awareness needed, which fits the "first-layer embedded subfiles as byte[]+filename+size" requirement well — the "filename" for OLE-embedded children would come from the stream/storage name (or from `Ole10Native`'s internal filename field, which is a separate concern from OpenMcdf itself).
- **Idiomatic, modern C# API** (v3.x line): nullable annotations, `TryXxx` patterns, exception hierarchy (`FileFormatException`, etc.), `netstandard2.0`/`net8.0`/`net10.0` multi-targeting — fits .NET 8 target cleanly.
- **CLSID exposed** on `EntryInfo` and `Storage` — useful for identifying embedded-object type by class ID rather than only by name, a more robust sniffing signal for embedded OLE objects.
- Real-world battle-testing via downstream consumers (MsgReader et al., 20.8M downloads) suggests it copes reasonably with the kind of imperfect files found in the wild, and the maintainer has been actively hardening directory-tree validation (see Changelog below) specifically in response to malformed/malicious inputs.

## Weaknesses / gaps

- **README explicitly states "Limited error tolerance/recovery."** For a read-only extraction pipeline that will see arbitrary/possibly-corrupt user-uploaded files, this is the most relevant caveat: expect to wrap reads in try/catch for `FileFormatException` and treat unparseable CFB files as a graceful failure/fallback path rather than assuming OpenMcdf will silently recover from mild corruption the way some parsers do.
- **CVE-2026-41511 (fixed in 3.1.3, GHSA-jxpf-xq2m-q525):** versions before 3.1.3 could enter an infinite loop (CPU/thread exhaustion, no exception recoverable) when parsing a CFB file with a crafted cycle in the directory-entry red-black tree (`LeftSiblingID`/`RightSiblingID`). This is a directory-tree-only exploit (i.e., it fires exactly on `EnumerateEntries()`/`OpenStream()` — the surface we'd rely on for sniffing), so **pin to v3.1.3 or later** (current is 3.2.0) and this is not a concern for a currently-implemented spike, but worth flagging as "must stay on a patched version" given it's the exact code path we depend on.
- No single-writer/multiple-reader support and no red-black tree self-balancing on write — irrelevant to us since we are read-only.
- `OpenMcdf.Ole` (property-stream metadata) is explicitly experimental; if we want legacy SummaryInformation metadata (author/title/etc.) via that package, expect API churn across versions.
- No dedicated "give me embedded OLE objects" high-level helper — we'd be writing our own logic on top of `EnumerateEntries`/`OpenStorage`/`OpenStream` to recognize `Ole10Native`, `\x01CompObj`, `Package`, `\x01Ole`, etc. by name/CLSID convention. This is expected/acceptable (matches the "generic" extraction approach) but means we own that mapping table ourselves; OpenMcdf gives us the tree-walk primitive, not domain knowledge of OLE embedding conventions.
- No official benchmark numbers published specifically for "open + enumerate directory only" (as opposed to full read/write); the `OpenMcdf.Benchmarks`/`OpenMcdf.Perf` projects exist in-repo but we did not find published results — if directory-walk latency matters for our sniffing hot path, plan to benchmark it ourselves against representative files.

## Unicode / RTL / emoji findings (entry names)

- CFB directory-entry names are stored internally as UTF-16LE in the 64-byte name field (up to 31 UTF-16 code units + null terminator, per the CFB spec), and OpenMcdf's `ThrowHelper.ThrowIfNameIsInvalid` confirms this explicitly in source: it validates via `Encoding.Unicode.GetByteCount(value) > DirectoryEntry.NameFieldLength - 2` (`Encoding.Unicode` = UTF-16LE in .NET) — i.e., the library operates on `System.String` (UTF-16 natively in .NET) for names throughout the public API (`EntryInfo.Name` is a plain `string`), so **no additional decoding/encoding work is needed on our side for Hebrew/Arabic/Persian/Russian/CJK/emoji characters in stream/storage names** — .NET's native UTF-16 string handling round-trips through OpenMcdf's API transparently.
- Caveat specific to our project's targets: legacy top-level stream names we actually key off for sniffing (`"WordDocument"`, `"Workbook"`, `"Book"`, `"PowerPoint Document"`) are fixed ASCII identifiers defined by the binary format specs themselves — they will never contain non-Latin script regardless of document content or locale. Unicode concerns arise instead for: (a) *nested* storage/stream names an author might create for embedded objects with custom names, and (b) the actual document text content (out of OpenMcdf's scope — that's ours to decode from the WordDocument/Workbook stream bytes per the binary format's own text encoding, typically UTF-16LE for legacy Word/Excel text). OpenMcdf itself is not a source of mojibake risk for names; one emoji code point outside the BMP would be a UTF-16 surrogate pair (4 bytes) which fits fine within the 62-byte usable name field budget alongside a few more characters, so no truncation surprises expected for realistic short names, though very long custom embedded-object names with many astral-plane emoji could in theory hit the 31-UTF-16-code-unit ceiling — this is a hard CFB format limit, not an OpenMcdf limitation.

## Citations / links

- [ironfede/openmcdf GitHub repo](https://github.com/ironfede/openmcdf) — README, source, issues
- [README.md](https://github.com/ironfede/openmcdf/blob/master/README.md)
- [CHANGELOG.md](https://github.com/ironfede/openmcdf/blob/master/CHANGELOG.md)
- [NuGet: OpenMcdf 3.2.0](https://www.nuget.org/packages/OpenMcdf)
- [NuGet: OpenMcdf.Ole](https://www.nuget.org/packages/OpenMcdf.Ole)
- [GitHub Releases](https://github.com/ironfede/openmcdf/releases) (v3.0.0 → v3.2.0 covered)
- [CVE-2026-41511 / GHSA-jxpf-xq2m-q525 writeup (miggo.io)](https://www.miggo.io/vulnerability-database/cve/CVE-2026-41511)
- Source files reviewed directly: `OpenMcdf/Storage.cs`, `OpenMcdf/EntryInfo.cs`, `OpenMcdf/RootStorage.cs`, `OpenMcdf/ThrowHelper.cs`, `OpenMcdf/Header.cs` (all at `github.com/ironfede/openmcdf`, `master` branch, fetched 2026-08-18)
