# Activity Log

## 2026-08-22 — T-eeb6684b: Downgrade ExtractorOLE.csproj to net8.0
- Changed TargetFramework net10.0 -> net8.0 (deliberate downgrade per docs/project-rules.md).
- Reviewer: no other files/refs touched, packages (DocumentFormat.OpenXml 3.5.1, Microsoft.Extensions.DependencyInjection 8.0.0) net8.0-compatible.
- Build verified: `dotnet build ExtractorOLE/ExtractorOLE.csproj` — 0 errors, 0 warnings.
- Acceptance criteria updated: replaced unbuildable `ExtractorOLE.slnx` target (unsupported by .NET 8 SDK CLI, MSB4068) with direct `.csproj` build in test_strategy + acceptance criteria.
- Skipped pre-existing, unrelated plugin failures: dotnet-format (whitespace in ExtractorOLE/Helpers/ExtractionHelper.cs, pre-dates this task), pr-body-validation (known broken, already in pre-push IGNORE_LIST).
- PR: https://github.com/yoavarad/ole-extractor/pull/4

## 2026-09-02 — T-b70522d2: Research full NPOI retirement (b2xtranslator for .xls too)
- Desk research + empirical spike: is b2xtranslator's Xls module viable for .xls, letting NPOI be fully retired?
- Forked b2xtranslator to github.com/yoavarad/b2xtranslator (org-controlled); retargeted Xls module to net8.0 (built clean); fixed a real NUL-terminator bug in Common/StructuredStorage/Common/InternalBitConverter.cs (culture-aware `IndexOf("\0")` silently truncated every OLE stream name under .NET Core's ICU globalization).
- Sourced + verified 2 real public-domain .xls fixtures from the govdocs1 corpus; ran an empirical conversion spike against the fork.
- Result: sheet/cell-text conversion is solid, but b2x's Xls output writes zero docProps metadata (Title/Creator/Created/Modified/LastModifiedBy all absent). Embedded-object preservation remains unconfirmed empirically (neither fixture had embeds) but desk research found no OleObjectMapping.cs equivalent in Xls's mapping code.
- Decision (docs/adrs/004-legacy-doc-ppt-parsing.md note, docs/research/legacy-xls-parsing.md): .xls stays on NPOI HSSF. Full NPOI retirement not adopted — NPOI's HPSF/POIFS roles for .xls remain required regardless of body-text library, so switching adds a dependency without letting NPOI be dropped.
- PR: https://github.com/yoavarad/ole-extractor/pull/64
