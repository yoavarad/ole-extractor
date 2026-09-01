# Graph Report - ole-extractor  (2026-09-01)

## Corpus Check
- 0 files · ~0 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1754 nodes · 2927 edges · 170 communities (117 shown, 53 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 143 edges (avg confidence: 0.79)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- MIME Detection Contracts
- Sample Generation & Embedding
- DOCX Text Extraction Tests
- Format Dispatch & Epics
- Spec Verification Report
- Sample Dataset Curation
- Format Metadata Entities
- Subfile / Embedding Walk
- OOXML Detection Guardrail Tests
- Legacy PPT Parsing Decision
- ExtractorOLE Solution Structure
- Overview Spec & Adversarial Review
- Unicode Fidelity & Extraction Result
- YDK Tooling & Build Verification
- Graphify Skill Documentation
- Format Taxonomy Specs
- OpenXML Library Dependencies
- Sample Generator Test Suite
- Public API Facade
- Text Extractor Implementations
- Community 20
- Community 21
- Community 22
- Community 23
- Community 24
- Community 25
- Community 26
- Community 27
- Community 28
- Community 29
- Community 30
- Community 31
- Community 32
- Community 33
- Community 34
- Community 35
- Community 36
- Community 37
- Community 38
- Community 39
- Community 40
- Community 41
- Community 42
- Community 43
- Community 44
- Community 45
- Community 46
- Community 47
- Community 48
- Community 49
- Community 50
- Community 51
- Community 52
- Community 53
- Community 54
- Community 55
- Community 56
- Community 57
- Community 58
- Community 59
- Community 60
- Community 61
- Community 62
- Community 63
- Community 64
- Community 65
- Community 66
- Community 67
- Community 68
- Community 69
- Community 70
- Community 71
- Community 72
- Community 73
- Community 74
- Community 75
- Community 76
- Community 77
- Community 78
- Community 79
- Community 80
- Community 81
- Community 82
- Community 83
- Community 84
- Community 85
- Community 86
- Community 87
- Community 88
- Community 89
- Community 90
- Community 91
- Community 92
- Community 93
- Community 94
- Community 95
- Community 96
- Community 97
- Community 98
- Community 99
- Community 100
- Community 101
- Community 102
- Community 103
- Community 104
- Community 105
- Community 106
- Community 107
- Community 108
- Community 109
- Community 110
- Community 111
- Community 112
- Community 113
- Community 114
- Community 115
- Community 116
- Community 117
- Community 118
- Community 119
- Community 120
- Community 121
- Community 122
- Community 123
- Community 124
- Community 125
- Community 126
- Community 127
- Community 128
- Community 129
- Community 130
- Community 131
- Community 132
- Community 133
- Community 134
- Community 135
- Community 136
- Community 137
- Community 138
- Community 139
- Community 140
- Community 141
- Community 142
- Community 143
- Community 144
- Community 145
- Community 146
- Community 147
- Community 150
- Community 151
- Community 152
- Community 153
- Community 154
- Community 155
- Community 156
- Community 157
- Community 159
- Community 160
- Community 161
- Community 162
- Community 163
- Community 164
- Community 165
- Community 166
- Community 167
- Community 168
- Community 169

## God Nodes (most connected - your core abstractions)
1. `ExtractorOLE.DTOs` - 42 edges
2. `SampleGenerator.Abstractions` - 29 edges
3. `DocumentExtractionResult` - 27 edges
4. `SampleGenerator.Generators` - 24 edges
5. `SampleSpec` - 22 edges
6. `ExtractorOLE.Helpers.FileTypeStrategy` - 19 edges
7. `OfficeMimeTypeEnum` - 17 edges
8. `ExtractorOLE.Helpers` - 16 edges
9. `Extract Contract` - 16 edges
10. `ExtractionResult Entity` - 16 edges

## Surprising Connections (you probably didn't know these)
- `HdrHistogram.NET` --semantically_similar_to--> `BenchmarkDotNet`  [INFERRED] [semantically similar]
  docs/research/stress-testing-tools.md → .ydk/stories/S-709fe2de.md
- `NBomber` --semantically_similar_to--> `NPOI library`  [INFERRED] [semantically similar]
  docs/research/stress-testing-tools.md → README.md
- `Spec Check Configuration` --semantically_similar_to--> `Acceptance Criteria`  [INFERRED] [semantically similar]
  .ydk/config.yaml → .github/ISSUE_TEMPLATE/task.md
- `Unit-Test-Backed Coverage` --references--> `Max File Size NFR`  [EXTRACTED]
  docs/specs/testing-strategy.md → .ydk/components/nfr/extraction/max-file-size.yaml
- `Story Reference Field` --shares_data_with--> `YDK Task Registry (Epics/Stories/Tasks)`  [INFERRED]
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

## Communities (170 total, 53 thin omitted)

### Community 0 - "MIME Detection Contracts"
Cohesion: 0.06
Nodes (57): Scope, OLE DocumentSummaryInformation, Epic E-46875bcf: OOXML extraction completeness, Epic E-a7ddf85b: Legacy OLE format extraction, ExcelFormatMetadata, ExtractFirstLayerEmbedded, FileMetadata, NPOI (+49 more)

### Community 1 - "Sample Generation & Embedding"
Cohesion: 0.06
Nodes (33): Fact, CuratedNestedEmbeddingSampleTests, IList, IPackageProperties, EmbeddedContentSpec, GeneratedSample, ISampleGenerator, SampleFormat (+25 more)

### Community 2 - "DOCX Text Extraction Tests"
Cohesion: 0.06
Nodes (25): MimeDetectionLimits, DetectedFormatEnum, MimeDetectionRequest, MimeDetectionResult, Action, int, string, CfbMimeDetector (+17 more)

### Community 3 - "Format Dispatch & Epics"
Cohesion: 0.07
Nodes (15): Fact, DocxTextExtractorTests, Fact, InlineData, Theory, XlsTextExtractorTests, Fact, InlineData (+7 more)

### Community 4 - "Spec Verification Report"
Cohesion: 0.08
Nodes (26): Content-Only MIME Detection Rule, Format Family (LegacyOle vs Ooxml), Format Kind (Word/Excel/PowerPoint), Subfile Scope (First-Layer Extraction), Glossary, Epic E-5717c1fe: Testing (unit/integration), Epic E-c2846232: Dataset curation, ExtractionHelper.ExtractFirstLayerEmbedded (+18 more)

### Community 5 - "Sample Dataset Curation"
Cohesion: 0.08
Nodes (26): ExtractorOLE.Old.Excel, subfile-scope requirement, SubfileItem entity, ExcelExtractor.GetFirstLayerEmbedded, SampleGenerator project, Story S-37c18545 (pptx embeddings walk), XlsxSampleGenerator, Activity Log (+18 more)

### Community 6 - "Format Metadata Entities"
Cohesion: 0.08
Nodes (16): Benchmark, ExtractorOLE, ExtractorOLE.Benchmarks, ExtractorOLE.StressHarness, byte, ExtractionBenchmarks, ITextExtractor, Program (+8 more)

### Community 7 - "Subfile / Embedding Walk"
Cohesion: 0.06
Nodes (32): 1. Placeholder scan — **PASS**, 2. Internal consistency — **C1–C3 resolved (re-run 2); C4, C5, C6 resolved (re-run 3); C5a resolved (re-run 4); one trivial residual, C5b**, 3. Scope — **PASS**, 4. Ambiguity — **PASS** (all 4 prior findings fixed; 4 minor residuals, none critical), 5. YAGNI — **PASS**, 6. Component reference check — **PASS**, Considered and not counted, Deterministic Scan Results (+24 more)

### Community 8 - "OOXML Detection Guardrail Tests"
Cohesion: 0.17
Nodes (21): ADR-001 Decision: Extraction Library Stack, ADR-004 Decision: b2xtranslator for Legacy Body Text, DocSharp (manfromarce/DocSharp), IllidanS4/npoi Scratchpad Retarget Fork, LibreOffice Headless Subprocess Conversion, modeverv/dotnet-poi, OSS-Only Library Constraint, Unicode/i18n Fidelity Requirement (+13 more)

### Community 9 - "Legacy PPT Parsing Decision"
Cohesion: 0.17
Nodes (6): Fact, OoxmlMimeDetectorGuardrailTests, Fact, OoxmlMimeDetectorTests, byte, MacroEmbedSampleSpecs

### Community 10 - "ExtractorOLE Solution Structure"
Cohesion: 0.10
Nodes (20): ExtractionResult entity, unicode-fidelity requirement, PptxTextExtractor, PptxTextExtractorTests, Story S-8d6146f0 (xlsx flat-text extraction), Activity Log, Description, 2026-08-25T21:08:52Z (UTC) (+12 more)

### Community 11 - "Overview Spec & Adversarial Review"
Cohesion: 0.10
Nodes (21): ADR-002, Corpus-Sample-Backed Coverage, Integration Tests, Rules, Stress-Harness-Backed Coverage, Stress Testing, Test Types, Testing Strategy (+13 more)

### Community 12 - "Unicode Fidelity & Extraction Result"
Cohesion: 0.16
Nodes (8): ExtractorOLE.Handlers, SampleGenerator.Abstractions, SampleGenerator.Generators, SampleGenerator.Fixtures, ExtractorOLE.Tests.SampleGeneration, DocumentFormat.OpenXml extraction path, CuratedSampleSpecs.BuildMeetingMinutesBoardroom, CuratedSampleSpecs.BuildProductLaunchDeck

### Community 13 - "YDK Tooling & Build Verification"
Cohesion: 0.17
Nodes (8): ExtractorOLE.Tests.Helpers, ExtractorOLE.DTOs, ExtractorOLE.Registry, ExtractorOLE.Helpers.MimeDetection, ExtractorOLE.Tests.MimeDetection, ExtractorOLE.Tests.Registry, ExtractorOLE.Configuration, OpenMcdf

### Community 14 - "Graphify Skill Documentation"
Cohesion: 0.11
Nodes (20): BenchmarkDotNet, ADR-002 Decision: Profiling and Stress Testing Approach, dotnet-counters Tool, Hand-Rolled Stress Harness, ADR-002: Time profiling and stress testing approach, Alternatives Considered, Consequences, Context (+12 more)

### Community 15 - "Format Taxonomy Specs"
Cohesion: 0.11
Nodes (17): MimeDetectionResult entity, Program.cs, Story S-072bfdf6 (DetectMimeType facade), Story S-7d040ba7 (macro-variant classification), Activity Log, Description, Activity Log, Description (+9 more)

### Community 16 - "OpenXML Library Dependencies"
Cohesion: 0.20
Nodes (20): Adversarial Review, Success Criteria, Task: Enforce max-file-size and nesting-depth-guard in CFB detection, Task: Implement pre-parse guardrail errors in Extract, Task: Produce oversized-declared-size and password-protected adversarial samples, Task: Unit tests for file-too-large on both public functions, Extract Contract, File Too Large Error (+12 more)

### Community 17 - "Sample Generator Test Suite"
Cohesion: 0.09
Nodes (19): .graphify_detect.json, graphify reference: transcribe video and audio, Step 2.5: Transcribe Video/Audio, Step 2.5 - Transcribe video / audio files (only if video files detected), transcribe_all(), .graphify_transcripts.json, GRAPHIFY_WHISPER_MODEL env var, Whisper Domain-Hint Prompt (+11 more)

### Community 18 - "Public API Facade"
Cohesion: 0.13
Nodes (20): ExtractorOLE.Benchmarks, ExtractorOLE, DocumentFormat.OpenXml (3.5.1), DocumentFormat.OpenXml.Features (3.5.1), DocumentFormat.OpenXml.Framework (3.5.1), Microsoft.Extensions.DependencyInjection (8.0.0), OpenMcdf (3.2.0), ExtractorOLE.Tests (+12 more)

### Community 19 - "Text Extractor Implementations"
Cohesion: 0.11
Nodes (11): DocxTextExtractor, PptxTextExtractor, XlsTextExtractor, Cell, List, WorkbookPart, XlsxTextExtractor, FakeTextExtractor (+3 more)

### Community 20 - "Community 20"
Cohesion: 0.17
Nodes (6): OfficeMimeTypeEnum, IExtractionHelper, MainExtractor, ITextExtractor, IFormatDispatchRegistry, FakeExtractionHelper

### Community 21 - "Community 21"
Cohesion: 0.10
Nodes (16): $note, samples, Story S-41ebb945 (synthetic sample generation), Story S-da936361 (curated sample dataset), Activity Log, Description, 2026-08-23T18:47:51Z (UTC), Activity Log (+8 more)

### Community 22 - "Community 22"
Cohesion: 0.15
Nodes (21): Content-Only MIME Detection Rule, Dependency Injection Convention for Format Components, Format Extensibility via Registry Pattern, Subfile Scope Definition, Unicode Fidelity Requirement, Time Profiling Epic, OOXML Extraction Completeness Epic, Test Suite Epic (+13 more)

### Community 23 - "Community 23"
Cohesion: 0.13
Nodes (17): github.com/IllidanS4/npoi (scratchpad retarget precedent), modeverv/dotnet-poi (rejected, provenance concerns), NPOI HPSF/POIFS (metadata, main tree), NPOI HSLF/HSLFSlideShow (.ppt, scratchpad), NPOI HWPFDocument (.doc, scratchpad), Story S-32677db6 (doc open component), 2026-08-24T10:28:45Z (UTC), Activity Log (+9 more)

### Community 24 - "Community 24"
Cohesion: 0.12
Nodes (15): Extensibility, Guardrails, How Detection Works, Macro-Enabled Variants, MIME Detection, Purpose, content-only-mime-detection requirement, detect-mime-type contract (+7 more)

### Community 25 - "Community 25"
Cohesion: 0.12
Nodes (15): ExtractorOLE.csproj, Story S-d01346bc (build/verification tooling), Story S-e496f356 (extraction library stack), 2026-08-22T18:00:40Z (UTC), Verification Proof, QD-fdc901: Land dropped status-sync fix commits, 2026-08-22T21:20:01Z (UTC), Activity Log (+7 more)

### Community 26 - "Community 26"
Cohesion: 0.12
Nodes (15): CFB detection component, Epic E-6728ee81: CFB detection, Epic E-b54e8704: Mime detection hardening, MimeDetectionResult, ZIP/OOXML relationship-based detection, Task: Implement unsupported-format rejection and null validation in Extract, Task: Unit tests for null validation on Extract, ExtractionRequest Entity (+7 more)

### Community 27 - "Community 27"
Cohesion: 0.18
Nodes (7): HSSFWorkbook, List, OpenXmlPackage, OpenXmlPart, ExtractionHelper, HSSFObjectData, HSSFPictureData

### Community 28 - "Community 28"
Cohesion: 0.19
Nodes (16): Task: Implement parse-time error handling in Extract, Task: Produce corrupt/truncated/zero-byte adversarial samples, Task: Produce wrong-extension and corrupt-embedded-object adversarial samples, Task: Assemble and finalize samples/manifest.json, Task: Integration tests for adversarial scenarios, Task: Integration tests for DetectMimeType against corpus manifest, Task: Integration tests for Extract against corpus manifest, Task: Build synthetic sample generator scaffolding (+8 more)

### Community 29 - "Community 29"
Cohesion: 0.22
Nodes (7): ExtractorOLE.Tests.PowerPoint, ExtractorOLE.Helpers.FileTypeStrategy, ExtractorOLE.Tests.Docx, ExtractorOLE.Tests.Excel, ExtractorOLE.Helpers, ExtractorOLE.Tests.MultiEmbedding, WordOpenStrategy

### Community 30 - "Community 30"
Cohesion: 0.15
Nodes (11): ExtractionRequest entity, Activity Log, Description, Activity Log, Description, Activity Log, Description, Activity Log (+3 more)

### Community 31 - "Community 31"
Cohesion: 0.16
Nodes (11): DateTime, List, DocumentExtractionResult, EmbeddedFileItem, FileMetadata, HSSFWorkbook, OpenXmlPackage, OpenXmlPart (+3 more)

### Community 32 - "Community 32"
Cohesion: 0.16
Nodes (7): IOpenStrategy, IDictionary, ITextExtractor, FormatDispatchRegistry, Fact, FakeOpenStrategy, FormatDispatchRegistryTests

### Community 33 - "Community 33"
Cohesion: 0.23
Nodes (3): DateTime, Fact, XlsOpenStrategyTests

### Community 34 - "Community 34"
Cohesion: 0.15
Nodes (12): ADR-004: b2xtranslator decision for legacy .ppt parsing, b2xtranslator (.ppt to .pptx converter library), NPOI HSLFSlideShow (non-functional scratchpad entry point), LibreOffice headless conversion fallback, samples/curated/ directory, Activity Log, Description, 2026-08-23T04:48:29Z (UTC) (+4 more)

### Community 35 - "Community 35"
Cohesion: 0.13
Nodes (15): Part A - Structural extraction for code files, Part B - Semantic extraction (parallel subagents), Part C - Merge AST + semantic into final extraction, Step 0 - GitHub repos and multi-path merge (only if a URL or several paths), Step 1 - Ensure graphify is installed, Step 2.5 - Video and audio (only if video files detected), Step 2 - Detect files, Step 3 - Extract entities and relationships (+7 more)

### Community 36 - "Community 36"
Cohesion: 0.22
Nodes (13): Any, compute_true_status_by_task_id(), correct_manifest_statuses(), correct_task_md_frontmatter(), gh_pr_list(), main(), Resolve stale slugs in manifest['tasks'][*]['dependencies'] in place. Returns…, Correct manifest['tasks'][*]['status'] in place to match true state.… (+5 more)

### Community 37 - "Community 37"
Cohesion: 0.15
Nodes (13): YDK Fork Task-Dependency Bugs, 1. Ship three global plugins, not one bundle, 2. Add a `dotnet` stack to `stacks.py`, 3. Centralize the SDK-vs-muxer detection — this is the real gotcha, 4. Target-discovery edge cases actually hit (and the fix already applied), 5. `tdd-guard` is Python-only — known gap, not fixed here, 7. Whole-target build/test checks can permanently block ALL pushes on a pre-existing broken baseline, 8. Priority summary (+5 more)

### Community 38 - "Community 38"
Cohesion: 0.14
Nodes (14): b2xtranslator (original) — dead, but see DocSharp fork below, CFB/OLE2 readers found (beyond OpenMcdf), Citations/links, Commercial libraries excluded, Conclusion, DocSharp (manfromarce/DocSharp) — actively maintained fork of b2xtranslator, notable finding, IKVM + Apache POI (Java interop) — not a sane approach, confirmed, Kaitai Struct-generated CFB parser (curiosity, not a library) (+6 more)

### Community 39 - "Community 39"
Cohesion: 0.15
Nodes (11): ExcelFormatMetadata entity, Story S-0602557d (xls open component), Activity Log, Description, 2026-08-28T21:24:32Z (UTC), 2026-08-28T21:25:01Z (UTC), Activity Log, Description (+3 more)

### Community 40 - "Community 40"
Cohesion: 0.30
Nodes (5): Fact, ShapeTree, SlidePart, PowerPointOpenStrategyTests, ISet

### Community 41 - "Community 41"
Cohesion: 0.18
Nodes (12): Description, Release Field (Epic), Scope, Spec Refs Field (Epic), Acceptance Criteria, Epic Field (Story), Spec Refs Field (Story), PR Template (+4 more)

### Community 42 - "Community 42"
Cohesion: 0.16
Nodes (11): Story S-709fe2de (benchmarking), Task: Establish committed baseline and CI regression documentation, Task: Implement Extract() latency/allocation benchmarks per format, Task: Scaffold separate BenchmarkDotNet project, Extraction Latency NFR, Activity Log, Description, Activity Log (+3 more)

### Community 43 - "Community 43"
Cohesion: 0.26
Nodes (14): Task: Implement CFB directory-entry detection component, Task: Register CFB detection component in DI registry, Task: Add null request/field validation to DetectMimeType, Task: Wire DetectMimeType facade to OOXML + CFB registry, Task: Harden OOXML detection to be strictly content-only, Task: Classify macro-enabled OOXML variants as base format, Task: Handle zero-byte input as Unknown in OOXML detection, Task: Implement >=50 concurrent-call exercise of both public functions (+6 more)

### Community 44 - "Community 44"
Cohesion: 0.14
Nodes (13): Command Log (all invocations used, in order), DAG Validation, Dependency graph modeled, Epics created, Open Questions / Risks for Stage 03, Pass 1 — Full Backlog (Epics + Stories), Pass 2 — Sprint 1 Tasks (Epic 1 + Epic 2), Sanity checks after Pass 1 (+5 more)

### Community 45 - "Community 45"
Cohesion: 0.17
Nodes (12): Token reduction benchmark, FalkorDB export, GraphML export, MCP stdio server, Neo4j export, SVG export, Wiki export, Step 4.5: Graph health check (+4 more)

### Community 46 - "Community 46"
Cohesion: 0.18
Nodes (10): DetectMimeType, Epic E-3ffdc4d5: Benchmark/profiling, Epic E-c2544f52: Stress testing, Extract() public facade, Activity Log, Description, Activity Log, Description (+2 more)

### Community 47 - "Community 47"
Cohesion: 0.17
Nodes (12): DI-registered format-dispatch registry, Auth & Security, Overview, Problem Statement, Public API, Epic E-bd43bc02: Public facade wiring, Extract() contract, ExtractionRequest (+4 more)

### Community 48 - "Community 48"
Cohesion: 0.20
Nodes (11): BFS/DFS traversal modes, /graphify explain, For /graphify explain, For /graphify path, graphify reference: query, path, explain, /graphify path, save-result feedback loop, Step 0 — Constrained query expansion (REQUIRED before traversal) (+3 more)

### Community 49 - "Community 49"
Cohesion: 0.26
Nodes (10): Component Architecture, Detection Component, Open Component, Text-Extraction Component, dependency-injection requirement, format-extensibility requirement, Activity Log, Description (+2 more)

### Community 50 - "Community 50"
Cohesion: 0.18
Nodes (7): List, ExcelFormatMetadata, FormatMetadata, PowerPointFormatMetadata, WordFormatMetadata, WorkbookPart, PresentationPart

### Community 51 - "Community 51"
Cohesion: 0.17
Nodes (11): 1. File Manifest, 2.1 Enumerating embedded OLE objects and inline pictures, 2.2 Reading a single embedded object, 2.3 Serializing a container `DirectoryEntry` to an opaque byte blob (AC #2), 2.4 Logging convention for the omitted/corrupt item, 2. Production-code NPOI APIs (verified against `third_party/npoi` @ `2.7.6-rc1`), 3.1 Building a valid embedded object, 3.2 Building the "corrupt" second embedded object (+3 more)

### Community 52 - "Community 52"
Cohesion: 0.36
Nodes (4): compressedSize, localHeaderOffset, ZipEntryCorruptor, uint

### Community 53 - "Community 53"
Cohesion: 0.18
Nodes (9): Epic E-d4ac3268: Generalization and build config, ExtractionHelper, IOpenStrategy interface, MainExtractor, OfficeMimeTypeEnum, Activity Log, Description, Activity Log (+1 more)

### Community 54 - "Community 54"
Cohesion: 0.20
Nodes (8): FileMetadata entity, WordFormatMetadata entity, Activity Log, Description, Activity Log, Description, Activity Log, Description

### Community 55 - "Community 55"
Cohesion: 0.33
Nodes (4): Fact, MultiEmbeddingSampleTests, byte, MultiEmbeddingSampleSpecs

### Community 56 - "Community 56"
Cohesion: 0.25
Nodes (5): IExtractionHelper, WordprocessingDocument, WordOpenStrategy, Fact, DocxEmbeddedXlsxAndCorruptionTests

### Community 57 - "Community 57"
Cohesion: 0.25
Nodes (6): Cell, IEnumerable, List, WorkbookPart, ExcelExtractor, IdPartPair

### Community 58 - "Community 58"
Cohesion: 0.20
Nodes (10): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Usage (+2 more)

### Community 59 - "Community 59"
Cohesion: 0.20
Nodes (10): Alternatives Considered, API Shape For Our Use Case, BenchmarkDotNet Research Spike, Citations/Links, License, Maintenance Signal, Strengths, Structure As A Separate Project (+2 more)

### Community 60 - "Community 60"
Cohesion: 0.20
Nodes (10): 1. Port NPOI's own scratchpad HWPF/HSLF to SDK-style/net8.0 ourselves, 2. b2xtranslator -- binary-to-OOXML converter, 3. modeverv/dotnet-poi -- from-scratch Apache POI-equivalent port, 4. LibreOffice headless subprocess conversion, 5. Rejected without deep evaluation, Candidates evaluated, Confirmed starting facts (from T-d6de875f's submodule work), Important decoupling this spike surfaces (+2 more)

### Community 61 - "Community 61"
Cohesion: 0.20
Nodes (10): API surface relevant to our use case, Citations / links, Library name, License — IMPORTANT FINDING, Maintenance signal, NPOI — Research Spike, Performance signal, Strengths (+2 more)

### Community 62 - "Community 62"
Cohesion: 0.22
Nodes (7): password-protected error, Activity Log, Description, Activity Log, Description, Activity Log, Description

### Community 63 - "Community 63"
Cohesion: 0.27
Nodes (5): Fact, InlineData, Theory, MacroEmbedSampleGeneratorTests, Type

### Community 64 - "Community 64"
Cohesion: 0.20
Nodes (9): Command Log, Component Coverage, Cross-Epic/Cross-Sprint Dependency Edges Modeled, DAG Validation, Open Questions / Risks for Stage 03, Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19, Summary, Task Counts Per Story (+1 more)

### Community 65 - "Community 65"
Cohesion: 0.25
Nodes (8): graphify reference: extraction subagent prompt, /graphify command, Step 1: Ensure graphify installed, Step 2: Detect files, Step 3: Extract entities and relationships, Hyperedges rule, Node ID format rule (avoid ghost duplicates), Semantic similarity edge rule

### Community 66 - "Community 66"
Cohesion: 0.22
Nodes (9): API surface relevant to our use case, Citations/links, DocumentFormat.OpenXml — Research Spike, Library name, License, Maintenance signal (stars/last release/last commit), Strengths, Unicode/RTL/emoji findings (+1 more)

### Community 67 - "Community 67"
Cohesion: 0.22
Nodes (9): API surface relevant to our use case, Citations / links, Library name, License, Maintenance signal, OpenMcdf — Research Spike Notes, Strengths, Unicode / RTL / emoji findings (entry names) (+1 more)

### Community 69 - "Community 69"
Cohesion: 0.22
Nodes (7): .ydk/manifest.yaml, 2026-08-29T15:46:00Z (UTC), 2026-08-29T16:17:47Z (UTC), 2026-08-29T16:22:37Z (UTC), Verification Proof, 2026-08-22T21:06:43Z (UTC), Verification Proof

### Community 70 - "Community 70"
Cohesion: 0.25
Nodes (5): /graphify add <url>, --watch folder auto-rebuild, For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 71 - "Community 71"
Cohesion: 0.25
Nodes (6): graphify, Native CLAUDE.md integration (graphify claude install), For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration, graphify post-commit hook

### Community 72 - "Community 72"
Cohesion: 0.25
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 73 - "Community 73"
Cohesion: 0.25
Nodes (7): ADR-003 Decision: Manual Spec Review Substitution, ADR-003: Manual LLM spec review in place of `ydk spec verify`, Alternatives Considered, Consequences, Context, Decision, Status

### Community 74 - "Community 74"
Cohesion: 0.32
Nodes (5): ExtractorOLE.Exceptions, Exception, ExtractorOleException, FileTooLargeException, OversizedNestedContentException

### Community 75 - "Community 75"
Cohesion: 0.25
Nodes (8): Error Handling, Extensibility & DI, Extraction, Flat Text, Format-Specific Metadata, General Metadata, Purpose, Subfiles

### Community 76 - "Community 76"
Cohesion: 0.25
Nodes (4): IExtractionHelper, SlidePart, PowerPointOpenStrategy, ZipFallbackOpenStrategy

### Community 77 - "Community 77"
Cohesion: 0.29
Nodes (4): HSSFWorkbook, IExtractionHelper, string, XlsOpenStrategy

### Community 78 - "Community 78"
Cohesion: 0.50
Nodes (3): Fact, MainExtractor, MainExtractorDispatchIntegrationTests

### Community 79 - "Community 79"
Cohesion: 0.25
Nodes (8): Dependencies Field, Story Reference Field, AI Provider Configuration, Execution Configuration, Pre-push Hooks Configuration, Task Management Configuration, YDK Task Registry (Epics/Stories/Tasks), YDK Todos Store

### Community 80 - "Community 80"
Cohesion: 0.32
Nodes (8): Contract Schema, Crosscut Schema, Entity Schema, Error Schema, Hook Schema, Page Schema, Req Schema, Route Schema

### Community 81 - "Community 81"
Cohesion: 0.29
Nodes (5): .ydk/batch-mapping.json, 2026-08-23T18:41:32Z (UTC), Activity Log, Description, Verification Proof

### Community 82 - "Community 82"
Cohesion: 0.33
Nodes (6): Code exploration: use graphify, not raw file reads, graphify, Use graphify instead of raw reads, Project Conventions — ole-extractor, Fast path: query existing graph, YDK workflow

### Community 83 - "Community 83"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Doc, WordprocessingDocument, DocExtractor

### Community 84 - "Community 84"
Cohesion: 0.29
Nodes (4): SampleGenerator, SampleGenerator/Fixtures/CuratedSampleSpecs.cs, IServiceCollection, ServiceRegistration

### Community 85 - "Community 85"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Util, OpenXmlPart, ExtractionUtil

### Community 86 - "Community 86"
Cohesion: 0.29
Nodes (4): docs/adrs directory, graphify-out/graph.json, third_party/npoi submodule, QD-94a6d3: Sync stale status for QD-c02a7c and QD-f9203a

### Community 87 - "Community 87"
Cohesion: 0.29
Nodes (7): ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection), Alternatives Considered, Consequences, Context, Decision, Note (2026-08-23), Status

### Community 88 - "Community 88"
Cohesion: 0.29
Nodes (5): Epic E-a2efe648: NPOI/OpenMcdf library validation and wiring, Activity Log, Description, Activity Log, Description

### Community 90 - "Community 90"
Cohesion: 0.29
Nodes (7): Build & test, Development workflow, Documentation, Library stack, ole-extractor, Project structure, Supported formats

### Community 91 - "Community 91"
Cohesion: 0.38
Nodes (3): byte, string, DocxEmbeddedXlsxSampleSpecs

### Community 92 - "Community 92"
Cohesion: 0.38
Nodes (6): _find_build_targets(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-build verification check.

### Community 93 - "Community 93"
Cohesion: 0.38
Nodes (6): _find_format_targets(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-format verification check.

### Community 94 - "Community 94"
Cohesion: 0.38
Nodes (6): _find_test_target(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Prefer a .sln (dotnet test filters to test projects automatically); else the…, Run the dotnet-test verification check.

### Community 95 - "Community 95"
Cohesion: 0.33
Nodes (5): 2026-08-22 — T-eeb6684b: Downgrade ExtractorOLE.csproj to net8.0, Activity Log, PR #4, Task T-eeb6684b: downgrade to net8.0, YDK Development Workflow

### Community 96 - "Community 96"
Cohesion: 0.47
Nodes (5): graphify reference: GitHub clone and cross-repo merge, Step 0 - Clone GitHub repo(s) (only if a GitHub URL was given), graphify clone, graphify merge-graphs, Multi-subfolder/monorepo merge flow

### Community 97 - "Community 97"
Cohesion: 0.33
Nodes (6): ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI), Alternatives Considered, Consequences, Context, Decision, Status

### Community 98 - "Community 98"
Cohesion: 0.33
Nodes (6): Brownfield Constraints, Conventions, Known Gotchas, Library Stack (locked), Product Context, Project Rules

### Community 99 - "Community 99"
Cohesion: 0.33
Nodes (4): IReadOnlyList, IMimeDetectionRegistry, IReadOnlyList, MimeDetectionRegistry

### Community 103 - "Community 103"
Cohesion: 0.40
Nodes (5): Cross-cutting note, Library Tradeoffs — Office File MIME Detection & Extraction, Table 1: OOXML parsing/metadata/text/embeddings (docx/xlsx/pptx), Table 2: Legacy OLE (doc/xls/ppt) parsing/metadata/text, Table 3: Raw CFB introspection for mime-sniffing and generic subfile walking

### Community 104 - "Community 104"
Cohesion: 0.40
Nodes (5): Corpus Composition, Corpus Manifest, Dataset Curation, Provenance & Licensing, Purpose

### Community 105 - "Community 105"
Cohesion: 0.40
Nodes (4): .env, .gitignore, 2026-08-29T16:58:46Z (UTC), Verification Proof

### Community 106 - "Community 106"
Cohesion: 0.40
Nodes (4): PowerPointFormatMetadata entity, PowerPointOpenStrategy, Activity Log, Description

### Community 107 - "Community 107"
Cohesion: 0.40
Nodes (4): Acceptance Criteria, Description, Test Strategy, Spec Check Configuration

### Community 108 - "Community 108"
Cohesion: 0.50
Nodes (4): main(), Subprocess-invoke sibling plugin ``name``'s check.py, returning its result., Run the dotnet-quality verification check., _run_sub_check()

### Community 110 - "Community 110"
Cohesion: 0.50
Nodes (3): main/NPOI.Core.csproj, NPOI.Core.sln, NU1903 vulnerability (System.Security.Cryptography.Xml 8.0.2)

### Community 111 - "Community 111"
Cohesion: 0.50
Nodes (4): Epic: OOXML Extraction Completeness, Story: docx extraction completeness, Story: pptx extraction completeness, Story: xlsx extraction completeness

### Community 112 - "Community 112"
Cohesion: 0.50
Nodes (4): Epic: Legacy OLE Extraction, Story: xls extraction (Excel legacy), Story: ppt extraction (PowerPoint legacy), Story: doc extraction (Word legacy)

### Community 113 - "Community 113"
Cohesion: 0.50
Nodes (4): Epic: Dataset Curation, Story: Synthetic sample generator (3 per format), Story: Shared adversarial set and corpus manifest, Story: Curated real-world samples (2 per format)

### Community 114 - "Community 114"
Cohesion: 0.50
Nodes (4): Epic: Project Restructure, Story: Generalize embedded-subfile walk and wire DI registry for format dispatch, Story: Downgrade to net8.0 and fix enum typo, Story: Wire Program.cs to invoke both public functions on a real file

### Community 115 - "Community 115"
Cohesion: 0.50
Nodes (4): 2026-08-28T21:35:18Z (UTC), 2026-08-28T21:36:52Z (UTC), 2026-08-29 - Sample already existed; added the missing test, Activity Log

### Community 116 - "Community 116"
Cohesion: 0.50
Nodes (4): 2026-08-24 - Verification, side-effect cleanup, and completion, 2026-08-24T20:56:24Z (UTC), 2026-08-24T20:57:53Z (UTC), Activity Log

### Community 131 - "Community 131"
Cohesion: 0.67
Nodes (3): Epic: Test Suite, Story: Unit tests for unit-test-backed scenarios, Story: Integration tests against the corpus manifest

### Community 132 - "Community 132"
Cohesion: 0.67
Nodes (3): Epic: Library Setup & Validation, Story: Empirically validate NPOI i18n fidelity on the legacy OLE path, Story: Wire self-compiled NPOI and OpenMcdf into the build

### Community 133 - "Community 133"
Cohesion: 0.67
Nodes (3): Epic: Public API Surface, Story: Wire the DetectMimeType public facade to the registry, Story: Wire the Extract public facade with full error handling

## Ambiguous Edges - Review These
- `YDK Task Registry (Epics/Stories/Tasks)` → `YDK Todos Store`  [AMBIGUOUS]
  .ydk/todos.yaml · relation: conceptually_related_to

## Knowledge Gaps
- **607 isolated node(s):** `BackupLogic`, `ExtractorOLE.Tests.Helpers`, `ExtractorOLE.Benchmarks`, `2026-08-28T21:35:18Z (UTC)`, `2026-08-28T21:36:52Z (UTC)` (+602 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **53 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `YDK Task Registry (Epics/Stories/Tasks)` and `YDK Todos Store`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `ExtractorOLE.DTOs` connect `YDK Tooling & Build Verification` to `Community 32`, `DOCX Text Extraction Tests`, `Sample Dataset Curation`, `Format Metadata Entities`, `Community 102`, `Community 76`, `Community 77`, `Community 50`, `Community 83`, `Community 20`, `Community 85`, `Community 118`, `Community 56`, `Community 29`, `Community 31`?**
  _High betweenness centrality (0.111) - this node is a cross-community bridge._
- **Why does `DocumentFormat.OpenXml extraction path` connect `Unicode Fidelity & Extraction Result` to `Community 34`, `Format Metadata Entities`, `ExtractorOLE Solution Structure`, `YDK Tooling & Build Verification`, `Community 29`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._
- **Why does `OpenMcdf` connect `YDK Tooling & Build Verification` to `Community 88`, `Community 25`, `Community 26`, `Community 24`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **What connects `BackupLogic`, `ExtractorOLE.Tests.Helpers`, `ExtractorOLE.Benchmarks` to the rest of the system?**
  _607 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `MIME Detection Contracts` be split into smaller, more focused modules?**
  _Cohesion score 0.05555555555555555 - nodes in this community are weakly interconnected._
- **Should `Sample Generation & Embedding` be split into smaller, more focused modules?**
  _Cohesion score 0.056051587301587304 - nodes in this community are weakly interconnected._