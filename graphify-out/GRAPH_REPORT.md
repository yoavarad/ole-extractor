# Graph Report - ole-extractor  (2026-09-25)

## Corpus Check
- 429 files · ~412,327 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 29 file(s) not represented in the graph (top: .doc 7, .pptx 6, (none) 4)

## Summary
- 3366 nodes · 6147 edges · 254 communities (186 shown, 68 thin omitted)
- Extraction: 90% EXTRACTED · 10% INFERRED · 0% AMBIGUOUS · INFERRED: 629 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3a47e960`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- ExtractionResult Entity
- SampleSpec
- XlsTextExtractor
- XlsxSampleGenerator
- manifest.json
- T-3b4e70f4.md
- .RunAsync
- Per-Criterion Detail
- README.md
- ExtractionBenchmarks.cs
- extraction.md
- specs/testing-strategy.md
- xunit
- ExtractorOLE.DTOs
- 002-profiling-and-stress-testing.md
- T-fb89c6f3.md
- Extract Contract
- --update Incremental Re-extraction
- ExtractorOLE
- Glossary of Key Concepts
- SpyRegistry
- dataset-curation.md
- Stage 02 Planning Report — Sprint 2 & 3
- T-bac988f5.md
- Legacy .xls parsing -- research spike (T-b70522d2)
- T-eeb6684b.md
- S-43c41d4a.md
- EmbeddedFileItem
- MainExtractorParseErrorTests
- DocOpenStrategy
- T-a2b91dc6.md
- system
- IOpenStrategy
- XlsOpenStrategyTests
- MimeDetectionRegistry
- What You Must Do When Invoked
- remediate_task_status.py
- Upstream recommendation: .NET/C# support in YDK
- Legacy Office Format Library Landscape Scan
- MimeDetectionRequest
- PowerPointOpenStrategy
- epic.md
- T-b62ffe04.md
- DetectMimeType Contract
- Stage 02 Planning Report — 2026-08-19
- exports.md
- CuratedSampleSpecs
- mime-detection.md
- query.md
- T-0a81d133.md
- PptxSampleGenerator
- T-9e6bfce8-plan.md
- CfbMimeDetector
- Steps
- T-90924b6d.md
- ExcelFormatMetadata
- PptSampleGenerator
- ExcelExtractor
- /graphify
- DocumentExtractionResult
- Candidates evaluated
- NPOI — Research Spike
- Task: Assemble and finalize samples/manifest.json
- MacroEmbedSampleGeneratorTests
- Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19
- extraction-spec.md
- DocumentFormat.OpenXml — Research Spike
- OpenMcdf — Research Spike Notes
- WordFormatMetadata
- QD-7c1726.md
- SKILL.md
- .claude/CLAUDE.md
- graphify reference: extra exports and benchmark
- ADR-003: Manual LLM spec review in place of `ydk spec verify`
- SubfileItem
- Extraction
- DocOpenStrategyTests
- Stage 02: Task Management & Planning — Glossary
- MainExtractorDispatchIntegrationTests
- YDK Task Registry (Epics/Stories/Tasks)
- Error Schema
- FileMetadata
- CLAUDE.md
- MimeDetectionResult
- b2xtranslator_openxmllib
- .GetFirstLayerEmbedded
- QD-c02a7c.md
- ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)
- Steps
- .BuildSpied
- documentformat_openxml_packaging
- QD-334b3d.md
- Activity Log
- specs/overview.md
- Stage 04: Learning and Improvement — Glossary
- ole-extractor
- github-and-merge.md
- Stage 03: Execution — Glossary
- Project Rules
- system_io
- ExtractCorpusManifestTests
- Task: Record curated samples in manifest.json
- .ExtractFromPptxBinary
- MainExtractorExtractRequestTests
- T-f4ed5c8f.md
- QD-012d8e.md
- DetectMimeTypeCorpusManifestTests
- task.md
- .ExtractFirstLayerEmbedded
- Steps
- DetectionCountingProxy
- Epic: OOXML Extraction Completeness
- Epic: Legacy OLE Extraction
- Epic: Dataset Curation
- Epic: Project Restructure
- DocFirstLayerEmbeddedWalkTests
- BenchmarkSampleGenerator
- Steps
- S-e496f356.md
- ExtractionRequest
- E-3ffdc4d5.md
- E-46875bcf.md
- E-5717c1fe.md
- E-6728ee81.md
- E-a2efe648.md
- E-a7ddf85b.md
- E-b54e8704.md
- E-bd43bc02.md
- E-c2544f52.md
- E-c2846232.md
- E-d4ac3268.md
- Epic: Test Suite
- Epic: Library Setup & Validation
- Epic: Public API Surface
- check-task-complete.sh
- Spec Refs Field
- Task T-eeb6684b: downgrade to net8.0
- YAGNI (You Aren't Gonna Need It)
- commit-msg
- pre-commit
- pre-push
- Epic: Time Profiling
- Epic: CFB / Legacy Mime Detection
- Epic: OOXML Mime Detection Hardening
- Epic: Stress Test Harness
- N05 Ambiguity Reviewer
- N08 No Technical Specs in Prose Reviewer
- Stage 04: Learning and Improvement — Overview
- ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)
- BenchmarkColumns.cs
- PptOpenStrategy
- DocSampleGenerator
- Library Tradeoffs — Office File MIME Detection & Extraction
- OfficeMimeTypeEnum
- UnsupportedFormatException
- EmbeddedContentSpec
- Sprint 2 Task Batch (Pass 3)
- Sprint 3 Task Batch (Pass 4)
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
- QD-17b2a5.md
- T-36b41128.md
- QD-3fcfb3.md
- QD-9e3c89.md
- T-12dfd045/orchestrator-progress.md
- Activity Log
- T-113fe4b2 Orchestrator Progress
- T-2c3440d2/orchestrator-progress.md
- T-9a4ff765 — Orchestrator Progress
- ExtractionHelper
- T-48110fcb.md
- QD-407442.md
- QD-82271f.md
- QD-7d0496.md
- Memory System
- Field Reference
- Stage 03: Execution — Overview
- What Gets Aggregated
- Change Management
- Stage 01: Brainstorming & Design — Glossary
- Proof-Based Development
- Verification
- PptTextFixtureBuilder
- Scaffolding
- DocTextExtractor
- Research Methodology
- Steps
- Brownfield Pipeline
- YDK CLI — Complete Reference
- Spec Alignment
- PptxSharedPartSubfileTests
- Spec Quality Enforcement Gate
- Code Review
- Knowledge Extraction
- BenchmarkSamples
- TDD — Red-Green-Refactor
- ExtractionBenchmarks
- YDK — Yoav Development Kit
- .GetValue
- Stage 01: Brainstorming & Design — Overview
- AllocatedPerInputByteColumn
- Consequences
- P95TargetColumn
- .Register
- DocTextExtractor.cs
- Stage 02: Task Management & Planning — Overview
- Observability
- Activity Log
- Activity Log
- Git Workflow
- .Open
- DocxTextExtractor
- FakeExtractionHelper
- T-b856b14e.md
- Testing Strategy (Cross-Cutting)
- T-afb7e032.md
- Component Manifest System
- Dataset Curation
- ExtractionRequest Entity
- QD-20cc64.md
- Activity Log
- Activity Log
- T-b62ffe04 benchmark results (Extract(ExtractionRequest), warm steady state)
- QD-e3ba80.md
- Activity Log
- T-2462e4c3.md
- T-5080b00a.md
- T-fa8b3ed9.md
- T-7916a198/orchestrator-progress.md
- T-b856b14e/orchestrator-progress.md

## God Nodes (most connected - your core abstractions)
1. `ExtractionHelper` - 91 edges
2. `SampleSpec` - 80 edges
3. `ExtractorOLE.DTOs` - 67 edges
4. `MimeDetectionRequest` - 55 edges
5. `Glossary of Key Concepts` - 43 edges
6. `DocumentExtractionResult` - 39 edges
7. `OfficeMimeTypeEnum` - 39 edges
8. `ExtractorOLE.Helpers` - 37 edges
9. `SampleGenerator.Abstractions` - 37 edges
10. `SampleGenerator.Generators` - 35 edges

## Surprising Connections (you probably didn't know these)
- `2026-08-24 - Verification, side-effect cleanup, and completion` --references--> `CuratedSampleSpecs`  [INFERRED]
  .ydk/tasks/T-6f033869.md → SampleGenerator/Fixtures/CuratedSampleSpecs.cs
- `Test Plan` --references--> `ExtractionHelperDetectMimeTypeTests`  [INFERRED]
  .ydk/proofs/T-ef73861f/summary.md → ExtractorOle/ExtractorOLE.Tests/Helpers/ExtractionHelperDetectMimeTypeTests.cs
- `Alternatives Considered` --references--> `PptTextFixtureBuilder`  [INFERRED]
  docs/adrs/005-legacy-doc-ppt-authoring.md → ExtractorOle/ExtractorOLE.Tests/PowerPoint/PptTextFixtureBuilder.cs
- `2026-09-13T20:14:58Z (UTC)` --references--> `DetectedFormatEnum`  [INFERRED]
  .ydk/tasks/T-71ab3bc0.md → ExtractorOle/ExtractorOLE/DTOs/DetectedFormatEnum.cs
- `HPSF -> OOXML core/app property mapping` --references--> `FileMetadata`  [INFERRED]
  docs/research/legacy-xls-parsing.md → ExtractorOle/ExtractorOLE/DTOs/DocumentExtractionResult.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Spec Refs Traceability Convention** — github_pull_request_template_spec_refs, github_issue_template_epic_spec_refs, github_issue_template_story_spec_refs [EXTRACTED 0.85]
- **Incremental Update Merge Pipeline** — claude_skills_graphify_references_update_detect_incremental, claude_skills_graphify_references_update_build_merge, claude_skills_graphify_references_update_save_manifest [EXTRACTED 0.90]
- **Sprint 2 & 3 Decomposition Wave** — ydk_reports_stage_02_planning_sprint23_2026_08_19_report, ydk_epics_e_6728ee81_epic, ydk_epics_e_a7ddf85b_epic, ydk_epics_e_b54e8704_epic, ydk_epics_e_46875bcf_epic, ydk_epics_e_bd43bc02_epic, ydk_epics_e_c2846232_epic, ydk_epics_e_5717c1fe_epic, ydk_epics_e_3ffdc4d5_epic, ydk_epics_e_c2544f52_epic [EXTRACTED 0.90]
- **Video Transcription Pipeline** — claude_skills_graphify_references_transcribe_step25, claude_skills_graphify_references_transcribe_whisper_prompt, claude_skills_graphify_references_transcribe_transcribe_all [EXTRACTED 0.90]
- **Extraction Requirements Set** — ydk_components_req_extraction_content_only_mime_detection_rule, ydk_components_req_extraction_dependency_injection_rule, ydk_components_req_extraction_format_extensibility_rule, ydk_components_req_extraction_subfile_scope_rule, ydk_components_req_extraction_unicode_fidelity_rule [EXTRACTED 0.95]
- **Sprint 1 Planning Wave** — ydk_reports_stage_02_planning_2026_08_19_report, ydk_epics_e_a2efe648_epic, ydk_epics_e_d4ac3268_epic [EXTRACTED 0.95]
- **Adversarial samples feeding corpus manifest assembly** — ydk_tasks_t_7cdaeceb, ydk_tasks_t_83b4a029, ydk_tasks_t_71ab3bc0 [EXTRACTED 1.00]
- **Three Adversarial Test Coverage Mechanisms** — docs_specs_testing_strategy_corpus_sample_backed, docs_specs_testing_strategy_unit_test_backed, docs_specs_testing_strategy_stress_harness_backed [EXTRACTED 1.00]
- **CFB Detection Pipeline (core, guardrails, registry)** — ydk_batches_pass3_sprint2_tasks_task_cfb_detect_core, ydk_batches_pass3_sprint2_tasks_task_cfb_detect_guardrails, ydk_batches_pass3_sprint2_tasks_task_cfb_detect_registry [EXTRACTED 1.00]
- **Corpus Manifest Assembly Flow** — ydk_batches_pass4_sprint3_tasks_task_synthetic_manifest, ydk_batches_pass4_sprint3_tasks_task_curated_manifest, ydk_batches_pass4_sprint3_tasks_task_corpus_manifest_finalize [EXTRACTED 1.00]
- **Extract Facade Error-Handling Suite** — ydk_batches_pass3_sprint2_tasks_task_extract_facade_guardrail_errors, ydk_batches_pass3_sprint2_tasks_task_extract_facade_parse_errors, ydk_batches_pass3_sprint2_tasks_task_extract_facade_unsupported_and_validation [EXTRACTED 1.00]
- **Container Parsing Error Family** — ydk_components_error_extraction_corrupt_file, ydk_components_error_extraction_truncated_container, ydk_components_error_extraction_oversized_nested_content, ydk_components_error_extraction_unsupported_format [EXTRACTED 1.00]
- **Detection/Open/Text-Extraction Pipeline** — docs_specs_overview_detection_component, docs_specs_overview_open_component, docs_specs_overview_text_extraction_component [EXTRACTED 1.00]
- **Extraction Resource-Safety Guards** — ydk_components_nfr_extraction_max_file_size, ydk_components_error_extraction_oversized_nested_content, ydk_components_nfr_extraction_memory_ceiling, ydk_components_nfr_extraction_nesting_depth_guard [EXTRACTED 1.00]
- **Graphify Full Pipeline** — claude_skills_graphify_skill_step1_ensure_installed, claude_skills_graphify_skill_step2_detect_files, claude_skills_graphify_skill_step3_extract, claude_skills_graphify_skill_step4_build_graph, claude_skills_graphify_skill_step5_label_communities, claude_skills_graphify_skill_step6_exports, claude_skills_graphify_skill_step9_manifest_cost [EXTRACTED 1.00]
- **Graphify Query Command Family** — claude_skills_graphify_references_query, claude_skills_graphify_references_query_bfs_dfs_modes, claude_skills_graphify_references_query_path_command, claude_skills_graphify_references_query_explain_command, claude_skills_graphify_references_query_vocab_expansion [EXTRACTED 1.00]
- **Legacy .doc/.ppt Parsing Alternatives (ADR-004)** — concept_illidans4_npoi_fork, concept_libreoffice_headless_conversion [EXTRACTED 1.00]
- **Generalized first-layer embedded-subfile walk applied across container formats** — ydk_tasks_t_29fbb7cf, ydk_tasks_t_30f2c1b8, ydk_tasks_t_05bd14c7 [EXTRACTED 1.00]
- **Stress Testing Tooling Evaluation (ADR-002)** — docs_research_stress_testing_tools_nbomber, concept_hand_rolled_stress_harness, docs_research_stress_testing_tools_hdrhistogram_net [EXTRACTED 1.00]
- **OLE Extractor Library Stack** — readme_documentformat_openxml, readme_npoi, readme_b2xtranslator, readme_openmcdf, benchmarkdotnet [EXTRACTED 1.00]
- **OOXML metadata parity tasks depending on DI registry** — ydk_tasks_t_9fac8963, ydk_tasks_t_b792f8e5, ydk_tasks_t_90924b6d [EXTRACTED 1.00]
- **Zip/CFB Bomb Resource-Exhaustion Defense** — ydk_components_nfr_extraction_nesting_depth_guard, ydk_components_nfr_extraction_memory_ceiling, ydk_components_error_extraction_oversized_nested_content [EXTRACTED 1.00]
- **Clarity Review Group** — ydk_spec_reviewers_n04_n04, ydk_spec_reviewers_n05_n05, ydk_spec_reviewers_n06_n06 [EXTRACTED 1.00]
- **Completeness Review Group** — ydk_spec_reviewers_n01_n01, ydk_spec_reviewers_n02_n02, ydk_spec_reviewers_n03_n03 [EXTRACTED 1.00]
- **Quality Review Group** — ydk_spec_reviewers_n07_n07, ydk_spec_reviewers_n08_n08, ydk_spec_reviewers_n09_n09 [EXTRACTED 1.00]
- **YDK Status-Sync Bug Workaround Pattern** — ydk_tasks_qd_94a6d3, ydk_tasks_qd_a09792, ydk_tasks_qd_b98650, ydk_tasks_qd_fdc901 [EXTRACTED 1.00]
- **Quality Gate Concepts (Spec Check + Acceptance Criteria)** — ydk_config_spec_check, github_issue_template_task_acceptance_criteria, github_issue_template_task_test_strategy [INFERRED 0.65]
- **OpenMcdf-based CFB detection with file-size and nesting-depth guards** — ydk_tasks_t_fd49ef39, ydk_tasks_t_f667e540, ydk_tasks_t_d7c81051 [INFERRED 0.75]
- **OOXML mime-detection path hardening (content-only, zero-byte, size/nesting guards)** — ydk_tasks_t_1653b76b, ydk_tasks_t_44812a03, ydk_tasks_t_33a58c59 [INFERRED 0.75]
- **YDK Task Governance Workflow** — github_issue_template_task_story, ydk_config_task_management, ydk_manifest_task_registry [INFERRED 0.75]
- **Synthetic sample generation and manifest recording for dataset curation** — ydk_tasks_t_d657bb2a, ydk_tasks_t_d91b2a46, ydk_tasks_t_f96fa73d [INFERRED 0.80]
- **Legacy .ppt format implementation (open + metadata + text extraction)** — ydk_tasks_t_517f89c5, ydk_tasks_t_2c3440d2, ydk_tasks_t_40fc15b4 [INFERRED 0.85]
- **Legacy .doc/.ppt parsing research -> ADR-004 decision unblocking doc/ppt open components** — ydk_tasks_t_bac988f5, ydk_tasks_t_c4123b53, ydk_tasks_t_d6de875f [INFERRED 0.85]
- **Null request/field validation testing pattern** — ydk_tasks_t_5e489191, ydk_tasks_t_9a4ff765, extraction_extractionrequest [INFERRED 0.85]
- **Concurrent-Load Stress Harness Flow** — ydk_tasks_t_0055ec7c, ydk_tasks_t_113fe4b2, docs_adrs_002_profiling_and_stress_testing [INFERRED 0.85]
- **WorksheetPart Embedded-Object Scanning Gap** — ydk_tasks_t_02637847, ydk_tasks_t_0834290d, extractionhelper_extractfirstlayerembedded [INFERRED 0.85]
- **Corpus Manifest Dataset Curation** — ydk_stories_s_41ebb945, ydk_stories_s_da936361, ydk_stories_s_6bc8566f [INFERRED 0.90]
- **Legacy OLE Format Extraction (doc/xls/ppt)** — ydk_stories_s_0602557d, ydk_stories_s_23b8b734, ydk_stories_s_32677db6 [INFERRED 0.90]
- **OOXML Format Completeness (docx/pptx/xlsx)** — ydk_stories_s_156ec71a, ydk_stories_s_37c18545, ydk_stories_s_8d6146f0 [INFERRED 0.90]

## Communities (254 total, 68 thin omitted)

### Community 0 - "ExtractionResult Entity"
Cohesion: 0.18
Nodes (20): Task: Map WordFormatMetadata for doc, Task: Implement doc open component (NPOI HWPFDocument), Task: Implement doc flat-text extraction with unicode fidelity, Task: Complete WordFormatMetadata parity for docx, Task: Implement docx flat-text extraction with unicode fidelity, Task: Map PowerPointFormatMetadata for ppt, Task: Implement ppt open component (NPOI HSLFSlideShow), Task: Implement ppt flat-text extraction with unicode fidelity (+12 more)

### Community 1 - "SampleSpec"
Cohesion: 0.10
Nodes (22): Fact, InlineData, MainExtractor, Theory, DocSampleGeneratorTests, Fact, InlineData, Theory (+14 more)

### Community 2 - "XlsTextExtractor"
Cohesion: 0.22
Nodes (9): XlsTextExtractor, Fact, InlineData, Theory, XlsTextExtractorTests, 2026-09-01T21:29:27Z (UTC), 2026-09-01T21:30:20Z (UTC), 2026-09-02 (+1 more)

### Community 3 - "XlsxSampleGenerator"
Cohesion: 0.12
Nodes (14): Cell, List, Row, SharedStringItem, WorkbookPart, XlsxTextExtractor, Fact, InlineData (+6 more)

### Community 4 - "manifest.json"
Cohesion: 0.05
Nodes (35): Epic E-c2846232: Dataset curation, ExtractionHelper.ExtractFirstLayerEmbedded, samples/curated/ directory, benchmarkSamples, $benchmarkSamplesNote, $curatedSamplesNote, $note, samples (+27 more)

### Community 5 - "T-3b4e70f4.md"
Cohesion: 0.07
Nodes (30): subfile-scope requirement, SubfileItem entity, ExcelExtractor.GetFirstLayerEmbedded, SampleGenerator project, Story S-37c18545 (pptx embeddings walk), XlsxSampleGenerator, Activity Log, Description (+22 more)

### Community 6 - ".RunAsync"
Cohesion: 0.06
Nodes (40): CancellationToken, ExtractorOLE.StressHarness, ExtractorOLE.Tests.StressHarness, Alternatives Considered, API Shape For Our Use Case, BenchmarkDotNet Research Spike, Citations/Links, License (+32 more)

### Community 7 - "Per-Criterion Detail"
Cohesion: 0.06
Nodes (32): 1. Placeholder scan — **PASS**, 2. Internal consistency — **C1–C3 resolved (re-run 2); C4, C5, C6 resolved (re-run 3); C5a resolved (re-run 4); one trivial residual, C5b**, 3. Scope — **PASS**, 4. Ambiguity — **PASS** (all 4 prior findings fixed; 4 minor residuals, none critical), 5. YAGNI — **PASS**, 6. Component reference check — **PASS**, Considered and not counted, Deterministic Scan Results (+24 more)

### Community 8 - "README.md"
Cohesion: 0.20
Nodes (17): ADR-001 Decision: Extraction Library Stack, ADR-004 Decision: b2xtranslator for Legacy Body Text, DocSharp (manfromarce/DocSharp), IllidanS4/npoi Scratchpad Retarget Fork, LibreOffice Headless Subprocess Conversion, modeverv/dotnet-poi, OSS-Only Library Constraint, Unicode/i18n Fidelity Requirement (+9 more)

### Community 9 - "ExtractionBenchmarks.cs"
Cohesion: 0.24
Nodes (9): benchmarkdotnet_attributes, benchmarkdotnet_configs, benchmarkdotnet_diagnosers, benchmarkdotnet_engines, benchmarkdotnet_jobs, Tier100MbConfig, Tier10MbConfig, TierConfig (+1 more)

### Community 10 - "extraction.md"
Cohesion: 0.07
Nodes (31): Content-Only MIME Detection Rule, Format Family (LegacyOle vs Ooxml), Format Kind (Word/Excel/PowerPoint), Subfile Scope (First-Layer Extraction), Glossary, ExtractionResult entity, unicode-fidelity requirement, PptxTextExtractor (+23 more)

### Community 11 - "specs/testing-strategy.md"
Cohesion: 0.07
Nodes (26): ADR-002, DetectMimeType, Integration Tests, Stress Testing, Test Types, Testing Strategy, Time Profiling, Unit Tests (+18 more)

### Community 12 - "xunit"
Cohesion: 0.15
Nodes (13): SampleGenerator.Abstractions, SampleGenerator, SampleGenerator.Generators, ExtractorOLE.Helpers, ExtractorOLE.Tests.MultiEmbedding, SampleGenerator.Fixtures, ExtractorOLE.Tests.SampleGeneration, microsoft_extensions_dependencyinjection (+5 more)

### Community 13 - "ExtractorOLE.DTOs"
Cohesion: 0.16
Nodes (12): ExtractorOLE.Tests.Helpers, ExtractorOLE.DTOs, ExtractorOLE.Helpers.MimeDetection, ExtractorOLE.Tests.MimeDetection, ExtractorOLE.Exceptions, ExtractorOLE.Tests.Registry, ExtractorOLE.Configuration, npoi_hssf_record_crypto (+4 more)

### Community 14 - "002-profiling-and-stress-testing.md"
Cohesion: 0.24
Nodes (11): BenchmarkDotNet, ADR-002 Decision: Profiling and Stress Testing Approach, dotnet-counters Tool, Hand-Rolled Stress Harness, Complexity to Adopt: NBomber vs. Hand-Rolling, Hand-Rolled Harness, HdrHistogram.NET, NBomber (+3 more)

### Community 15 - "T-fb89c6f3.md"
Cohesion: 0.08
Nodes (22): DetectMimeType contract, MimeDetectionResult entity, Story S-072bfdf6 (DetectMimeType facade), Story S-3bc6b929 (CFB detection path), Story S-7d040ba7 (macro-variant classification), Activity Log, Description, Activity Log (+14 more)

### Community 16 - "Extract Contract"
Cohesion: 0.16
Nodes (24): Adversarial Review, Corpus-Sample-Backed Coverage, Rules, Stress-Harness-Backed Coverage, Unit-Test-Backed Coverage, Task: Enforce max-file-size and nesting-depth-guard in CFB detection, Task: Implement pre-parse guardrail errors in Extract, Task: Produce oversized-declared-size and password-protected adversarial samples (+16 more)

### Community 17 - "--update Incremental Re-extraction"
Cohesion: 0.09
Nodes (19): .graphify_detect.json, graphify reference: transcribe video and audio, Step 2.5: Transcribe Video/Audio, Step 2.5 - Transcribe video / audio files (only if video files detected), transcribe_all(), .graphify_transcripts.json, GRAPHIFY_WHISPER_MODEL env var, Whisper Domain-Hint Prompt (+11 more)

### Community 18 - "ExtractorOLE"
Cohesion: 0.06
Nodes (30): ExtractorOLE.Benchmarks, net8.0, Microsoft.NET.Sdk, ExtractorOLE, net8.0, DocumentFormat.OpenXml (3.5.1), DocumentFormat.OpenXml.Features (3.5.1), DocumentFormat.OpenXml.Framework (3.5.1) (+22 more)

### Community 19 - "Glossary of Key Concepts"
Cohesion: 0.05
Nodes (43): Agent Reply Marker, Auto-Repair Loop, Batch YAML, Buffer Zone, Catalog, Compaction, Complexity Score, Component Coverage (+35 more)

### Community 20 - "SpyRegistry"
Cohesion: 0.40
Nodes (3): ITextExtractor, SpyRegistry, Lookups

### Community 21 - "dataset-curation.md"
Cohesion: 0.07
Nodes (29): Story S-41ebb945 (synthetic sample generation), Story S-da936361 (curated sample dataset), Activity Log, Description, Description, Verification Proof, Activity Log, Description (+21 more)

### Community 22 - "Stage 02 Planning Report — Sprint 2 & 3"
Cohesion: 0.15
Nodes (21): Content-Only MIME Detection Rule, Dependency Injection Convention for Format Components, Format Extensibility via Registry Pattern, Subfile Scope Definition, Unicode Fidelity Requirement, Time Profiling Epic, OOXML Extraction Completeness Epic, Test Suite Epic (+13 more)

### Community 23 - "T-bac988f5.md"
Cohesion: 0.09
Nodes (26): ADR-004: b2xtranslator decision for legacy .ppt parsing, b2xtranslator (.ppt to .pptx converter library), NPOI HSLFSlideShow (non-functional scratchpad entry point), github.com/IllidanS4/npoi (scratchpad retarget precedent), LibreOffice headless conversion fallback, modeverv/dotnet-poi (rejected, provenance concerns), NPOI, NPOI HPSF/POIFS (metadata, main tree) (+18 more)

### Community 24 - "Legacy .xls parsing -- research spike (T-b70522d2)"
Cohesion: 0.15
Nodes (12): Adoption evidence, Alternatives, Critical gap: embedded OLE objects, Current repo reality (verified in-code, not just b2x-side), Empirical spike results (2026-09-02), Fork attempt blocked, Legacy .xls parsing -- research spike (T-b70522d2), Module overview (+4 more)

### Community 25 - "T-eeb6684b.md"
Cohesion: 0.07
Nodes (26): .ydk/batch-mapping.json, Epic E-d4ac3268: Generalization and build config, ExtractionHelper, ExtractorOLE.csproj, OfficeMimeTypeEnum, Story S-d01346bc (build/verification tooling), Story S-e496f356 (extraction library stack), Activity Log (+18 more)

### Community 26 - "S-43c41d4a.md"
Cohesion: 0.13
Nodes (15): DI-registered format-dispatch registry, Epic E-46875bcf: OOXML extraction completeness, ExcelFormatMetadata, ExtractFirstLayerEmbedded, FileMetadata, Activity Log, Description, Activity Log (+7 more)

### Community 27 - "EmbeddedFileItem"
Cohesion: 0.12
Nodes (17): CoreFilePropertiesPart, DocumentEntry, Entry, ExtendedFilePropertiesPart, EmbeddedFileItem, BinaryData, FileName, PackagePath (+9 more)

### Community 28 - "MainExtractorParseErrorTests"
Cohesion: 0.09
Nodes (25): Exception, CorruptFileException, Code, DetectedMimeType, ParseFailureReason, StatusCode, ExtractorOleException, Code (+17 more)

### Community 29 - "DocOpenStrategy"
Cohesion: 0.16
Nodes (12): DirectoryNode, DocumentSummaryInformation, Epic E-a7ddf85b: Legacy OLE format extraction, DirectoryEntry, Func, IExtractionHelper, DocOpenStrategy, SummaryInformation (+4 more)

### Community 30 - "T-a2b91dc6.md"
Cohesion: 0.09
Nodes (18): ExtractionRequest entity, password-protected error, Activity Log, Description, Activity Log, Description, 2026-09-19T21:47:40Z (UTC), 2026-09-19T22:01:48Z (UTC) (+10 more)

### Community 31 - "system"
Cohesion: 0.09
Nodes (14): ExtractorOLE, ExtractorOLE.Registry, ExtractorOLE.Old, ExtractorOLE.Old.Util, ExtractorOLE.Old.Excel, ExtractorOLE.Old.PowerPoint, FormatMetadata, BackupLogic (+6 more)

### Community 32 - "IOpenStrategy"
Cohesion: 0.14
Nodes (15): IOpenStrategy, IDictionary, ITextExtractor, FormatDispatchRegistry, Fact, MainExtractor, FakeOpenStrategy, FakeTextExtractor (+7 more)

### Community 33 - "XlsOpenStrategyTests"
Cohesion: 0.22
Nodes (6): IExtractionHelper, XlsOpenStrategy, DateTime, Fact, IDictionary, XlsOpenStrategyTests

### Community 34 - "MimeDetectionRegistry"
Cohesion: 0.18
Nodes (12): IMimeTypeDetector, IReadOnlyList, IMimeDetectionRegistry, Detectors, IReadOnlyList, MimeDetectionRegistry, Detectors, Fact (+4 more)

### Community 35 - "What You Must Do When Invoked"
Cohesion: 0.13
Nodes (15): Part A - Structural extraction for code files, Part B - Semantic extraction (parallel subagents), Part C - Merge AST + semantic into final extraction, Step 0 - GitHub repos and multi-path merge (only if a URL or several paths), Step 1 - Ensure graphify is installed, Step 2.5 - Video and audio (only if video files detected), Step 2 - Detect files, Step 3 - Extract entities and relationships (+7 more)

### Community 36 - "remediate_task_status.py"
Cohesion: 0.06
Nodes (50): Any, YDK unified guard hook - all guard checks in one script., json, pathlib, re, shutil, subprocess, sys (+42 more)

### Community 37 - "Upstream recommendation: .NET/C# support in YDK"
Cohesion: 0.15
Nodes (13): YDK Fork Task-Dependency Bugs, 1. Ship three global plugins, not one bundle, 2. Add a `dotnet` stack to `stacks.py`, 3. Centralize the SDK-vs-muxer detection — this is the real gotcha, 4. Target-discovery edge cases actually hit (and the fix already applied), 5. `tdd-guard` is Python-only — known gap, not fixed here, 7. Whole-target build/test checks can permanently block ALL pushes on a pre-existing broken baseline, 8. Priority summary (+5 more)

### Community 38 - "Legacy Office Format Library Landscape Scan"
Cohesion: 0.14
Nodes (14): b2xtranslator (original) — dead, but see DocSharp fork below, CFB/OLE2 readers found (beyond OpenMcdf), Citations/links, Commercial libraries excluded, Conclusion, DocSharp (manfromarce/DocSharp) — actively maintained fork of b2xtranslator, notable finding, IKVM + Apache POI (Java interop) — not a sane approach, confirmed, Kaitai Struct-generated CFB parser (curiosity, not a library) (+6 more)

### Community 39 - "MimeDetectionRequest"
Cohesion: 0.16
Nodes (14): MimeDetectionRequest, FileBytes, FileName, IOoxmlMimeDetector, Action, OoxmlMimeDetector, Fact, OoxmlMimeDetectorGuardrailTests (+6 more)

### Community 40 - "PowerPointOpenStrategy"
Cohesion: 0.12
Nodes (19): PowerPointFormatMetadata, HasMacros, NotesSlideCount, SlideCount, IExtractionHelper, PresentationPart, SlidePart, Text (+11 more)

### Community 41 - "epic.md"
Cohesion: 0.18
Nodes (12): Description, Release Field (Epic), Scope, Spec Refs Field (Epic), Acceptance Criteria, Epic Field (Story), Spec Refs Field (Story), PR Template (+4 more)

### Community 42 - "T-b62ffe04.md"
Cohesion: 0.17
Nodes (10): Story S-709fe2de (benchmarking), 2026-09-15T20:57:17Z (UTC), 2026-09-19T16:26:56Z (UTC), 2026-09-19T21:56:04Z (UTC), 2026-09-19T21:58:06Z (UTC), Activity Log, Description, Verification Proof (+2 more)

### Community 43 - "DetectMimeType Contract"
Cohesion: 0.38
Nodes (11): Task: Implement CFB directory-entry detection component, Task: Register CFB detection component in DI registry, Task: Add null request/field validation to DetectMimeType, Task: Wire DetectMimeType facade to OOXML + CFB registry, Task: Harden OOXML detection to be strictly content-only, Task: Classify macro-enabled OOXML variants as base format, Task: Handle zero-byte input as Unknown in OOXML detection, Task: Unit tests for null validation on DetectMimeType (+3 more)

### Community 44 - "Stage 02 Planning Report — 2026-08-19"
Cohesion: 0.14
Nodes (13): Command Log (all invocations used, in order), DAG Validation, Dependency graph modeled, Epics created, Open Questions / Risks for Stage 03, Pass 1 — Full Backlog (Epics + Stories), Pass 2 — Sprint 1 Tasks (Epic 1 + Epic 2), Sanity checks after Pass 1 (+5 more)

### Community 45 - "exports.md"
Cohesion: 0.17
Nodes (12): Token reduction benchmark, FalkorDB export, GraphML export, MCP stdio server, Neo4j export, SVG export, Wiki export, Step 4.5: Graph health check (+4 more)

### Community 46 - "CuratedSampleSpecs"
Cohesion: 0.21
Nodes (4): Fact, CuratedNestedEmbeddingSampleTests, CuratedSampleSpecs, 2026-09-13T20:14:58Z (UTC)

### Community 47 - "mime-detection.md"
Cohesion: 0.11
Nodes (18): CFB detection component, Extensibility, Guardrails, How Detection Works, Macro-Enabled Variants, MIME Detection, Purpose, Scope (+10 more)

### Community 48 - "query.md"
Cohesion: 0.20
Nodes (11): BFS/DFS traversal modes, /graphify explain, For /graphify explain, For /graphify path, graphify reference: query, path, explain, /graphify path, save-result feedback loop, Step 0 — Constrained query expansion (REQUIRED before traversal) (+3 more)

### Community 49 - "T-0a81d133.md"
Cohesion: 0.20
Nodes (13): Auth & Security, Component Architecture, Detection Component, Open Component, Overview, Problem Statement, Success Criteria, Text-Extraction Component (+5 more)

### Community 50 - "PptxSampleGenerator"
Cohesion: 0.06
Nodes (32): PptTextExtractor, IEnumerable, PresentationPart, SlideId, SlidePart, Text, PptxTextExtractor, Fact (+24 more)

### Community 51 - "T-9e6bfce8-plan.md"
Cohesion: 0.17
Nodes (11): 1. File Manifest, 2.1 Enumerating embedded OLE objects and inline pictures, 2.2 Reading a single embedded object, 2.3 Serializing a container `DirectoryEntry` to an opaque byte blob (AC #2), 2.4 Logging convention for the omitted/corrupt item, 2. Production-code NPOI APIs (verified against `third_party/npoi` @ `2.7.6-rc1`), 3.1 Building a valid embedded object, 3.2 Building the "corrupt" second embedded object (+3 more)

### Community 52 - "CfbMimeDetector"
Cohesion: 0.06
Nodes (40): extractor, MimeDetectionLimits, MaxDeclaredEntryBytes, MaxDeclaredNestedContentBytes, MaxFileSizeBytes, FileTooLargeException, ActualBytes, Code (+32 more)

### Community 53 - "Steps"
Cohesion: 0.05
Nodes (37): 0. Catalog Discovery, 10. Interface Contracts → Component Manifests, 11. Auth & Security, 12. Error Scenarios & Edge Cases → Component Manifests, 13. Non-Functional Requirements → Component Manifests, 14. Cross-Cutting Concerns, 15. Scope Declaration, 16. Testing Strategy (+29 more)

### Community 54 - "T-90924b6d.md"
Cohesion: 0.09
Nodes (21): ExcelFormatMetadata entity, FileMetadata entity, PowerPointFormatMetadata entity, WordFormatMetadata entity, PowerPointOpenStrategy, Activity Log, Description, Activity Log (+13 more)

### Community 55 - "ExcelFormatMetadata"
Cohesion: 0.11
Nodes (18): List, ExcelFormatMetadata, ActiveSheetIndex, SheetCount, SheetNames, IExtractionHelper, WorkbookPart, ExcelOpenStrategy (+10 more)

### Community 56 - "PptSampleGenerator"
Cohesion: 0.16
Nodes (11): CurrentUser, Document, DateTime, DirectoryEntry, IDictionary, IEnumerable, NPOIFSFileSystem, LegacyCfbHelper (+3 more)

### Community 57 - "ExcelExtractor"
Cohesion: 0.22
Nodes (8): Cell, IEnumerable, List, Row, SharedStringItem, WorkbookPart, ExcelExtractor, IdPartPair

### Community 58 - "/graphify"
Cohesion: 0.20
Nodes (10): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Usage (+2 more)

### Community 59 - "DocumentExtractionResult"
Cohesion: 0.13
Nodes (13): List, DocumentExtractionResult, EmbeddedFiles, ExtractedText, Metadata, MimeType, HasMacros, HSSFWorkbook (+5 more)

### Community 60 - "Candidates evaluated"
Cohesion: 0.20
Nodes (10): 1. Port NPOI's own scratchpad HWPF/HSLF to SDK-style/net8.0 ourselves, 2. b2xtranslator -- binary-to-OOXML converter, 3. modeverv/dotnet-poi -- from-scratch Apache POI-equivalent port, 4. LibreOffice headless subprocess conversion, 5. Rejected without deep evaluation, Candidates evaluated, Confirmed starting facts (from T-d6de875f's submodule work), Important decoupling this spike surfaces (+2 more)

### Community 61 - "NPOI — Research Spike"
Cohesion: 0.20
Nodes (10): API surface relevant to our use case, Citations / links, Library name, License — IMPORTANT FINDING, Maintenance signal, NPOI — Research Spike, Performance signal, Strengths (+2 more)

### Community 62 - "Task: Assemble and finalize samples/manifest.json"
Cohesion: 0.15
Nodes (21): Task: Implement parse-time error handling in Extract, Task: Produce corrupt/truncated/zero-byte adversarial samples, Task: Produce wrong-extension and corrupt-embedded-object adversarial samples, Task: Establish committed baseline and CI regression documentation, Task: Implement Extract() latency/allocation benchmarks per format, Task: Scaffold separate BenchmarkDotNet project, Task: Assemble and finalize samples/manifest.json, Task: Integration tests for adversarial scenarios (+13 more)

### Community 63 - "MacroEmbedSampleGeneratorTests"
Cohesion: 0.27
Nodes (5): Fact, InlineData, Theory, MacroEmbedSampleGeneratorTests, Type

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
Cohesion: 0.10
Nodes (21): compressedSize, WordFormatMetadata, CharacterCount, Company, HasMacros, LineCount, Manager, PageCount (+13 more)

### Community 69 - "QD-7c1726.md"
Cohesion: 0.22
Nodes (7): .ydk/manifest.yaml, 2026-08-29T15:46:00Z (UTC), 2026-08-29T16:17:47Z (UTC), 2026-08-29T16:22:37Z (UTC), Verification Proof, 2026-08-22T21:06:43Z (UTC), Verification Proof

### Community 70 - "SKILL.md"
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

### Community 74 - "SubfileItem"
Cohesion: 0.43
Nodes (8): Task: Implement doc first-layer embedded-subfile walk, Task: Wire docx first-layer embeddings/media walk, Task: Wire Extract facade to format-dispatch registry, Task: Implement ppt first-layer embedded-subfile walk, Task: Wire pptx first-layer embeddings/media walk, Task: Implement xls first-layer embedded-subfile walk, Task: Wire xlsx first-layer embeddings/media walk, SubfileItem

### Community 75 - "Extraction"
Cohesion: 0.25
Nodes (8): Error Handling, Extensibility & DI, Extraction, Flat Text, Format-Specific Metadata, General Metadata, Purpose, Subfiles

### Community 76 - "DocOpenStrategyTests"
Cohesion: 0.24
Nodes (5): DateTime, Fact, IDictionary, ITextExtractor, DocOpenStrategyTests

### Community 77 - "Stage 02: Task Management & Planning — Glossary"
Cohesion: 0.07
Nodes (29): Batch YAML, Blocked-by-code, Blocked-by-decision, Buffer Zone, Compaction, Complexity Score, Component Coverage, component_refs (+21 more)

### Community 78 - "MainExtractorDispatchIntegrationTests"
Cohesion: 0.50
Nodes (3): Fact, MainExtractor, MainExtractorDispatchIntegrationTests

### Community 79 - "YDK Task Registry (Epics/Stories/Tasks)"
Cohesion: 0.25
Nodes (8): Dependencies Field, Story Reference Field, AI Provider Configuration, Execution Configuration, Pre-push Hooks Configuration, Task Management Configuration, YDK Task Registry (Epics/Stories/Tasks), YDK Todos Store

### Community 80 - "Error Schema"
Cohesion: 0.32
Nodes (8): Contract Schema, Crosscut Schema, Entity Schema, Error Schema, Hook Schema, Page Schema, Req Schema, Route Schema

### Community 81 - "FileMetadata"
Cohesion: 0.18
Nodes (10): HPSF -> OOXML core/app property mapping, DateTime, FileMetadata, Created, Creator, LastModifiedBy, Modified, Title (+2 more)

### Community 82 - "CLAUDE.md"
Cohesion: 0.33
Nodes (6): Code exploration: use graphify, not raw file reads, graphify, Use graphify instead of raw reads, Project Conventions — ole-extractor, Fast path: query existing graph, YDK workflow

### Community 83 - "MimeDetectionResult"
Cohesion: 0.11
Nodes (13): DetectedFormatEnum, Doc, Docx, Ppt, Pptx, Unknown, Xls, Xlsx (+5 more)

### Community 86 - "QD-c02a7c.md"
Cohesion: 0.29
Nodes (4): docs/adrs directory, graphify-out/graph.json, third_party/npoi submodule, QD-94a6d3: Sync stale status for QD-c02a7c and QD-f9203a

### Community 87 - "ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)"
Cohesion: 0.29
Nodes (7): ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection), Alternatives Considered, Consequences, Context, Decision, Note (2026-08-23), Status

### Community 88 - "Steps"
Cohesion: 0.07
Nodes (28): 1. Release Definition, 2. Epic & Story Creation, 3. Coverage Check, 4. Story Decomposition into Tasks, 4A. Understand, 4B. Identify Work Units, 4C. Assign TODOs to Tasks, 4D. Define Each Task (+20 more)

### Community 89 - ".BuildSpied"
Cohesion: 0.18
Nodes (13): CallCountingProxy, ArgumentNullException, Fact, InlineData, MainExtractor, Theory, MainExtractorExtractValidationTests, Spied (+5 more)

### Community 90 - "documentformat_openxml_packaging"
Cohesion: 0.17
Nodes (12): ExtractorOLE.Tests.PowerPoint, ExtractorOLE.Handlers, ExtractorOLE.Tests.Docx, ExtractorOLE.Tests.Excel, DocumentFormat.OpenXml extraction path, documentformat_openxml_drawing, documentformat_openxml_extendedproperties, documentformat_openxml_packaging (+4 more)

### Community 92 - "Activity Log"
Cohesion: 0.40
Nodes (4): 2026-09-18T10:51:34Z (UTC), 2026-09-19T07:12:12Z (UTC), Activity Log, Description

### Community 93 - "specs/overview.md"
Cohesion: 0.13
Nodes (14): Public API, Epic E-bd43bc02: Public facade wiring, Extract() public facade, Extract() contract, ExtractionRequest, ExtractionResult, MimeDetectionRequest, Program.cs (+6 more)

### Community 94 - "Stage 04: Learning and Improvement — Glossary"
Cohesion: 0.09
Nodes (22): BM25, Bootstrap, ChromaDB, Consolidation, Contradiction Detection, Decision Memory, Embedding, Extraction (+14 more)

### Community 95 - "ole-extractor"
Cohesion: 0.29
Nodes (7): Build & test, Development workflow, Documentation, Library stack, ole-extractor, Project structure, Supported formats

### Community 96 - "github-and-merge.md"
Cohesion: 0.47
Nodes (5): graphify reference: GitHub clone and cross-repo merge, Step 0 - Clone GitHub repo(s) (only if a GitHub URL was given), graphify clone, graphify merge-graphs, Multi-subfolder/monorepo merge flow

### Community 97 - "Stage 03: Execution — Glossary"
Cohesion: 0.09
Nodes (21): Agent Reply Marker, AI Code Review, Auto-Fix, Auto-Repair Loop, Collapsible PR Body, Commit-Message Hook, Conventional Commit, Deterministic Enforcement (+13 more)

### Community 98 - "Project Rules"
Cohesion: 0.33
Nodes (6): Brownfield Constraints, Conventions, Known Gotchas, Library Stack (locked), Product Context, Project Rules

### Community 99 - "system_io"
Cohesion: 0.16
Nodes (9): ExtractorOLE.Helpers.FileTypeStrategy, ExtractorOLE.Tests.Doc, npoi_hpsf, npoi_hssf_record, npoi_hssf_usermodel, npoi_poifs_filesystem, npoi_ss_usermodel, npoi_util (+1 more)

### Community 100 - "ExtractCorpusManifestTests"
Cohesion: 0.13
Nodes (13): FormatMetadata, DateTime, Fact, IEnumerable, List, MainExtractor, MemberData, Theory (+5 more)

### Community 101 - "Task: Record curated samples in manifest.json"
Cohesion: 0.83
Nodes (4): Task: Record curated samples in manifest.json, Task: Author 2+-level nested-embedding curated sample, Task: Author real multi-user revision-history curated sample, Task: Source/author 12 curated real-world-style samples

### Community 103 - "MainExtractorExtractRequestTests"
Cohesion: 0.32
Nodes (5): DetectionCountingProxy, Fact, MainExtractor, MainExtractorExtractRequestTests, ServiceProvider

### Community 104 - "T-f4ed5c8f.md"
Cohesion: 0.20
Nodes (9): content-only-mime-detection requirement, detect-mime-type contract, Story S-f6d7124a (mime-detection integration tests), Activity Log, Description, Activity Log, Description, Activity Log (+1 more)

### Community 105 - "QD-012d8e.md"
Cohesion: 0.40
Nodes (4): .env, .gitignore, 2026-08-29T16:58:46Z (UTC), Verification Proof

### Community 106 - "DetectMimeTypeCorpusManifestTests"
Cohesion: 0.12
Nodes (16): Fact, IEnumerable, List, MemberData, Theory, DetectMimeTypeCorpusManifestTests, ExpectedDetectedFormat, Enum (+8 more)

### Community 107 - "task.md"
Cohesion: 0.40
Nodes (4): Acceptance Criteria, Description, Test Strategy, Spec Check Configuration

### Community 108 - ".ExtractFirstLayerEmbedded"
Cohesion: 0.29
Nodes (5): DirectoryEntry, HSSFWorkbook, NPOIFSFileSystem, OpenXmlPackage, OpenXmlPart

### Community 109 - "Steps"
Cohesion: 0.09
Nodes (21): 10. PR Review Feedback Loop, 1. Start the Task, 2. Plan the Approach, 3. Scaffold (if applicable), 4. Implement TODOs (TDD), 5. Verify, 6. Review (External), 7. Capture Knowledge (+13 more)

### Community 110 - "DetectionCountingProxy"
Cohesion: 0.18
Nodes (9): DispatchProxy, MethodInfo, DetectionCountingProxy, DetectionCalls, Inner, MethodInfo, CallCountingProxy, Calls (+1 more)

### Community 111 - "Epic: OOXML Extraction Completeness"
Cohesion: 0.50
Nodes (4): Epic: OOXML Extraction Completeness, Story: docx extraction completeness, Story: pptx extraction completeness, Story: xlsx extraction completeness

### Community 112 - "Epic: Legacy OLE Extraction"
Cohesion: 0.50
Nodes (4): Epic: Legacy OLE Extraction, Story: xls extraction (Excel legacy), Story: ppt extraction (PowerPoint legacy), Story: doc extraction (Word legacy)

### Community 113 - "Epic: Dataset Curation"
Cohesion: 0.50
Nodes (4): Epic: Dataset Curation, Story: Synthetic sample generator (3 per format), Story: Shared adversarial set and corpus manifest, Story: Curated real-world samples (2 per format)

### Community 114 - "Epic: Project Restructure"
Cohesion: 0.50
Nodes (4): Epic: Project Restructure, Story: Generalize embedded-subfile walk and wire DI registry for format dispatch, Story: Downgrade to net8.0 and fix enum typo, Story: Wire Program.cs to invoke both public functions on a real file

### Community 115 - "DocFirstLayerEmbeddedWalkTests"
Cohesion: 0.35
Nodes (4): Action, DirectoryEntry, Fact, DocFirstLayerEmbeddedWalkTests

### Community 116 - "BenchmarkSampleGenerator"
Cohesion: 0.06
Nodes (27): Bytes, DateTimeOffset, Fact, IEnumerable, MainExtractor, MemberData, Theory, BenchmarkSampleGeneratorTests (+19 more)

### Community 117 - "Steps"
Cohesion: 0.09
Nodes (21): 10. Procedural Memory Report, 1. Index Knowledge, 2. Review Extractions, 3. Record Decisions, 4. Sprint Retrospective, 5. Consolidate Memories, 6. Audit, 7. Template Discovery (+13 more)

### Community 118 - "S-e496f356.md"
Cohesion: 0.29
Nodes (5): Epic E-a2efe648: NPOI/OpenMcdf library validation and wiring, Activity Log, Description, Activity Log, Description

### Community 119 - "ExtractionRequest"
Cohesion: 0.33
Nodes (4): ExtractionRequest, DetectedMimeType, FileBytes, FileName

### Community 131 - "Epic: Test Suite"
Cohesion: 0.67
Nodes (3): Epic: Test Suite, Story: Unit tests for unit-test-backed scenarios, Story: Integration tests against the corpus manifest

### Community 132 - "Epic: Library Setup & Validation"
Cohesion: 0.67
Nodes (3): Epic: Library Setup & Validation, Story: Empirically validate NPOI i18n fidelity on the legacy OLE path, Story: Wire self-compiled NPOI and OpenMcdf into the build

### Community 133 - "Epic: Public API Surface"
Cohesion: 0.67
Nodes (3): Epic: Public API Surface, Story: Wire the DetectMimeType public facade to the registry, Story: Wire the Extract public facade with full error handling

### Community 136 - "Task T-eeb6684b: downgrade to net8.0"
Cohesion: 0.33
Nodes (5): 2026-08-22 — T-eeb6684b: Downgrade ExtractorOLE.csproj to net8.0, Activity Log, PR #4, Task T-eeb6684b: downgrade to net8.0, YDK Development Workflow

### Community 147 - "Stage 04: Learning and Improvement — Overview"
Cohesion: 0.09
Nodes (21): Aspects, Automatic Integration, Consolidation, Contradiction Detection, Core Concept: Project Memory, Decision Memory, Entry Criteria, Exit Criteria (+13 more)

### Community 148 - "ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)"
Cohesion: 0.33
Nodes (6): ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI), Alternatives Considered, Consequences, Context, Decision, Status

### Community 149 - "BenchmarkColumns.cs"
Cohesion: 0.29
Nodes (5): benchmarkdotnet_columns, benchmarkdotnet_reports, benchmarkdotnet_running, ExtractorOLE.Benchmarks, SampleParam

### Community 150 - "PptOpenStrategy"
Cohesion: 0.13
Nodes (12): IExtractionHelper, PresentationPart, SlidePart, Text, PptOpenStrategy, PptToPptxConverter, Fact, PptFirstLayerEmbeddedWalkTests (+4 more)

### Community 151 - "DocSampleGenerator"
Cohesion: 0.15
Nodes (11): ISampleGenerator, Format, IReadOnlyList, DocSampleGenerator, Format, IDictionary, IServiceCollection, ServiceRegistration (+3 more)

### Community 152 - "Library Tradeoffs — Office File MIME Detection & Extraction"
Cohesion: 0.40
Nodes (5): Cross-cutting note, Library Tradeoffs — Office File MIME Detection & Extraction, Table 1: OOXML parsing/metadata/text/embeddings (docx/xlsx/pptx), Table 2: Legacy OLE (doc/xls/ppt) parsing/metadata/text, Table 3: Raw CFB introspection for mime-sniffing and generic subfile walking

### Community 153 - "OfficeMimeTypeEnum"
Cohesion: 0.13
Nodes (12): OfficeMimeTypeEnum, Excel, ExcelLegacy, OpenXmlUnknown, PowerPoint, PowerPointLegacy, Word, WordLegacy (+4 more)

### Community 154 - "UnsupportedFormatException"
Cohesion: 0.40
Nodes (4): UnsupportedFormatException, Code, DetectedMimeType, StatusCode

### Community 155 - "EmbeddedContentSpec"
Cohesion: 0.07
Nodes (24): Fact, ExcelExtractorEmbeddedObjectTests, ISheet, EmbeddedContentSpec, Content, ContentType, FileName, GeneratedSample (+16 more)

### Community 171 - "ADR-002: Time profiling and stress testing approach"
Cohesion: 0.33
Nodes (6): ADR-002: Time profiling and stress testing approach, Alternatives Considered, Consequences, Context, Decision, Status

### Community 172 - "BenchmarkSampleGenerator.cs"
Cohesion: 0.18
Nodes (10): ExtractorOLE.Old.Doc, ExtractorOLE.Tests.Integration, system_globalization, system_io_compression, system_io_packaging, system_security_cryptography, system_text_json, system_text_json_serialization (+2 more)

### Community 174 - "T-36b41128.md"
Cohesion: 0.40
Nodes (4): 2026-09-01T21:05:25Z (UTC), Activity Log, Description, Verification Proof

### Community 178 - "Activity Log"
Cohesion: 0.40
Nodes (4): 2026-09-13T20:14:11Z (UTC), 2026-09-19T17:10:00Z (UTC), Activity Log, Description

### Community 179 - "T-113fe4b2 Orchestrator Progress"
Cohesion: 0.20
Nodes (9): CRITICAL BUG HIT AND FIXED: ydk task done did NOT auto-commit working-tree changes, CRITICAL REMINDER, Done, Done (cont.), Done (final), Exact next step, STATUS: COMPLETE, Status: DONE — PR #82 merged (+1 more)

### Community 181 - "T-9a4ff765 — Orchestrator Progress"
Cohesion: 0.29
Nodes (6): Exact next command (if resuming), Side issue encountered and resolved: stale task-status bookkeeping, Status: DONE — PR open, awaiting merge, T-9a4ff765 — Orchestrator Progress, What's left, What was done

### Community 183 - "ExtractionHelper"
Cohesion: 0.21
Nodes (8): ExtractionHelper, ArgumentNullException, Fact, ExtractionHelperDetectMimeTypeTests, SpyDetector, WasCalled, Summary, Test Plan

### Community 194 - "Memory System"
Cohesion: 0.10
Nodes (20): 1. Vector Similarity Search, 2. BM25 Keyword Search, 3. Reciprocal Rank Fusion (RRF), Access Weight (0.10), Architecture, Bootstrap, Category Weight (0.50), Configuration (+12 more)

### Community 195 - "Field Reference"
Cohesion: 0.10
Nodes (19): ai, catalog, components, Example, execution, Field Reference, Full Schema, hooks (+11 more)

### Community 199 - "Stage 03: Execution — Overview"
Cohesion: 0.11
Nodes (18): Aspects, Auto-Fix, Auto-Repair Loop, Commit-Message Hook, Core Concept: Proof-Based Development, Entry Criteria, Exit Criteria (per task), Git Workflow (+10 more)

### Community 200 - "What Gets Aggregated"
Cohesion: 0.11
Nodes (18): Abandoned Approaches, Contradictions Resolved, Procedural Memory Analysis, Purpose, Repeating Patterns, Retrospective, Step 1: Gather Data, Step 2: Search for Patterns (+10 more)

### Community 201 - "Change Management"
Cohesion: 0.11
Nodes (17): ADRs, Branch Naming, Change Lifecycle, Change Management, Conventional Commits, For code changes (Stage 03):, For config changes:, For spec changes (Stage 01): (+9 more)

### Community 202 - "Stage 01: Brainstorming & Design — Glossary"
Cohesion: 0.11
Nodes (17): ADR (Architecture Decision Record), Adversarial Review, Brownfield Assessment, Component Manifest, E2E Walkthrough, Exit Criteria, Layer A (Deterministic Linker), Layer B (LLM Scanner) (+9 more)

### Community 203 - "Proof-Based Development"
Cohesion: 0.11
Nodes (17): Deterministic Proof Capture, Issue Comment Format, PR Comment Format (via `--pr` flag), PR Description Format (Collapsible), Proof Artifacts, Proof-Based Development, Proof File Layout, Screenshots as Proof (+9 more)

### Community 204 - "Verification"
Cohesion: 0.11
Nodes (17): Auto-Fix, Auto-Repair Loop, CLI Commands, Creating a New Verification Plugin, Git Hook Integration, Layers, Parallelization, Plugin Architecture (+9 more)

### Community 205 - "PptTextFixtureBuilder"
Cohesion: 0.22
Nodes (9): ADR-005: Authoring legacy .doc/.ppt (and .xls) test samples in SampleGenerator, Alternatives Considered, Context, Decision, Status, Dictionary, NPOIFSFileSystem, PptTextFixtureBuilder (+1 more)

### Community 206 - "Scaffolding"
Cohesion: 0.12
Nodes (16): Choosing the right approach, CLI Commands, Component-Driven Scaffolding, Creating New Templates, Extra mappings with `--map`, Generated File Headers, Manifest Format, No Composition (+8 more)

### Community 207 - "DocTextExtractor"
Cohesion: 0.24
Nodes (6): DocTextExtractor, Fact, InlineData, Theory, DocTextExtractorTests, Paragraph

### Community 208 - "Research Methodology"
Cohesion: 0.12
Nodes (15): Code Exploration (minutes), Deep Dive (minutes), Quick Lookup (seconds), Research Caching, Research Depth Levels, Research in Each Stage, Research Methodology, Research Tools (+7 more)

### Community 209 - "Steps"
Cohesion: 0.12
Nodes (15): 1. Preview the Generation Plan, 2. Run Ignition, 3. Verify the Skeleton Runs, 4. Review TODOs, 5. Commit and PR, Developer modified a generated file, Generated code has import errors, Ignition fails with "no pack installed" (+7 more)

### Community 210 - "Brownfield Pipeline"
Cohesion: 0.13
Nodes (14): After the Pipeline, Brownfield Pipeline, Confidence scoring, Don't modify existing code during exploration, Key Principles, Phase 0: Orientation (automated, ~5 minutes), Phase 1: Structural Extraction (automated, ~15 minutes), Phase 2: Semantic Analysis (AI-assisted, ~30 minutes) (+6 more)

### Community 211 - "YDK CLI — Complete Reference"
Cohesion: 0.13
Nodes (15): Catalog, Component Manifests, Guard System (Real-Time Enforcement), Ignition, Initialization & Config, PR Review Feedback, Spec Evolution, Stage 01 — Spec Quality (+7 more)

### Community 212 - "Spec Alignment"
Cohesion: 0.14
Nodes (13): 1. Entity Accuracy, 2. Interface Compliance, 3. Error Handling Completeness, 4. Boundary Respect, 5. Scope Compliance, 6. Cross-Cutting Adherence, How It Works, Running It (+5 more)

### Community 213 - "PptxSharedPartSubfileTests"
Cohesion: 0.33
Nodes (7): Action, Fact, ImagePart, PresentationPart, SlidePart, PptxSharedPartSubfileTests, VmlDrawingPart

### Community 214 - "Spec Quality Enforcement Gate"
Cohesion: 0.15
Nodes (12): CLI Usage, Configuration, Custom Reviewers, Deterministic Tools, How It Works, Model Tiers, Orphaned Components, Spec Quality Enforcement Gate (+4 more)

### Community 215 - "Code Review"
Cohesion: 0.15
Nodes (12): Blocking vs Non-Blocking, Code Review, How It Works, Parallelization, Quality Reviewer, Review Configuration, Review Output, Review Perspectives (+4 more)

### Community 216 - "Knowledge Extraction"
Cohesion: 0.15
Nodes (12): Abandoned (Negative Knowledge), Contradiction Detection on Write, Decisions, Discoveries, Gotchas, How Extraction Works, Knowledge Extraction, Patterns (+4 more)

### Community 217 - "BenchmarkSamples"
Cohesion: 0.21
Nodes (6): Ext, IEnumerable, BenchmarkSamples, GlobalSetup, InvalidOperationException, Mime

### Community 218 - "TDD — Red-Green-Refactor"
Cohesion: 0.17
Nodes (11): E2E Tests (`tests/e2e/`), Integration Tests (`tests/integration/`), Naming Conventions, TDD in Practice, TDD — Red-Green-Refactor, Test Data, Testing Strategy, The Cycle (+3 more)

### Community 219 - "ExtractionBenchmarks"
Cohesion: 0.18
Nodes (10): Benchmark, IEnumerable, Extract100MbBenchmarks, Tier, Extract10MbBenchmarks, Tier, ExtractionBenchmarks, Sample (+2 more)

### Community 220 - "YDK — Yoav Development Kit"
Cohesion: 0.18
Nodes (10): Cross-Cutting Concerns, Entering a Stage, Generator Quality (v0.46+), How YDK Works, Quick Start, The Development Lifecycle, Universal Principles, What is YDK? (+2 more)

### Community 221 - ".GetValue"
Cohesion: 0.36
Nodes (3): BenchmarkCase, Summary, SummaryStyle

### Community 222 - "Stage 01: Brainstorming & Design — Overview"
Cohesion: 0.20
Nodes (9): Connects To, Entry Criteria, Exit Criteria, Key Files, Modes, Stage 01: Brainstorming & Design — Overview, What This Stage Does, What to Read (+1 more)

### Community 223 - "AllocatedPerInputByteColumn"
Cohesion: 0.20
Nodes (10): ColumnCategory, AllocatedPerInputByteColumn, AlwaysShow, Category, ColumnName, Id, IsNumeric, Legend (+2 more)

### Community 224 - "Consequences"
Cohesion: 0.20
Nodes (7): Consequences, IReadOnlyList, MultilingualFixtures, 2026-09-19T18:06:06Z (UTC), Activity Log, Description, Verification Proof

### Community 225 - "P95TargetColumn"
Cohesion: 0.20
Nodes (10): UnitType, P95TargetColumn, AlwaysShow, Category, ColumnName, Id, IsNumeric, Legend (+2 more)

### Community 226 - ".Register"
Cohesion: 0.24
Nodes (5): ITextExtractor, Program, IDictionary, IServiceCollection, ServiceRegistration

### Community 227 - "DocTextExtractor.cs"
Cohesion: 0.22
Nodes (7): b2xtranslator_docfileformat, b2xtranslator_openxmllib_presentationml_presentationdocument, b2xtranslator_openxmllib_wordprocessingml_wordprocessingdocument, b2xtranslator_pptfileformat, b2xtranslator_presentationmlmapping, b2xtranslator_structuredstorage_reader, b2xtranslator_wordprocessingmlmapping

### Community 228 - "Stage 02: Task Management & Planning — Overview"
Cohesion: 0.22
Nodes (8): Connects To, Entry Criteria, Exit Criteria, Stage 02: Task Management & Planning — Overview, The PM Hierarchy, What This Stage Does, What to Read, Why This Stage Matters

### Community 229 - "Observability"
Cohesion: 0.22
Nodes (8): How Progress Is Tracked, Observability, Sprint-Level Visibility, Status Commands, What NOT to Do, What You See Without Any Extra Setup, When to Check, When Updates Are Posted

### Community 230 - "Activity Log"
Cohesion: 0.22
Nodes (8): 2026-09-20T10:13:10Z (UTC), 2026-09-20T10:16:19Z (UTC), 2026-09-20T10:18:07Z (UTC), 2026-09-20T10:22:43Z (UTC), 2026-09-20T10:31:01Z (UTC), Activity Log, Description, Verification Proof

### Community 231 - "Activity Log"
Cohesion: 0.22
Nodes (8): 2026-09-20 (UTC), 2026-09-20T10:22:42Z (UTC), 2026-09-20T10:26:25Z (UTC), 2026-09-20T10:32:49Z (UTC), 2026-09-20T10:33:58Z (UTC), Activity Log, Description, Verification Proof

### Community 232 - "Git Workflow"
Cohesion: 0.25
Nodes (7): Branch Strategy, Commit-Message Hook, Git Workflow, Hooks, Rules, The Flow, Worktree Lifecycle

### Community 233 - ".Open"
Cohesion: 0.46
Nodes (3): EmbeddedObjectPart, Fact, PptxFirstLayerEmbeddedWalkTests

### Community 234 - "DocxTextExtractor"
Cohesion: 0.57
Nodes (3): DocxTextExtractor, Fact, DocxTextExtractorTests

### Community 236 - "T-b856b14e.md"
Cohesion: 0.29
Nodes (6): 2026-09-19T22:04:19Z (UTC), 2026-09-20T07:49:35Z (UTC), 2026-09-20T07:50:01Z (UTC), Activity Log, Description, Verification Proof

### Community 237 - "Testing Strategy (Cross-Cutting)"
Cohesion: 0.33
Nodes (5): Rules, Structure, TDD is Mandatory, Testing Strategy (Cross-Cutting), Unit Test Mirroring Rule

### Community 238 - "T-afb7e032.md"
Cohesion: 0.33
Nodes (5): 2026-09-20T10:16:46Z (UTC), 2026-09-20T10:29:59Z (UTC), Activity Log, Description, Verification Proof

### Community 239 - "Component Manifest System"
Cohesion: 0.40
Nodes (5): Component CLI, Component Manifest System, Linking, Schemas, Tasks and Components

### Community 240 - "Dataset Curation"
Cohesion: 0.40
Nodes (5): Corpus Composition, Corpus Manifest, Dataset Curation, Provenance & Licensing, Purpose

### Community 241 - "ExtractionRequest Entity"
Cohesion: 0.40
Nodes (5): MimeDetectionResult, Task: Implement unsupported-format rejection and null validation in Extract, Task: Unit tests for null validation on Extract, ExtractionRequest Entity, Unsupported Format Error

### Community 242 - "QD-20cc64.md"
Cohesion: 0.40
Nodes (4): 2026-09-19T18:33:08Z (UTC), 2026-09-19T18:33:40Z (UTC), 2026-09-19T18:36:24Z (UTC), Verification Proof

### Community 243 - "Activity Log"
Cohesion: 0.40
Nodes (5): 2026-09-13T20:24:35Z (UTC), 2026-09-13T20:25:38Z (UTC), 2026-09-13T20:26:31Z (UTC), 2026-09-13T20:29:01Z (UTC), Activity Log

### Community 244 - "Activity Log"
Cohesion: 0.40
Nodes (5): 2026-09-18T19:55:07Z (UTC), 2026-09-18T20:03:18Z (UTC), 2026-09-18T20:06:10Z (UTC), 2026-09-18T20:06:37Z (UTC), Activity Log

### Community 245 - "T-b62ffe04 benchmark results (Extract(ExtractionRequest), warm steady state)"
Cohesion: 0.50
Nodes (3): 100MB tier (target P95 < 5 s), 10MB tier (target P95 < 500 ms), T-b62ffe04 benchmark results (Extract(ExtractionRequest), warm steady state)

### Community 246 - "QD-e3ba80.md"
Cohesion: 0.50
Nodes (3): 2026-09-20T09:48:05Z (UTC), 2026-09-20T09:49:19Z (UTC), Verification Proof

### Community 247 - "Activity Log"
Cohesion: 0.67
Nodes (3): 2026-09-19T21:42:22Z (UTC), 2026-09-19T21:51:08Z (UTC), Activity Log

## Ambiguous Edges - Review These
- `YDK Task Registry (Epics/Stories/Tasks)` → `YDK Todos Store`  [AMBIGUOUS]
  .ydk/todos.yaml · relation: conceptually_related_to

## Knowledge Gaps
- **1371 isolated node(s):** `check-task-complete.sh script`, `Id`, `ColumnName`, `Legend`, `AlwaysShow` (+1366 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1699 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **68 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `YDK Task Registry (Epics/Stories/Tasks)` and `YDK Todos Store`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `ExtractionHelper` connect `ExtractionHelper` to `SampleSpec`, `PptOpenStrategy`, `OfficeMimeTypeEnum`, `EmbeddedFileItem`, `system`, `XlsOpenStrategyTests`, `MimeDetectionRegistry`, `PowerPointOpenStrategy`, `CuratedSampleSpecs`, `PptxSampleGenerator`, `CfbMimeDetector`, `ExcelFormatMetadata`, `WordFormatMetadata`, `DocOpenStrategyTests`, `MimeDetectionResult`, `PptxSharedPartSubfileTests`, `.Register`, `ExtractCorpusManifestTests`, `.Open`, `DetectMimeTypeCorpusManifestTests`, `DocFirstLayerEmbeddedWalkTests`, `BenchmarkSampleGenerator`?**
  _High betweenness centrality (0.043) - this node is a cross-community bridge._
- **Why does `ExtractorOLE.DTOs` connect `ExtractorOLE.DTOs` to `system_io`, `WordFormatMetadata`, `PowerPointOpenStrategy`, `ExtractionBenchmarks.cs`, `BenchmarkSampleGenerator.cs`, `xunit`, `MimeDetectionResult`, `BenchmarkColumns.cs`, `documentformat_openxml_packaging`, `system`?**
  _High betweenness centrality (0.037) - this node is a cross-community bridge._
- **Why does `MainExtractor` connect `OfficeMimeTypeEnum` to `.Register`, `.RunAsync`, `MainExtractorExtractRequestTests`, `CfbMimeDetector`, `BenchmarkSamples`, `ExtractionBenchmarks`, `system`?**
  _High betweenness centrality (0.035) - this node is a cross-community bridge._
- **Are the 72 inferred relationships involving `ExtractionHelper` (e.g. with `.Open_DocWithContainerEmbeddedObject_ReturnsOneOpaqueSubfileItem()` and `.Open_DocWithNoObjectPool_ReturnsEmptyEmbeddedFiles()`) actually correct?**
  _`ExtractionHelper` has 72 INFERRED edges - model-reasoned connections that need verification._
- **Are the 53 inferred relationships involving `SampleSpec` (e.g. with `.CombiningCharacterSequence_NotNormalized()` and `.EmptyBody_ReturnsEmptyString_NeverNull()`) actually correct?**
  _`SampleSpec` has 53 INFERRED edges - model-reasoned connections that need verification._
- **Are the 40 inferred relationships involving `MimeDetectionRequest` (e.g. with `.DetectMimeType_CorpusSample_MatchesManifestExpectedFormat()` and `.DetectMimeType_OversizedDeclaredSizeBomb_ThrowsOversizedNestedContentException()`) actually correct?**
  _`MimeDetectionRequest` has 40 INFERRED edges - model-reasoned connections that need verification._