# Legacy .doc/.ppt parsing -- research spike (T-bac988f5)

Follow-up to `docs/research/npoi.md` and ADR-001, after T-d6de875f (self-compiling NPOI
as a git submodule) found that ADR-001's "NPOI covers doc/xls/ppt" assumption only holds
for **.xls**. This spike evaluates what to do for **.doc** and **.ppt** specifically.

## Confirmed starting facts (from T-d6de875f's submodule work)

- NPOI's `main/` tree (SDK-style, net8.0-buildable, already wired via the submodule) contains
  `HSSF` (xls), `POIFS` (generic CFB/OLE2 access), and **`HPSF`** -- the property-set reader
  for `SummaryInformation`/`DocumentSummaryInformation`. HPSF is format-agnostic: it reads the
  same CFB property-set streams regardless of whether the container is a doc/xls/ppt.
- `HWPF` (.doc body parser) and `HSLF` (.ppt body parser) exist only in NPOI's `scratchpad/`
  tree: old-style (ToolsVersion="4.0") .csproj, hard-targeted at .NET Framework 2.0, excluded
  from NPOI's own CI/build/pack, and not shipped in NPOI's published NuGet package at all
  (verified against the real 2.7.6 nupkg contents).

## Important decoupling this spike surfaces

`docs/specs/extraction.md` requires, per format: general metadata (title/author/etc, sourced
from SummaryInformation/DocumentSummaryInformation), format-specific metadata (word/page counts,
macro flag, etc -- largely also cached in those same property streams by the originating
Office app), flat text body, and first-layer subfiles (embedded objects, discoverable by walking
POIFS directory entries/naming conventions).

Because HPSF and POIFS already live in NPOI's `main/` tree and already build today, the
FileMetadata acceptance criteria on T-c4123b53 and T-517f89c5 are not actually blocked by the
scratchpad problem -- they can be read directly off the CFB property-set streams for .doc/.ppt
the same way they will be for .xls, independent of whichever approach is chosen below for body
text. The genuinely hard, still-unsolved problem is flat-text body extraction (and, to a
lesser extent, subfile discovery, which is also POIFS-tree-walking rather than HWPF/HSLF-dependent
in the common case). The implementation tasks should exploit this: wire metadata via
HPSF/POIFS now, regardless of which body-text library ADR-004 lands on.

## Candidates evaluated

### 1. Port NPOI's own scratchpad HWPF/HSLF to SDK-style/net8.0 ourselves

A public fork, IllidanS4/npoi (github.com/IllidanS4/npoi, forked from nissl-lab/npoi,
Apache-2.0, last pushed 2023-07-17), already did close to exactly this:
- scratchpad/HWPF/HWPF.csproj and scratchpad/HSLF/HSLF.csproj are both retargeted to
  Sdk="Microsoft.NET.Sdk" / netstandard2.0 (netstandard2.0 is net8.0-consumable).
- Commit history shows the actual diff needed was small: "update to netstandard2.0" +
  "remove AllowPartiallyTrustedCallers" (an assembly attribute invalid under modern CLR) +
  a couple of "fix new version errors" follow-ups.
- Published to NuGet as ScratchPad.NPOI.HWPF (52.4K downloads across versions, still small
  but non-zero real-world use) and ScratchPad.NPOI.HSLF (2.8K downloads).
- Pinned to NPOI 2.5.6 as a dependency -- older than our submodule's 2.7.6-rc1 pin, so we
  would not consume this fork's NuGet package directly, but it is a validated recipe for
  applying the same retarget to our own submodule's scratchpad/HWPF and scratchpad/HSLF trees.
- A second, independent, much staler fork exists: Fireprufe15/npoi -> published as
  NPOI.HWPFCore on NuGet (Apache-2.0, .NET Standard 2.0), but last touched 2019-07-05 (7
  years stale) and pinned to ancient NPOI 2.3.0. Lower-quality signal than IllidanS4's fork;
  noted for completeness only.

HWPF vs HSLF asymmetry found: scratchpad/HWPF exposes HWPFDocument -> Range/Paragraph
as a reasonably complete, genuinely-used-in-the-wild public surface (borne out by
NPOI.HWPFCore's real download count and two real dependent packages on NuGet). scratchpad/HSLF
does contain a real usermodel layer (Model/Slide.cs, Sheet.cs, TextRun.cs, TextBox.cs,
etc -- more than "just record classes"), but no top-level HSLFSlideShow-equivalent entry
point exists anywhere in NPOI's C# scratchpad (confirmed via GitHub code search across
nissl-lab/npoi and IllidanS4/npoi -- every HSLFSlideShow hit found is Java/Apache POI or
Android ports, none C#/NPOI). Opening a .ppt end-to-end via this route would require writing
the missing "open file -> walk records -> assemble Slide/TextRun objects" glue ourselves, not
just a mechanical retarget. This matches both this repo's own npoi.md research finding and
upstream Apache POI's own characterization of HSLF as the least mature/least-attention legacy
component.

### 2. b2xtranslator -- binary-to-OOXML converter

EvolutionJobs/b2xtranslator (github.com/EvolutionJobs/b2xtranslator, BSD-3-Clause, 87 stars,
0 external dependencies). Converts legacy .doc -> .docx, .xls -> .xlsx, and .ppt -> .pptx
in-process, producing real OOXML that the project's already-built, already-i18n-considered
DocumentFormat.OpenXml pipeline can then read directly -- no second parsing/metadata-mapping
code path needed for body text.

- Pedigree: originally written 2009 by DIaLOGIKa as part of Microsoft's own Office
  interoperability initiative, built directly against the published MS-DOC/MS-PPT/MS-XLS
  binary format specs (referenced by an official Microsoft interoperability team blog post at
  the time) -- not a hobbyist reverse-engineering effort.
- Ported to .NET Core in 2017 (EvolutionJobs fork, moved to System.IO.Compression).
  Both Doc/b2xtranslator.doc.csproj and Ppt/b2xtranslator.ppt.csproj are SDK-style,
  netcoreapp2.0, zero project dependencies beyond each other and Common. Retargeting to
  net8.0 should be a one-line TFM bump plus fixing whatever's since gone obsolete in the BCL --
  a much smaller, lower-risk task than adapting non-SDK-style, Framework-2.0-era MSBuild files.
- Stale: last real commit 2018-06-26 (~8 years). No upstream fixes will land for anything
  we discover -- same "we own any bugs we hit" risk class as self-porting NPOI's scratchpad,
  just relocated to a different codebase.
- Symmetric coverage: both Doc/ and Ppt/ projects exist as equally-developed, parallel-structured
  modules (FileFormat + MLMapping projects each), unlike NPOI's C# port
  where HWPF and HSLF are asymmetric in completeness.
- Unicode/RTL/CJK/emoji fidelity through the conversion step is unverified -- no evidence
  found either way. Same empirical-testing obligation already established project-wide for
  legacy formats (ADR-001, docs/specs/extraction.md unicode-fidelity requirement) applies
  here and must cover the b2x conversion step specifically, not just downstream OpenXml reading.

### 3. modeverv/dotnet-poi -- from-scratch Apache POI-equivalent port

Found via GitHub code search (the only hit for a C# HSLFSlideShow.cs outside Java/Android
ports). github.com/modeverv/dotnet-poi, Apache-2.0, not a fork -- a fresh reimplementation
covering HSSF/HWPF/HSLF/XSSF/XWPF/XSLF. Structurally the most complete-looking single answer
found (has both HWPF/UserModel/HWPFDocument.cs and HSLF/UserModel/HSLFSlideShow.cs, commit
messages claim "Java POI interop tests").

Rejected for now on trust/provenance grounds, not license grounds: the entire commit history
(HSSF through HWPF through HSLF through XWPF through packaging/CI) spans roughly 48 hours
(2026-05-07 to 2026-05-09), the repo is 3 months old with 1 star and no independent
adoption/scrutiny, and the commit-message density/structure is consistent with rapid,
largely-unattended AI-agent-driven development rather than a human-reviewed effort. Adopting an
unvetted, days-old, single-author reimplementation of complex legacy binary-format parsing as a
shipped-product dependency is a materially different risk than either NPOI (6.2k stars, years of
real bug reports/fixes) or b2xtranslator (spec-driven pedigree, real if dated adoption). Worth
re-checking in 6-12 months if it accumulates real usage; not recommended today.

### 4. LibreOffice headless subprocess conversion

soffice --headless --convert-to (or UNO API) can convert doc/xls/ppt uniformly, including to
plain text or OOXML. Mature, actively maintained, best real-world fidelity of anything evaluated
(LibreOffice's binary-format filters are long-lived, heavily field-tested code). License (MPL-2.0
core / LGPLv3-or-later for some components) poses no problem for subprocess invocation (no
static/dynamic linking, no derivative-work argument). Rejected as disproportionate for now:
requires bundling/installing a large (500MB+) desktop office suite in the deployment/runtime
environment, per-file process-spawn latency unlike every other in-process approach this project
uses, and a materially larger security/sandboxing surface for a service that processes
untrusted input files. Documented as the fallback of last resort if both b2xtranslator and a
scratchpad port fail empirical fidelity testing.

### 5. Rejected without deep evaluation

- Antiword / wvWare: .doc-only (no ppt), GPL/LGPL, effectively unmaintained since the mid-2000s,
  CLI/C-library shape requiring subprocess or P/Invoke interop for no fidelity advantage over
  the candidates above.
- Aspose.Words / Aspose.Slides / GemBox.Document / GemBox.Presentation / Syncfusion:
  commercial SDKs -- same class of paid-license problem this repo already rejected NPOI 2.8+'s
  NuGet EULA and NBomber for (ADR-001, ADR-002). Not OSS-only-compliant.

## Sources

- https://github.com/nissl-lab/npoi (scratchpad/HWPF, scratchpad/HSLF structure)
- https://github.com/IllidanS4/npoi (SDK-style scratchpad retarget precedent)
- https://www.nuget.org/packages/ScratchPad.NPOI.HWPF / ScratchPad.NPOI.HSLF
- https://github.com/Fireprufe15/npoi , https://www.nuget.org/packages/NPOI.HWPFCore
- https://github.com/EvolutionJobs/b2xtranslator
- https://blogs.msdn.microsoft.com/interoperability/2009/05/11/binary-to-open-xml-b2x-translator-interoperability-for-the-office-binary-file-formats/
- https://github.com/modeverv/dotnet-poi
- Apache POI upstream docs on HSLF maturity: https://poi.apache.org/components/slideshow/
