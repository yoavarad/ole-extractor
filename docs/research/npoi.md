# NPOI — Research Spike

Repo: https://github.com/nissl-lab/npoi (canonical, by tonyqus/nissl-lab). Note: an older fork `dotnetcore/npoi` also exists but is not the actively-developed line — use nissl-lab.

## Library name

**NPOI** — a C#/.NET port of Apache POI. NuGet packages:
- `NPOI` — main package (HSSF/XSSF spreadsheets, XWPF/HWPF documents bundled in recent versions, OOXML support, POIFS).
- `NPOI.OOXML` — in some historical versions OOXML (XSSF/XWPF/XSLF) support shipped as a separate package from core `NPOI` (which had HSSF/HWPF + POIFS). Recent 2.x releases have consolidated more into the main `NPOI` package — verify exact package split for whatever version is pinned, since this has moved around across releases.
- `NPOI.HWPF` — has also been published as a standalone package at times (e.g. 2.3.0 on NuGet) — signals HWPF is treated as a semi-detachable "scratchpad" component, consistent with upstream Apache POI's own treatment of HWPF/HSLF as less mature than HSSF/XSSF.

## License — IMPORTANT FINDING

Source code license: **Apache-2.0** (OSI-approved), shown as the repo's license badge. This part is unambiguous OSS.

**However**, starting with **NPOI 2.8.0 (released ~April 6–10, 2026)**, the project added an **Open Source Maintenance Fee (OSMF) EULA** that applies to *binary releases* (the precompiled DLLs published on GitHub Releases and NuGet.org), not to the source code itself:
- The EULA (`OSMFEULA.txt` in the repo) requires a **monthly fee from any organization/user generating revenue with annual gross revenue ≥ US$10,000**, on a tiered scale ($15/mo small orgs up to $99/mo for 100+ staff orgs), enforced via GitHub Sponsors.
- Orgs under the $10k/year revenue threshold, hobbyists, and freelancers are explicitly exempt.
- Per the EULA text itself: *"The Software's source code is licensed to User under the OSI License and remains freely distributable... The Fee is not a license fee."* Non-payment "may result in suspending access to the Binary Release" but explicitly **does not restrict self-compiling from source** or redistributing binaries obtained elsewhere.
- Net effect: this is a "fair-source"-style binary paywall layered on top of a genuinely open-source (Apache-2.0) codebase — comparable in spirit to Sentry/other open-core moves. A revenue-generating company consuming the NuGet package as-is would technically owe the fee; the same company self-building from the Apache-2.0-licensed source would not, per the EULA's own text.
- **Action item for this project**: given the stated "OSS-only, no commercial libraries" constraint, this needs an explicit decision: (a) treat NPOI 2.8.0+ as now carrying a commercial obligation and disqualify it or budget for the fee, (b) pin to the last pre-fee release (2.7.x, pure Apache-2.0 binary, no EULA) and accept forgoing future fixes, or (c) self-compile from source under Apache-2.0 to sidestep the EULA per its own terms. This is a policy question, not a technical one — flagging for explicit decision rather than assuming.

Sources: [OSMFEULA.txt](https://github.com/nissl-lab/npoi/blob/master/OSMFEULA.txt), [Issue #1785 – Introduction to Open Source Maintenance Fee license](https://github.com/nissl-lab/npoi/issues/1785), [Tony Qu – What's New in NPOI 2.8.0](https://tonyqus.medium.com/whats-new-in-npoi-2-8-0-a-new-chapter-for-sustainable-open-source-e0a9ae383a07), [NPOI 2.8.0 Release Notes (Discussion #1751)](https://github.com/nissl-lab/npoi/discussions/1751).

## Maintenance signal

- **Stars**: ~6.2k (fork count ~1.5k) on nissl-lab/npoi.
- **Commits**: ~4,694 total commits on master; recent activity present (commits within the last couple of months as of this research).
- **Releases**: actively cut — 2.7.4 → 2.7.6 → 2.8.0 (incl. RCs 2.8.0-rc2/rc3) through April 2026. Release cadence looks healthy, several releases per year.
- **Open issues**: ~51 open at time of check — a manageable, not-abandoned backlog.
- **Overall**: this reads as a healthy, actively maintained project by conventional OSS signals (stars, commit recency, release cadence) — the maintenance-fee move (above) appears to be a funding-sustainability effort rather than a sign of the project dying; if anything it signals the maintainer intends to keep investing. But it does change the calculus on "is this free to use" for a commercial consumer.

## API surface relevant to our use case

**POIFS (CFB/OLE2 layer)** — yes, it is exposed as a usable, generic public API independent of HSSF/HWPF/HSLF:
- Classes mirror Apache POI's Java API: `POIFSFileSystem`, `DirectoryEntry`/`DirectoryNode`, `DocumentEntry`/`DocumentNode`, plus an event-driven reader (`POIFS/EventFileSystem/POIFSReader.cs`).
- `DirectoryEntry` supports enumerating child entries generically (analogous to Apache POI's `getEntries()`), so walking an arbitrary CFB container's storage/stream tree without knowing the format ahead of time is supported.
- This means **file-type sniffing by inspecting root-level stream names** (`"WordDocument"` → doc, `"Workbook"`/`"Book"` → xls, `"PowerPoint Document"` → ppt) is a viable, low-level approach using POIFS directly, independent of whether you then use HWPF/HSSF/HSLF to actually parse the content.
- **Embedded OLE object extraction**: consistent with upstream Apache POI conventions (which NPOI mirrors closely), embedded objects live in known locations — Excel typically stores them in root-level subdirectories prefixed `MBD########`; Word stores them under an `ObjectPool` subdirectory; the native/raw payload for a non-OLE-aware embed sits in a stream named `\1Ole10Native`. There isn't a single "give me all embeds" call — the general pattern (true in Java POI, presumably ported as-is in NPOI since it's a fairly direct port) is: iterate the directory tree, identify subdirectories that look like OLE storages, and hand each `DirectoryEntry` + the `POIFSFileSystem` to the appropriate constructor/opener. This is workable but is manual tree-walking, not a one-line "extract embeds" API — plan for writing that traversal logic ourselves.

**HWPF (legacy .doc)**:
- Reads/writes Word 97 format; only limited read support for Word 6/Word 95 (older files throw `OldWordFileFormatException`); missing table stream can cause a doc to be flagged corrupt.
- Described by upstream POI's own docs (which NPOI inherits verbatim as a port) as "moderately functional" — text extraction is strong, but other features (styles, complex formatting) are incomplete and may require dropping to low-level record access. Error-checking is described as sparse — malformed-but-plausible files can produce corrupted output on write, less of a concern for us since we're read/extract-only.
- No common interface shared with XWPF (OOXML docx) — HWPF and XWPF are separate object models, so a "read either doc or docx transparently" abstraction is on us to build.
- A multi-thread bug (`InvalidOperationException: Collection was modified`) has been reported and reportedly addressed; 2.3.0 shipped bugfixes for `HWPFDocument` load failures — evidence this component still receives real bug fixes, but also evidence it's had real bugs.

**HSLF (legacy .ppt)**:
- Handles the older binary PPT format; per upstream POI's own characterization (ported to NPOI docs unchanged in spirit), this is the least mature of the three legacy parsers. Known upstream limitations are largely about *rendering* (Graphics2D compliance, font/line-break differences vs real PowerPoint) — less relevant to a pure extraction (not rendering) use case, but it signals HSLF overall gets the least engineering attention of the three legacy formats.
- No common interface with XSLF (OOXML pptx), same caveat as above.
- Text/shape/metadata extraction is reported as functional for basic cases; treat as the least battle-tested of the three legacy formats for anything beyond straightforward text pull.

**HSSF (legacy .xls)**: not specifically flagged with major issues in this search — HSSF is historically the most mature/most-used component of the POI family (both Java and NPOI ports), and is NPOI's flagship feature ("NPOI is still the king of legacy Excel processing in .NET" — Tony Qu). Treat as the most trustworthy of the three legacy formats.

**OOXML support (XWPF/XSSF/XSLF) as alternative to DocumentFormat.OpenXml**:
- Yes, NPOI can read/write docx/xlsx/pptx via XWPF/XSSF/XSLF (and SXSSF for streaming-write large xlsx). This is functionally overlapping with what the project already does via DocumentFormat.OpenXml.
- General positioning found: NPOI's OOXML API is **higher-level/more convenient for common tasks** (e.g., "give me the value of this cell", "give me the text of this paragraph") than the OpenXml SDK's much lower-level, schema-literal object model — at the cost of being less complete/less spec-faithful for edge cases and advanced formatting. Since this project already has a working DocumentFormat.OpenXml integration for the OOXML formats, **there is no clear case to replace it with NPOI's XWPF/XSSF/XSLF** — the main reason to pull NPOI in at all is for legacy OLE format support (doc/xls/ppt) and possibly for POIFS-based generic CFB introspection, not for OOXML.
- No detailed line-by-line feature parity matrix was found in this search; comparisons found were high-level/anecdotal (blog posts), not a maintained compatibility table.

## Strengths

- Only mainstream, actively-maintained pure-.NET (no COM/interop, no Office install required) library that covers **all three legacy binary formats** (doc/xls/ppt) plus OOXML, in one dependency.
- HSSF (xls) specifically is mature and widely production-proven.
- POIFS gives low-level, generic CFB/OLE2 access — useful for both file-type sniffing by stream name and manual embedded-object extraction, independent of the higher-level parsers.
- Direct, fairly faithful port of Apache POI's Java API — meaning Apache POI's much larger body of documentation, StackOverflow answers, and bug history largely transfers conceptually (though not always exactly, since NPOI's own bug list can differ from upstream Java POI's).
- .NET 8 explicitly targeted/supported as of 2.7.6 and 2.8.0.
- Real release cadence and commit activity — not abandoned.

## Weaknesses / gaps

- **Licensing complication (see above)**: 2.8.0+ binaries carry a paid-maintenance obligation for revenue-generating orgs over $10k/year revenue, conflicting with a strict "OSS-only, no commercial cost" policy unless self-compiled from source or pinned to a pre-2.8.0 Apache-2.0-only binary.
- HWPF (doc) and especially HSLF (ppt) are, by the project's/upstream's own characterization, less mature than HSSF — sparser error-checking, documented past bugs (multi-thread issue, load failures fixed in 2.3.0), no unified interface with their OOXML counterparts.
- No single high-level "extract all embedded objects" API — embedded-OLE extraction via POIFS requires writing the directory-tree-walk / naming-convention logic ourselves (MBD-prefixed dirs, ObjectPool, `\1Ole10Native` stream), mirroring what one would do in Java POI.
- Documented memory/performance concerns on large files (reports of memory ballooning, e.g. 300MB → 3GB on large-data Excel generation) and third-party benchmarks (from a competing commercial vendor, so read with skepticism) claiming NPOI is dramatically slower than commercial alternatives on heavy formula-recalculation workloads. Less directly relevant to us since we're extracting/reading, not doing heavy formula computation or writing huge files, but worth load-testing on real large legacy files before committing.
- No dedicated, first-party documentation on Unicode/RTL/emoji correctness (see below) — have to infer from scattered bug history rather than an explicit compatibility statement.

## Unicode/RTL/emoji findings

- Direct, targeted search for Hebrew/Arabic/Persian/RTL-specific NPOI issues turned up **nothing NPOI-specific** — no open or historical GitHub issues specifically about RTL/bidi text in legacy binary formats were found in this search. This is a gap in available evidence, not a confirmation of correctness — legacy binary Word/Excel/PowerPoint formats do use internal compressed (CP1252-range, 1 byte/char) vs. uncompressed (UTF-16LE, 2 bytes/char) string encoding switches (Apache POI's `HWPF`/`HSSF` handle this via internal "unicode flag" bits per string), and NPOI as a port should replicate that logic, but no independent verification/bug report confirming correct RTL/bidi handling in NPOI's port was found.
- Confirmed evidence exists of NPOI having and fixing a **surrogate-pair (i.e., emoji/rare-CJK, code points above the BMP) bug**: NPOI 2.7.4 release notes cite a fix for "Bug 61246" where SXSSF (streaming xlsx writer) sheet data had Unicode surrogate characters replaced with `?` — i.e., a real, confirmed-and-fixed mojibake/truncation bug for exactly the class of character (emoji, supplementary-plane CJK) this project cares about. This is evidence both that (a) such bugs have existed, and (b) the maintainers do fix them when reported — but it's from the OOXML streaming-write path (SXSSF), not directly evidence about the legacy HWPF/HSSF/HSLF read path we'd actually be using.
- **Recommendation given the gap**: since exotic-script and emoji correctness is a hard constraint for this project and NPOI's track record here is thin/unverified for the legacy-format read path specifically, this needs empirical validation before relying on NPOI for text extraction — build a small test corpus of real .doc/.xls/.ppt files containing Hebrew, Arabic, Persian, Russian, CJK, and emoji content and verify NPOI's extracted text against expected values, rather than trusting the port's fidelity by inference from Apache POI's Java behavior.

## Performance signal

- No first-party/maintained benchmark suite found from the NPOI project itself.
- A third-party vendor benchmark (Mescius/GcExcel, a commercial competitor — treat as biased marketing material) claims their product is ~190x faster and ~22x more memory-efficient than NPOI on formula-heavy large-xlsx workloads; not independently verified and not representative of a read/extract-metadata-and-text workload like ours.
- A GitHub issue (#560) reports a memory leak / high memory usage on large data volumes.
- No performance data specific to reading/extracting from legacy doc/xls/ppt (our actual use case) was found — should be measured directly against representative file sizes if NPOI is adopted.

## Citations / links

- Repo: https://github.com/nissl-lab/npoi
- Releases: https://github.com/nissl-lab/npoi/releases
- NuGet (NPOI): https://www.nuget.org/packages/NPOI/
- NuGet (NPOI.HWPF, standalone historical package): https://www.nuget.org/packages/NPOI.HWPF
- OSMF EULA text: https://github.com/nissl-lab/npoi/blob/master/OSMFEULA.txt
- Maintenance fee announcement issue: https://github.com/nissl-lab/npoi/issues/1785
- NPOI 2.8.0 release notes discussion: https://github.com/nissl-lab/npoi/discussions/1751
- Tony Qu (maintainer), "What's New in NPOI 2.8.0: A New Chapter for Sustainable Open Source": https://tonyqus.medium.com/whats-new-in-npoi-2-8-0-a-new-chapter-for-sustainable-open-source-e0a9ae383a07
- Tony Qu, "Why NPOI Is Still the King of Legacy Excel Processing in .NET": https://tonyqus.medium.com/why-npoi-is-still-the-king-of-legacy-excel-processing-in-net-06ec4bbb2327
- Apache POI (upstream, for HWPF/HSLF format-maturity context which NPOI ports): https://poi.apache.org/components/document/ and https://poi.apache.org/components/slideshow/
- Apache POI POIFS embedded-object docs (conceptually mirrored by NPOI's POIFS port): https://poi.apache.org/components/poifs/embeded.html
- Memory leak issue: https://github.com/nissl-lab/npoi/issues/560
- .NET Framework AutoSizeColumn font exception issue: https://github.com/nissl-lab/npoi/issues/1485
- Third-party (competitor) performance comparison, read with skepticism: https://developer.mescius.com/blogs/npoi-alternative-spreadsheet-api-performance-test-c-sharp-net
