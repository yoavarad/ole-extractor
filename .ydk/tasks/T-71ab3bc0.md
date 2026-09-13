---
id: T-71ab3bc0
title: Assemble and finalize samples/manifest.json as the ground-truth corpus manifest
story: S-6bc8566f
status: done
assignee: null
labels:
- in-review
dependencies:
- T-f96fa73d
- T-c573e72e
- T-3bddbe75
- T-83b4a029
- T-7cdaeceb
spec_refs:
- docs/specs/dataset-curation.md
component_refs:
- ydk:nfr:extraction/nesting-depth-guard
test_strategy: "No test \u2014 infra/tooling task; this is the manifest-validation\
  \ gate Epic 9's integration tests depend on."
acceptance_criteria:
- text: "27 of the originally-scoped 37 samples exist and are fully recorded in samples/manifest.json\
    \ (9 synthetic [3 rules x 3 achievable formats \u2014 docx/xlsx/pptx] + 11 curated\
    \ + 7 adversarial), accepted as the complete corpus under current project scope;\
    \ the remaining 10 (9 synthetic + 1 curated legacy doc/xls/ppt samples) are out\
    \ of scope pending resolution of the NPOI/legacy-format gap (ADR-001) and are\
    \ deferred to follow-up task T-07571b32 (blocked on ADR-001)"
  done: false
- text: Every sample file on disk under samples/ has a corresponding manifest entry;
    a sample without a manifest entry does not count toward corpus coverage
  done: false
- text: Every manifest entry's format, composition-rule, expected-detected-format,
    and exact-expected-content fields are present and non-placeholder
  done: false
milestone: null
complexity: null
gates: []
created: '2026-08-19T12:47:21Z'
updated: '2026-09-13T20:51:38Z'
---

## Description

Merge the synthetic, curated, and adversarial manifest entries into one finalized samples/manifest.json, validating every sample file on disk has a corresponding manifest entry and every manifest entry points to a real sample file, closing out Dataset Curation as the ground truth the test suite asserts against.


## Activity Log
### 2026-09-13T20:14:58Z (UTC)
Executed PLAN-T-71ab3bc0.md's field-gap fixes in samples/manifest.json (27/27 samples, no new files added/removed):

- **Criterion 1 reworded** (per task-owner decision, pattern matches T-f96fa73d): accepted 27/27 as the complete corpus under current scope; the remaining 10 (9 synthetic + 1 curated legacy doc/xls/ppt) deferred to new follow-up task **T-07571b32** ("Generate remaining 10 legacy-format samples once ADR-001 lands"), created via `ydk task create` and immediately `ydk task block`'d with reason "ADR-001". `done` left `false` on this criterion, as instructed.
- **`format: "adversarial"` fixed on 4 entries** (was a placeholder duplicating `status`, not a real format enum): `oversized-declared-size-bomb.doc` -> `doc`, `corrupt-container.docx` -> `docx`, `truncated-container.docx` -> `docx`, `zero-byte.docx` -> `docx` -- each set to `targetFormats[0]` for that entry. `targetFormats` arrays left unchanged.
- **`expectedDetectedFormat` added to the 5 entries missing it**, determined empirically (not from prose) by copying `CfbMimeDetector.cs`/`OoxmlMimeDetector.cs` byte-for-byte into a standalone scratch harness -- this repo's own ExtractorOLE build is currently unbuildable here (NPOI submodule at third_party/npoi is empty, confirming the pre-existing ADR-001 gap) -- and running it against the real files on disk:
  - `password-protected.docx` -> `{Doc, application/msword}`. Empirically surprising: a root-entry dump shows this genuine Apache POI MS-OFFCRYPTO file has a legacy `WordDocument` stream alongside `EncryptionInfo`/`EncryptedPackage`, so CfbMimeDetector structurally matches Doc, not Docx, despite top-level format/targetFormats being docx.
  - `zero-byte.docx` -> `{Unknown, application/octet-stream}`. Matches its existing `expectedBehavior.outcome` text -- no conflict.
  - `corrupt-container.docx` and `truncated-container.docx` -> both `{Unknown, application/octet-stream}`. **This contradicts each entry's own pre-existing `expectedBehavior.outcome` text ("detected as docx, then rejected on full parse")** -- empirically, `Package.Open` cannot even locate a valid EOCD/central directory for either corrupted/truncated file, so `OoxmlMimeDetector` returns `Unknown` before any full-parse step is reached. Flagged in each entry's `notes` rather than silently rewriting `expectedBehavior` (out of this task's authorized field-gap scope); task owner/reviewer should decide whether T-3bddbe75's outcome prose needs a follow-up correction.
  - `oversized-declared-size-bomb.doc` -> recorded as `{Doc, application/msword}` with a `notes` caveat: empirically confirmed genuinely ambiguous -- `CfbMimeDetector` throws `OversizedNestedContentException` from its declared-size guard *before* inspecting any entry names, so no `DetectedFormatEnum` value is ever actually computed; `Doc` is a nominal placeholder tied to `targetFormats[0]`, not an observed value.
- **Gap (3) untouched, as instructed**: the 2 curated pptx entries (`noaa-transition-team-briefing-2008.pptx` count 58, `ornl-quasi-elastic-neutron-scattering-introduction.pptx` count 83) still have `expectedSubfileCount` > 0 with empty `expectedSubfiles: []` -- pre-existing, documented, deliberately deferred by T-c573e72e; left as-is.
- **Gap (4) confirmed as no fix needed**: checked `SampleGenerator/Fixtures/CuratedSampleSpecs.cs` (`BuildMeetingMinutesBoardroom`, `BuildProjectStatusReport`, `BuildProductLaunchDeck` -- none set a `Metadata` dictionary, unlike sibling specs that do) and `SampleGenerator/Program.cs`'s `GenerateAdversarialSamples()` (`wrong-extension-sample.txt`, `corrupt-embedded-object.docx` -- neither sets `Metadata` either). All 5 `metadata: {}` entries are the generator's genuine, non-placeholder output -- left unchanged.
- Re-ran the §6 validation script (written fresh, deleted after use): 27 entries / 27 disk files, 0 manifest-entries-without-file, 0 disk-files-without-entry, 0 missing `expectedDetectedFormat`, 0 remaining `format: "adversarial"` placeholders.
- Scratch verification harness (`/tmp/mimecheck`, outside the repo) deleted after use. No files touched under `SampleGenerator/`, `ExtractorOle/`, or `docs/` (read-only, for verification).
### 2026-09-13T20:24:35Z (UTC)
**Verification FAILED:**
FAIL dotnet-build: === ExtractorOle\ExtractorOLE\ExtractorOLE.csproj ===
Determining projects to restore...
  Skipping 
FAIL dotnet-quality: [dotnet-build] === ExtractorOle\ExtractorOLE\ExtractorOLE.csproj ===
Determining projects to restore
FAIL dotnet-test: Determining projects to restore...
  Skipping project "C:\Users\yoava\Projects\ole-extractor\.ydk\wo
### 2026-09-13T20:25:38Z (UTC)
**Verification FAILED:**
FAIL dotnet-build: === ExtractorOle\ExtractorOLE\ExtractorOLE.csproj ===
Determining projects to restore...
  Skipping 
FAIL dotnet-quality: [dotnet-build] === ExtractorOle\ExtractorOLE\ExtractorOLE.csproj ===
Determining projects to restore
FAIL dotnet-test: Determining projects to restore...
  Skipping project "C:\Users\yoava\Projects\ole-extractor\.ydk\wo
### 2026-09-13T20:26:31Z (UTC)
**Verification FAILED:**
FAIL dotnet-build: === ExtractorOle\ExtractorOLE\ExtractorOLE.csproj ===
Determining projects to restore...
  Skipping 
FAIL dotnet-quality: [dotnet-build] === ExtractorOle\ExtractorOLE\ExtractorOLE.csproj ===
Determining projects to restore
FAIL dotnet-test: Determining projects to restore...
  Skipping project "C:\Users\yoava\Projects\ole-extractor\.ydk\wo
### 2026-09-13T20:29:01Z (UTC)
## Verification Proof

OK dotnet-build (4.7s)
OK dotnet-format (39.0s)
OK dotnet-quality (43.3s)
OK ai-code-review (0.0s)
OK cli-error-handling (0.0s)
OK cli-output-format (0.0s)
OK fastapi-adapter-isolation (0.0s)
OK fastapi-core-purity (0.0s)
OK fastapi-no-mocks-e2e (0.0s)
OK fastapi-no-mocks-integration (0.0s)
OK fastapi-route-delegation (0.0s)
OK fastapi-route-splitting (0.0s)
OK fastapi-service-sync (0.0s)
OK nextjs-file-sizes (0.0s)
OK nextjs-fsd-imports (0.0s)
OK nextjs-fsd-layers (0.0s)
OK nextjs-module-density (0.0s)
OK nextjs-no-direct-env (0.0s)
OK nextjs-no-direct-fetch (0.0s)
OK nextjs-no-direct-ui-imports (0.0s)
OK nextjs-no-query-key-strings (0.0s)
OK nextjs-no-useeffect-fetch (0.0s)
OK nextjs-no-zustand-module-level (0.0s)
OK nextjs-page-purity (0.0s)
OK nextjs-server-client-boundary (0.0s)
OK nextjs-sse-abort-controller (0.0s)
OK python-file-length (0.0s)
OK python-no-future-annotations (0.0s)
OK react-fsd-imports (0.0s)
OK spec-alignment (1.3s)
OK terraform-dangling-resources (0.1s)
OK terraform-external-iam (0.1s)
OK terraform-format (0.0s)
OK terraform-glacier-public (0.1s)
OK terraform-paravirt-ec2 (0.1s)
OK terraform-public-ami (0.1s)
OK terraform-security (0.0s)
OK terraform-tagging (0.0s)
OK dotnet-test (13.7s)
OK tests-pytest (0.0s)

PR: https://github.com/yoavarad/ole-extractor/pull/75