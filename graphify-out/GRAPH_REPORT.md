# Graph Report - ExtractorOle  (2026-08-18)

## Corpus Check
- Corpus is ~7,536 words - fits in a single context window. You may not need a graph.

## Summary
- 127 nodes · 202 edges · 14 communities (12 shown, 2 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 10 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- DTO <-> Strategy Cross-Refs
- DI Wiring & Entry Point
- MainExtractor & Mime Contract
- ExtractionHelper Implementation
- Text Extractor Handlers
- Old Excel Extractor (dead code)
- Project/Build Metadata
- Old Doc Extractor (dead code)
- Old Extraction Util (dead code)
- Old PowerPoint Extractor (dead code)
- Old Backup Logic (dead code)

## God Nodes (most connected - your core abstractions)
1. `DocumentExtractionResult` - 20 edges
2. `ExtractorOLE.DTOs` - 15 edges
3. `OfficeMimeTypeEnum` - 10 edges
4. `ExtractionHelper` - 10 edges
5. `IExtractionHelper` - 10 edges
6. `MainExtractor` - 9 edges
7. `ExtractorOLE` - 7 edges
8. `ExtractorOLE.Helpers.FileTypeStrategy` - 7 edges
9. `IOpenStrategy` - 7 edges
10. `ZipFallbackOpenStrategy` - 6 edges

## Surprising Connections (you probably didn't know these)
- `ExtractionHelper` --implements--> `IExtractionHelper`  [EXTRACTED]
  ExtractorOLE/Helpers/ExtractionHelper.cs → ExtractorOLE/Helpers/IExtractionHelper.cs
- `ExcelOpenStrategy` --implements--> `IOpenStrategy`  [EXTRACTED]
  ExtractorOLE/Helpers/FileTypeStrategy/ExcelOpenStrategy.cs → ExtractorOLE/Helpers/FileTypeStrategy/IOpenStrategy.cs
- `ZipFallbackOpenStrategy` --references--> `ExcelOpenStrategy`  [EXTRACTED]
  ExtractorOLE/Helpers/FileTypeStrategy/ZipFallbackOpenStrategy.cs → ExtractorOLE/Helpers/FileTypeStrategy/ExcelOpenStrategy.cs
- `PowerPointOpenStrategy` --implements--> `IOpenStrategy`  [EXTRACTED]
  ExtractorOLE/Helpers/FileTypeStrategy/PowerPointOpenStrategy.cs → ExtractorOLE/Helpers/FileTypeStrategy/IOpenStrategy.cs
- `WordOpenStrategy` --implements--> `IOpenStrategy`  [EXTRACTED]
  ExtractorOLE/Helpers/FileTypeStrategy/WordOpenStrategy.cs → ExtractorOLE/Helpers/FileTypeStrategy/IOpenStrategy.cs

## Import Cycles
- None detected.

## Communities (14 total, 2 thin omitted)

### Community 0 - "DTO <-> Strategy Cross-Refs"
Cohesion: 0.15
Nodes (15): DateTime, List, DocumentExtractionResult, EmbeddedFileItem, FileMetadata, IExtractionHelper, ExcelOpenStrategy, IOpenStrategy (+7 more)

### Community 1 - "DI Wiring & Entry Point"
Cohesion: 0.16
Nodes (7): ExtractorOLE, ExtractorOLE.DTOs, ExtractorOLE.Helpers.FileTypeStrategy, ExtractorOLE.Helpers, Program, ServiceRegistration, IServiceCollection

### Community 2 - "MainExtractor & Mime Contract"
Cohesion: 0.20
Nodes (6): Dictionary, OfficeMimeTypeEnum, IExtractionHelper, ITextExtractor, MainExtractor, IDictionary

### Community 3 - "ExtractionHelper Implementation"
Cohesion: 0.26
Nodes (4): List, OpenXmlPackage, OpenXmlPart, ExtractionHelper

### Community 4 - "Text Extractor Handlers"
Cohesion: 0.22
Nodes (5): ExtractorOLE.Handlers, DocxTextExtractor, PptxTextExtractor, XlsxTextExtractor, ITextExtractor

### Community 5 - "Old Excel Extractor (dead code)"
Cohesion: 0.27
Nodes (5): Cell, ExtractorOLE.Old.Excel, List, ExcelExtractor, WorkbookPart

### Community 6 - "Project/Build Metadata"
Cohesion: 0.25
Nodes (7): ExtractorOLE, net10.0, DocumentFormat.OpenXml (3.5.1), DocumentFormat.OpenXml.Features (3.5.1), DocumentFormat.OpenXml.Framework (3.5.1), Microsoft.Extensions.DependencyInjection (8.0.0), Microsoft.NET.Sdk

### Community 7 - "Old Doc Extractor (dead code)"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Doc, DocExtractor, WordprocessingDocument

### Community 8 - "Old Extraction Util (dead code)"
Cohesion: 0.38
Nodes (3): ExtractorOLE.Old.Util, OpenXmlPart, ExtractionUtil

## Knowledge Gaps
- **12 isolated node(s):** `net10.0`, `DocumentFormat.OpenXml (3.5.1)`, `DocumentFormat.OpenXml.Features (3.5.1)`, `DocumentFormat.OpenXml.Framework (3.5.1)`, `Microsoft.Extensions.DependencyInjection (8.0.0)` (+7 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **2 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `DocumentExtractionResult` connect `DTO <-> Strategy Cross-Refs` to `MainExtractor & Mime Contract`, `ExtractionHelper Implementation`, `Old Excel Extractor (dead code)`, `Old Doc Extractor (dead code)`, `Old Extraction Util (dead code)`, `Old PowerPoint Extractor (dead code)`?**
  _High betweenness centrality (0.256) - this node is a cross-community bridge._
- **Why does `ExtractorOLE.DTOs` connect `DI Wiring & Entry Point` to `DTO <-> Strategy Cross-Refs`, `Old Excel Extractor (dead code)`, `Old Doc Extractor (dead code)`, `Old Extraction Util (dead code)`, `Old PowerPoint Extractor (dead code)`?**
  _High betweenness centrality (0.203) - this node is a cross-community bridge._
- **Why does `MainExtractor` connect `MainExtractor & Mime Contract` to `DI Wiring & Entry Point`?**
  _High betweenness centrality (0.068) - this node is a cross-community bridge._
- **What connects `net10.0`, `DocumentFormat.OpenXml (3.5.1)`, `DocumentFormat.OpenXml.Features (3.5.1)` to the rest of the system?**
  _12 weakly-connected nodes found - possible documentation gaps or missing edges._