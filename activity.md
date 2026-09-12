# Activity Log

## 2026-09-02 — T-07188baf: Wire Program.cs to invoke both public functions on a real file
- Investigated Program.cs and found it already invokes `IExtractionHelper.DetectMimeTypeFromBytes` then `MainExtractor.Extract` in sequence against a real file (CLI arg or default fallback to `samples/curated/docx/invoice-acme-corp.docx`), printing real detection/extraction output.
- Verified by building and running the console app: real detected format, metadata, extracted text preview, and embedded file list printed to stdout — no placeholder output.
- No code changes required; both acceptance criteria already satisfied by prior work (T-90924b6d). Updated task frontmatter (acceptance_criteria done: true) and Activity Log only.
- All 40 verification plugins passed (dotnet-build, dotnet-format, dotnet-quality, dotnet-test, etc.).
- PR: https://github.com/yoavarad/ole-extractor/pull/61

## 2026-08-22 — T-eeb6684b: Downgrade ExtractorOLE.csproj to net8.0
- Changed TargetFramework net10.0 -> net8.0 (deliberate downgrade per docs/project-rules.md).
- Reviewer: no other files/refs touched, packages (DocumentFormat.OpenXml 3.5.1, Microsoft.Extensions.DependencyInjection 8.0.0) net8.0-compatible.
- Build verified: `dotnet build ExtractorOLE/ExtractorOLE.csproj` — 0 errors, 0 warnings.
- Acceptance criteria updated: replaced unbuildable `ExtractorOLE.slnx` target (unsupported by .NET 8 SDK CLI, MSB4068) with direct `.csproj` build in test_strategy + acceptance criteria.
- Skipped pre-existing, unrelated plugin failures: dotnet-format (whitespace in ExtractorOLE/Helpers/ExtractionHelper.cs, pre-dates this task), pr-body-validation (known broken, already in pre-push IGNORE_LIST).
- PR: https://github.com/yoavarad/ole-extractor/pull/4
