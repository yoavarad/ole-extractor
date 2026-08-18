# Legacy Office Format Library Landscape Scan

Date: 2026-08-18
Scope: OSS-only, .NET 8-viable libraries relevant to (a) parsing legacy binary Office formats
(doc/xls/ppt, CFB-based) and (b) reading raw CFB/OLE2 compound files, beyond the two libraries
already under separate evaluation (NPOI and OpenMcdf).

## Legacy binary format (doc/xls/ppt) parsers found (beyond NPOI)

### b2xtranslator (original) — dead, but see DocSharp fork below
- Origin: Microsoft/DIaLOGIKa "Office Binary Translator to Open XML" project (BSD-3-Clause),
  ported to .NET Core by a third party around 2017.
- Repos found: `dirvo/b2xtranslator` (685 commits, 7 stars, no visible recent activity),
  `EvolutionJobs/b2xtranslator` (.NET Core port, 87 stars, 35 forks, 7 open issues, last
  meaningful activity years ago), `SebastiaanLubbers/B2XTranslator` (another stale mirror/fork).
- NuGet package `b2xtranslator` is at v1.0.0, published 2018-06-21 — effectively abandoned.
- Usable as a library: yes, in principle (it's a set of parser/writer classes, not just a CLI),
  but the upstream packages are stale and undocumented for library consumption.
- Verdict: dead upstream. Do not depend on it directly — see DocSharp, which forked and
  continues it.

### DocSharp (manfromarce/DocSharp) — actively maintained fork of b2xtranslator, notable finding
- This is the most relevant new find of this scan. It's a continuation of b2xtranslator, MIT
  licensed, multi-targets **.NET 8.0, 9.0, 10.0, and .NET Framework 4.6.2+**.
- Converts Office 97-2003 binary documents (doc/xls/ppt) to OOXML (docx/xlsx/pptx), plus DOCX
  ↔ RTF/HTML/Markdown/plain-text/PDF/images/SVG/XPS conversions.
- NuGet packages: `DocSharp.Binary.Doc`, `DocSharp.Binary.Xls`, `DocSharp.Binary.Ppt`,
  `DocSharp.Binary.Common`, `DocSharp.Docx`, `DocSharp.Markdown`, `DocSharp.Renderer`
  (plus optional image-conversion add-ons for ImageSharp/SkiaSharp/System.Drawing/MagickNET).
- Activity: genuinely alive — `DocSharp.Binary.Doc` latest version 0.21.0 was published
  2026-08-11 (one week before this scan), with 20+ prior releases through 2025-2026. 643+
  commits on the main branch.
- License: MIT for the core; the optional `DocSharp.Renderer` package pulls in QuestPDF, which
  is community-license-free only under revenue/org-size thresholds — avoid that package or
  verify QuestPDF's community license terms if PDF rendering is ever wanted (not needed for
  our project's scope).
- Important caveats found in the README/TODO:
  - "There is no common DOM to manipulate or generate documents, this library is mainly for
    conversion." It is architected as a converter (binary → OOXML), not a metadata/text/object
    extraction API. Helper methods on top of Open XML SDK exist but are "mostly intended for
    internal use."
  - **OLE object embedding/extraction is an open TODO item** — not implemented yet. Image
    extraction works only if an output folder is configured, and format support is limited
    (JPEG/PNG/GIF full support, BMP partial, no automatic conversion for unsupported formats
    without the optional image packages).
  - Explicitly documented as "a hobby project" with no maintenance guarantee, despite currently
    being active.
  - Does not support pre-97 binary formats or XLSB.
- Relevance to our project: DocSharp is not a drop-in replacement for NPOI+OpenMcdf for our
  extraction needs (no native embedded-object extraction, no direct metadata API, conversion-
  oriented design). But it's worth keeping on the radar as either (a) a fallback/cross-check
  for doc/xls/ppt → OOXML normalization, letting a single OOXML-based extraction path handle
  both legacy and modern formats, or (b) a source of well-tested binary-format parsing code to
  reference if NPOI's HWPF/HSSF/HSLF coverage has gaps. Given its "hobby project" framing and
  incomplete OLE-object support, it should not be treated as production-primary for our use
  case today.
- Fork note: `fluxlinkage/DocSharp_legacy` exists as a secondary fork that ports the original
  b2xtranslator code to VS2019+/.NET Framework 4.7 — a dead end for .NET 8 purposes, superseded
  by manfromarce/DocSharp's own modern multi-targeting.

### IKVM + Apache POI (Java interop) — not a sane approach, confirmed
- `koustubhmoharir/poi-ikvm`: a small project (5 commits, 2 stars, 1 open issue, MIT) with
  scripts to run `ikvmc` over Apache POI JARs and publish the resulting DLL to NuGet. No
  releases or NuGet package evidence of adoption; effectively unmaintained since inception.
- IKVM itself (`ikvmc`/IKVM 8.x) is alive and does target modern .NET, but translating the full
  Apache POI dependency graph (including AWT/graphics dependencies used for shape/image
  handling in HSLF/HSSF) through IKVM is fragile and not something any current, maintained
  package does for consumption. No evidence anyone maintains a usable, current POI-via-IKVM
  NuGet package.
- Verdict: confirmed not a reasonable approach for .NET 8. NPOI (a native C# port, already
  under separate evaluation) is strictly the better path if POI-equivalent functionality is
  wanted in .NET.

### Lightweight / narrow-scope extractors — mostly not OSS or not relevant
- `FileFormat.Words for .NET` (fileformat-words/FileFormat.Words-for-.NET): MIT licensed, marketed
  as supporting DOC/DOCX/RTF/HTML/PDF/EPUB/ODT, but it is primarily a wrapper around the
  OpenXML SDK for DOCX manipulation; the standalone NuGet package has been deprecated/
  discontinued ("legacy and no longer maintained") even though the GitHub repo remains public.
  No clear evidence it does true binary .doc (CFB/HWPF) parsing rather than OOXML-only. Not a
  credible option for legacy binary format support.
- `unixfreak0037/officeparser`: Python, not applicable to .NET.
- fileformat.com's Node.js `officeparser`: JavaScript, not applicable to .NET.
- No other credible lightweight (non-full-POI-style) .doc/.ppt text/metadata extractor for
  .NET was found beyond the above.

## CFB/OLE2 readers found (beyond OpenMcdf)

### OpenMcdf forks/history
- `CodeCavePro/OpenMCDF`: a historical fork/packaging of ironfede/openmcdf for broader
  .NET Framework/.NET Standard targeting. **Archived by its owner on 2023-04-07, read-only.**
  No longer relevant — upstream `openmcdf/openmcdf` (now the canonical org, formerly
  `ironfede/openmcdf`) already multi-targets netstandard2.0, net8.0, and net10.0 directly,
  making this fork's original purpose obsolete.
- No other active forks of OpenMcdf were found. OpenMcdf itself is actively released (v3.2.0 is
  current; v3.x line adds write support, lazy loading, and streaming reads of large streams),
  and remains the only actively maintained pure-C# CFB library found.

### "StructuredStorage" or similar
- OpenMcdf's own project historically self-described as a "Structured Storage .net component"
  (see openmcdf.sourceforge.net) — this is the same project, not a distinct one. No separate,
  independently maintained "StructuredStorage" library for .NET was found.
- COM Structured Storage interop (calling the native Windows `IStorage`/`IStream` COM APIs
  directly from .NET, e.g. via P/Invoke as described in older "COM structured storage from
  .NET" articles) is possible but requires Windows-only COM interop, not a portable OSS library,
  and is a poor fit for a cross-platform .NET 8 library — not recommended.

### libolecf / libgsf ports
- `libyal/libolecf` (C library, part of the libyal forensics suite) has no .NET port found.
- GNOME's `libgsf` ("structured file library") has no .NET port found either.
- Both remain C/native libraries with no maintained C#/.NET bindings; not viable without
  building and shipping native interop, which defeats the "pure C#, easy to consume from .NET 8"
  goal implicit in evaluating OpenMcdf.

### Kaitai Struct-generated CFB parser (curiosity, not a library)
- formats.kaitai.io hosts a Kaitai Struct definition for Microsoft CFB that can *generate* a
  C# parser class. This is a code-generation template, not a maintained library/package — it
  would need to be generated, vendored, and maintained in-house, and provides only structural
  parsing (no OLE Properties, no directory-entry-to-stream convenience API that OpenMcdf
  provides). Not a practical alternative to OpenMcdf.

## Commercial libraries excluded

The following were surfaced during research and are excluded per the OSS-only constraint —
noted only for completeness, not evaluated further:
- Aspose (Aspose.Words, Aspose.Cells, Aspose.Slides, Aspose.Total) — excluded, commercial.
- GemBox (GemBox.Document, GemBox.Spreadsheet) — excluded, commercial.
- Syncfusion (Essential DocIO, XlsIO, Presentation) — excluded, commercial.
- Spire.Doc / Spire.XLS / Spire.Presentation (including "Free Spire.*" editions, which are
  feature-capped freemium tiers of a commercial product, not OSS) — excluded, commercial.
- GroupDocs.Parser / GroupDocs.Total — excluded, commercial (confirmed: paid licensing tiers,
  metered licensing, free-with-limitations evaluation mode only; not open source).
- LEADTOOLS — excluded, commercial (surfaced only in passing during search, not evaluated).

## Conclusion

**NPOI + OpenMcdf remain the best-fit OSS options for .NET 8** for this project's legacy
doc/xls/ppt + CFB needs. Nothing found in this scan is a better or more current fit:

- **b2xtranslator** (the other historically notable OSS legacy-format project) is dead upstream,
  but its actively maintained fork **DocSharp** (manfromarce/DocSharp) is a genuinely live,
  MIT-licensed, .NET 8/9/10-targeting project with releases as recent as 2026-08-11. It is worth
  tracking, but it is architected purely as a format *converter* (binary → OOXML/Markdown/etc.),
  explicitly has no embedded-OLE-object extraction yet (open TODO), and self-describes as a
  "hobby project" with no maintenance SLA. It does not replace NPOI's HWPF/HSSF/HSLF parsing +
  metadata + embedded-object APIs, which are more mature and purpose-built for extraction rather
  than conversion. It could be revisited later as a secondary/fallback conversion path, but not
  as a primary dependency now.
- **IKVM-wrapped Apache POI** is confirmed impractical: the one attempt found
  (`poi-ikvm`) is essentially an abandoned proof-of-concept with no real adoption, and using
  IKVM to translate the full POI/AWT dependency graph is fragile compared to NPOI's native port.
- No other lightweight OSS .doc/.ppt extractor of any real maturity was found.
- **No active alternative or fork of OpenMcdf exists.** The one fork found (CodeCavePro) is
  archived and functionally superseded by OpenMcdf's own current multi-targeting. No .NET port
  of libolecf/libgsf exists, and COM Structured Storage interop is a Windows-only, non-portable
  dead end. A Kaitai-Struct-generated CFB parser exists only as a code-gen template, not a
  library, and lacks OpenMcdf's higher-level convenience API.

Recommendation: proceed with NPOI + OpenMcdf as planned. Optionally keep DocSharp bookmarked as
a project to re-check in 6-12 months in case its OLE-object-extraction TODO is completed and it
matures beyond "hobby project" status — at that point it could become a useful conversion-based
alternative path, but it is not a reason to delay or change the current NPOI + OpenMcdf plan.

## Citations/links

- b2xtranslator (dirvo fork): https://github.com/dirvo/b2xtranslator
- b2xtranslator (.NET Core port): https://github.com/EvolutionJobs/b2xtranslator
- b2xtranslator (SebastiaanLubbers mirror): https://github.com/SebastiaanLubbers/B2XTranslator
- b2xtranslator on NuGet: https://www.nuget.org/packages/b2xtranslator
- b2xtranslator on SourceForge (original DIaLOGIKa project): https://sourceforge.net/projects/b2xtranslator/
- DocSharp (active fork): https://github.com/manfromarce/DocSharp
- DocSharp README: https://github.com/manfromarce/DocSharp/blob/main/README.md
- DocSharp_legacy (.NET Framework fork): https://github.com/fluxlinkage/DocSharp_legacy
- DocSharp.Binary.Doc on NuGet: https://www.nuget.org/packages/DocSharp.Binary.Doc
- DocSharp.Docx on NuGet: https://www.nuget.org/packages/DocSharp.Docx
- poi-ikvm: https://github.com/koustubhmoharir/poi-ikvm
- IKVM on NuGet: https://www.nuget.org/packages/ikvm/
- FileFormat.Words for .NET: https://github.com/fileformat-words/FileFormat.Words-for-.NET
- FileFormat.Words on NuGet (deprecated): https://www.nuget.org/packages/FileFormat.Words/24.12.0
- OpenMcdf (canonical, active): https://github.com/openmcdf/openmcdf
- OpenMcdf on NuGet: https://www.nuget.org/packages/OpenMcdf
- OpenMcdf (original ironfede repo, now under openmcdf org): https://github.com/ironfede/openmcdf
- OpenMCDF on SourceForge: https://openmcdf.sourceforge.net/
- CodeCavePro/OpenMCDF (archived fork): https://github.com/CodeCavePro/OpenMCDF
- libyal/libolecf: https://github.com/libyal/libolecf
- Kaitai Struct Microsoft CFB C# parser generator: https://formats.kaitai.io/microsoft_cfb/csharp.html
- GroupDocs.Parser licensing: https://docs.groupdocs.com/parser/net/evaluation-limitations-and-licensing/
