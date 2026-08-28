# Graph Report - ole-extractor  (2026-08-24)

## Corpus Check
- 223 files · ~103,794 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1004 nodes · 1088 edges · 178 communities (62 shown, 116 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 20 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `19afd64e`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- DocumentExtractionResult
- .Detect
- OfficeMimeTypeEnum
- Per-Criterion Detail
- ExtractorOLE.Handlers
- ExcelExtractor
- ExtractorOLE
- DocExtractor
- ExtractionUtil
- NPOI — Research Spike
- BackupLogic.cs
- What You Must Do When Invoked
- BenchmarkDotNet Research Spike
- ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)
- SampleSpec
- IOpenStrategy
- SampleGenerator.Abstractions
- Program
- remediate_task_status.py
- Stage 02 Planning Report — 2026-08-19
- SampleGenerator.Fixtures
- PptxSampleGenerator
- Candidates evaluated
- .Generate
- Upstream recommendation: .NET/C# support in YDK
- MacroEmbedSampleGeneratorTests
- Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19
- graphify reference: extra exports and benchmark
- DocumentFormat.OpenXml — Research Spike
- Extraction
- Overview
- .Build
- .AddEmbeddings
- ADR-003: Manual LLM spec review in place of `ydk spec verify`
- ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)
- MIME Detection
- main
- main
- main
- graphify reference: query, path, explain
- Dataset Curation
- Project Conventions — ole-extractor
- ExcelExtractorEmbeddedObjectTests
- T-6ac3f215.md
- T-bac988f5.md
- T-ce2e3cc1.md
- T-d657bb2a.md
- Activity Log
- T-d91b2a46.md
- graphify reference: add a URL and watch a folder
- graphify reference: commit hook and native CLAUDE.md integration
- graphify reference: incremental update and cluster-only
- Testing Strategy
- task.md
- PULL_REQUEST_TEMPLATE.md
- T-517f89c5.md
- T-c4123b53.md
- Activity Log
- graphify reference: GitHub clone and cross-repo merge
- graphify reference: transcribe video and audio
- epic.md
- ServiceRegistration
- manifest.json
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
- S-0602557d.md
- S-072bfdf6.md
- S-156ec71a.md
- S-23b8b734.md
- S-32677db6.md
- S-37c18545.md
- S-3bc6b929.md
- S-418e5f1b.md
- S-41ebb945.md
- S-43c41d4a.md
- S-51d277f1.md
- S-6bc8566f.md
- S-709fe2de.md
- S-7d040ba7.md
- S-814fa928.md
- S-8d6146f0.md
- S-d01346bc.md
- S-d88f514b.md
- S-da936361.md
- S-e496f356.md
- S-f6d7124a.md
- S-fe24ecff.md
- QD-8c1895.md
- QD-a380e4.md
- T-0055ec7c.md
- T-02637847.md
- T-05bd14c7.md
- T-07188baf.md
- T-0834290d.md
- T-09102f84.md
- T-0a81d133.md
- T-0c83ff28.md
- T-113fe4b2.md
- T-12dfd045.md
- T-1653b76b.md
- T-1df33d6c.md
- T-29fbb7cf.md
- T-2ac2f140.md
- T-2c3440d2.md
- T-2e13ee4b.md
- T-30e939fe.md
- T-30f2c1b8.md
- T-3335d397.md
- T-33a58c59.md
- T-3b4e70f4.md
- T-3bddbe75.md
- T-3f3dab44.md
- T-40ac397c.md
- T-40fc15b4.md
- T-44812a03.md
- T-47fea79d.md
- T-4b624fef.md
- T-505f5b14.md
- T-5e489191.md
- T-5e9dbd18.md
- T-61477563.md
- T-6f033869.md
- T-71ab3bc0.md
- T-7916a198.md
- T-7cdaeceb.md
- T-83b4a029.md
- T-90924b6d.md
- T-94570c2b.md
- T-96b5181b.md
- T-9a4ff765.md
- T-9e6bfce8.md
- T-9fac8963.md
- T-a2b91dc6.md
- T-a5f1d074.md
- T-a77b2992.md
- T-a8b6b4ec.md
- T-b62ffe04.md
- T-b792f8e5.md
- T-c2a60f06.md
- T-c3f1ec05.md
- T-c573e72e.md
- T-d7c81051.md
- T-e0a5bfec.md
- T-eeb6684b.md
- T-ef11e30d.md
- T-ef73861f.md
- T-f4ed5c8f.md
- T-f653c5fb.md
- T-f667e540.md
- T-f96fa73d.md
- T-fb06300e.md
- T-fb89c6f3.md
- T-fd49ef39.md
- .claude/CLAUDE.md
- check-task-complete.sh
- extraction-spec.md
- glossary.md
- story.md
- commit-msg
- pre-commit
- pre-push
- ExtractorOLE.DTOs
- MainExtractor
- OpenMcdf — Research Spike Notes
- ExtractionBenchmarks
- SlidesExtractor.cs
- MetadataFields.cs

## God Nodes (most connected - your core abstractions)
1. `ExtractorOLE.DTOs` - 22 edges
2. `DocumentExtractionResult` - 22 edges
3. `SampleGenerator.Abstractions` - 20 edges
4. `SampleSpec` - 14 edges
5. `ExtractorOLE` - 12 edges
6. `SampleGenerator.Generators` - 12 edges
7. `What You Must Do When Invoked` - 12 edges
8. `Per-Criterion Detail` - 11 edges
9. `OfficeMimeTypeEnum` - 10 edges
10. `ExtractionHelper` - 10 edges

## Surprising Connections (you probably didn't know these)
- `MacroEmbedSampleGeneratorTests` --references--> `SampleSpec`  [EXTRACTED]
  ExtractorOle/ExtractorOLE.Tests/SampleGeneration/MacroEmbedSampleGeneratorTests.cs → SampleGenerator/Abstractions/SampleSpec.cs
- `ExtractionBenchmarks` --references--> `MainExtractor`  [EXTRACTED]
  ExtractorOle/ExtractorOLE.Benchmarks/ExtractionBenchmarks.cs → ExtractorOle/ExtractorOLE/MainExtractor.cs
- `MainExtractor` --references--> `IExtractionHelper`  [EXTRACTED]
  ExtractorOle/ExtractorOLE/MainExtractor.cs → ExtractorOle/ExtractorOLE/Helpers/IExtractionHelper.cs
- `SampleSpec` --references--> `EmbeddedContentSpec`  [EXTRACTED]
  SampleGenerator/Abstractions/SampleSpec.cs → SampleGenerator/Abstractions/EmbeddedContentSpec.cs
- `DocxSampleGenerator` --implements--> `ISampleGenerator`  [EXTRACTED]
  SampleGenerator/Generators/DocxSampleGenerator.cs → SampleGenerator/Abstractions/ISampleGenerator.cs

## Import Cycles
- None detected.

## Communities (178 total, 116 thin omitted)

### Community 0 - "DocumentExtractionResult"
Cohesion: 0.24
Nodes (8): DateTime, List, DocumentExtractionResult, EmbeddedFileItem, FileMetadata, List, OpenXmlPackage, OpenXmlPart

### Community 1 - ".Detect"
Cohesion: 0.16
Nodes (10): DetectedFormatEnum, MimeDetectionRequest, MimeDetectionResult, string, CfbMimeDetector, ICfbMimeDetector, Fact, InlineData (+2 more)

### Community 2 - "OfficeMimeTypeEnum"
Cohesion: 0.19
Nodes (4): OfficeMimeTypeEnum, ExtractionHelper, OpenXmlPart, IExtractionHelper

### Community 3 - "Per-Criterion Detail"
Cohesion: 0.06
Nodes (32): 1. Placeholder scan — **PASS**, 2. Internal consistency — **C1–C3 resolved (re-run 2); C4, C5, C6 resolved (re-run 3); C5a resolved (re-run 4); one trivial residual, C5b**, 3. Scope — **PASS**, 4. Ambiguity — **PASS** (all 4 prior findings fixed; 4 minor residuals, none critical), 5. YAGNI — **PASS**, 6. Component reference check — **PASS**, Considered and not counted, Deterministic Scan Results (+24 more)

### Community 4 - "ExtractorOLE.Handlers"
Cohesion: 0.22
Nodes (5): ExtractorOLE.Handlers, DocxTextExtractor, PptxTextExtractor, XlsxTextExtractor, ITextExtractor

### Community 5 - "ExcelExtractor"
Cohesion: 0.21
Nodes (7): Cell, ExtractorOLE.Old.Excel, IEnumerable, List, ExcelExtractor, IdPartPair, WorkbookPart

### Community 6 - "ExtractorOLE"
Cohesion: 0.07
Nodes (27): ExtractorOLE.Benchmarks, net8.0, Microsoft.NET.Sdk, ExtractorOLE, net8.0, DocumentFormat.OpenXml (3.5.1), DocumentFormat.OpenXml.Features (3.5.1), DocumentFormat.OpenXml.Framework (3.5.1) (+19 more)

### Community 7 - "DocExtractor"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Doc, DocExtractor, WordprocessingDocument

### Community 8 - "ExtractionUtil"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Util, OpenXmlPart, ExtractionUtil

### Community 9 - "NPOI — Research Spike"
Cohesion: 0.06
Nodes (29): b2xtranslator (original) — dead, but see DocSharp fork below, CFB/OLE2 readers found (beyond OpenMcdf), Citations/links, Commercial libraries excluded, Conclusion, DocSharp (manfromarce/DocSharp) — actively maintained fork of b2xtranslator, notable finding, IKVM + Apache POI (Java interop) — not a sane approach, confirmed, Kaitai Struct-generated CFB parser (curiosity, not a library) (+21 more)

### Community 11 - "What You Must Do When Invoked"
Cohesion: 0.08
Nodes (24): For /graphify add and --watch, For /graphify query, For the commit hook and native CLAUDE.md integration, For --update and --cluster-only, /graphify, Honesty Rules, Interpreter guard for subcommands, Part A - Structural extraction for code files (+16 more)

### Community 12 - "BenchmarkDotNet Research Spike"
Cohesion: 0.09
Nodes (20): Alternatives Considered, API Shape For Our Use Case, BenchmarkDotNet Research Spike, Citations/Links, License, Maintenance Signal, Strengths, Structure As A Separate Project (+12 more)

### Community 13 - "ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection)"
Cohesion: 0.09
Nodes (19): ADR-001: Extraction library stack (OOXML, legacy OLE, CFB introspection), Alternatives Considered, Consequences, Context, Decision, Note (2026-08-23), Status, ADR-002: Time profiling and stress testing approach (+11 more)

### Community 14 - "SampleSpec"
Cohesion: 0.15
Nodes (9): IList, GeneratedSample, ISampleGenerator, SampleFormat, IDictionary, SampleSpec, DocSampleGenerator, PptSampleGenerator (+1 more)

### Community 15 - "IOpenStrategy"
Cohesion: 0.15
Nodes (9): IExtractionHelper, ExcelOpenStrategy, IOpenStrategy, IExtractionHelper, PowerPointOpenStrategy, IExtractionHelper, WordOpenStrategy, ZipFallbackOpenStrategy (+1 more)

### Community 16 - "SampleGenerator.Abstractions"
Cohesion: 0.22
Nodes (6): SampleGenerator.Abstractions, SampleGenerator, SampleGenerator.Generators, ExtractorOLE.Tests.Excel, ExtractorOLE.Tests.SampleGeneration, OpenXmlPackagePropertiesHelper

### Community 17 - "Program"
Cohesion: 0.15
Nodes (9): ExtractorOLE.StressHarness, Program, IServiceCollection, ServiceRegistration, List, Program, int, Task (+1 more)

### Community 18 - "remediate_task_status.py"
Cohesion: 0.22
Nodes (13): Any, compute_true_status_by_task_id(), correct_manifest_statuses(), correct_task_md_frontmatter(), gh_pr_list(), main(), Resolve stale slugs in manifest['tasks'][*]['dependencies'] in place. Returns…, Correct manifest['tasks'][*]['status'] in place to match true state.… (+5 more)

### Community 19 - "Stage 02 Planning Report — 2026-08-19"
Cohesion: 0.14
Nodes (13): Command Log (all invocations used, in order), DAG Validation, Dependency graph modeled, Epics created, Open Questions / Risks for Stage 03, Pass 1 — Full Backlog (Epics + Stories), Pass 2 — Sprint 1 Tasks (Epic 1 + Epic 2), Sanity checks after Pass 1 (+5 more)

### Community 20 - "SampleGenerator.Fixtures"
Cohesion: 0.18
Nodes (6): SampleGenerator.Fixtures, IReadOnlyList, byte, MacroEmbedSampleSpecs, string, MultilingualFixtures

### Community 21 - "PptxSampleGenerator"
Cohesion: 0.31
Nodes (7): PresentationPart, PptxSampleGenerator, ShapeTree, SlideLayoutPart, SlideMasterPart, SlidePart, ThemePart

### Community 22 - "Candidates evaluated"
Cohesion: 0.18
Nodes (10): 1. Port NPOI's own scratchpad HWPF/HSLF to SDK-style/net8.0 ourselves, 2. b2xtranslator -- binary-to-OOXML converter, 3. modeverv/dotnet-poi -- from-scratch Apache POI-equivalent port, 4. LibreOffice headless subprocess conversion, 5. Rejected without deep evaluation, Candidates evaluated, Confirmed starting facts (from T-d6de875f's submodule work), Important decoupling this spike surfaces (+2 more)

### Community 23 - ".Generate"
Cohesion: 0.24
Nodes (6): IPackageProperties, IEnumerable, DocxSampleGenerator, IDictionary, XlsxSampleGenerator, SharedStringTable

### Community 24 - "Upstream recommendation: .NET/C# support in YDK"
Cohesion: 0.20
Nodes (9): 1. Ship three global plugins, not one bundle, 2. Add a `dotnet` stack to `stacks.py`, 3. Centralize the SDK-vs-muxer detection — this is the real gotcha, 4. Target-discovery edge cases actually hit (and the fix already applied), 5. `tdd-guard` is Python-only — known gap, not fixed here, 7. Whole-target build/test checks can permanently block ALL pushes on a pre-existing broken baseline, 8. Priority summary, Source material (+1 more)

### Community 25 - "MacroEmbedSampleGeneratorTests"
Cohesion: 0.27
Nodes (5): Fact, InlineData, Theory, MacroEmbedSampleGeneratorTests, Type

### Community 26 - "Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19"
Cohesion: 0.20
Nodes (9): Command Log, Component Coverage, Cross-Epic/Cross-Sprint Dependency Edges Modeled, DAG Validation, Open Questions / Risks for Stage 03, Stage 02 Planning Report — Sprint 2 & Sprint 3 — 2026-08-19, Summary, Task Counts Per Story (+1 more)

### Community 27 - "graphify reference: extra exports and benchmark"
Cohesion: 0.22
Nodes (8): graphify reference: extra exports and benchmark, Step 6b - Wiki (only if --wiki flag), Step 7 - Neo4j export (only if --neo4j or --neo4j-push flag), Step 7a - FalkorDB export (only if --falkordb or --falkordb-push flag), Step 7b - SVG export (only if --svg flag), Step 7c - GraphML export (only if --graphml flag), Step 7d - MCP server (only if --mcp flag), Step 8 - Token reduction benchmark (only if total_words > 5000)

### Community 28 - "DocumentFormat.OpenXml — Research Spike"
Cohesion: 0.22
Nodes (9): API surface relevant to our use case, Citations/links, DocumentFormat.OpenXml — Research Spike, Library name, License, Maintenance signal (stars/last release/last commit), Strengths, Unicode/RTL/emoji findings (+1 more)

### Community 29 - "Extraction"
Cohesion: 0.22
Nodes (8): Error Handling, Extensibility & DI, Extraction, Flat Text, Format-Specific Metadata, General Metadata, Purpose, Subfiles

### Community 30 - "Overview"
Cohesion: 0.22
Nodes (8): Adversarial Review, Auth & Security, Component Architecture, Overview, Problem Statement, Public API, Scope, Success Criteria

### Community 31 - ".Build"
Cohesion: 0.39
Nodes (4): Fact, MultiEmbeddingSampleTests, byte, MultiEmbeddingSampleSpecs

### Community 32 - ".AddEmbeddings"
Cohesion: 0.36
Nodes (4): EmbeddedContentSpec, IEnumerable, OpenXmlPart, OpenXmlEmbeddingHelper

### Community 33 - "ADR-003: Manual LLM spec review in place of `ydk spec verify`"
Cohesion: 0.29
Nodes (6): ADR-003: Manual LLM spec review in place of `ydk spec verify`, Alternatives Considered, Consequences, Context, Decision, Status

### Community 34 - "ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI)"
Cohesion: 0.29
Nodes (6): ADR-004: Legacy .doc/.ppt body-text parsing (b2xtranslator, not NPOI), Alternatives Considered, Consequences, Context, Decision, Status

### Community 35 - "MIME Detection"
Cohesion: 0.29
Nodes (6): Extensibility, Guardrails, How Detection Works, Macro-Enabled Variants, MIME Detection, Purpose

### Community 36 - "main"
Cohesion: 0.38
Nodes (6): _find_build_targets(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-build verification check.

### Community 37 - "main"
Cohesion: 0.38
Nodes (6): _find_format_targets(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Find a .sln first, else every real (non-test/bench/harness) .csproj.…, Run the dotnet-format verification check.

### Community 38 - "main"
Cohesion: 0.38
Nodes (6): _find_test_target(), _has_sdk(), main(), Check that an actual .NET SDK is installed, not just the dotnet muxer. On…, Prefer a .sln (dotnet test filters to test projects automatically); else the…, Run the dotnet-test verification check.

### Community 39 - "graphify reference: query, path, explain"
Cohesion: 0.33
Nodes (5): For /graphify explain, For /graphify path, graphify reference: query, path, explain, Step 0 — Constrained query expansion (REQUIRED before traversal), Step 1 — Traversal

### Community 40 - "Dataset Curation"
Cohesion: 0.33
Nodes (5): Corpus Composition, Corpus Manifest, Dataset Curation, Provenance & Licensing, Purpose

### Community 41 - "Project Conventions — ole-extractor"
Cohesion: 0.40
Nodes (4): Code exploration: use graphify, not raw file reads, graphify, Project Conventions — ole-extractor, YDK workflow

### Community 43 - "T-6ac3f215.md"
Cohesion: 0.40
Nodes (4): 2026-08-23T18:41:32Z (UTC), Activity Log, Description, Verification Proof

### Community 44 - "T-bac988f5.md"
Cohesion: 0.40
Nodes (4): 2026-08-24T10:28:45Z (UTC), Activity Log, Description, Verification Proof

### Community 45 - "T-ce2e3cc1.md"
Cohesion: 0.40
Nodes (4): 2026-08-22T21:20:01Z (UTC), Activity Log, Description, Verification Proof

### Community 46 - "T-d657bb2a.md"
Cohesion: 0.40
Nodes (4): 2026-08-23T18:47:51Z (UTC), Activity Log, Description, Verification Proof

### Community 47 - "Activity Log"
Cohesion: 0.40
Nodes (4): 2026-08-23T04:52:05Z (UTC), 2026-08-23T04:52:36Z (UTC), Activity Log, Description

### Community 48 - "T-d91b2a46.md"
Cohesion: 0.40
Nodes (4): 2026-08-23T18:36:50Z (UTC), Activity Log, Description, Verification Proof

### Community 49 - "graphify reference: add a URL and watch a folder"
Cohesion: 0.50
Nodes (3): For /graphify add, For --watch, graphify reference: add a URL and watch a folder

### Community 50 - "graphify reference: commit hook and native CLAUDE.md integration"
Cohesion: 0.50
Nodes (3): For git commit hook, For native CLAUDE.md integration, graphify reference: commit hook and native CLAUDE.md integration

### Community 51 - "graphify reference: incremental update and cluster-only"
Cohesion: 0.50
Nodes (3): For --cluster-only, For --update (incremental re-extraction), graphify reference: incremental update and cluster-only

### Community 52 - "Testing Strategy"
Cohesion: 0.50
Nodes (3): Rules, Test Types, Testing Strategy

### Community 53 - "task.md"
Cohesion: 0.50
Nodes (3): Acceptance Criteria, Description, Test Strategy

### Community 54 - "PULL_REQUEST_TEMPLATE.md"
Cohesion: 0.50
Nodes (3): Changes, Spec refs, Verification Proof

### Community 55 - "T-517f89c5.md"
Cohesion: 0.50
Nodes (3): 2026-08-23T04:48:29Z (UTC), Activity Log, Description

### Community 56 - "T-c4123b53.md"
Cohesion: 0.50
Nodes (3): 2026-08-23T04:48:27Z (UTC), Activity Log, Description

### Community 123 - "T-44812a03.md"
Cohesion: 0.29
Nodes (6): 2026-08-24T15:22:16Z (UTC), 2026-08-24T15:23:29Z (UTC), Activity Log, Description, Verification Proof, Verification Proof

### Community 172 - "ExtractorOLE.DTOs"
Cohesion: 0.17
Nodes (8): ExtractorOLE, ExtractorOLE.DTOs, ExtractorOLE.Helpers.FileTypeStrategy, ExtractorOLE.Helpers.MimeDetection, ExtractorOLE.Tests.MimeDetection, ExtractorOLE.Benchmarks, ExtractorOLE.Helpers, ExtractorOLE.Tests.MultiEmbedding

### Community 173 - "MainExtractor"
Cohesion: 0.28
Nodes (4): Dictionary, ITextExtractor, IDictionary, MainExtractor

### Community 174 - "OpenMcdf — Research Spike Notes"
Cohesion: 0.22
Nodes (9): API surface relevant to our use case, Citations / links, Library name, License, Maintenance signal, OpenMcdf — Research Spike Notes, Strengths, Unicode / RTL / emoji findings (entry names) (+1 more)

### Community 175 - "ExtractionBenchmarks"
Cohesion: 0.33
Nodes (4): Benchmark, byte, ExtractionBenchmarks, GlobalSetup

## Knowledge Gaps
- **483 isolated node(s):** `check-task-complete.sh script`, `ExtractorOLE.Benchmarks`, `net8.0`, `BenchmarkDotNet (0.14.0)`, `Microsoft.NET.Sdk` (+478 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **116 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `ExtractorOLE.DTOs` connect `ExtractorOLE.DTOs` to `DocumentExtractionResult`, `.Detect`, `OfficeMimeTypeEnum`, `ExcelExtractor`, `DocExtractor`, `ExtractionUtil`, `IOpenStrategy`, `SlidesExtractor.cs`?**
  _High betweenness centrality (0.036) - this node is a cross-community bridge._
- **Why does `DocumentExtractionResult` connect `DocumentExtractionResult` to `OfficeMimeTypeEnum`, `ExcelExtractor`, `DocExtractor`, `ExtractionUtil`, `IOpenStrategy`, `ExtractionBenchmarks`, `SlidesExtractor.cs`?**
  _High betweenness centrality (0.015) - this node is a cross-community bridge._
- **Why does `SampleGenerator.Abstractions` connect `SampleGenerator.Abstractions` to `.AddEmbeddings`, `MetadataFields.cs`, `SampleGenerator.Fixtures`, `SampleSpec`?**
  _High betweenness centrality (0.015) - this node is a cross-community bridge._
- **What connects `check-task-complete.sh script`, `ExtractorOLE.Benchmarks`, `net8.0` to the rest of the system?**
  _483 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Per-Criterion Detail` be split into smaller, more focused modules?**
  _Cohesion score 0.06060606060606061 - nodes in this community are weakly interconnected._
- **Should `ExtractorOLE` be split into smaller, more focused modules?**
  _Cohesion score 0.07096774193548387 - nodes in this community are weakly interconnected._
- **Should `NPOI — Research Spike` be split into smaller, more focused modules?**
  _Cohesion score 0.0625 - nodes in this community are weakly interconnected._