# Spike 237: where the ~3.5GB peak in b2xtranslator .doc conversion comes from

Task 237 (story 145). Follows task 194, which measured ~3.5GB peak working set (~6.4GB allocated)
extracting `benchmark-100mb.doc` regardless of input mode.

## Method
`docs/research/doc-memory-profile-harness/` is a throwaway console app (NOT in `ExtractorOle.slnx`)
that copies `DocTextExtractor.ConvertToDocx` step by step. It prints allocated bytes, heap, working set
and GC counts after each phase, samples peak working set every 5ms, and (mode `alloc`) attributes
allocations by type via the runtime `GCAllocationTick` event (one tick per ~100KB, so shares are
statistical). net10.0, Release, workstation non-concurrent GC, `benchmark-100mb.doc`
(101,894,144 bytes; one piece, 31,774,936 characters, Unicode).

    dotnet run -c Release --project docs/research/doc-memory-profile-harness -- samples/benchmark/benchmark-100mb.doc alloc

## Phase results (allocated / live heap / working set)
| Phase | Allocated | Heap | Working set |
|---|---|---|---|
| input loaded (byte[]) | 97MB | 97MB | 122MB |
| StructuredStorageReader ctor | 97MB | 97MB | 123MB |
| **WordDocument ctor** | **4,067MB** | **3,616MB** | **3,171MB** |
| Converter.Convert (mapping, before package write) | 5,559MB | 3,816MB | 3,342MB (peak) |
| package written / ToArray | 5,561MB | 3,818MB | 3,345MB |

Peak working set 3,344MB, peak private 3,934MB, 6s. **~95% of the peak is reached inside the
`WordDocument` constructor, before any docx mapping runs.** The mapping phase adds only ~1.5GB of
short-lived gen0 garbage (109 gen0 GCs) and ~200MB of live heap.

## Allocation share by type (whole run, 5,570MB sampled)
| Type | Share | Source |
|---|---|---|
| `Entry<int,int>[]` (Dictionary entries arrays) | 52.4% (2,916MB) | `PieceTable` per-character dictionaries |
| `Int32[]` (Dictionary buckets) | 13.1% (731MB) | same |
| `Char[]` | 22.2% (1,239MB) | `PieceTable.GetAllChars` -> `List<char>` growth (WordDocument.Text), plus per-run `GetChars` / strings during mapping |
| `Byte[]` | 7.5% (417MB) | piece byte reads, `ToCharArray`, stream buffers |
| `String` + XML DOM | ~3% | mapping output |

Retained after a full GC following the constructor: 1,996MB = **1,830MB the two dictionaries**,
64MB `Text` (`List<char>`), ~100MB rest (the input byte[] etc).

## Re-run on current b2xtranslator (930217bd)
Initial profile used b2xtranslator `d20ff458`; re-run after PR #240 on `930217bd` (npoi `502d75df7`),
same harness, sample and mode. `d20ff458..930217bd` touches only mapping files (#148 `HasAttributes`
emptiness checks), README and graphify output; `PieceTable.cs` / `WordDocument.cs` are unchanged.

| Metric | d20ff458 | 930217bd |
|---|---|---|
| WordDocument ctor: allocated / heap / ws | 4,067 / 3,616 / 3,171MB | 4,067 / 3,616 / 3,176MB |
| Peak working set / private | 3,344 / 3,934MB | 3,213 / 3,762MB |
| Total allocated | 5,561MB | 5,566MB |
| `Entry<int,int>[]` + `Int32[]` share | 52.4% + 13.1% | 52.4% + 13.1% |
| Heap after Convert | 3,816MB | 2,190MB (GC timing, not a code change) |

The ~4% lower peak is run-to-run GC noise; allocations are identical. The headline finding and the
ranked follow-ups above still hold unchanged: ~95% of the peak is still reached in the `WordDocument`
constructor from the `PieceTable` per-character dictionaries.

## Root cause
`third_party/b2xtranslator/Doc/DocFileFormat/PieceTable.cs`, constructor, inner loop
`for (c = cpStart; c < cpEnd; c++)`: for **every character** of the document it inserts into two
`Dictionary<int,int>`:
- `FileCharacterPositions` (cp -> fc)
- `CharacterPositions` (fc -> cp)

For 31.8M characters that is 63.5M dictionary entries. Dictionary doubling means each resize
allocates a new entries array and abandons the old one, so ~2.9GB of `Entry[]` + 0.7GB of buckets are
allocated for 1.8GB retained. Both maps are pure arithmetic over the piece list (a piece is a
contiguous cp range mapping linearly to fc with stride 1 or 2), so they carry no information the
`Pieces` list does not already hold.

Usage: `FileCharacterPositions` is read by index in 21 places (`DocumentMapping`, `HeaderMapping`,
`FooterMapping`, `MainDocumentMapping`, ...). `PieceTable.CharacterPositions` (fc -> cp) has no
external readers in the fork or in ole-extractor (the `Plex.CharacterPositions` hits are a different,
unrelated list).

## Ranked follow-ups (expected savings on the 100MB doc)
1. **Replace the per-char dictionaries with piece arithmetic** (fork): add `PieceTable.GetFc(cp)`
   (binary search `Pieces` by `cpStart`, `fc + (cp - cpStart) * (unicode ? 2 : 1)`, plus the special
   trailing `maxCp -> fcMac` entry) and rewrite the 21 indexer sites; drop `CharacterPositions`
   (unused). Removes ~1.8GB retained and ~3.6GB allocated: peak ~3.3GB -> ~1.3-1.5GB.
   Highest value, low risk (pure refactor, existing DocTextExtractor/Doc fixtures cover it; add a unit
   test with a multi-piece mixed ANSI/Unicode piece table asserting cp->fc equals the old dictionary).
   Must keep the old first-wins behavior for overlapping cp ranges.
2. **Stop materializing `WordDocument.Text` as `List<char>`** (fork) or pre-size it
   (`new List<char>(totalChars)` from the piece list; avoids doubling garbage, ~64MB retained +
   ~128MB garbage -> 64MB). Small after #1, but `GetAllChars` also boxes through `ToCharArray()` +
   per-char `Add` (3 copies of every piece transiently: ~3x text size). Expected 150-300MB off peak.
3. **Per-run `GetChars` allocation in `DocumentMapping`** (`PieceTable.GetChars` returns a fresh
   `List<char>` and re-reads bytes for every CHPX run): reuse `WordDocument.Text` slices
   (`Text.GetRange` / span) instead of re-reading the stream. Cuts the ~1.5GB mapping-phase gen0
   churn and the 417MB of `Byte[]`; lowers GC count, not the peak.
4. **Our side, bounded**: `DocTextExtractor` still builds the full docx then re-parses it; an
   in-process guardrail on piece-table character count (e.g. refuse > N chars) would bound worst-case
   memory for adversarial .doc files (a tiny declared piece table can request a multi-GB dictionary
   today). Independent of #1 but cheap; also protects if #1 is deferred.
5. (No action) `StructuredStorageReader`/input buffering: already measured in ADR-008, one
   input-size (~100MB), not the driver.

Suggested split into follow-up tasks: #1 (fork PR + submodule bump + parity test), #2+#3 (fork),
#4 (ole-extractor guardrail).
