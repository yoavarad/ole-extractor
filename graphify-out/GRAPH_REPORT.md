# Graph Report - 173  (2026-10-07)

## Corpus Check
- 213 files · ~154,111 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 36 file(s) not represented in the graph (top: .doc 7, (none) 6, .pptx 6)

## Summary
- 2218 nodes · 4753 edges · 158 communities (123 shown, 35 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 597 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `25ba3492`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- BenchmarkSampleGenerator
- SampleSpec
- XlsTextExtractor
- XlsxSampleGenerator
- manifest.json
- 4. Process and tooling findings (YDK)
- .RunAsync
- Per-Criterion Detail
- 001-extraction-library-stack.md
- ExtractionBenchmarks.cs
- .Apply
- ExtractManyAsyncTests
- xunit
- ExtractorOLE.DTOs
- project-rules.md
- BatchExtractionResult
- Extract Contract
- --update Incremental Re-extraction
- ExtractorOLE
- IExtractionHelper
- .Extract
- specs/glossary.md
- Spec Quality Verification Report (Re-run 4)
- ExcelFormatMetadata
- Legacy .xls parsing -- research spike (T-b70522d2)
- dotnet-build verification manifest
- LoadGeneratorTests.cs
- EmbeddedFileItem
- MainExtractorParseErrorTests
- ExtractionRequest
- test_compare_benchmarks.py
- system
- IOpenStrategy
- XlsOpenStrategy
- MimeDetectionRegistry
- What You Must Do When Invoked
- dotnet-quality/check.py
- Upstream recommendation: .NET/C# support in YDK
- Legacy Office Format Library Landscape Scan
- OoxmlMimeDetector
- PowerPointFormatMetadata
- epic.md
- dotnet-test/check.py
- dotnet-format/check.py
- Stage 02 Planning Report — 2026-08-19
- exports.md
- CfbMimeDetector
- dotnet-build/check.py
- query.md
- MIME Detection
- PptxSampleGenerator
- compare_benchmarks.py
- .Build
- .Generate
- AI Analysis
- ExcelOpenStrategy
- PptSampleGenerator
- ExcelExtractor
- /graphify
- DocumentExtractionResult
- Candidates evaluated
- NPOI — Research Spike
- ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)
- ADR-006: GitHub Issues as the YDK task backend
- Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19
- extraction-spec.md
- DocumentFormat.OpenXml — Research Spike
- OpenMcdf — Research Spike Notes
- WordFormatMetadata
- .RunDetections
- graphify/SKILL.md
- .claude/CLAUDE.md
- graphify reference: extra exports and benchmark
- ADR-003: Manual LLM spec review in place of `ydk spec verify`
- AttemptCounter
- Extraction
- DocOpenStrategy
- CallCountingProxy
- ITextExtractor
- Dependencies Field
- Error Schema
- FileMetadata
- CLAUDE.md
- DetectedFormatEnum
- NoTempFileIoTests
- .GetFirstLayerEmbedded
- PptFirstLayerEmbeddedWalkTests
- .BuildThroughProductionDi
- Benchmark baseline and regression policy
- .BuildSpied
- documentformat_openxml_packaging
- UnsupportedFormatException
- Overview
- README.md
- github-and-merge.md
- Story Reference Field
- Project Rules
- system_io
- ExtractCorpusManifestTests
- typing
- yaml
- MainExtractorExtractRequestTests
- EmbeddedContentSpec
- Content-Only MIME Detection Rule
- DetectMimeTypeCorpusManifestTests
- task.md
- .ExtractFirstLayerEmbedded
- AI Provider Configuration
- Execution Configuration
- Pre-push Hooks Configuration
- Task Management Configuration
- SampleFormat
- check-task-complete.sh
- Spec Refs Field
- ExtractionTelemetry
- YAGNI (You Aren't Gonna Need It)
- commit-msg
- pre-commit
- pre-push
- N05 Ambiguity Reviewer
- N08 No Technical Specs in Prose Reviewer
- ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)
- BenchmarkColumns.cs
- PptOpenStrategy
- .Extract
- Library Tradeoffs — Office File MIME Detection & Extraction
- OfficeMimeTypeEnum
- GeneratedSample
- Component Schema Definition (UI)
- Config Schema
- Event Schema
- Ext Schema
- NFR Schema
- Test Schema
- N01 Problem Statement Reviewer
- N02 Success Criteria Reviewer
- N03 Scope Boundaries Reviewer
- N04 Terminology Consistency Reviewer
- N06 Flow Completeness Reviewer
- CorruptedContainerSampleGenerator
- ADR-002: Time profiling and stress testing approach
- BenchmarkSampleGenerator.cs
- MimeDetectionRequest
- ExtractionHelper
- PptTextExtractorTests
- DocTextExtractor
- PptxSharedPartSubfileTests
- BenchmarkSamples
- ExtractionBenchmarks
- .GetValue
- AllocatedPerInputByteColumn
- P95TargetColumn
- .Register
- MimeDetectionLimits
- PowerPointOpenStrategy
- XlsSampleGenerator

## God Nodes (most connected - your core abstractions)
1. `ExtractionHelper` - 92 edges
2. `SampleSpec` - 84 edges
3. `ExtractorOLE.DTOs` - 73 edges
4. `MimeDetectionRequest` - 55 edges
5. `DocumentExtractionResult` - 46 edges
6. `ExtractorOLE.Helpers` - 40 edges
7. `OfficeMimeTypeEnum` - 39 edges
8. `SampleGenerator.Abstractions` - 38 edges
9. `SampleGenerator.Generators` - 36 edges
10. `ExtractorOLE.Helpers.FileTypeStrategy` - 35 edges

## Surprising Connections (you probably didn't know these)
- `Alternatives Considered` --references--> `PptTextFixtureBuilder`  [INFERRED]
  docs/adrs/005-legacy-doc-ppt-authoring.md → ExtractorOle/ExtractorOLE.Tests/PowerPoint/PptTextFixtureBuilder.cs
- `HPSF -> OOXML core/app property mapping` --references--> `FileMetadata`  [INFERRED]
  docs/research/legacy-xls-parsing.md → ExtractorOle/ExtractorOLE/DTOs/DocumentExtractionResult.cs
- `7. Whole-target build/test checks can permanently block ALL pushes on a pre-existing broken baseline` --references--> `run()`  [INFERRED]
  docs/research/ydk-dotnet-support-recommendation.md → .github/scripts/test_compare_benchmarks.py
- `3. Live counters against the StressHarness` --references--> `ExtractionTelemetry`  [INFERRED]
  docs/benchmarks/profiling.md → ExtractorOle/ExtractorOLE/ExtractionTelemetry.cs
- `Consequences` --references--> `PptOpenStrategy`  [INFERRED]
  docs/adrs/005-legacy-doc-ppt-authoring.md → ExtractorOle/ExtractorOLE/Helpers/FileTypeStrategy/PptOpenStrategy.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Spec Refs Traceability Convention** — github_pull_request_template_spec_refs, github_issue_template_epic_spec_refs, github_issue_template_story_spec_refs [EXTRACTED 0.85]
- **Incremental Update Merge Pipeline** — claude_skills_graphify_references_update_detect_incremental, claude_skills_graphify_references_update_build_merge, claude_skills_graphify_references_update_save_manifest [EXTRACTED 0.90]
- **Video Transcription Pipeline** — claude_skills_graphify_references_transcribe_step25, claude_skills_graphify_references_transcribe_whisper_prompt, claude_skills_graphify_references_transcribe_transcribe_all [EXTRACTED 0.90]
- **Extraction Requirements Set** — ydk_components_req_extraction_content_only_mime_detection_rule, ydk_components_req_extraction_dependency_injection_rule, ydk_components_req_extraction_format_extensibility_rule, ydk_components_req_extraction_subfile_scope_rule, ydk_components_req_extraction_unicode_fidelity_rule [EXTRACTED 0.95]
- **Three Adversarial Test Coverage Mechanisms** — docs_specs_testing_strategy_corpus_sample_backed, docs_specs_testing_strategy_unit_test_backed, docs_specs_testing_strategy_stress_harness_backed [EXTRACTED 1.00]
- **Container Parsing Error Family** — ydk_components_error_extraction_corrupt_file, ydk_components_error_extraction_truncated_container, ydk_components_error_extraction_oversized_nested_content, ydk_components_error_extraction_unsupported_format [EXTRACTED 1.00]
- **Detection/Open/Text-Extraction Pipeline** — docs_specs_overview_detection_component, docs_specs_overview_open_component, docs_specs_overview_text_extraction_component [EXTRACTED 1.00]
- **Extraction Resource-Safety Guards** — ydk_components_nfr_extraction_max_file_size, ydk_components_error_extraction_oversized_nested_content, ydk_components_nfr_extraction_memory_ceiling, ydk_components_nfr_extraction_nesting_depth_guard [EXTRACTED 1.00]
- **Graphify Full Pipeline** — claude_skills_graphify_skill_step1_ensure_installed, claude_skills_graphify_skill_step2_detect_files, claude_skills_graphify_skill_step3_extract, claude_skills_graphify_skill_step4_build_graph, claude_skills_graphify_skill_step5_label_communities, claude_skills_graphify_skill_step6_exports, claude_skills_graphify_skill_step9_manifest_cost [EXTRACTED 1.00]
- **Graphify Query Command Family** — claude_skills_graphify_references_query, claude_skills_graphify_references_query_bfs_dfs_modes, claude_skills_graphify_references_query_path_command, claude_skills_graphify_references_query_explain_command, claude_skills_graphify_references_query_vocab_expansion [EXTRACTED 1.00]
- **Legacy .doc/.ppt Parsing Alternatives (ADR-004)** — concept_illidans4_npoi_fork, concept_libreoffice_headless_conversion [EXTRACTED 1.00]
- **Stress Testing Tooling Evaluation (ADR-002)** — docs_research_stress_testing_tools_nbomber, concept_hand_rolled_stress_harness, docs_research_stress_testing_tools_hdrhistogram_net [EXTRACTED 1.00]
- **Zip/CFB Bomb Resource-Exhaustion Defense** — ydk_components_nfr_extraction_nesting_depth_guard, ydk_components_nfr_extraction_memory_ceiling, ydk_components_error_extraction_oversized_nested_content [EXTRACTED 1.00]
- **Clarity Review Group** — ydk_spec_reviewers_n04_n04, ydk_spec_reviewers_n05_n05, ydk_spec_reviewers_n06_n06 [EXTRACTED 1.00]
- **Completeness Review Group** — ydk_spec_reviewers_n01_n01, ydk_spec_reviewers_n02_n02, ydk_spec_reviewers_n03_n03 [EXTRACTED 1.00]
- **Quality Review Group** — ydk_spec_reviewers_n07_n07, ydk_spec_reviewers_n08_n08, ydk_spec_reviewers_n09_n09 [EXTRACTED 1.00]
- **Quality Gate Concepts (Spec Check + Acceptance Criteria)** — ydk_config_spec_check, github_issue_template_task_acceptance_criteria, github_issue_template_task_test_strategy [INFERRED 0.65]

## Communities (158 total, 35 thin omitted)

### Community 0 - "BenchmarkSampleGenerator"
Cohesion: 0.13
Nodes (10): Bytes, DateTimeOffset, Index, Length, Name, Func, IEnumerable, IReadOnlyList (+2 more)

### Community 1 - "SampleSpec"
Cohesion: 0.13
Nodes (12): Fact, InlineData, Theory, PptSampleGeneratorTests, IDictionary, IList, SampleSpec, BodyText (+4 more)

### Community 2 - "XlsTextExtractor"
Cohesion: 0.32
Nodes (5): XlsTextExtractor, Fact, InlineData, Theory, XlsTextExtractorTests

### Community 3 - "XlsxSampleGenerator"
Cohesion: 0.06
Nodes (25): Cell, List, Row, SharedStringItem, SpreadsheetDocument, WorkbookPart, XlsxTextExtractor, Fact (+17 more)

### Community 4 - "manifest.json"
Cohesion: 0.33
Nodes (5): benchmarkSamples, $benchmarkSamplesNote, $curatedSamplesNote, $note, samples

### Community 5 - "4. Process and tooling findings (YDK)"
Cohesion: 0.08
Nodes (23): 1. Nothing signals that an epic has ended, 1. What shipped, 2. Status-sync churn, 2. What went well (keep doing), 3.1 The library ADR checked licenses but not the APIs we would actually build against, 3.2 The integration test came last but found the most bugs, 3.3 Tasks marked done without their output existing, 3.4 Expectations written from prose instead of runs (+15 more)

### Community 6 - ".RunAsync"
Cohesion: 0.07
Nodes (35): Alternatives Considered, API Shape For Our Use Case, BenchmarkDotNet Research Spike, Citations/Links, License, Maintenance Signal, Strengths, Structure As A Separate Project (+27 more)

### Community 7 - "Per-Criterion Detail"
Cohesion: 0.06
Nodes (32): 1. Placeholder scan — **PASS**, 2. Internal consistency — **C1–C3 resolved (re-run 2); C4, C5, C6 resolved (re-run 3); C5a resolved (re-run 4); one trivial residual, C5b**, 3. Scope — **PASS**, 4. Ambiguity — **PASS** (all 4 prior findings fixed; 4 minor residuals, none critical), 5. YAGNI — **PASS**, 6. Component reference check — **PASS**, Considered and not counted, Deterministic Scan Results (+24 more)

### Community 8 - "001-extraction-library-stack.md"
Cohesion: 0.28
Nodes (8): ADR-004 Decision: b2xtranslator for Legacy Body Text, DocSharp (manfromarce/DocSharp), IllidanS4/npoi Scratchpad Retarget Fork, LibreOffice Headless Subprocess Conversion, modeverv/dotnet-poi, Unicode/i18n Fidelity Requirement, ADR-004 Legacy doc/ppt Parsing, b2xtranslator library

### Community 9 - "ExtractionBenchmarks.cs"
Cohesion: 0.32
Nodes (7): benchmarkdotnet_attributes, benchmarkdotnet_engines, benchmarkdotnet_jobs, Tier100MbConfig, Tier10MbConfig, TierConfig, ManualConfig

### Community 10 - ".Apply"
Cohesion: 0.15
Nodes (10): CoreFilePropertiesPart, ExtendedFilePropertiesPart, OpenXmlPackage, DateTime, SummaryInformation, MetadataConversions, Fact, InlineData (+2 more)

### Community 11 - "ExtractManyAsyncTests"
Cohesion: 0.20
Nodes (10): ArgumentOutOfRangeException, ArgumentNullException, Fact, Func, Lazy, List, MainExtractor, Task (+2 more)

### Community 12 - "xunit"
Cohesion: 0.14
Nodes (11): SampleGenerator.Abstractions, SampleGenerator, SampleGenerator.Generators, ExtractorOLE.Tests.Excel, ExtractorOLE.Tests.MultiEmbedding, SampleGenerator.Fixtures, ExtractorOLE.Tests.SampleGeneration, microsoft_extensions_dependencyinjection (+3 more)

### Community 13 - "ExtractorOLE.DTOs"
Cohesion: 0.15
Nodes (11): ExtractorOLE.Tests.Helpers, ExtractorOLE.DTOs, ExtractorOLE.Helpers.MimeDetection, ExtractorOLE.Tests.MimeDetection, ExtractorOLE.Exceptions, ExtractorOLE.Tests.Registry, ExtractorOLE.Configuration, npoi_hssf_record_crypto (+3 more)

### Community 14 - "project-rules.md"
Cohesion: 0.26
Nodes (11): ADR-002 Decision: Profiling and Stress Testing Approach, dotnet-counters Tool, Hand-Rolled Stress Harness, OSS-Only Library Constraint, Complexity to Adopt: NBomber vs. Hand-Rolling, Hand-Rolled Harness, HdrHistogram.NET, NBomber (+3 more)

### Community 15 - "BatchExtractionResult"
Cohesion: 0.10
Nodes (18): ADR-007: Batch extraction API with bounded parallelism, Alternatives Considered, Consequences, Context, Decision, Measured throughput, Status, Exception (+10 more)

### Community 16 - "Extract Contract"
Cohesion: 0.14
Nodes (27): ADR-002, Adversarial Review, Success Criteria, Corpus-Sample-Backed Coverage, Integration Tests, Rules, Stress-Harness-Backed Coverage, Stress Testing (+19 more)

### Community 17 - "--update Incremental Re-extraction"
Cohesion: 0.09
Nodes (19): .graphify_detect.json, graphify reference: transcribe video and audio, Step 2.5: Transcribe Video/Audio, Step 2.5 - Transcribe video / audio files (only if video files detected), transcribe_all(), .graphify_transcripts.json, GRAPHIFY_WHISPER_MODEL env var, Whisper Domain-Hint Prompt (+11 more)

### Community 18 - "ExtractorOLE"
Cohesion: 0.07
Nodes (28): ExtractorOLE.Benchmarks, net8.0, Microsoft.NET.Sdk, ExtractorOLE, net8.0, DocumentFormat.OpenXml (3.5.1), DocumentFormat.OpenXml.Features (3.5.1), DocumentFormat.OpenXml.Framework (3.5.1) (+20 more)

### Community 19 - "IExtractionHelper"
Cohesion: 0.15
Nodes (9): IExtractionHelper, Func, MainExtractor, Task, BatchMode, MethodInfo, DetectionCountingProxy, DetectionCalls (+1 more)

### Community 20 - ".Extract"
Cohesion: 0.27
Nodes (4): DetectionCountingProxy, MainExtractor, ITextExtractor, IFormatDispatchRegistry

### Community 21 - "specs/glossary.md"
Cohesion: 0.18
Nodes (10): Content-Only MIME Detection Rule, Format Family (LegacyOle vs Ooxml), Format Kind (Word/Excel/PowerPoint), Subfile Scope (First-Layer Extraction), Corpus Composition, Corpus Manifest, Dataset Curation, Provenance & Licensing (+2 more)

### Community 22 - "Spec Quality Verification Report (Re-run 4)"
Cohesion: 0.33
Nodes (7): Dependency Injection Convention for Format Components, Format Extensibility via Registry Pattern, Subfile Scope Definition, Unicode Fidelity Requirement, Spec Quality Verification Report (Re-run 4), Stage 02 Planning Report — Sprint 1, Stage 02 Planning Report — Sprint 2 & 3

### Community 23 - "ExcelFormatMetadata"
Cohesion: 0.20
Nodes (10): List, ExcelFormatMetadata, ActiveSheetIndex, HasMacros, SheetCount, SheetNames, Fact, VbaProjectPart (+2 more)

### Community 24 - "Legacy .xls parsing -- research spike (T-b70522d2)"
Cohesion: 0.14
Nodes (13): Adoption evidence, Alternatives, Critical gap: embedded OLE objects, Current repo reality (verified in-code, not just b2x-side), Empirical spike results (2026-09-02), Fork attempt blocked, HPSF -> OOXML core/app property mapping, Legacy .xls parsing -- research spike (T-b70522d2) (+5 more)

### Community 26 - "LoadGeneratorTests.cs"
Cohesion: 0.17
Nodes (8): ExtractorOLE.StressHarness, ExtractorOLE.Tests.Telemetry, ExtractorOLE.Tests.StressHarness, system_diagnostics, system_diagnostics_metrics, system_runtime_compilerservices, system_threading, system_threading_tasks

### Community 27 - "EmbeddedFileItem"
Cohesion: 0.12
Nodes (16): DocumentEntry, Entry, EmbeddedFileItem, BinaryData, FileName, PackagePath, SizeInBytes, DirectoryEntry (+8 more)

### Community 28 - "MainExtractorParseErrorTests"
Cohesion: 0.09
Nodes (25): Exception, CorruptFileException, Code, DetectedMimeType, ParseFailureReason, StatusCode, ExtractorOleException, Code (+17 more)

### Community 29 - "ExtractionRequest"
Cohesion: 0.23
Nodes (7): ExtractionRequest, DetectedMimeType, FileBytes, FileName, Fact, MainExtractor, ExtractionTelemetryTests

### Community 30 - "test_compare_benchmarks.py"
Cohesion: 0.27
Nodes (7): 7. Whole-target build/test checks can permanently block ALL pushes on a pre-existing broken baseline, CompareBenchmarksTests, run(), write_report(), subprocess, tempfile, unittest

### Community 31 - "system"
Cohesion: 0.09
Nodes (14): ExtractorOLE, ExtractorOLE.Tests.Architecture, ExtractorOLE.Registry, ExtractorOLE.Old, ExtractorOLE.Old.Util, ExtractorOLE.Old.Excel, ExtractorOLE.Old.PowerPoint, FormatMetadata (+6 more)

### Community 32 - "IOpenStrategy"
Cohesion: 0.16
Nodes (13): IOpenStrategy, IDictionary, ITextExtractor, FormatDispatchRegistry, Fact, MainExtractor, FakeOpenStrategy, FakeTextExtractor (+5 more)

### Community 33 - "XlsOpenStrategy"
Cohesion: 0.18
Nodes (7): HSSFWorkbook, IExtractionHelper, XlsOpenStrategy, DateTime, Fact, IDictionary, XlsOpenStrategyTests

### Community 34 - "MimeDetectionRegistry"
Cohesion: 0.22
Nodes (12): IMimeTypeDetector, IReadOnlyList, IMimeDetectionRegistry, Detectors, IReadOnlyList, MimeDetectionRegistry, Detectors, Fact (+4 more)

### Community 35 - "What You Must Do When Invoked"
Cohesion: 0.13
Nodes (15): Part A - Structural extraction for code files, Part B - Semantic extraction (parallel subagents), Part C - Merge AST + semantic into final extraction, Step 0 - GitHub repos and multi-path merge (only if a URL or several paths), Step 1 - Ensure graphify is installed, Step 2.5 - Video and audio (only if video files detected), Step 2 - Detect files, Step 3 - Extract entities and relationships (+7 more)

### Community 36 - "dotnet-quality/check.py"
Cohesion: 0.24
Nodes (9): YDK unified guard hook - all guard checks in one script., json, pathlib, sys, main(), Verification plugin: dotnet-quality bundle. Thin bundle that runs the dotnet-…, Subprocess-invoke sibling plugin ``name``'s check.py, returning its result., Run the dotnet-quality verification check. (+1 more)

### Community 37 - "Upstream recommendation: .NET/C# support in YDK"
Cohesion: 0.17
Nodes (12): YDK Fork Task-Dependency Bugs, 1. Ship three global plugins, not one bundle, 2. Add a `dotnet` stack to `stacks.py`, 3. Centralize the SDK-vs-muxer detection — this is the real gotcha, 4. Target-discovery edge cases actually hit (and the fix already applied), 5. `tdd-guard` is Python-only — known gap, not fixed here, 8. Priority summary, Source material (+4 more)

### Community 38 - "Legacy Office Format Library Landscape Scan"
Cohesion: 0.14
Nodes (14): b2xtranslator (original) — dead, but see DocSharp fork below, CFB/OLE2 readers found (beyond OpenMcdf), Citations/links, Commercial libraries excluded, Conclusion, DocSharp (manfromarce/DocSharp) — actively maintained fork of b2xtranslator, notable finding, IKVM + Apache POI (Java interop) — not a sane approach, confirmed, Kaitai Struct-generated CFB parser (curiosity, not a library) (+6 more)

### Community 39 - "OoxmlMimeDetector"
Cohesion: 0.21
Nodes (9): Action, OoxmlMimeDetector, PackageOpenAttemptCount, Fact, OoxmlMimeDetectorTests, MacroEmbedSampleSpecs, DocxSampleGenerator, Format (+1 more)

### Community 40 - "PowerPointFormatMetadata"
Cohesion: 0.18
Nodes (11): PowerPointFormatMetadata, HasMacros, NotesSlideCount, SlideCount, Fact, NotesSlidePart, ShapeTree, SlideId (+3 more)

### Community 41 - "epic.md"
Cohesion: 0.18
Nodes (12): Description, Release Field (Epic), Scope, Spec Refs Field (Epic), Acceptance Criteria, Epic Field (Story), Spec Refs Field (Story), PR Template (+4 more)

### Community 42 - "dotnet-test/check.py"
Cohesion: 0.28
Nodes (8): re, _find_test_target(), _has_sdk(), main(), Verification plugin: dotnet test. Runs the .NET test suite via ``dotnet test``.…, Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Prefer a .sln (dotnet test filters to test projects automatically); else the…, Run the dotnet-test verification check.

### Community 43 - "dotnet-format/check.py"
Cohesion: 0.28
Nodes (8): shutil, _find_format_targets(), _has_sdk(), main(), Verification plugin: dotnet format. Verifies .NET code formatting via ``dotnet…, Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-format verification check.

### Community 44 - "Stage 02 Planning Report — 2026-08-19"
Cohesion: 0.14
Nodes (13): Command Log (all invocations used, in order), DAG Validation, Dependency graph modeled, Epics created, Open Questions / Risks for Stage 03, Pass 1 — Full Backlog (Epics + Stories), Pass 2 — Sprint 1 Tasks (Epic 1 + Epic 2), Sanity checks after Pass 1 (+5 more)

### Community 45 - "exports.md"
Cohesion: 0.17
Nodes (12): Token reduction benchmark, FalkorDB export, GraphML export, MCP stdio server, Neo4j export, SVG export, Wiki export, Step 4.5: Graph health check (+4 more)

### Community 46 - "CfbMimeDetector"
Cohesion: 0.19
Nodes (9): Action, CfbMimeDetector, OpenMcdfParseAttemptCount, Fact, CfbMimeDetectorGuardrailTests, Fact, InlineData, Theory (+1 more)

### Community 47 - "dotnet-build/check.py"
Cohesion: 0.28
Nodes (8): time, _find_build_targets(), _has_sdk(), main(), Verification plugin: dotnet build. Builds the .NET solution (or every non-…, Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-build verification check.

### Community 48 - "query.md"
Cohesion: 0.20
Nodes (11): BFS/DFS traversal modes, /graphify explain, For /graphify explain, For /graphify path, graphify reference: query, path, explain, /graphify path, save-result feedback loop, Step 0 — Constrained query expansion (REQUIRED before traversal) (+3 more)

### Community 49 - "MIME Detection"
Cohesion: 0.33
Nodes (6): Extensibility, Guardrails, How Detection Works, Macro-Enabled Variants, MIME Detection, Purpose

### Community 50 - "PptxSampleGenerator"
Cohesion: 0.07
Nodes (27): IEnumerable, PresentationDocument, PresentationPart, SlideId, SlidePart, Text, PptxTextExtractor, Fact (+19 more)

### Community 51 - "compare_benchmarks.py"
Cohesion: 0.36
Nodes (7): csv, build_table(), load(), main(), Compare BenchmarkDotNet P95 results against the committed baseline. Usage:…, to_ms(), os

### Community 52 - ".Build"
Cohesion: 0.11
Nodes (20): extractor, FileTooLargeException, ActualBytes, Code, LimitBytes, StatusCode, OversizedNestedContentException, Code (+12 more)

### Community 53 - ".Generate"
Cohesion: 0.46
Nodes (4): InlineData, MainExtractor, Theory, MainExtractorOoxmlSingleOpenTests

### Community 54 - "AI Analysis"
Cohesion: 0.25
Nodes (7): AI Analysis, Patterns, Retrospective: 144, Rule suggestions, Shipped, Summary, Template suggestions

### Community 55 - "ExcelOpenStrategy"
Cohesion: 0.16
Nodes (10): IExtractionHelper, WorkbookPart, ExcelOpenStrategy, EmbeddedObjectPart, Fact, WorksheetPart, MultiEmbeddingSampleTests, MultiEmbeddingSampleSpecs (+2 more)

### Community 56 - "PptSampleGenerator"
Cohesion: 0.34
Nodes (5): CurrentUser, Document, IReadOnlyList, PptSampleGenerator, Format

### Community 57 - "ExcelExtractor"
Cohesion: 0.22
Nodes (8): Cell, IEnumerable, List, Row, SharedStringItem, WorkbookPart, ExcelExtractor, IdPartPair

### Community 58 - "/graphify"
Cohesion: 0.20
Nodes (10): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Usage (+2 more)

### Community 59 - "DocumentExtractionResult"
Cohesion: 0.12
Nodes (13): List, DocumentExtractionResult, EmbeddedFiles, ExtractedText, Metadata, MimeType, ParsedDocument, PresentationPart (+5 more)

### Community 60 - "Candidates evaluated"
Cohesion: 0.20
Nodes (10): 1. Port NPOI's own scratchpad HWPF/HSLF to SDK-style/net8.0 ourselves, 2. b2xtranslator -- binary-to-OOXML converter, 3. modeverv/dotnet-poi -- from-scratch Apache POI-equivalent port, 4. LibreOffice headless subprocess conversion, 5. Rejected without deep evaluation, Candidates evaluated, Confirmed starting facts (from T-d6de875f's submodule work), Important decoupling this spike surfaces (+2 more)

### Community 61 - "NPOI — Research Spike"
Cohesion: 0.20
Nodes (10): API surface relevant to our use case, Citations / links, Library name, License — IMPORTANT FINDING, Maintenance signal, NPOI — Research Spike, Performance signal, Strengths (+2 more)

### Community 62 - "ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)"
Cohesion: 0.29
Nodes (7): ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection), Alternatives Considered, Consequences, Context, Decision, Note (2026-08-23), Status

### Community 63 - "ADR-006: GitHub Issues as the YDK task backend"
Cohesion: 0.29
Nodes (6): ADR-006: GitHub Issues as the YDK task backend, Alternatives Considered, Consequences, Context, Decision, Status

### Community 64 - "Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19"
Cohesion: 0.20
Nodes (9): Command Log, Component Coverage, Cross-Epic/Cross-Sprint Dependency Edges Modeled, DAG Validation, Open Questions / Risks for Stage 03, Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19, Summary, Task Counts Per Story (+1 more)

### Community 65 - "extraction-spec.md"
Cohesion: 0.25
Nodes (8): graphify reference: extraction subagent prompt, /graphify command, Step 1: Ensure graphify installed, Step 2: Detect files, Step 3: Extract entities and relationships, Hyperedges rule, Node ID format rule (avoid ghost duplicates), Semantic similarity edge rule

### Community 66 - "DocumentFormat.OpenXml — Research Spike"
Cohesion: 0.22
Nodes (9): API surface relevant to our use case, Citations/links, DocumentFormat.OpenXml — Research Spike, Library name, License, Maintenance signal (stars/last release/last commit), Strengths, Unicode/RTL/emoji findings (+1 more)

### Community 67 - "OpenMcdf — Research Spike Notes"
Cohesion: 0.22
Nodes (9): API surface relevant to our use case, Citations / links, Library name, License, Maintenance signal, OpenMcdf — Research Spike Notes, Strengths, Unicode / RTL / emoji findings (entry names) (+1 more)

### Community 68 - "WordFormatMetadata"
Cohesion: 0.07
Nodes (27): compressedSize, WordFormatMetadata, CharacterCount, Company, HasMacros, LineCount, Manager, PageCount (+19 more)

### Community 69 - ".RunDetections"
Cohesion: 0.48
Nodes (3): Fact, Task, DetectorCounterConcurrencyTests

### Community 70 - "graphify/SKILL.md"
Cohesion: 0.25
Nodes (5): /graphify add <url>, --watch folder auto-rebuild, For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 71 - ".claude/CLAUDE.md"
Cohesion: 0.25
Nodes (6): graphify, Native CLAUDE.md integration (graphify claude install), For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration, graphify post-commit hook

### Community 72 - "graphify reference: extra exports and benchmark"
Cohesion: 0.25
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 73 - "ADR-003: Manual LLM spec review in place of `ydk spec verify`"
Cohesion: 0.25
Nodes (7): ADR-003 Decision: Manual Spec Review Substitution, ADR-003: Manual LLM spec review in place of `ydk spec verify`, Alternatives Considered, Consequences, Context, Decision, Status

### Community 74 - "AttemptCounter"
Cohesion: 0.33
Nodes (5): AsyncLocal, Box, AttemptCounter, Value, Box

### Community 75 - "Extraction"
Cohesion: 0.25
Nodes (8): Error Handling, Extensibility & DI, Extraction, Flat Text, Format-Specific Metadata, General Metadata, Purpose, Subfiles

### Community 76 - "DocOpenStrategy"
Cohesion: 0.10
Nodes (16): DirectoryNode, DocumentSummaryInformation, DirectoryEntry, Func, IExtractionHelper, SummaryInformation, DocOpenStrategy, Action (+8 more)

### Community 77 - "CallCountingProxy"
Cohesion: 0.33
Nodes (5): DispatchProxy, MethodInfo, CallCountingProxy, Calls, Inner

### Community 80 - "Error Schema"
Cohesion: 0.32
Nodes (8): Contract Schema, Crosscut Schema, Entity Schema, Error Schema, Hook Schema, Page Schema, Req Schema, Route Schema

### Community 81 - "FileMetadata"
Cohesion: 0.12
Nodes (16): DateTime, FileMetadata, ApplicationName, Comments, Created, Creator, EditingDurationMinutes, Keywords (+8 more)

### Community 82 - "CLAUDE.md"
Cohesion: 0.33
Nodes (6): Code exploration: use graphify, not raw file reads, graphify, Use graphify instead of raw reads, Project Conventions — ole-extractor, Fast path: query existing graph, YDK workflow

### Community 83 - "DetectedFormatEnum"
Cohesion: 0.20
Nodes (8): DetectedFormatEnum, Doc, Docx, Ppt, Pptx, Unknown, Xls, Xlsx

### Community 84 - "NoTempFileIoTests"
Cohesion: 0.47
Nodes (3): Fact, IEnumerable, NoTempFileIoTests

### Community 87 - ".BuildThroughProductionDi"
Cohesion: 0.26
Nodes (8): Fact, ExtractionHelperFileTooLargeTests, SpyDetector, Calls, helper, limits, SpyDetector, trailingSpy

### Community 88 - "Benchmark baseline and regression policy"
Cohesion: 0.40
Nodes (4): Benchmark baseline and regression policy, Committed baseline, Regression rule, Separate from the test gate

### Community 89 - ".BuildSpied"
Cohesion: 0.18
Nodes (13): CallCountingProxy, ArgumentNullException, Fact, InlineData, MainExtractor, Theory, MainExtractorExtractValidationTests, Spied (+5 more)

### Community 90 - "documentformat_openxml_packaging"
Cohesion: 0.19
Nodes (9): ExtractorOLE.Handlers, ExtractorOLE.Tests.Docx, documentformat_openxml_drawing, documentformat_openxml_extendedproperties, documentformat_openxml_packaging, documentformat_openxml_presentation, documentformat_openxml_spreadsheet, documentformat_openxml_wordprocessing (+1 more)

### Community 91 - "UnsupportedFormatException"
Cohesion: 0.40
Nodes (4): UnsupportedFormatException, Code, DetectedMimeType, StatusCode

### Community 93 - "Overview"
Cohesion: 0.14
Nodes (15): Auth & Security, Component Architecture, Detection Component, Open Component, Overview, Problem Statement, Public API, Scope (+7 more)

### Community 95 - "README.md"
Cohesion: 0.16
Nodes (14): ADR-001 Decision: Extraction Library Stack, ADR-001 Extraction Library Stack, ADR-002 Profiling and Stress Testing, ADR-003 Manual Spec Review in Place of Bedrock, Build & test, Development workflow, Documentation, DocumentFormat.OpenXml library (+6 more)

### Community 96 - "github-and-merge.md"
Cohesion: 0.47
Nodes (5): graphify reference: GitHub clone and cross-repo merge, Step 0 - Clone GitHub repo(s) (only if a GitHub URL was given), graphify clone, graphify merge-graphs, Multi-subfolder/monorepo merge flow

### Community 98 - "Project Rules"
Cohesion: 0.33
Nodes (6): Brownfield Constraints, Conventions, Known Gotchas, Library Stack (locked), Product Context, Project Rules

### Community 99 - "system_io"
Cohesion: 0.13
Nodes (12): ExtractorOLE.Tests.PowerPoint, ExtractorOLE.Tests, ExtractorOLE.Helpers.FileTypeStrategy, ExtractorOLE.Tests.Doc, ExtractorOLE.Helpers, npoi_hpsf, npoi_hssf_record, npoi_hssf_usermodel (+4 more)

### Community 100 - "ExtractCorpusManifestTests"
Cohesion: 0.13
Nodes (13): FormatMetadata, DateTime, Fact, IEnumerable, Lazy, List, MainExtractor, MemberData (+5 more)

### Community 103 - "MainExtractorExtractRequestTests"
Cohesion: 0.38
Nodes (4): Fact, MainExtractor, MainExtractorExtractRequestTests, ServiceProvider

### Community 104 - "EmbeddedContentSpec"
Cohesion: 0.16
Nodes (11): Fact, ExcelExtractorEmbeddedObjectTests, EmbeddedContentSpec, Content, ContentType, FileName, EmbeddedObjectPart, IEnumerable (+3 more)

### Community 106 - "DetectMimeTypeCorpusManifestTests"
Cohesion: 0.12
Nodes (16): Fact, IEnumerable, List, MemberData, Theory, DetectMimeTypeCorpusManifestTests, ExpectedDetectedFormat, Enum (+8 more)

### Community 107 - "task.md"
Cohesion: 0.40
Nodes (4): Acceptance Criteria, Description, Test Strategy, Spec Check Configuration

### Community 108 - ".ExtractFirstLayerEmbedded"
Cohesion: 0.29
Nodes (5): DirectoryEntry, HSSFWorkbook, NPOIFSFileSystem, OpenXmlPackage, OpenXmlPart

### Community 116 - "SampleFormat"
Cohesion: 0.10
Nodes (17): Fact, IEnumerable, MainExtractor, MemberData, Theory, BenchmarkSampleGeneratorTests, SkipUnlessGeneratedAttribute, SampleFormat (+9 more)

### Community 136 - "ExtractionTelemetry"
Cohesion: 0.07
Nodes (24): Activity, 2026-08-22 — T-eeb6684b: Downgrade ExtractorOLE.csproj to net8.0, Activity Log, PR #4, Task T-eeb6684b: downgrade to net8.0, ActivitySource, Counter, 1. Capture a trace for one sample (+16 more)

### Community 148 - "ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)"
Cohesion: 0.33
Nodes (6): ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI), Alternatives Considered, Consequences, Context, Decision, Status

### Community 149 - "BenchmarkColumns.cs"
Cohesion: 0.22
Nodes (7): benchmarkdotnet_columns, benchmarkdotnet_configs, benchmarkdotnet_diagnosers, benchmarkdotnet_reports, benchmarkdotnet_running, ExtractorOLE.Benchmarks, SampleParam

### Community 150 - "PptOpenStrategy"
Cohesion: 0.23
Nodes (8): IExtractionHelper, SlidePart, Text, PptOpenStrategy, DateTime, Fact, IDictionary, PptOpenStrategyTests

### Community 151 - ".Extract"
Cohesion: 0.15
Nodes (11): Fact, InlineData, MainExtractor, Theory, DocSampleGeneratorTests, IReadOnlyList, DocSampleGenerator, Format (+3 more)

### Community 152 - "Library Tradeoffs — Office File MIME Detection & Extraction"
Cohesion: 0.40
Nodes (5): Cross-cutting note, Library Tradeoffs — Office File MIME Detection & Extraction, Table 1: OOXML parsing/metadata/text/embeddings (docx/xlsx/pptx), Table 2: Legacy OLE (doc/xls/ppt) parsing/metadata/text, Table 3: Raw CFB introspection for mime-sniffing and generic subfile walking

### Community 153 - "OfficeMimeTypeEnum"
Cohesion: 0.10
Nodes (12): OfficeMimeTypeEnum, Excel, ExcelLegacy, OpenXmlUnknown, PowerPoint, PowerPointLegacy, Word, WordLegacy (+4 more)

### Community 155 - "GeneratedSample"
Cohesion: 0.09
Nodes (18): ISheet, GeneratedSample, Content, FileName, IEnumerable, VbaProjectPart, DateTime, DirectoryEntry (+10 more)

### Community 171 - "ADR-002: Time profiling and stress testing approach"
Cohesion: 0.33
Nodes (6): ADR-002: Time profiling and stress testing approach, Alternatives Considered, Consequences, Context, Decision, Status

### Community 172 - "BenchmarkSampleGenerator.cs"
Cohesion: 0.16
Nodes (10): ExtractorOLE.Old.Doc, ExtractorOLE.Tests.Batch, ExtractorOLE.Tests.Integration, system_io_compression, system_io_packaging, system_security_cryptography, system_text_json, system_text_json_serialization (+2 more)

### Community 181 - "MimeDetectionRequest"
Cohesion: 0.16
Nodes (9): MimeDetectionRequest, FileBytes, FileName, MimeDetectionResult, DetectedFormat, IsSupported, MimeType, ICfbMimeDetector (+1 more)

### Community 183 - "ExtractionHelper"
Cohesion: 0.26
Nodes (6): ExtractionHelper, ArgumentNullException, Fact, ExtractionHelperDetectMimeTypeTests, SpyDetector, WasCalled

### Community 205 - "PptTextExtractorTests"
Cohesion: 0.16
Nodes (10): PptTextExtractor, Fact, IDictionary, ITextExtractor, MainExtractor, PptTextExtractorTests, Dictionary, NPOIFSFileSystem (+2 more)

### Community 207 - "DocTextExtractor"
Cohesion: 0.07
Nodes (26): B2xPresentationDocument, b2xtranslator_docfileformat, b2xtranslator_openxmllib, b2xtranslator_openxmllib_presentationml_presentationdocument, b2xtranslator_openxmllib_wordprocessingml_wordprocessingdocument, b2xtranslator_pptfileformat, b2xtranslator_presentationmlmapping, b2xtranslator_structuredstorage_reader (+18 more)

### Community 213 - "PptxSharedPartSubfileTests"
Cohesion: 0.33
Nodes (7): Action, Fact, ImagePart, PresentationPart, SlidePart, PptxSharedPartSubfileTests, VmlDrawingPart

### Community 217 - "BenchmarkSamples"
Cohesion: 0.21
Nodes (6): Ext, IEnumerable, BenchmarkSamples, GlobalSetup, InvalidOperationException, Mime

### Community 219 - "ExtractionBenchmarks"
Cohesion: 0.18
Nodes (10): Benchmark, IEnumerable, Extract100MbBenchmarks, Tier, Extract10MbBenchmarks, Tier, ExtractionBenchmarks, Sample (+2 more)

### Community 221 - ".GetValue"
Cohesion: 0.36
Nodes (3): BenchmarkCase, Summary, SummaryStyle

### Community 223 - "AllocatedPerInputByteColumn"
Cohesion: 0.20
Nodes (10): ColumnCategory, AllocatedPerInputByteColumn, AlwaysShow, Category, ColumnName, Id, IsNumeric, Legend (+2 more)

### Community 225 - "P95TargetColumn"
Cohesion: 0.20
Nodes (10): UnitType, P95TargetColumn, AlwaysShow, Category, ColumnName, Id, IsNumeric, Legend (+2 more)

### Community 226 - ".Register"
Cohesion: 0.32
Nodes (4): Program, IDictionary, IServiceCollection, ServiceRegistration

### Community 227 - "MimeDetectionLimits"
Cohesion: 0.25
Nodes (6): MimeDetectionLimits, MaxDeclaredEntryBytes, MaxDeclaredNestedContentBytes, MaxFileSizeBytes, Fact, OoxmlMimeDetectorGuardrailTests

### Community 233 - "PowerPointOpenStrategy"
Cohesion: 0.18
Nodes (10): IExtractionHelper, PresentationPart, SlidePart, Text, VbaProjectPart, PowerPointOpenStrategy, ZipFallbackOpenStrategy, EmbeddedObjectPart (+2 more)

### Community 240 - "XlsSampleGenerator"
Cohesion: 0.12
Nodes (17): ADR-005: Authoring legacy .doc/.ppt (and .xls) test samples in SampleGenerator, Alternatives Considered, Consequences, Context, Decision, Status, Fact, InlineData (+9 more)

## Knowledge Gaps
- **528 isolated node(s):** `check-task-complete.sh script`, `Id`, `ColumnName`, `Legend`, `AlwaysShow` (+523 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 846 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **35 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ExtractionHelper` connect `ExtractionHelper` to `SampleSpec`, `XlsxSampleGenerator`, `.Apply`, `IExtractionHelper`, `PptOpenStrategy`, `ExcelFormatMetadata`, `.Extract`, `OfficeMimeTypeEnum`, `EmbeddedFileItem`, `system`, `XlsOpenStrategy`, `MimeDetectionRegistry`, `PowerPointFormatMetadata`, `ExcelOpenStrategy`, `DocumentExtractionResult`, `WordFormatMetadata`, `DocOpenStrategy`, `PptTextExtractorTests`, `DetectedFormatEnum`, `PptxSharedPartSubfileTests`, `PptFirstLayerEmbeddedWalkTests`, `.BuildThroughProductionDi`, `.Register`, `ExtractCorpusManifestTests`, `PowerPointOpenStrategy`, `DetectMimeTypeCorpusManifestTests`, `XlsSampleGenerator`, `SampleFormat`?**
  _High betweenness centrality (0.117) - this node is a cross-community bridge._
- **Why does `DocumentExtractionResult` connect `DocumentExtractionResult` to `.Apply`, `ExtractManyAsyncTests`, `BatchExtractionResult`, `IExtractionHelper`, `.Extract`, `.Extract`, `EmbeddedFileItem`, `system`, `IOpenStrategy`, `XlsOpenStrategy`, `ExcelOpenStrategy`, `ExcelExtractor`, `WordFormatMetadata`, `DocOpenStrategy`, `ITextExtractor`, `FileMetadata`, `.GetFirstLayerEmbedded`, `PptxSharedPartSubfileTests`, `ExtractionBenchmarks`, `ExtractCorpusManifestTests`, `PowerPointOpenStrategy`, `.ExtractFirstLayerEmbedded`, `SampleFormat`?**
  _High betweenness centrality (0.085) - this node is a cross-community bridge._
- **Why does `ExtractorOLE.DTOs` connect `ExtractorOLE.DTOs` to `system_io`, `WordFormatMetadata`, `LoadGeneratorTests.cs`, `PowerPointFormatMetadata`, `ExtractionBenchmarks.cs`, `BenchmarkSampleGenerator.cs`, `xunit`, `DetectedFormatEnum`, `MimeDetectionRequest`, `BenchmarkColumns.cs`, `documentformat_openxml_packaging`, `system`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
- **Are the 73 inferred relationships involving `ExtractionHelper` (e.g. with `.Open_DocWithContainerEmbeddedObject_ReturnsOneOpaqueSubfileItem()` and `.Open_DocWithNoObjectPool_ReturnsEmptyEmbeddedFiles()`) actually correct?**
  _`ExtractionHelper` has 73 INFERRED edges - model-reasoned connections that need verification._
- **Are the 55 inferred relationships involving `SampleSpec` (e.g. with `.CombiningCharacterSequence_NotNormalized()` and `.EmptyBody_ReturnsEmptyString_NeverNull()`) actually correct?**
  _`SampleSpec` has 55 INFERRED edges - model-reasoned connections that need verification._
- **Are the 40 inferred relationships involving `MimeDetectionRequest` (e.g. with `.DetectMimeType_CorpusSample_MatchesManifestExpectedFormat()` and `.DetectMimeType_OversizedDeclaredSizeBomb_ThrowsOversizedNestedContentException()`) actually correct?**
  _`MimeDetectionRequest` has 40 INFERRED edges - model-reasoned connections that need verification._
- **What connects `check-task-complete.sh script`, `Id`, `ColumnName` to the rest of the system?**
  _528 weakly-connected nodes found - possible documentation gaps or missing edges._