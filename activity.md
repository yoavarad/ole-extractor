# Activity Log

## 2026-09-14 — T-96b5181b: Implement >=50 concurrent-call exercise of both public functions — PR: https://github.com/yoavarad/ole-extractor/pull/80

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

## 2026-09-02 — T-b70522d2: Research full NPOI retirement (b2xtranslator for .xls too)
- Desk research + empirical spike: is b2xtranslator's Xls module viable for .xls, letting NPOI be fully retired?
- Forked b2xtranslator to github.com/yoavarad/b2xtranslator (org-controlled); retargeted Xls module to net8.0 (built clean); fixed a real NUL-terminator bug in Common/StructuredStorage/Common/InternalBitConverter.cs (culture-aware `IndexOf("\0")` silently truncated every OLE stream name under .NET Core's ICU globalization).
- Sourced + verified 2 real public-domain .xls fixtures from the govdocs1 corpus; ran an empirical conversion spike against the fork.
- Result: sheet/cell-text conversion is solid, but b2x's Xls output writes zero docProps metadata (Title/Creator/Created/Modified/LastModifiedBy all absent). Embedded-object preservation remains unconfirmed empirically (neither fixture had embeds) but desk research found no OleObjectMapping.cs equivalent in Xls's mapping code.
- Decision (docs/adrs/004-legacy-doc-ppt-parsing.md note, docs/research/legacy-xls-parsing.md): .xls stays on NPOI HSSF. Full NPOI retirement not adopted — NPOI's HPSF/POIFS roles for .xls remain required regardless of body-text library, so switching adds a dependency without letting NPOI be dropped.
- PR: https://github.com/yoavarad/ole-extractor/pull/64
## 2026-09-02 — T-f96fa73d: Record synthetic samples in samples/manifest.json
- T-4b624fef had wired the macro-embed generator/fixture/test but never run `SampleGenerator/Program.cs`, so `samples/synthetic/macro-embed/` had no files on disk. Ran the existing, already-tested generator (docx/xlsx/pptx only; legacy doc/xls/ppt remain `NotSupportedException` stubs pending ADR-001) to produce `sample.docm`/`.xlsm`/`.pptm`.
- Recorded all 3 in `samples/manifest.json`: composition rule, expected detected format, exact body text/metadata (byte-identical to existing entries), `expectedFormatMetadata.hasMacros`, exact subfile count/filename/size/packagePath — each independently verified against the real file via the `ExtractorOLE` CLI.
- Reviewer flagged one gap (fixed): manifest lacked an expected macro-flag field even though the code sets `HasMacros`; added `expectedFormatMetadata.hasMacros: true` to all 3 new entries.
- Brings the synthetic corpus to 9 of the acceptance criterion's stated 18 samples (3 rules x 3 achievable formats); legacy formats blocked on ADR-001 — flagged in a task comment for a scope/wording decision on criterion 1. Also flagged (out of scope to fix here): the pre-existing xlsx multi-embedding entry looks stale (detectable:false), since `ExtractionHelper`'s embedding scan was since made generic across all container parts.
- `ydk task done` initially failed on a stale cached `pr-body-validation` result (`.ydk/cache/verification/pr-body-validation/`); clearing the cache resolved it.
- PR: https://github.com/yoavarad/ole-extractor/pull/63
## 2026-09-12 — T-270c32fe: Source genuine human-provenance revision-history samples (docx/pptx; xlsx gap documented)
- Sourced 4 genuine (not scripted) human-provenance samples from the public-domain govdocs1 corpus (real, unmodified U.S. federal documents): 2 docx (national-park-service-forest-vegetation-monitoring-protocol-2008.docx, RevisionNumber=820; blm-interagency-hotshot-crew-peer-review-standards.docx, RevisionNumber=13) and 2 pptx (noaa-transition-team-briefing-2008.pptx, RevisionNumber=1154; ornl-quasi-elastic-neutron-scattering-introduction.pptx, RevisionNumber=436). Each verified directly from docProps/core.xml + app.xml: Author != LastModifiedBy, RevisionNumber > 1, real elapsed EditingDurationMinutes.
- xlsx: exhaustively scanned all 37 real .xlsx files in govdocs1's xlsx.zip — real Microsoft Excel never populates cp:revision (0/37), so the RevisionNumber>1 criterion is structurally unverifiable from this corpus for this format. Documented as a genuine corpus limitation in samples/manifest.json rather than forcing a fit; no genuine xlsx sample added. Acceptance criterion (2 per format across docx/xlsx/pptx) is therefore only partially met — 2/3 formats fully satisfied.
- Reviewer (cavecrew-reviewer): no issues — JSON valid, sha256/metadata re-verified against actual file bytes (4/4 match), pre-existing scripted entries' notes-only updated, nested-embedding samples untouched, no scope creep.
- Skipped pr-body-validation via `--skip-plugin` (same known pre-existing tool bug already documented in .ydk/hooks/pre-push and hit previously by sibling task T-0c83ff28).
- PR: https://github.com/yoavarad/ole-extractor/pull/66

## 2026-09-13 — T-83b4a029: Produce oversized-declared-size (bomb) and password-protected adversarial samples
- New `samples/adversarial/` directory holding docs/specs/dataset-curation.md's shared, format-agnostic adversarial set (first two of its seven scenarios).
- Oversized-declared-size sample (`samples/adversarial/oversized-declared-size-bomb.doc`, 2560 bytes on disk, targets doc/xls/ppt): new `SampleGenerator.Generators.AdversarialSampleGenerator.BuildCfbSizeBomb()` builds a minimal real CFB (OpenMcdf) and forges the FAT sector count at header offset 0x2C to 100000 (declared ~6.1GB), mirroring the exact forgery already proven in `CfbMimeDetectorGuardrailTests.Detect_ForgedFatSectorCountImplyingOver2GB_ThrowsOversizedNestedContentException`. Added `OpenMcdf 3.2.0` to `SampleGenerator.csproj` and a small block to `SampleGenerator/Program.cs`. Verified end-to-end against the real `ExtractorOLE` CLI: throws `OversizedNestedContentException` ("CFB FAT sector count 100000 at sector size 512 bytes implies 6553600000 bytes total (limit 2147483648)").
- Password-protected sample (`samples/adversarial/password-protected.docx`, 33792 bytes, targets docx): sourced verbatim from Apache POI's public test-data corpus (Apache License 2.0), `test-data/document/bug53475-password-is-solrcell.docx` — a genuine Word-produced, CFB-wrapped MS-OFFCRYPTO encrypted docx (password `solrcell`). Confirmed genuine (not just named to look the part) via a raw UTF-16LE byte scan of the file: both `EncryptionInfo` and `EncryptedPackage` stream names present.
- Recorded both in `samples/manifest.json` with a new `status: "adversarial"` and `targetFormats`/`targetFormatsNote`/`expectedBehavior` fields (no prior adversarial-set precedent existed in the manifest to follow, so these are new but minimal, in the spirit of the existing schema). Noted in each entry's `expectedBehavior.currentBehaviorNote` that password-protected detection isn't implemented in production code yet (only the bomb's guard is) — that's expected per T-83b4a029's infra/tooling scope; Epic 9's integration tests assert against these later.
- Full `SampleGenerator/Program.cs` run regenerates all samples non-deterministically (documented pre-existing behavior — random GUID/timestamp per zip-container write); reverted the incidental byte-diffs on unrelated pre-existing sample files (`git checkout --`) to keep this change surgical.
- `dotnet test ExtractorOle/ExtractorOLE.Tests`: 129 passed, 0 failed.
- PR: (opened via `ydk task done`)

## 2026-09-13 — T-7cdaeceb: Produce wrong-extension and corrupt-embedded-object adversarial samples
- Added `samples/adversarial/wrong-extension-sample.txt`: a real docx package (valid body text, no embeddings) saved with a misleading `.txt` extension — proves content-based MIME detection ignores the filename/extension entirely.
- Added `samples/adversarial/corrupt-embedded-object.docx`: a docx with 3 first-layer embeddings (an embedded xlsx, an inline PNG, and a third blob), where the third embedding's zip entry is bitwise-corrupted in place after generation via the existing `SampleGenerator.Generators.ZipEntryCorruptor.CorruptEntryData` (leaving the Central Directory and the other two embeddings byte-identical) — proves the per-subfile-failure rule: extraction succeeds and returns the 2 valid embeddings, silently omitting the corrupt one (stderr warning), rather than failing the whole call.
- Both generators wired into `SampleGenerator/Program.cs` behind a new `--adversarial` CLI flag (`GenerateAdversarialSamples()`), gated so normal generator runs don't touch these files or regenerate the existing (partly non-deterministic) sample corpus.
- Both samples registered in `samples/manifest.json` with exact CLI-verified `expectedDetectedFormat`/`expectedSubfileCount`/`expectedSubfiles`, each citing the corresponding existing unit test that already proves the same behavior at the code level (`OoxmlMimeDetectorTests.Detect_WrongExtension_StillDetectsCorrectFormat`; `DocxEmbeddedXlsxAndCorruptionTests.Docx_CorruptedEmbeddedPart_IsOmittedAndLogged_WithoutFailingExtraction`).
- Both tagged to docx as their target format — this is the shared (not per-format) adversarial set per `docs/specs/dataset-curation.md`.
- All verification plugins passed (dotnet-build, dotnet-format, dotnet-quality, dotnet-test, etc.).
- PR: https://github.com/yoavarad/ole-extractor/pull/71

## 2026-09-13 — T-3bddbe75: Produce corrupt, truncated, and zero-byte adversarial samples
- Added `SampleGenerator/Generators/CorruptedContainerSampleGenerator.cs` and wired it into `SampleGenerator/Program.cs` (new `samples/adversarial/` output block) so the corrupt/truncated samples are reproducibly derived from the existing valid `samples/synthetic/multi-embedding/sample.docx` (3714 bytes), not one-off/undocumented binary edits.
- `corrupt-container.docx` (3714 bytes): every byte from the start of the ZIP Central Directory (offset 3022) through end-of-file is bitwise-complemented, destroying the Central Directory and End Of Central Directory record while leaving all local file headers/compressed data byte-identical to the source — still sniffs as a zip/docx via the leading `PK\x03\x04` signature but has no locatable Central Directory, so full parsing fails (`ydk:error:extraction/corrupt-file`).
- `truncated-container.docx` (222 bytes): keeps only the first 200 bytes plus the original, unmodified 22-byte End Of Central Directory record. That EOCD still declares a Central Directory offset/size implying the container should extend to byte 3692, but the file is only 222 bytes long (`ydk:error:extraction/truncated-container`).
- `zero-byte.docx` (0 bytes): a literal empty file, written directly since it needs no derivation.
- All 3 placed in a new `samples/adversarial/` directory (flat, not per-format, matching the convention established by sibling adversarial tasks T-83b4a029/T-7cdaeceb) and registered in `samples/manifest.json` with `format`/`status: "adversarial"`, `targetFormats`, `targetFormatsNote`, and an `expectedBehavior` block citing the relevant error component (`ydk:error:extraction/corrupt-file` or `ydk:error:extraction/truncated-container`) — same schema shape sibling PRs #70/#71 used for their non-extractable adversarial entries.
- `detectable: false` on all 3 pending empirical verification against a running ExtractorOLE build — no extractor code path exercises these error paths yet, matching this task's test_strategy ("No test — infra/tooling task; these samples are asserted against by Epic 9's integration tests once recorded in the corpus manifest").
- Note: sibling tasks T-83b4a029 (PR #70) and T-7cdaeceb (PR #71) also touch `samples/manifest.json` and `activity.md` on parallel branches off the same base — expect merge conflicts at merge time between these 3 PRs; not resolved here.
- PR: https://github.com/yoavarad/ole-extractor/pull/72

## 2026-09-14 — T-48110fcb: Fix stale expectedBehavior/errorId on corrupt-container.docx and truncated-container.docx manifest entries — PR: https://github.com/yoavarad/ole-extractor/pull/78

## 2026-09-14 — T-ef73861f: Wire DetectMimeType facade to the OOXML + CFB detection registry — PR: https://github.com/yoavarad/ole-extractor/pull/79
