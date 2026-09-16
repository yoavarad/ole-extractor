# Graph Report - T-517f89c5  (2026-09-16)

## Corpus Check
- 318 files · ~289,099 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1956 nodes · 3226 edges · 188 communities (140 shown, 48 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 149 edges (avg confidence: 0.79)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3cc42e70`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- ExtractionResult Entity
- .Apply
- .Detect
- XlsxTextExtractorTests
- extraction.md
- T-3b4e70f4.md
- .RunAsync
- Per-Criterion Detail
- 001-extraction-library-stack.md
- README.md
- unicode-fidelity requirement
- testing-strategy.md
- SampleGenerator.Abstractions
- ExtractorOLE.Exceptions
- project-rules.md
- T-fb89c6f3.md
- Adversarial Review
- --update Incremental Re-extraction
- ExtractorOLE
- ITextExtractor
- OfficeMimeTypeEnum
- T-f96fa73d.md
- Stage 02 Planning Report — Sprint 2 & 3
- T-bac988f5.md
- Legacy .xls parsing -- research spike (T-b70522d2)
- T-eeb6684b.md
- S-43c41d4a.md
- ExtractionHelper
- Extract Contract
- S-0602557d.md
- T-a2b91dc6.md
- ExtractorOLE.Helpers.MimeDetection
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
- DetectMimeType Contract
- Stage 02 Planning Report — 2026-08-19
- exports.md
- SampleSpec
- S-072bfdf6.md
- query.md
- mime-detection.md
- XlsOpenStrategy
- T-9e6bfce8-plan.md
- ZipEntryCorruptor
- T-2ac2f140.md
- T-c2a60f06.md
- .Build
- IOpenStrategy
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
- SubfileItem
- Extraction
- DocOpenStrategyTests
- T-71ab3bc0.md
- MainExtractorDispatchIntegrationTests
- YDK Task Registry (Epics/Stories/Tasks)
- Error Schema
- DocOpenStrategy
- CLAUDE.md
- DocExtractor
- ServiceRegistration
- ExtractionUtil
- QD-c02a7c.md
- ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)
- S-e496f356.md
- .Open
- ole-extractor
- DocxEmbeddedXlsxSampleSpecs
- main
- main
- main
- Task T-eeb6684b: downgrade to net8.0
- github-and-merge.md
- ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)
- Project Rules
- ExtractorOLE.DTOs
- ExcelOpenStrategyTests
- Task: Record curated samples in manifest.json
- SlidesExtractor.cs
- Library Tradeoffs — Office File MIME Detection & Extraction
- Dataset Curation
- QD-012d8e.md
- DetectMimeTypeCorpusManifestTests
- task.md
- main
- DocumentFormat.OpenXml extraction path
- QD-8a231b.md
- Epic: OOXML Extraction Completeness
- Epic: Legacy OLE Extraction
- Epic: Dataset Curation
- Epic: Project Restructure
- T-02637847.md
- GeneratedSample
- BackupLogic.cs
- Activity Log
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
- PptxSampleGenerator
- DocumentExtractionResult
- T-0055ec7c.md
- ExtractionBenchmarks
- .AddEmbeddings
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
- T-36b41128.md
- T-07571b32.md
- T-113fe4b2 Orchestrator Progress
- PptOpenStrategyTests
- T-9a4ff765 — Orchestrator Progress
- T-ef73861f/summary.md
- T-48110fcb.md
- QD-407442.md
- QD-82271f.md
- MIME Detection
- Performance & Stress Testing Tooling — Tradeoffs
- Activity Log

## God Nodes (most connected - your core abstractions)
1. `ExtractorOLE.DTOs` - 48 edges
2. `DocumentExtractionResult` - 30 edges
3. `SampleGenerator.Abstractions` - 30 edges
4. `SampleGenerator.Generators` - 28 edges
5. `ExtractorOLE.Helpers.FileTypeStrategy` - 23 edges
6. `SampleSpec` - 22 edges
7. `ExtractorOLE.Helpers` - 20 edges
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
- `BenchmarkDotNet` --cites--> `ADR-002 Profiling and Stress Testing`  [EXTRACTED]
  .ydk/stories/S-709fe2de.md → README.md

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

## Communities (188 total, 48 thin omitted)

### Community 0 - "ExtractionResult Entity"
Cohesion: 0.18
Nodes (20): Task: Map WordFormatMetadata for doc, Task: Implement doc open component (NPOI HWPFDocument), Task: Implement doc flat-text extraction with unicode fidelity, Task: Complete WordFormatMetadata parity for docx, Task: Implement docx flat-text extraction with unicode fidelity, Task: Map PowerPointFormatMetadata for ppt, Task: Implement ppt open component (NPOI HSLFSlideShow), Task: Implement ppt flat-text extraction with unicode fidelity (+12 more)

### Community 1 - ".Apply"
Cohesion: 0.19
Nodes (8): IPackageProperties, IEnumerable, DocxSampleGenerator, DateTime, IDictionary, OpenXmlPackagePropertiesHelper, XlsxSampleGenerator, SharedStringTable

### Community 2 - ".Detect"
Cohesion: 0.12
Nodes (11): MimeDetectionLimits, Action, int, OoxmlMimeDetector, Fact, OoxmlMimeDetectorGuardrailTests, Fact, OoxmlMimeDetectorTests (+3 more)

### Community 3 - "XlsxTextExtractorTests"
Cohesion: 0.07
Nodes (15): Fact, DocxTextExtractorTests, Fact, InlineData, Theory, XlsTextExtractorTests, Fact, InlineData (+7 more)

### Community 4 - "extraction.md"
Cohesion: 0.09
Nodes (25): Format Family (LegacyOle vs Ooxml), Format Kind (Word/Excel/PowerPoint), Subfile Scope (First-Layer Extraction), Unicode/i18n Fidelity Requirement, Glossary, Epic E-c2846232: Dataset curation, ExtractionHelper.ExtractFirstLayerEmbedded, samples/manifest.json (+17 more)

### Community 5 - "T-3b4e70f4.md"
Cohesion: 0.08
Nodes (25): subfile-scope requirement, SubfileItem entity, Story S-37c18545 (pptx embeddings walk), Activity Log, Description, Activity Log, Description, Activity Log (+17 more)

### Community 6 - ".RunAsync"
Cohesion: 0.08
Nodes (23): CancellationToken, ExtractorOLE.StressHarness, ExtractorOLE.Tests.StressHarness, Program, IServiceCollection, ServiceRegistration, IReadOnlyList, Task (+15 more)

### Community 7 - "Per-Criterion Detail"
Cohesion: 0.06
Nodes (32): 1. Placeholder scan — **PASS**, 2. Internal consistency — **C1–C3 resolved (re-run 2); C4, C5, C6 resolved (re-run 3); C5a resolved (re-run 4); one trivial residual, C5b**, 3. Scope — **PASS**, 4. Ambiguity — **PASS** (all 4 prior findings fixed; 4 minor residuals, none critical), 5. YAGNI — **PASS**, 6. Component reference check — **PASS**, Considered and not counted, Deterministic Scan Results (+24 more)

### Community 8 - "001-extraction-library-stack.md"
Cohesion: 0.27
Nodes (7): ADR-004 Decision: b2xtranslator for Legacy Body Text, DocSharp (manfromarce/DocSharp), IllidanS4/npoi Scratchpad Retarget Fork, LibreOffice Headless Subprocess Conversion, modeverv/dotnet-poi, ADR-004 Legacy doc/ppt Parsing, b2xtranslator library

### Community 9 - "README.md"
Cohesion: 0.29
Nodes (11): ADR-001 Decision: Extraction Library Stack, ADR-001 Extraction Library Stack, ADR-002 Profiling and Stress Testing, ADR-003 Manual Spec Review in Place of Bedrock, DocumentFormat.OpenXml library, ITextExtractor.cs, MainExtractor.cs, NPOI library (+3 more)

### Community 10 - "unicode-fidelity requirement"
Cohesion: 0.10
Nodes (20): ExtractionResult entity, unicode-fidelity requirement, PptxTextExtractor, PptxTextExtractorTests, Story S-8d6146f0 (xlsx flat-text extraction), Activity Log, Description, Activity Log (+12 more)

### Community 11 - "testing-strategy.md"
Cohesion: 0.10
Nodes (21): DetectMimeType, Corpus-Sample-Backed Coverage, Integration Tests, Rules, Stress-Harness-Backed Coverage, Test Types, Testing Strategy, Unit-Test-Backed Coverage (+13 more)

### Community 12 - "SampleGenerator.Abstractions"
Cohesion: 0.12
Nodes (12): ExtractorOLE.Tests.PowerPoint, ExtractorOLE.Tests.Helpers, SampleGenerator.Abstractions, SampleGenerator, SampleGenerator.Generators, ExtractorOLE.Tests.MultiEmbedding, SampleGenerator.Fixtures, ExtractorOLE.Tests.SampleGeneration (+4 more)

### Community 13 - "ExtractorOLE.Exceptions"
Cohesion: 0.15
Nodes (9): ExtractorOLE.Tests.MimeDetection, ExtractorOLE.Exceptions, ExtractorOLE.Configuration, Exception, ExtractorOleException, FileTooLargeException, OversizedNestedContentException, OpenMcdf (+1 more)

### Community 14 - "project-rules.md"
Cohesion: 0.27
Nodes (11): BenchmarkDotNet, ADR-002 Decision: Profiling and Stress Testing Approach, dotnet-counters Tool, Hand-Rolled Stress Harness, OSS-Only Library Constraint, Complexity to Adopt: NBomber vs. Hand-Rolling, Hand-Rolled Harness, HdrHistogram.NET (+3 more)

### Community 15 - "T-fb89c6f3.md"
Cohesion: 0.11
Nodes (16): MimeDetectionResult entity, Story S-072bfdf6 (DetectMimeType facade), Story S-7d040ba7 (macro-variant classification), Activity Log, Description, Activity Log, Description, 2026-08-29T15:47:20Z (UTC) (+8 more)

### Community 16 - "Adversarial Review"
Cohesion: 0.16
Nodes (20): Adversarial Review, Story S-3bc6b929 (CFB detection path), Task: Enforce max-file-size and nesting-depth-guard in CFB detection, Task: Implement pre-parse guardrail errors in Extract, Task: Produce oversized-declared-size and password-protected adversarial samples, Task: Unit tests for file-too-large on both public functions, File Too Large Error, Oversized Nested Content Error (+12 more)

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
Cohesion: 0.12
Nodes (7): OfficeMimeTypeEnum, IExtractionHelper, ITextExtractor, MainExtractor, ITextExtractor, IFormatDispatchRegistry, FakeExtractionHelper

### Community 21 - "T-f96fa73d.md"
Cohesion: 0.09
Nodes (18): $curatedSamplesNote, $note, samples, Story S-41ebb945 (synthetic sample generation), Story S-da936361 (curated sample dataset), Activity Log, Description, 2026-08-23T18:47:51Z (UTC) (+10 more)

### Community 22 - "Stage 02 Planning Report — Sprint 2 & 3"
Cohesion: 0.15
Nodes (21): Content-Only MIME Detection Rule, Dependency Injection Convention for Format Components, Format Extensibility via Registry Pattern, Subfile Scope Definition, Unicode Fidelity Requirement, Time Profiling Epic, OOXML Extraction Completeness Epic, Test Suite Epic (+13 more)

### Community 23 - "T-bac988f5.md"
Cohesion: 0.09
Nodes (26): ADR-004: b2xtranslator decision for legacy .ppt parsing, b2xtranslator (.ppt to .pptx converter library), NPOI HSLFSlideShow (non-functional scratchpad entry point), github.com/IllidanS4/npoi (scratchpad retarget precedent), LibreOffice headless conversion fallback, modeverv/dotnet-poi (rejected, provenance concerns), NPOI, NPOI HPSF/POIFS (metadata, main tree) (+18 more)

### Community 24 - "Legacy .xls parsing -- research spike (T-b70522d2)"
Cohesion: 0.14
Nodes (13): Adoption evidence, Alternatives, Critical gap: embedded OLE objects, Current repo reality (verified in-code, not just b2x-side), Empirical spike results (2026-09-02), Fork attempt blocked, HPSF -> OOXML core/app property mapping, Legacy .xls parsing -- research spike (T-b70522d2) (+5 more)

### Community 25 - "T-eeb6684b.md"
Cohesion: 0.09
Nodes (20): .ydk/batch-mapping.json, ExtractorOLE.csproj, Story S-d01346bc (build/verification tooling), Story S-e496f356 (extraction library stack), 2026-08-22T18:00:40Z (UTC), Verification Proof, QD-fdc901: Land dropped status-sync fix commits, 2026-08-23T18:41:32Z (UTC) (+12 more)

### Community 26 - "S-43c41d4a.md"
Cohesion: 0.15
Nodes (13): Epic E-46875bcf: OOXML extraction completeness, ExcelFormatMetadata, ExtractFirstLayerEmbedded, FileMetadata, PowerPointFormatMetadata, Activity Log, Description, Activity Log (+5 more)

### Community 27 - "ExtractionHelper"
Cohesion: 0.24
Nodes (8): EmbeddedFileItem, HSSFWorkbook, List, OpenXmlPackage, OpenXmlPart, ExtractionHelper, HSSFObjectData, HSSFPictureData

### Community 28 - "Extract Contract"
Cohesion: 0.16
Nodes (20): Task: Implement parse-time error handling in Extract, Task: Produce corrupt/truncated/zero-byte adversarial samples, Task: Produce wrong-extension and corrupt-embedded-object adversarial samples, Task: Assemble and finalize samples/manifest.json, Task: Integration tests for adversarial scenarios, Task: Integration tests for DetectMimeType against corpus manifest, Task: Integration tests for Extract against corpus manifest, Task: Implement >=50 concurrent-call exercise of both public functions (+12 more)

### Community 29 - "S-0602557d.md"
Cohesion: 0.23
Nodes (9): OLE DocumentSummaryInformation, Epic E-a7ddf85b: Legacy OLE format extraction, SummaryInformation, Activity Log, Description, Activity Log, Description, Activity Log (+1 more)

### Community 30 - "T-a2b91dc6.md"
Cohesion: 0.15
Nodes (11): ExtractionRequest entity, MimeDetectionResult, Unsupported Format Error, Activity Log, Description, Activity Log, Description, Activity Log (+3 more)

### Community 31 - "ExtractorOLE.Helpers.MimeDetection"
Cohesion: 0.24
Nodes (3): ExtractorOLE.Registry, ExtractorOLE.Helpers.MimeDetection, ExtractorOLE.Tests.Registry

### Community 32 - "FormatDispatchRegistryTests"
Cohesion: 0.21
Nodes (6): IDictionary, ITextExtractor, FormatDispatchRegistry, Fact, FakeOpenStrategy, FormatDispatchRegistryTests

### Community 33 - "XlsOpenStrategyTests"
Cohesion: 0.23
Nodes (3): DateTime, Fact, XlsOpenStrategyTests

### Community 34 - "T-6f033869.md"
Cohesion: 0.14
Nodes (12): samples/curated/ directory, Activity Log, Description, Activity Log, Description, Activity Log, Description, Description (+4 more)

### Community 35 - "What You Must Do When Invoked"
Cohesion: 0.13
Nodes (15): Part A - Structural extraction for code files, Part B - Semantic extraction (parallel subagents), Part C - Merge AST + semantic into final extraction, Step 0 - GitHub repos and multi-path merge (only if a URL or several paths), Step 1 - Ensure graphify is installed, Step 2.5 - Video and audio (only if video files detected), Step 2 - Detect files, Step 3 - Extract entities and relationships (+7 more)

### Community 36 - "remediate_task_status.py"
Cohesion: 0.22
Nodes (13): Any, compute_true_status_by_task_id(), correct_manifest_statuses(), correct_task_md_frontmatter(), gh_pr_list(), main(), Resolve stale slugs in manifest['tasks'][*]['dependencies'] in place. Returns…, Correct manifest['tasks'][*]['status'] in place to match true state.… (+5 more)

### Community 37 - "Upstream recommendation: .NET/C# support in YDK"
Cohesion: 0.15
Nodes (13): YDK Fork Task-Dependency Bugs, 1. Ship three global plugins, not one bundle, 2. Add a `dotnet` stack to `stacks.py`, 3. Centralize the SDK-vs-muxer detection — this is the real gotcha, 4. Target-discovery edge cases actually hit (and the fix already applied), 5. `tdd-guard` is Python-only — known gap, not fixed here, 7. Whole-target build/test checks can permanently block ALL pushes on a pre-existing broken baseline, 8. Priority summary (+5 more)

### Community 38 - "Legacy Office Format Library Landscape Scan"
Cohesion: 0.14
Nodes (14): b2xtranslator (original) — dead, but see DocSharp fork below, CFB/OLE2 readers found (beyond OpenMcdf), Citations/links, Commercial libraries excluded, Conclusion, DocSharp (manfromarce/DocSharp) — actively maintained fork of b2xtranslator, notable finding, IKVM + Apache POI (Java interop) — not a sane approach, confirmed, Kaitai Struct-generated CFB parser (curiosity, not a library) (+6 more)

### Community 39 - ".Detect"
Cohesion: 0.05
Nodes (27): DetectedFormatEnum, MimeDetectionRequest, MimeDetectionResult, Action, int, string, CfbMimeDetector, ICfbMimeDetector (+19 more)

### Community 40 - "PowerPointOpenStrategyTests"
Cohesion: 0.30
Nodes (5): Fact, ShapeTree, SlidePart, PowerPointOpenStrategyTests, ISet

### Community 41 - "epic.md"
Cohesion: 0.18
Nodes (12): Description, Release Field (Epic), Scope, Spec Refs Field (Epic), Acceptance Criteria, Epic Field (Story), Spec Refs Field (Story), PR Template (+4 more)

### Community 42 - "T-b62ffe04.md"
Cohesion: 0.13
Nodes (17): ADR-002, Success Criteria, Stress Testing, Time Profiling, Story S-709fe2de (benchmarking), Task: Establish committed baseline and CI regression documentation, Task: Implement Extract() latency/allocation benchmarks per format, Task: Scaffold separate BenchmarkDotNet project (+9 more)

### Community 43 - "DetectMimeType Contract"
Cohesion: 0.38
Nodes (11): Task: Implement CFB directory-entry detection component, Task: Register CFB detection component in DI registry, Task: Add null request/field validation to DetectMimeType, Task: Wire DetectMimeType facade to OOXML + CFB registry, Task: Harden OOXML detection to be strictly content-only, Task: Classify macro-enabled OOXML variants as base format, Task: Handle zero-byte input as Unknown in OOXML detection, Task: Unit tests for null validation on DetectMimeType (+3 more)

### Community 44 - "Stage 02 Planning Report — 2026-08-19"
Cohesion: 0.14
Nodes (13): Command Log (all invocations used, in order), DAG Validation, Dependency graph modeled, Epics created, Open Questions / Risks for Stage 03, Pass 1 — Full Backlog (Epics + Stories), Pass 2 — Sprint 1 Tasks (Epic 1 + Epic 2), Sanity checks after Pass 1 (+5 more)

### Community 45 - "exports.md"
Cohesion: 0.17
Nodes (12): Token reduction benchmark, FalkorDB export, GraphML export, MCP stdio server, Neo4j export, SVG export, Wiki export, Step 4.5: Graph health check (+4 more)

### Community 46 - "SampleSpec"
Cohesion: 0.17
Nodes (8): Fact, CuratedNestedEmbeddingSampleTests, IList, IDictionary, SampleSpec, byte, string, CuratedSampleSpecs

### Community 47 - "S-072bfdf6.md"
Cohesion: 0.08
Nodes (24): CFB detection component, DI-registered format-dispatch registry, Public API, Epic E-6728ee81: CFB detection, Epic E-b54e8704: Mime detection hardening, Epic E-bd43bc02: Public facade wiring, Extract() contract, ExtractionRequest (+16 more)

### Community 48 - "query.md"
Cohesion: 0.20
Nodes (11): BFS/DFS traversal modes, /graphify explain, For /graphify explain, For /graphify path, graphify reference: query, path, explain, /graphify path, save-result feedback loop, Step 0 — Constrained query expansion (REQUIRED before traversal) (+3 more)

### Community 49 - "mime-detection.md"
Cohesion: 0.15
Nodes (16): Content-Only MIME Detection Rule, Auth & Security, Component Architecture, Detection Component, Open Component, Overview, Problem Statement, Scope (+8 more)

### Community 50 - "XlsOpenStrategy"
Cohesion: 0.15
Nodes (9): List, ExcelFormatMetadata, FormatMetadata, PowerPointFormatMetadata, WordFormatMetadata, HSSFWorkbook, IExtractionHelper, string (+1 more)

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

### Community 55 - ".Build"
Cohesion: 0.36
Nodes (4): Fact, MultiEmbeddingSampleTests, byte, MultiEmbeddingSampleSpecs

### Community 56 - "IOpenStrategy"
Cohesion: 0.16
Nodes (10): IExtractionHelper, WorkbookPart, ExcelOpenStrategy, IOpenStrategy, IExtractionHelper, WordprocessingDocument, WordOpenStrategy, ZipFallbackOpenStrategy (+2 more)

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
Cohesion: 0.13
Nodes (13): content-only-mime-detection requirement, detect-mime-type contract, password-protected error, Story S-f6d7124a (mime-detection integration tests), Activity Log, Description, 2026-09-13T10:19:05Z (UTC), Activity Log (+5 more)

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
Cohesion: 0.29
Nodes (11): Task: Implement doc first-layer embedded-subfile walk, Task: Wire docx first-layer embeddings/media walk, Task: Implement unsupported-format rejection and null validation in Extract, Task: Wire Extract facade to format-dispatch registry, Task: Implement ppt first-layer embedded-subfile walk, Task: Wire pptx first-layer embeddings/media walk, Task: Implement xls first-layer embedded-subfile walk, Task: Wire xlsx first-layer embeddings/media walk (+3 more)

### Community 75 - "Extraction"
Cohesion: 0.25
Nodes (8): Error Handling, Extensibility & DI, Extraction, Flat Text, Format-Specific Metadata, General Metadata, Purpose, Subfiles

### Community 76 - "DocOpenStrategyTests"
Cohesion: 0.31
Nodes (4): DateTime, Fact, string, DocOpenStrategyTests

### Community 77 - "T-71ab3bc0.md"
Cohesion: 0.22
Nodes (8): 2026-09-13T20:14:58Z (UTC), 2026-09-13T20:24:35Z (UTC), 2026-09-13T20:25:38Z (UTC), 2026-09-13T20:26:31Z (UTC), 2026-09-13T20:29:01Z (UTC), Activity Log, Description, Verification Proof

### Community 78 - "MainExtractorDispatchIntegrationTests"
Cohesion: 0.50
Nodes (3): Fact, MainExtractor, MainExtractorDispatchIntegrationTests

### Community 79 - "YDK Task Registry (Epics/Stories/Tasks)"
Cohesion: 0.25
Nodes (8): Dependencies Field, Story Reference Field, AI Provider Configuration, Execution Configuration, Pre-push Hooks Configuration, Task Management Configuration, YDK Task Registry (Epics/Stories/Tasks), YDK Todos Store

### Community 80 - "Error Schema"
Cohesion: 0.32
Nodes (8): Contract Schema, Crosscut Schema, Entity Schema, Error Schema, Hook Schema, Page Schema, Req Schema, Route Schema

### Community 81 - "DocOpenStrategy"
Cohesion: 0.50
Nodes (3): DirectoryEntry, IExtractionHelper, DocOpenStrategy

### Community 82 - "CLAUDE.md"
Cohesion: 0.33
Nodes (6): Code exploration: use graphify, not raw file reads, graphify, Use graphify instead of raw reads, Project Conventions — ole-extractor, Fast path: query existing graph, YDK workflow

### Community 83 - "DocExtractor"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Doc, WordprocessingDocument, DocExtractor

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

### Community 89 - ".Open"
Cohesion: 0.24
Nodes (6): IExtractionHelper, PresentationPart, SlidePart, PowerPointOpenStrategy, Fact, PptxFirstLayerEmbeddedWalkTests

### Community 90 - "ole-extractor"
Cohesion: 0.29
Nodes (7): Build & test, Development workflow, Documentation, Library stack, ole-extractor, Project structure, Supported formats

### Community 91 - "DocxEmbeddedXlsxSampleSpecs"
Cohesion: 0.38
Nodes (3): byte, string, DocxEmbeddedXlsxSampleSpecs

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

### Community 99 - "ExtractorOLE.DTOs"
Cohesion: 0.14
Nodes (7): ExtractorOLE, ExtractorOLE.DTOs, ExtractorOLE.Helpers.FileTypeStrategy, ExtractorOLE.Tests.Doc, ExtractorOLE.Benchmarks, ExtractorOLE.Helpers, ExtractorOLE.Tests.Integration

### Community 101 - "Task: Record curated samples in manifest.json"
Cohesion: 0.83
Nodes (4): Task: Record curated samples in manifest.json, Task: Author 2+-level nested-embedding curated sample, Task: Author real multi-user revision-history curated sample, Task: Source/author 12 curated real-world-style samples

### Community 103 - "Library Tradeoffs — Office File MIME Detection & Extraction"
Cohesion: 0.40
Nodes (5): Cross-cutting note, Library Tradeoffs — Office File MIME Detection & Extraction, Table 1: OOXML parsing/metadata/text/embeddings (docx/xlsx/pptx), Table 2: Legacy OLE (doc/xls/ppt) parsing/metadata/text, Table 3: Raw CFB introspection for mime-sniffing and generic subfile walking

### Community 104 - "Dataset Curation"
Cohesion: 0.40
Nodes (5): Corpus Composition, Corpus Manifest, Dataset Curation, Provenance & Licensing, Purpose

### Community 105 - "QD-012d8e.md"
Cohesion: 0.40
Nodes (4): .env, .gitignore, 2026-08-29T16:58:46Z (UTC), Verification Proof

### Community 106 - "DetectMimeTypeCorpusManifestTests"
Cohesion: 0.16
Nodes (12): ExpectedDetectedFormat, Fact, IEnumerable, List, string, Theory, DetectMimeTypeCorpusManifestTests, ExpectedDetectedFormat (+4 more)

### Community 107 - "task.md"
Cohesion: 0.40
Nodes (4): Acceptance Criteria, Description, Test Strategy, Spec Check Configuration

### Community 108 - "main"
Cohesion: 0.50
Nodes (4): main(), Subprocess-invoke sibling plugin ``name``'s check.py, returning its result., Run the dotnet-quality verification check., _run_sub_check()

### Community 109 - "DocumentFormat.OpenXml extraction path"
Cohesion: 0.21
Nodes (6): ExtractorOLE.Handlers, ExtractorOLE.Tests.Docx, ExtractorOLE.Tests.Excel, DocumentFormat.OpenXml extraction path, Fact, ExcelExtractorEmbeddedObjectTests

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

### Community 116 - "GeneratedSample"
Cohesion: 0.23
Nodes (6): GeneratedSample, ISampleGenerator, SampleFormat, DocSampleGenerator, PptSampleGenerator, XlsSampleGenerator

### Community 118 - "Activity Log"
Cohesion: 0.50
Nodes (4): 2026-09-02T05:45:01Z (UTC), 2026-09-02T05:46:06Z (UTC), 2026-09-02T05:47:27Z (UTC), Activity Log

### Community 131 - "Epic: Test Suite"
Cohesion: 0.67
Nodes (3): Epic: Test Suite, Story: Unit tests for unit-test-backed scenarios, Story: Integration tests against the corpus manifest

### Community 132 - "Epic: Library Setup & Validation"
Cohesion: 0.67
Nodes (3): Epic: Library Setup & Validation, Story: Empirically validate NPOI i18n fidelity on the legacy OLE path, Story: Wire self-compiled NPOI and OpenMcdf into the build

### Community 133 - "Epic: Public API Surface"
Cohesion: 0.67
Nodes (3): Epic: Public API Surface, Story: Wire the DetectMimeType public facade to the registry, Story: Wire the Extract public facade with full error handling

### Community 147 - "PptxSampleGenerator"
Cohesion: 0.31
Nodes (7): PresentationPart, ShapeTree, SlidePart, PptxSampleGenerator, SlideLayoutPart, SlideMasterPart, ThemePart

### Community 150 - "DocumentExtractionResult"
Cohesion: 0.12
Nodes (14): DateTime, List, DocumentExtractionResult, FileMetadata, IExtractionHelper, PresentationPart, SlidePart, PptOpenStrategy (+6 more)

### Community 152 - "T-0055ec7c.md"
Cohesion: 0.17
Nodes (10): DetectMimeType contract, Activity Log, Description, 2026-09-14T20:48:35Z (UTC), 2026-09-14T21:01:57Z (UTC), Activity Log, Description, Verification Proof (+2 more)

### Community 153 - "ExtractionBenchmarks"
Cohesion: 0.33
Nodes (4): Benchmark, byte, ExtractionBenchmarks, GlobalSetup

### Community 155 - ".AddEmbeddings"
Cohesion: 0.43
Nodes (4): EmbeddedContentSpec, IEnumerable, OpenXmlPart, OpenXmlEmbeddingHelper

### Community 171 - "ADR-002: Time profiling and stress testing approach"
Cohesion: 0.33
Nodes (6): ADR-002: Time profiling and stress testing approach, Alternatives Considered, Consequences, Context, Decision, Status

### Community 174 - "T-36b41128.md"
Cohesion: 0.40
Nodes (4): 2026-09-01T21:05:25Z (UTC), Activity Log, Description, Verification Proof

### Community 178 - "T-07571b32.md"
Cohesion: 0.50
Nodes (3): 2026-09-13T20:14:11Z (UTC), Activity Log, Description

### Community 179 - "T-113fe4b2 Orchestrator Progress"
Cohesion: 0.20
Nodes (9): CRITICAL BUG HIT AND FIXED: ydk task done did NOT auto-commit working-tree changes, CRITICAL REMINDER, Done, Done (cont.), Done (final), Exact next step, STATUS: COMPLETE, Status: STARTED - investigation phase (+1 more)

### Community 180 - "PptOpenStrategyTests"
Cohesion: 0.43
Nodes (3): DateTime, Fact, PptOpenStrategyTests

### Community 181 - "T-9a4ff765 — Orchestrator Progress"
Cohesion: 0.29
Nodes (6): Exact next command (if resuming), Side issue encountered and resolved: stale task-status bookkeeping, Status: DONE — PR open, awaiting merge, T-9a4ff765 — Orchestrator Progress, What's left, What was done

### Community 190 - "MIME Detection"
Cohesion: 0.33
Nodes (6): Extensibility, Guardrails, How Detection Works, Macro-Enabled Variants, MIME Detection, Purpose

### Community 192 - "Performance & Stress Testing Tooling — Tradeoffs"
Cohesion: 0.50
Nodes (4): 1. Time Profiling: BenchmarkDotNet vs. Alternatives, 2. Stress Testing: NBomber vs. Hand-Rolled Harness, Performance & Stress Testing Tooling — Tradeoffs, Summary — If Forced to Pick Today

### Community 195 - "Activity Log"
Cohesion: 0.50
Nodes (4): 2026-08-24 - Verification, side-effect cleanup, and completion, 2026-08-24T20:56:24Z (UTC), 2026-08-24T20:57:53Z (UTC), Activity Log

## Ambiguous Edges - Review These
- `YDK Task Registry (Epics/Stories/Tasks)` → `YDK Todos Store`  [AMBIGUOUS]
  .ydk/todos.yaml · relation: conceptually_related_to

## Knowledge Gaps
- **675 isolated node(s):** `check-task-complete.sh script`, `ExtractorOLE.Benchmarks`, `net8.0`, `BenchmarkDotNet (0.14.0)`, `Microsoft.NET.Sdk` (+670 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **48 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `YDK Task Registry (Epics/Stories/Tasks)` and `YDK Todos Store`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `ExtractorOLE.DTOs` connect `ExtractorOLE.DTOs` to `SlidesExtractor.cs`, `.Detect`, `SampleGenerator.Abstractions`, `ExtractorOLE.Exceptions`, `DocumentFormat.OpenXml extraction path`, `XlsOpenStrategy`, `DocExtractor`, `OfficeMimeTypeEnum`, `T-02637847.md`, `DocumentExtractionResult`, `ExtractionUtil`, `ExtractorOLE.Helpers.MimeDetection`?**
  _High betweenness centrality (0.102) - this node is a cross-community bridge._
- **Why does `DocumentFormat.OpenXml extraction path` connect `DocumentFormat.OpenXml extraction path` to `ExtractorOLE.DTOs`, `unicode-fidelity requirement`, `SampleGenerator.Abstractions`, `T-bac988f5.md`, `ExtractorOLE.Helpers.MimeDetection`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **Why does `OpenMcdf` connect `ExtractorOLE.Exceptions` to `S-e496f356.md`, `T-eeb6684b.md`, `Adversarial Review`, `S-072bfdf6.md`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **What connects `check-task-complete.sh script`, `ExtractorOLE.Benchmarks`, `net8.0` to the rest of the system?**
  _675 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `.Detect` be split into smaller, more focused modules?**
  _Cohesion score 0.11806543385490754 - nodes in this community are weakly interconnected._
- **Should `XlsxTextExtractorTests` be split into smaller, more focused modules?**
  _Cohesion score 0.0707070707070707 - nodes in this community are weakly interconnected._