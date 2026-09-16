# Graph Report - T-c4123b53  (2026-09-16)

## Corpus Check
- 315 files · ~286,121 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1936 nodes · 3187 edges · 189 communities (138 shown, 51 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 147 edges (avg confidence: 0.79)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `ff1fa4bc`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- ExtractionResult Entity
- SampleSpec
- .Detect
- XlsxTextExtractorTests
- extraction.md
- T-3b4e70f4.md
- .RunAsync
- Per-Criterion Detail
- project-rules.md
- README.md
- unicode-fidelity requirement
- testing-strategy.md
- SampleGenerator.Abstractions
- ExtractorOLE.DTOs
- 002-profiling-and-stress-testing.md
- T-fb89c6f3.md
- Extract Contract
- --update Incremental Re-extraction
- ExtractorOLE
- ITextExtractor
- OfficeMimeTypeEnum
- T-f96fa73d.md
- Stage 02 Planning Report — Sprint 2 & 3
- T-bac988f5.md
- Legacy .xls parsing -- research spike (T-b70522d2)
- T-eeb6684b.md
- mime-detection.md
- ExtractionHelper
- Task: Assemble and finalize samples/manifest.json
- ExtractorOLE.Helpers.FileTypeStrategy
- T-a2b91dc6.md
- DocumentExtractionResult
- FormatDispatchRegistryTests
- XlsOpenStrategyTests
- T-6f033869.md
- What You Must Do When Invoked
- remediate_task_status.py
- Upstream recommendation: .NET/C# support in YDK
- Legacy Office Format Library Landscape Scan
- .Detect
- PowerPointOpenStrategyTests
- epic.md
- T-b62ffe04.md
- Task: Wire DetectMimeType facade to OOXML + CFB registry
- Stage 02 Planning Report — 2026-08-19
- exports.md
- DetectMimeTypeCorpusManifestTests
- S-51d277f1.md
- query.md
- T-90924b6d.md
- ExcelOpenStrategy
- T-9e6bfce8-plan.md
- ZipEntryCorruptor
- T-2ac2f140.md
- T-c2a60f06.md
- .Open
- .Open
- ExcelExtractor
- /graphify
- BenchmarkDotNet Research Spike
- Candidates evaluated
- NPOI — Research Spike
- T-f4ed5c8f.md
- MacroEmbedSampleGeneratorTests
- Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19
- extraction-spec.md
- DocumentFormat.OpenXml — Research Spike
- OpenMcdf — Research Spike Notes
- WordOpenStrategyTests
- QD-7c1726.md
- SKILL.md
- .claude/CLAUDE.md
- graphify reference: extra exports and benchmark
- ADR-003: Manual LLM spec review in place of `ydk spec verify`
- ExtractorOleException
- Extraction
- PowerPointOpenStrategy
- XlsOpenStrategy
- MainExtractorDispatchIntegrationTests
- YDK Task Registry (Epics/Stories/Tasks)
- Error Schema
- T-6ac3f215.md
- CLAUDE.md
- DocExtractor
- SampleGenerator/ServiceRegistration.cs
- ExtractionUtil
- QD-c02a7c.md
- ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)
- S-e496f356.md
- .Open
- S-43c41d4a.md
- DocxEmbeddedXlsxSampleSpecs
- main
- main
- main
- Task T-eeb6684b: downgrade to net8.0
- github-and-merge.md
- ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)
- Project Rules
- S-0602557d.md
- ExcelOpenStrategyTests
- SubfileItem
- SlidesExtractor.cs
- Library Tradeoffs — Office File MIME Detection & Extraction
- Dataset Curation
- QD-012d8e.md
- DocOpenStrategyTests
- task.md
- main
- ExcelExtractorEmbeddedObjectTests.cs
- QD-8a231b.md
- Epic: OOXML Extraction Completeness
- Epic: Legacy OLE Extraction
- Epic: Dataset Curation
- Epic: Project Restructure
- T-02637847.md
- T-113fe4b2 Orchestrator Progress
- BackupLogic.cs
- T-44812a03.md
- MetadataFields.cs
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
- SampleGenerator/Abstractions/MetadataFields.cs
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
- DocOpenStrategy
- T-71ab3bc0.md
- T-ce2e3cc1.md
- T-9a4ff765 — Orchestrator Progress
- ExtractionBenchmarks
- Scope
- Rules
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
- .DetectMimeTypeFromBytes
- ydk-dotnet-support-recommendation.md
- T-36b41128.md
- Task: Record curated samples in manifest.json
- ExtractorOLE.StressHarness
- T-07571b32.md
- AdversarialSampleGenerator.cs
- T-ef73861f/summary.md
- T-48110fcb.md
- QD-407442.md
- QD-82271f.md

## God Nodes (most connected - your core abstractions)
1. `ExtractorOLE.DTOs` - 46 edges
2. `SampleGenerator.Abstractions` - 30 edges
3. `DocumentExtractionResult` - 28 edges
4. `SampleGenerator.Generators` - 28 edges
5. `SampleSpec` - 22 edges
6. `ExtractorOLE.Helpers.FileTypeStrategy` - 21 edges
7. `ExtractorOLE.Helpers` - 19 edges
8. `MimeDetectionResult` - 17 edges
9. `OfficeMimeTypeEnum` - 17 edges
10. `SampleGenerator.Fixtures` - 16 edges

## Surprising Connections (you probably didn't know these)
- `NBomber` --semantically_similar_to--> `NPOI library`  [INFERRED] [semantically similar]
  docs/research/stress-testing-tools.md → README.md
- `Spec Check Configuration` --semantically_similar_to--> `Acceptance Criteria`  [INFERRED] [semantically similar]
  .ydk/config.yaml → .github/ISSUE_TEMPLATE/task.md
- `HdrHistogram.NET` --semantically_similar_to--> `BenchmarkDotNet`  [INFERRED] [semantically similar]
  docs/research/stress-testing-tools.md → .ydk/stories/S-709fe2de.md
- `Unit-Test-Backed Coverage` --references--> `Max File Size NFR`  [EXTRACTED]
  docs/specs/testing-strategy.md → .ydk/components/nfr/extraction/max-file-size.yaml
- `Dependencies Field` --shares_data_with--> `YDK Task Registry (Epics/Stories/Tasks)`  [INFERRED]
  .github/ISSUE_TEMPLATE/task.md → .ydk/manifest.yaml

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

## Communities (189 total, 51 thin omitted)

### Community 0 - "ExtractionResult Entity"
Cohesion: 0.21
Nodes (17): Task: Implement doc open component (NPOI HWPFDocument), Task: Implement doc flat-text extraction with unicode fidelity, Task: Implement docx flat-text extraction with unicode fidelity, Task: Map PowerPointFormatMetadata for ppt, Task: Implement ppt open component (NPOI HSLFSlideShow), Task: Implement ppt flat-text extraction with unicode fidelity, Task: Complete PowerPointFormatMetadata parity for pptx, Task: Implement pptx flat-text extraction with unicode fidelity (+9 more)

### Community 1 - "SampleSpec"
Cohesion: 0.06
Nodes (33): Fact, CuratedNestedEmbeddingSampleTests, IList, IPackageProperties, EmbeddedContentSpec, GeneratedSample, ISampleGenerator, SampleFormat (+25 more)

### Community 2 - ".Detect"
Cohesion: 0.05
Nodes (26): MimeDetectionLimits, DetectedFormatEnum, MimeDetectionRequest, MimeDetectionResult, Action, int, string, CfbMimeDetector (+18 more)

### Community 3 - "XlsxTextExtractorTests"
Cohesion: 0.07
Nodes (15): Fact, DocxTextExtractorTests, Fact, InlineData, Theory, XlsTextExtractorTests, Fact, InlineData (+7 more)

### Community 4 - "extraction.md"
Cohesion: 0.08
Nodes (25): Format Family (LegacyOle vs Ooxml), Format Kind (Word/Excel/PowerPoint), Subfile Scope (First-Layer Extraction), Glossary, Epic E-c2846232: Dataset curation, ExtractionHelper.ExtractFirstLayerEmbedded, samples/manifest.json, Activity Log (+17 more)

### Community 5 - "T-3b4e70f4.md"
Cohesion: 0.09
Nodes (22): subfile-scope requirement, SubfileItem entity, Story S-37c18545 (pptx embeddings walk), Activity Log, Description, Activity Log, Description, Activity Log (+14 more)

### Community 6 - ".RunAsync"
Cohesion: 0.09
Nodes (21): CancellationToken, Program, IServiceCollection, ServiceRegistration, IReadOnlyList, Task, FileSelectionStrategy, LoadGenerationOptions (+13 more)

### Community 7 - "Per-Criterion Detail"
Cohesion: 0.06
Nodes (32): 1. Placeholder scan — **PASS**, 2. Internal consistency — **C1–C3 resolved (re-run 2); C4, C5, C6 resolved (re-run 3); C5a resolved (re-run 4); one trivial residual, C5b**, 3. Scope — **PASS**, 4. Ambiguity — **PASS** (all 4 prior findings fixed; 4 minor residuals, none critical), 5. YAGNI — **PASS**, 6. Component reference check — **PASS**, Considered and not counted, Deterministic Scan Results (+24 more)

### Community 8 - "project-rules.md"
Cohesion: 0.33
Nodes (8): ADR-001 Decision: Extraction Library Stack, DocSharp (manfromarce/DocSharp), OSS-Only Library Constraint, Unicode/i18n Fidelity Requirement, ADR-001 Extraction Library Stack, DocumentFormat.OpenXml library, NPOI library, OpenMcdf library

### Community 9 - "README.md"
Cohesion: 0.17
Nodes (14): ADR-003 Manual Spec Review in Place of Bedrock, ADR-004 Legacy doc/ppt Parsing, b2xtranslator library, Build & test, Development workflow, Documentation, ITextExtractor.cs, Library stack (+6 more)

### Community 10 - "unicode-fidelity requirement"
Cohesion: 0.09
Nodes (23): ExtractionResult entity, unicode-fidelity requirement, PptxTextExtractor, PptxTextExtractorTests, Story S-8d6146f0 (xlsx flat-text extraction), Activity Log, Description, 2026-08-25T21:08:52Z (UTC) (+15 more)

### Community 11 - "testing-strategy.md"
Cohesion: 0.07
Nodes (28): ADR-002, DetectMimeType, Integration Tests, Stress Testing, Time Profiling, Unit Tests, Epic E-3ffdc4d5: Benchmark/profiling, Epic E-5717c1fe: Testing (unit/integration) (+20 more)

### Community 12 - "SampleGenerator.Abstractions"
Cohesion: 0.17
Nodes (7): ExtractorOLE.Tests.PowerPoint, ExtractorOLE.Handlers, SampleGenerator.Abstractions, SampleGenerator.Generators, SampleGenerator.Fixtures, ExtractorOLE.Tests.SampleGeneration, DocumentFormat.OpenXml extraction path

### Community 13 - "ExtractorOLE.DTOs"
Cohesion: 0.12
Nodes (14): ExtractorOLE.Tests.Helpers, ExtractorOLE.DTOs, ExtractorOLE.Registry, ExtractorOLE.Helpers.MimeDetection, ExtractorOLE.Tests.MimeDetection, ExtractorOLE.Exceptions, ExtractorOLE.Tests.Registry, ExtractorOLE.Tests.Integration (+6 more)

### Community 14 - "002-profiling-and-stress-testing.md"
Cohesion: 0.17
Nodes (15): BenchmarkDotNet, ADR-002 Decision: Profiling and Stress Testing Approach, dotnet-counters Tool, Hand-Rolled Stress Harness, Complexity to Adopt: NBomber vs. Hand-Rolling, Hand-Rolled Harness, HdrHistogram.NET, NBomber (+7 more)

### Community 15 - "T-fb89c6f3.md"
Cohesion: 0.11
Nodes (17): MimeDetectionResult entity, Story S-072bfdf6 (DetectMimeType facade), Story S-3bc6b929 (CFB detection path), Story S-7d040ba7 (macro-variant classification), Activity Log, Description, 2026-08-29T15:47:20Z (UTC), Activity Log (+9 more)

### Community 16 - "Extract Contract"
Cohesion: 0.18
Nodes (23): Adversarial Review, Success Criteria, Task: Implement CFB directory-entry detection component, Task: Enforce max-file-size and nesting-depth-guard in CFB detection, Task: Register CFB detection component in DI registry, Task: Implement pre-parse guardrail errors in Extract, Task: Produce oversized-declared-size and password-protected adversarial samples, Task: Unit tests for file-too-large on both public functions (+15 more)

### Community 17 - "--update Incremental Re-extraction"
Cohesion: 0.09
Nodes (19): .graphify_detect.json, graphify reference: transcribe video and audio, Step 2.5: Transcribe Video/Audio, Step 2.5 - Transcribe video / audio files (only if video files detected), transcribe_all(), .graphify_transcripts.json, GRAPHIFY_WHISPER_MODEL env var, Whisper Domain-Hint Prompt (+11 more)

### Community 18 - "ExtractorOLE"
Cohesion: 0.07
Nodes (28): ExtractorOLE.Benchmarks, net8.0, Microsoft.NET.Sdk, ExtractorOLE, net8.0, DocumentFormat.OpenXml (3.5.1), DocumentFormat.OpenXml.Features (3.5.1), DocumentFormat.OpenXml.Framework (3.5.1) (+20 more)

### Community 19 - "ITextExtractor"
Cohesion: 0.09
Nodes (12): DocTextExtractor, DocxTextExtractor, PptxTextExtractor, XlsTextExtractor, Cell, List, WorkbookPart, XlsxTextExtractor (+4 more)

### Community 20 - "OfficeMimeTypeEnum"
Cohesion: 0.11
Nodes (8): OfficeMimeTypeEnum, IOpenStrategy, IExtractionHelper, ITextExtractor, MainExtractor, ITextExtractor, IFormatDispatchRegistry, FakeExtractionHelper

### Community 21 - "T-f96fa73d.md"
Cohesion: 0.12
Nodes (16): Story S-41ebb945 (synthetic sample generation), 2026-08-23T18:47:51Z (UTC), Activity Log, Description, Verification Proof, 2026-08-23T18:36:50Z (UTC), Activity Log, Description (+8 more)

### Community 22 - "Stage 02 Planning Report — Sprint 2 & 3"
Cohesion: 0.15
Nodes (21): Content-Only MIME Detection Rule, Dependency Injection Convention for Format Components, Format Extensibility via Registry Pattern, Subfile Scope Definition, Unicode Fidelity Requirement, Time Profiling Epic, OOXML Extraction Completeness Epic, Test Suite Epic (+13 more)

### Community 23 - "T-bac988f5.md"
Cohesion: 0.09
Nodes (30): ADR-004: b2xtranslator decision for legacy .ppt parsing, b2xtranslator (.ppt to .pptx converter library), ADR-004 Decision: b2xtranslator for Legacy Body Text, IllidanS4/npoi Scratchpad Retarget Fork, LibreOffice Headless Subprocess Conversion, modeverv/dotnet-poi, NPOI HSLFSlideShow (non-functional scratchpad entry point), github.com/IllidanS4/npoi (scratchpad retarget precedent) (+22 more)

### Community 24 - "Legacy .xls parsing -- research spike (T-b70522d2)"
Cohesion: 0.14
Nodes (13): Adoption evidence, Alternatives, Critical gap: embedded OLE objects, Current repo reality (verified in-code, not just b2x-side), Empirical spike results (2026-09-02), Fork attempt blocked, HPSF -> OOXML core/app property mapping, Legacy .xls parsing -- research spike (T-b70522d2) (+5 more)

### Community 25 - "T-eeb6684b.md"
Cohesion: 0.20
Nodes (7): Story S-d01346bc (build/verification tooling), 2026-08-22T18:00:40Z (UTC), Verification Proof, Activity Log, Description, Activity Log, Description

### Community 26 - "mime-detection.md"
Cohesion: 0.10
Nodes (19): CFB detection component, Content-Only MIME Detection Rule, Extensibility, Guardrails, How Detection Works, Macro-Enabled Variants, MIME Detection, Purpose (+11 more)

### Community 27 - "ExtractionHelper"
Cohesion: 0.18
Nodes (7): HSSFWorkbook, List, OpenXmlPackage, OpenXmlPart, ExtractionHelper, HSSFObjectData, HSSFPictureData

### Community 28 - "Task: Assemble and finalize samples/manifest.json"
Cohesion: 0.15
Nodes (19): Task: Implement parse-time error handling in Extract, Task: Produce corrupt/truncated/zero-byte adversarial samples, Task: Produce wrong-extension and corrupt-embedded-object adversarial samples, Task: Assemble and finalize samples/manifest.json, Task: Integration tests for adversarial scenarios, Task: Integration tests for DetectMimeType against corpus manifest, Task: Integration tests for Extract against corpus manifest, Task: Implement >=50 concurrent-call exercise of both public functions (+11 more)

### Community 29 - "ExtractorOLE.Helpers.FileTypeStrategy"
Cohesion: 0.15
Nodes (10): ExtractorOLE, ExtractorOLE.Helpers.FileTypeStrategy, ExtractorOLE.Tests.Doc, ExtractorOLE.Benchmarks, ExtractorOLE.Tests.Excel, ExtractorOLE.Helpers, ExtractorOLE.Tests.MultiEmbedding, CuratedSampleSpecs.BuildMeetingMinutesBoardroom (+2 more)

### Community 30 - "T-a2b91dc6.md"
Cohesion: 0.33
Nodes (5): ExtractionRequest entity, Activity Log, Description, Activity Log, Description

### Community 31 - "DocumentExtractionResult"
Cohesion: 0.16
Nodes (11): DateTime, List, DocumentExtractionResult, EmbeddedFileItem, FileMetadata, HSSFWorkbook, OpenXmlPackage, OpenXmlPart (+3 more)

### Community 32 - "FormatDispatchRegistryTests"
Cohesion: 0.21
Nodes (6): IDictionary, ITextExtractor, FormatDispatchRegistry, Fact, FakeOpenStrategy, FormatDispatchRegistryTests

### Community 33 - "XlsOpenStrategyTests"
Cohesion: 0.23
Nodes (3): DateTime, Fact, XlsOpenStrategyTests

### Community 34 - "T-6f033869.md"
Cohesion: 0.10
Nodes (17): SampleGenerator/Fixtures/CuratedSampleSpecs.cs, samples/curated/ directory, Activity Log, Description, Activity Log, Description, Activity Log, Description (+9 more)

### Community 35 - "What You Must Do When Invoked"
Cohesion: 0.13
Nodes (15): Part A - Structural extraction for code files, Part B - Semantic extraction (parallel subagents), Part C - Merge AST + semantic into final extraction, Step 0 - GitHub repos and multi-path merge (only if a URL or several paths), Step 1 - Ensure graphify is installed, Step 2.5 - Video and audio (only if video files detected), Step 2 - Detect files, Step 3 - Extract entities and relationships (+7 more)

### Community 36 - "remediate_task_status.py"
Cohesion: 0.22
Nodes (13): Any, compute_true_status_by_task_id(), correct_manifest_statuses(), correct_task_md_frontmatter(), gh_pr_list(), main(), Resolve stale slugs in manifest['tasks'][*]['dependencies'] in place. Returns…, Correct manifest['tasks'][*]['status'] in place to match true state.… (+5 more)

### Community 37 - "Upstream recommendation: .NET/C# support in YDK"
Cohesion: 0.22
Nodes (9): 1. Ship three global plugins, not one bundle, 2. Add a `dotnet` stack to `stacks.py`, 3. Centralize the SDK-vs-muxer detection — this is the real gotcha, 4. Target-discovery edge cases actually hit (and the fix already applied), 5. `tdd-guard` is Python-only — known gap, not fixed here, 7. Whole-target build/test checks can permanently block ALL pushes on a pre-existing broken baseline, 8. Priority summary, Source material (+1 more)

### Community 38 - "Legacy Office Format Library Landscape Scan"
Cohesion: 0.14
Nodes (14): b2xtranslator (original) — dead, but see DocSharp fork below, CFB/OLE2 readers found (beyond OpenMcdf), Citations/links, Commercial libraries excluded, Conclusion, DocSharp (manfromarce/DocSharp) — actively maintained fork of b2xtranslator, notable finding, IKVM + Apache POI (Java interop) — not a sane approach, confirmed, Kaitai Struct-generated CFB parser (curiosity, not a library) (+6 more)

### Community 39 - ".Detect"
Cohesion: 0.17
Nodes (6): Fact, OoxmlMimeDetectorGuardrailTests, Fact, OoxmlMimeDetectorTests, byte, MacroEmbedSampleSpecs

### Community 40 - "PowerPointOpenStrategyTests"
Cohesion: 0.30
Nodes (5): Fact, ShapeTree, SlidePart, PowerPointOpenStrategyTests, ISet

### Community 41 - "epic.md"
Cohesion: 0.18
Nodes (12): Description, Release Field (Epic), Scope, Spec Refs Field (Epic), Acceptance Criteria, Epic Field (Story), Spec Refs Field (Story), PR Template (+4 more)

### Community 42 - "T-b62ffe04.md"
Cohesion: 0.15
Nodes (12): Story S-709fe2de (benchmarking), Task: Establish committed baseline and CI regression documentation, Task: Implement Extract() latency/allocation benchmarks per format, Task: Scaffold separate BenchmarkDotNet project, Extraction Latency NFR, Activity Log, Description, 2026-09-15T20:57:17Z (UTC) (+4 more)

### Community 43 - "Task: Wire DetectMimeType facade to OOXML + CFB registry"
Cohesion: 0.43
Nodes (8): Task: Add null request/field validation to DetectMimeType, Task: Wire DetectMimeType facade to OOXML + CFB registry, Task: Harden OOXML detection to be strictly content-only, Task: Classify macro-enabled OOXML variants as base format, Task: Handle zero-byte input as Unknown in OOXML detection, Task: Unit tests for null validation on DetectMimeType, MimeDetectionRequest Entity, MimeDetectionResult Entity

### Community 44 - "Stage 02 Planning Report — 2026-08-19"
Cohesion: 0.14
Nodes (13): Command Log (all invocations used, in order), DAG Validation, Dependency graph modeled, Epics created, Open Questions / Risks for Stage 03, Pass 1 — Full Backlog (Epics + Stories), Pass 2 — Sprint 1 Tasks (Epic 1 + Epic 2), Sanity checks after Pass 1 (+5 more)

### Community 45 - "exports.md"
Cohesion: 0.17
Nodes (12): Token reduction benchmark, FalkorDB export, GraphML export, MCP stdio server, Neo4j export, SVG export, Wiki export, Step 4.5: Graph health check (+4 more)

### Community 46 - "DetectMimeTypeCorpusManifestTests"
Cohesion: 0.16
Nodes (12): ExpectedDetectedFormat, Fact, IEnumerable, List, string, Theory, DetectMimeTypeCorpusManifestTests, ExpectedDetectedFormat (+4 more)

### Community 47 - "S-51d277f1.md"
Cohesion: 0.17
Nodes (11): DI-registered format-dispatch registry, Public API, Epic E-bd43bc02: Public facade wiring, Extract() contract, ExtractionRequest, ExtractionResult, MimeDetectionRequest, Activity Log (+3 more)

### Community 48 - "query.md"
Cohesion: 0.20
Nodes (11): BFS/DFS traversal modes, /graphify explain, For /graphify explain, For /graphify path, graphify reference: query, path, explain, /graphify path, save-result feedback loop, Step 0 — Constrained query expansion (REQUIRED before traversal) (+3 more)

### Community 49 - "T-90924b6d.md"
Cohesion: 0.12
Nodes (19): Auth & Security, Component Architecture, Detection Component, Open Component, Overview, Problem Statement, Text-Extraction Component, dependency-injection requirement (+11 more)

### Community 50 - "ExcelOpenStrategy"
Cohesion: 0.20
Nodes (7): List, ExcelFormatMetadata, FormatMetadata, WordFormatMetadata, IExtractionHelper, WorkbookPart, ExcelOpenStrategy

### Community 51 - "T-9e6bfce8-plan.md"
Cohesion: 0.17
Nodes (11): 1. File Manifest, 2.1 Enumerating embedded OLE objects and inline pictures, 2.2 Reading a single embedded object, 2.3 Serializing a container `DirectoryEntry` to an opaque byte blob (AC #2), 2.4 Logging convention for the omitted/corrupt item, 2. Production-code NPOI APIs (verified against `third_party/npoi` @ `2.7.6-rc1`), 3.1 Building a valid embedded object, 3.2 Building the "corrupt" second embedded object (+3 more)

### Community 52 - "ZipEntryCorruptor"
Cohesion: 0.36
Nodes (4): compressedSize, localHeaderOffset, uint, ZipEntryCorruptor

### Community 53 - "T-2ac2f140.md"
Cohesion: 0.18
Nodes (9): Epic E-d4ac3268: Generalization and build config, ExtractionHelper, IOpenStrategy interface, MainExtractor, OfficeMimeTypeEnum, Activity Log, Description, Activity Log (+1 more)

### Community 54 - "T-c2a60f06.md"
Cohesion: 0.07
Nodes (25): ExcelFormatMetadata entity, FileMetadata entity, PowerPointFormatMetadata entity, WordFormatMetadata entity, PowerPointOpenStrategy, Story S-0602557d (xls open component), Activity Log, Description (+17 more)

### Community 55 - ".Open"
Cohesion: 0.33
Nodes (4): Fact, MultiEmbeddingSampleTests, byte, MultiEmbeddingSampleSpecs

### Community 56 - ".Open"
Cohesion: 0.27
Nodes (4): IExtractionHelper, WordprocessingDocument, WordOpenStrategy, ZipFallbackOpenStrategy

### Community 57 - "ExcelExtractor"
Cohesion: 0.25
Nodes (6): Cell, IEnumerable, List, WorkbookPart, ExcelExtractor, IdPartPair

### Community 58 - "/graphify"
Cohesion: 0.20
Nodes (10): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Usage (+2 more)

### Community 59 - "BenchmarkDotNet Research Spike"
Cohesion: 0.20
Nodes (10): Alternatives Considered, API Shape For Our Use Case, BenchmarkDotNet Research Spike, Citations/Links, License, Maintenance Signal, Strengths, Structure As A Separate Project (+2 more)

### Community 60 - "Candidates evaluated"
Cohesion: 0.20
Nodes (10): 1. Port NPOI's own scratchpad HWPF/HSLF to SDK-style/net8.0 ourselves, 2. b2xtranslator -- binary-to-OOXML converter, 3. modeverv/dotnet-poi -- from-scratch Apache POI-equivalent port, 4. LibreOffice headless subprocess conversion, 5. Rejected without deep evaluation, Candidates evaluated, Confirmed starting facts (from T-d6de875f's submodule work), Important decoupling this spike surfaces (+2 more)

### Community 61 - "NPOI — Research Spike"
Cohesion: 0.20
Nodes (10): API surface relevant to our use case, Citations / links, Library name, License — IMPORTANT FINDING, Maintenance signal, NPOI — Research Spike, Performance signal, Strengths (+2 more)

### Community 62 - "T-f4ed5c8f.md"
Cohesion: 0.18
Nodes (9): password-protected error, Story S-f6d7124a (mime-detection integration tests), 2026-09-13T10:19:05Z (UTC), Activity Log, Description, Activity Log, Description, Activity Log (+1 more)

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

### Community 68 - "WordOpenStrategyTests"
Cohesion: 0.36
Nodes (3): ExtractorOLE.Tests.Docx, Fact, WordOpenStrategyTests

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

### Community 74 - "ExtractorOleException"
Cohesion: 0.29
Nodes (4): Exception, ExtractorOleException, FileTooLargeException, OversizedNestedContentException

### Community 75 - "Extraction"
Cohesion: 0.25
Nodes (8): Error Handling, Extensibility & DI, Extraction, Flat Text, Format-Specific Metadata, General Metadata, Purpose, Subfiles

### Community 76 - "PowerPointOpenStrategy"
Cohesion: 0.22
Nodes (5): PowerPointFormatMetadata, IExtractionHelper, PresentationPart, SlidePart, PowerPointOpenStrategy

### Community 77 - "XlsOpenStrategy"
Cohesion: 0.29
Nodes (4): HSSFWorkbook, IExtractionHelper, string, XlsOpenStrategy

### Community 78 - "MainExtractorDispatchIntegrationTests"
Cohesion: 0.50
Nodes (3): Fact, MainExtractor, MainExtractorDispatchIntegrationTests

### Community 79 - "YDK Task Registry (Epics/Stories/Tasks)"
Cohesion: 0.25
Nodes (8): Dependencies Field, Story Reference Field, AI Provider Configuration, Execution Configuration, Pre-push Hooks Configuration, Task Management Configuration, YDK Task Registry (Epics/Stories/Tasks), YDK Todos Store

### Community 80 - "Error Schema"
Cohesion: 0.32
Nodes (8): Contract Schema, Crosscut Schema, Entity Schema, Error Schema, Hook Schema, Page Schema, Req Schema, Route Schema

### Community 81 - "T-6ac3f215.md"
Cohesion: 0.29
Nodes (5): .ydk/batch-mapping.json, 2026-08-23T18:41:32Z (UTC), Activity Log, Description, Verification Proof

### Community 82 - "CLAUDE.md"
Cohesion: 0.33
Nodes (6): Code exploration: use graphify, not raw file reads, graphify, Use graphify instead of raw reads, Project Conventions — ole-extractor, Fast path: query existing graph, YDK workflow

### Community 83 - "DocExtractor"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Doc, WordprocessingDocument, DocExtractor

### Community 84 - "SampleGenerator/ServiceRegistration.cs"
Cohesion: 0.40
Nodes (3): SampleGenerator, IServiceCollection, ServiceRegistration

### Community 85 - "ExtractionUtil"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Util, OpenXmlPart, ExtractionUtil

### Community 86 - "QD-c02a7c.md"
Cohesion: 0.29
Nodes (4): docs/adrs directory, graphify-out/graph.json, third_party/npoi submodule, QD-94a6d3: Sync stale status for QD-c02a7c and QD-f9203a

### Community 87 - "ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)"
Cohesion: 0.29
Nodes (7): ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection), Alternatives Considered, Consequences, Context, Decision, Note (2026-08-23), Status

### Community 88 - "S-e496f356.md"
Cohesion: 0.29
Nodes (5): Epic E-a2efe648: NPOI/OpenMcdf library validation and wiring, Activity Log, Description, Activity Log, Description

### Community 90 - "S-43c41d4a.md"
Cohesion: 0.17
Nodes (11): Epic E-46875bcf: OOXML extraction completeness, ExtractFirstLayerEmbedded, FileMetadata, Activity Log, Description, Activity Log, Description, Activity Log (+3 more)

### Community 91 - "DocxEmbeddedXlsxSampleSpecs"
Cohesion: 0.25
Nodes (5): Fact, DocxEmbeddedXlsxAndCorruptionTests, byte, string, DocxEmbeddedXlsxSampleSpecs

### Community 92 - "main"
Cohesion: 0.38
Nodes (6): _find_build_targets(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-build verification check.

### Community 93 - "main"
Cohesion: 0.38
Nodes (6): _find_format_targets(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-format verification check.

### Community 94 - "main"
Cohesion: 0.38
Nodes (6): _find_test_target(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Prefer a .sln (dotnet test filters to test projects automatically); else the…, Run the dotnet-test verification check.

### Community 95 - "Task T-eeb6684b: downgrade to net8.0"
Cohesion: 0.33
Nodes (5): 2026-08-22 — T-eeb6684b: Downgrade ExtractorOLE.csproj to net8.0, Activity Log, PR #4, Task T-eeb6684b: downgrade to net8.0, YDK Development Workflow

### Community 96 - "github-and-merge.md"
Cohesion: 0.47
Nodes (5): graphify reference: GitHub clone and cross-repo merge, Step 0 - Clone GitHub repo(s) (only if a GitHub URL was given), graphify clone, graphify merge-graphs, Multi-subfolder/monorepo merge flow

### Community 97 - "ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)"
Cohesion: 0.33
Nodes (6): ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI), Alternatives Considered, Consequences, Context, Decision, Status

### Community 98 - "Project Rules"
Cohesion: 0.33
Nodes (6): Brownfield Constraints, Conventions, Known Gotchas, Library Stack (locked), Product Context, Project Rules

### Community 99 - "S-0602557d.md"
Cohesion: 0.21
Nodes (10): OLE DocumentSummaryInformation, Epic E-a7ddf85b: Legacy OLE format extraction, NPOI, NPOI HSSFWorkbook (.xls), Activity Log, Description, Activity Log, Description (+2 more)

### Community 101 - "SubfileItem"
Cohesion: 0.29
Nodes (11): Task: Implement doc first-layer embedded-subfile walk, Task: Wire docx first-layer embeddings/media walk, Task: Implement unsupported-format rejection and null validation in Extract, Task: Wire Extract facade to format-dispatch registry, Task: Implement ppt first-layer embedded-subfile walk, Task: Wire pptx first-layer embeddings/media walk, Task: Implement xls first-layer embedded-subfile walk, Task: Wire xlsx first-layer embeddings/media walk (+3 more)

### Community 103 - "Library Tradeoffs — Office File MIME Detection & Extraction"
Cohesion: 0.40
Nodes (5): Cross-cutting note, Library Tradeoffs — Office File MIME Detection & Extraction, Table 1: OOXML parsing/metadata/text/embeddings (docx/xlsx/pptx), Table 2: Legacy OLE (doc/xls/ppt) parsing/metadata/text, Table 3: Raw CFB introspection for mime-sniffing and generic subfile walking

### Community 104 - "Dataset Curation"
Cohesion: 0.40
Nodes (5): Corpus Composition, Corpus Manifest, Dataset Curation, Provenance & Licensing, Purpose

### Community 105 - "QD-012d8e.md"
Cohesion: 0.40
Nodes (4): .env, .gitignore, 2026-08-29T16:58:46Z (UTC), Verification Proof

### Community 106 - "DocOpenStrategyTests"
Cohesion: 0.31
Nodes (4): DateTime, Fact, string, DocOpenStrategyTests

### Community 107 - "task.md"
Cohesion: 0.40
Nodes (4): Acceptance Criteria, Description, Test Strategy, Spec Check Configuration

### Community 108 - "main"
Cohesion: 0.50
Nodes (4): main(), Subprocess-invoke sibling plugin ``name``'s check.py, returning its result., Run the dotnet-quality verification check., _run_sub_check()

### Community 110 - "QD-8a231b.md"
Cohesion: 0.50
Nodes (3): main/NPOI.Core.csproj, NPOI.Core.sln, NU1903 vulnerability (System.Security.Cryptography.Xml 8.0.2)

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

### Community 115 - "T-02637847.md"
Cohesion: 0.32
Nodes (6): ExtractorOLE.Old.Excel, ExcelExtractor.GetFirstLayerEmbedded, SampleGenerator project, XlsxSampleGenerator, Activity Log, Description

### Community 116 - "T-113fe4b2 Orchestrator Progress"
Cohesion: 0.20
Nodes (9): CRITICAL BUG HIT AND FIXED: ydk task done did NOT auto-commit working-tree changes, CRITICAL REMINDER, Done, Done (cont.), Done (final), Exact next step, STATUS: COMPLETE, Status: STARTED - investigation phase (+1 more)

### Community 118 - "T-44812a03.md"
Cohesion: 0.29
Nodes (6): content-only-mime-detection requirement, detect-mime-type contract, Activity Log, Description, Activity Log, Description

### Community 131 - "Epic: Test Suite"
Cohesion: 0.67
Nodes (3): Epic: Test Suite, Story: Unit tests for unit-test-backed scenarios, Story: Integration tests against the corpus manifest

### Community 132 - "Epic: Library Setup & Validation"
Cohesion: 0.67
Nodes (3): Epic: Library Setup & Validation, Story: Empirically validate NPOI i18n fidelity on the legacy OLE path, Story: Wire self-compiled NPOI and OpenMcdf into the build

### Community 133 - "Epic: Public API Surface"
Cohesion: 0.67
Nodes (3): Epic: Public API Surface, Story: Wire the DetectMimeType public facade to the registry, Story: Wire the Extract public facade with full error handling

### Community 147 - "DocOpenStrategy"
Cohesion: 0.33
Nodes (4): DirectoryEntry, IExtractionHelper, DocOpenStrategy, SummaryInformation

### Community 150 - "T-71ab3bc0.md"
Cohesion: 0.12
Nodes (14): $curatedSamplesNote, $note, samples, Story S-da936361 (curated sample dataset), 2026-09-13T20:14:58Z (UTC), 2026-09-13T20:24:35Z (UTC), 2026-09-13T20:25:38Z (UTC), 2026-09-13T20:26:31Z (UTC) (+6 more)

### Community 151 - "T-ce2e3cc1.md"
Cohesion: 0.29
Nodes (7): ExtractorOLE.csproj, QD-fdc901: Land dropped status-sync fix commits, 2026-08-22T21:20:01Z (UTC), Activity Log, Description, Verification Proof, dotnet-build verification manifest

### Community 152 - "T-9a4ff765 — Orchestrator Progress"
Cohesion: 0.29
Nodes (6): Exact next command (if resuming), Side issue encountered and resolved: stale task-status bookkeeping, Status: DONE — PR open, awaiting merge, T-9a4ff765 — Orchestrator Progress, What's left, What was done

### Community 153 - "ExtractionBenchmarks"
Cohesion: 0.33
Nodes (4): Benchmark, byte, ExtractionBenchmarks, GlobalSetup

### Community 154 - "Scope"
Cohesion: 0.40
Nodes (6): Scope, ExcelFormatMetadata, PowerPointFormatMetadata, Task: Map WordFormatMetadata for doc, Task: Complete WordFormatMetadata parity for docx, WordFormatMetadata

### Community 155 - "Rules"
Cohesion: 0.33
Nodes (6): Corpus-Sample-Backed Coverage, Rules, Stress-Harness-Backed Coverage, Test Types, Testing Strategy, Unit-Test-Backed Coverage

### Community 171 - "ADR-002: Time profiling and stress testing approach"
Cohesion: 0.33
Nodes (6): ADR-002: Time profiling and stress testing approach, Alternatives Considered, Consequences, Context, Decision, Status

### Community 173 - "ydk-dotnet-support-recommendation.md"
Cohesion: 0.50
Nodes (4): YDK Fork Task-Dependency Bugs, dotnet-format Verification Manifest, dotnet-quality Verification Manifest, dotnet-test Verification Manifest

### Community 174 - "T-36b41128.md"
Cohesion: 0.40
Nodes (4): 2026-09-01T21:05:25Z (UTC), Activity Log, Description, Verification Proof

### Community 175 - "Task: Record curated samples in manifest.json"
Cohesion: 0.83
Nodes (4): Task: Record curated samples in manifest.json, Task: Author 2+-level nested-embedding curated sample, Task: Author real multi-user revision-history curated sample, Task: Source/author 12 curated real-world-style samples

### Community 178 - "T-07571b32.md"
Cohesion: 0.50
Nodes (3): 2026-09-13T20:14:11Z (UTC), Activity Log, Description

## Ambiguous Edges - Review These
- `YDK Task Registry (Epics/Stories/Tasks)` → `YDK Todos Store`  [AMBIGUOUS]
  .ydk/todos.yaml · relation: conceptually_related_to

## Knowledge Gaps
- **675 isolated node(s):** `check-task-complete.sh script`, `ExtractorOLE.Benchmarks`, `net8.0`, `BenchmarkDotNet (0.14.0)`, `Microsoft.NET.Sdk` (+670 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **51 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `YDK Task Registry (Epics/Stories/Tasks)` and `YDK Todos Store`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `ExtractorOLE.DTOs` connect `ExtractorOLE.DTOs` to `.Detect`, `WordOpenStrategyTests`, `SlidesExtractor.cs`, `PowerPointOpenStrategy`, `XlsOpenStrategy`, `SampleGenerator.Abstractions`, `ExcelOpenStrategy`, `DocOpenStrategy`, `OfficeMimeTypeEnum`, `DocExtractor`, `T-02637847.md`, `ExtractionUtil`, `.Open`, `ExtractorOLE.Helpers.FileTypeStrategy`, `DocumentExtractionResult`?**
  _High betweenness centrality (0.110) - this node is a cross-community bridge._
- **Why does `DocumentFormat.OpenXml extraction path` connect `SampleGenerator.Abstractions` to `WordOpenStrategyTests`, `unicode-fidelity requirement`, `ExtractorOLE.DTOs`, `T-bac988f5.md`, `ExtractorOLE.Helpers.FileTypeStrategy`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._
- **Why does `OpenMcdf` connect `ExtractorOLE.DTOs` to `T-fb89c6f3.md`, `AdversarialSampleGenerator.cs`, `S-e496f356.md`, `T-eeb6684b.md`, `mime-detection.md`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **What connects `check-task-complete.sh script`, `ExtractorOLE.Benchmarks`, `net8.0` to the rest of the system?**
  _675 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `SampleSpec` be split into smaller, more focused modules?**
  _Cohesion score 0.056051587301587304 - nodes in this community are weakly interconnected._
- **Should `.Detect` be split into smaller, more focused modules?**
  _Cohesion score 0.05368382080710848 - nodes in this community are weakly interconnected._