# Profiling findings (top 5 hot frames)

Captured 2026-09-30, Windows 11, .NET 8, Release, `--profile` (CpuSampling) on the 100MB samples:

```
dotnet run -c Release --project ExtractorOle/ExtractorOLE.Benchmarks -- --profile --filter "*xls-100mb*" "*ppt-100mb*" "*pptx-100mb*"
```

Percentages are self time on the benchmark thread, from the speedscope output. Synthetic
`CPU_TIME`/`UNMANAGED_CODE_TIME` leaves are folded into the parent frame. Sampling is coarse (~1 ms), so treat
the numbers as a ranking, not a measurement. The common theme: byte copying (`Buffer.Memmove`) dominates all three.

## xls-100mb

| # | Self % | Frame |
|---|-------:|-------|
| 1 | 51.2 | `System.Buffer._Memmove` |
| 2 | 25.2 | `NPOI.HSSF.Record.RecordInputStream.ReadStringCommon` |
| 3 | 9.0 | `System.Array.Copy` |
| 4 | 5.6 | `System.Buffer.Memmove` |
| 5 | 2.6 | `NPOI.POIFS.FileSystem.NDocumentInputStream.ReadFully` |

## ppt-100mb

| # | Self % | Frame |
|---|-------:|-------|
| 1 | 58.7 | `System.Buffer._Memmove` |
| 2 | 10.6 | `System.Buffer.Memmove` |
| 3 | 4.6 | `System.IO.MemoryStream.ToArray` |
| 4 | 2.6 | `b2xtranslator.OfficeDrawing.BitmapBlip..ctor` |
| 5 | 1.4 | `System.IO.BinaryReader.ReadBytes` |

## pptx-100mb

| # | Self % | Frame |
|---|-------:|-------|
| 1 | 38.0 | `System.Buffer._Memmove` |
| 2 | 13.3 | `Interop+Kernel32.WriteFile` |
| 3 | 9.5 | unresolved (`?!?`) |
| 4 | 4.9 | `System.Text.StringBuilder.ExpandByABlock` |
| 5 | 3.8 | `Interop+Kernel32.ReadFile` |

## Takeaways (leads, not conclusions)

- ppt: picture blips are copied via `BinaryReader.ReadBytes` + `MemoryStream.ToArray` (extra full-size copies).
- xls: `ReadStringCommon` (shared string table) and stream copying dominate.
- pptx: file I/O (`WriteFile`/`ReadFile`) suggests temp-file or stream spooling; `StringBuilder` growth suggests presizing.
