# ADR-008: Stream/file-path input instead of whole-file byte[] (spike #174)

## Status
Proposed: **GO** (phased, OOXML first). Spike for story #161. 2026-10-07.

## Context
`IOpenStrategy.Open(byte[])` and `ExtractionRequest.FileBytes` force the whole input (up to
100MB) into one LOH array before any work, plus per-step `new MemoryStream(fileBytes)` wrappers
(`OpenXmlPackages`, `DocOpenStrategy`, `XlsOpenStrategy`, `PptOpenStrategy`, `CfbMimeDetector`,
`OoxmlMimeDetector`). Question: which parsers can consume a seekable `Stream`/`FileStream`
directly, and what does it save?

## Per-parser stream capability

| Parser | Used for | Seekable Stream/FileStream directly? | Evidence |
|---|---|---|---|
| DocumentFormat.OpenXml 3.5.1 | docx/xlsx/pptx | Yes. `*.Open(Stream, false)` reads the zip lazily via `System.IO.Packaging`/`ZipArchive`; needs seekable. | measured below |
| OpenMcdf 3.2.0 | CFB mime detection | Yes. `RootStorage.Open(Stream)` is lazy and seek-based. | measured below |
| b2xtranslator `StructuredStorageReader` | .doc/.ppt -> docx/pptx conversion | Yes. `InputHandler` wraps the stream and `Seek`s per sector; `(string path)` ctor already uses a `FileStream`. | source + measured |
| NPOI POIFS `NPOIFSFileSystem` | .xls (HSSF), ppt metadata | Partly. `NPOIFSFileSystem(FileStream)` is channel-backed (low memory). `NPOIFSFileSystem(Stream)` and `HSSFWorkbook(Stream)` buffer the whole file (source comment: "need to buffer the whole file into memory when working with an InputStream"). Must pass `FileStream`, not a generic `Stream`. | source + measured |

Not stream-capable today (our code, not the parsers): `ParseTimeErrorGuard.Preflight/OpenOrThrow`,
`ExtractionGuardrails`, `CfbMimeDetector.TryGetDeclaredCfbTotalSize` and
`OoxmlMimeDetector.TryGetDeclaredZipTotalSize` take `byte[]` and index into it.
`DocTextExtractor.ConvertToDocx` already materialises a second `byte[]` (converted docx), which
streaming input does not remove.

## Measurements
Harness: a throwaway console app (removed from the PR; see commit history of task 174) One child process
per mode, 100MB benchmark samples (`SampleGenerator --benchmark`), .NET 8 Release,
`Process.PeakWorkingSet64` and `GC.GetTotalAllocatedBytes`. Open + minimal text/structure walk
only; not the full `MainExtractor.Extract` pipeline. Single run each, indicative not statistical.

| Mode (100MB input) | Peak WS MB | Allocated MB |
|---|---|---|
| docx, byte[] + MemoryStream | 413 | 604 |
| docx, FileStream | 314 | 507 |
| xlsx, byte[] + MemoryStream | 226 | 290 |
| xlsx, FileStream | 129 | 193 |
| xls, byte[] -> HSSFWorkbook(Stream) | 354 | 1970 |
| xls, NPOIFSFileSystem(FileStream) -> HSSFWorkbook | 254 | 1975 |
| .doc b2x reader, byte[] | 125 | 97 |
| .doc b2x reader, FileStream | 28 | 0 |
| .doc OpenMcdf, byte[] | 125 | 97 |
| .doc OpenMcdf, FileStream | 28 | 0 |

Every parser saves about one input-size (~97-100MB) of peak working set and removes the
100MB LOH allocation. The remainder (docx 314MB, xls 254MB) is parser-internal and unaffected.

## Decision
GO, phased:
1. Add `ExtractionRequest` input as `Stream` (seekable) or path alongside `FileBytes`
   (keep `byte[]` overload; callers that already hold bytes wrap in `MemoryStream`).
2. OOXML first: `OpenXmlPackages` takes a `Stream`; Preflight/guardrails/mime detectors read
   the zip header/central directory from the stream (seek to end-record) instead of `byte[]`.
3. Legacy CFB: pass `FileStream` to `NPOIFSFileSystem`, b2x `StructuredStorageReader`, OpenMcdf.
   Do not use generic `Stream` overloads for NPOI (buffers everything).

Risks: the service boundary may only receive bytes (HTTP body); gain applies only if the caller
can supply a file or seekable stream. Non-seekable network streams need spooling to a temp file.
Open strategies hand `ParsedDocument` ownership to `MainExtractor`; the stream lifetime must be
tied to that dispose. Concurrency: one FileStream per request, shared-read open.

## Follow-up tasks
- #192 Stream-based `ExtractionRequest` + OOXML open path (OpenXmlPackages, strategies).
- #193 Stream-based Preflight/guardrails/MimeDetectors (header + zip central directory via seek).
- #194 Legacy CFB (xls/doc/ppt) via FileStream-backed NPOIFS/b2x/OpenMcdf.
- Re-measure full `Extract` with BenchmarkDotNet MemoryDiagnoser once 1-3 land.
