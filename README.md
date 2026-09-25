# ole-extractor

A C# library that classifies the exact Office file format of a byte stream — using only the
file's content, never its filename — and extracts general metadata, format-specific metadata,
flat body text, and first-layer embedded subfiles (OLE objects and inline media) into one
structured result.

It targets systems that ingest arbitrary Office files from untrusted sources (content
pipelines, document management systems, search indexing, security scanning), where the
filename can't be trusted and a renamed or mislabeled file still needs to classify and extract
correctly. Only open-source libraries are used — see [Library Stack](#library-stack) below.

See `docs/specs/overview.md` for the full problem statement, success criteria, and scope.

## Supported formats

| Format | Family | Status |
|---|---|---|
| `.docx` (incl. `.docm`) | OOXML | Supported — via `DocumentFormat.OpenXml` |
| `.xlsx` (incl. `.xlsm`) | OOXML | Supported — via `DocumentFormat.OpenXml` |
| `.pptx` (incl. `.pptm`) | OOXML | Supported — via `DocumentFormat.OpenXml` |
| `.xls` | Legacy OLE/CFB | Supported — via self-compiled NPOI (`HSSFWorkbook`) |
| `.doc` | Legacy OLE/CFB | Supported — via self-compiled `b2xtranslator` (doc→docx), see [ADR-004](docs/adrs/004-legacy-doc-ppt-parsing.md) |
| `.ppt` | Legacy OLE/CFB | Supported — via self-compiled `b2xtranslator` (ppt→pptx), see [ADR-004](docs/adrs/004-legacy-doc-ppt-parsing.md) |

A macro-enabled file (`docm`/`xlsm`/`pptm`) is detected as its base format (`Docx`/`Xlsx`/`Pptx`)
rather than as a separate enum value — macro presence surfaces as a flag on the format-specific
metadata instead. Detection is content-only: it inspects the file's actual container structure
(ZIP central directory for OOXML, CFB directory entries for legacy OLE), never the filename or
extension. See `docs/specs/mime-detection.md`.

The architecture is registry-based specifically so a 7th format (e.g. rtf, odt) can be added by
registering a new detection/open/text-extraction component set, without changing the two public
entry points (`MainExtractor` and the mime-detection helper). See
`docs/specs/extraction.md` and [ADR-001](docs/adrs/001-extraction-library-stack.md).

Out of scope for v1: password-protected files (fail cleanly instead of decrypting), and
recursive unpacking of subfiles nested inside other subfiles (only first-layer embeddings are
extracted). See `docs/specs/overview.md` for the full scope list.

## Project structure

```
ole-extractor/
├── ExtractorOle/                     # .NET solution (ExtractorOle.slnx)
│   ├── ExtractorOLE/                 # Main library (+ minimal console host)
│   │   ├── MainExtractor.cs          # Public extraction entry point
│   │   ├── ITextExtractor.cs         # Per-format text-extraction contract
│   │   ├── DTOs/                     # Requests/results, detected-format & mime enums
│   │   ├── Handlers/                 # Per-format ITextExtractor implementations
│   │   ├── Helpers/
│   │   │   ├── MimeDetection/        # Content-only OOXML/CFB mime detectors
│   │   │   └── FileTypeStrategy/     # Per-format "open" strategies
│   │   ├── Registry/                 # Format-dispatch registry (detection → open → text)
│   │   ├── Configuration/            # Limits (max file size, nesting-depth guard, etc.)
│   │   ├── Exceptions/               # Domain exceptions (too-large, oversized-nested, ...)
│   │   └── Old/                      # Dead code from the original scaffold — not on the live path
│   ├── ExtractorOLE.Tests/           # xUnit test suite
│   ├── ExtractorOLE.Benchmarks/      # BenchmarkDotNet time-profiling harness
│   └── ExtractorOLE.StressHarness/   # Hand-rolled stress-test harness (see ADR-002)
├── SampleGenerator/                  # Generates synthetic/curated test-corpus samples
├── samples/                          # Curated + generated sample corpus + manifest.json
├── third_party/npoi/                 # Git submodule: NPOI, self-compiled from Apache-2.0 source
├── third_party/b2xtranslator/        # Git submodule: b2xtranslator, self-compiled (legacy .doc/.ppt)
├── docs/
│   ├── specs/                        # Overview, extraction, mime-detection, testing-strategy, ...
│   ├── adrs/                         # Architecture decision records
│   └── research/                     # Library/tooling research spikes backing the ADRs
└── .ydk/                             # YDK dev-workflow state (tasks, epics, manifest, etc.)
```

## Library stack

| Concern | Library | ADR |
|---|---|---|
| OOXML (docx/xlsx/pptx) | `DocumentFormat.OpenXml` (MIT) | [ADR-001](docs/adrs/001-extraction-library-stack.md) |
| Legacy OLE — `.xls` | `NPOI` (Apache-2.0, self-compiled — see below) | [ADR-001](docs/adrs/001-extraction-library-stack.md) |
| Legacy OLE — `.doc`/`.ppt` | `b2xtranslator` (BSD-3-Clause, self-compiled — see below) | [ADR-004](docs/adrs/004-legacy-doc-ppt-parsing.md) |
| CFB introspection / mime-sniffing | `OpenMcdf` ≥3.1.3 (MPL-2.0) | [ADR-001](docs/adrs/001-extraction-library-stack.md) |
| Time profiling | `BenchmarkDotNet` | [ADR-002](docs/adrs/002-profiling-and-stress-testing.md) |
| Stress testing | Hand-rolled harness | [ADR-002](docs/adrs/002-profiling-and-stress-testing.md) |

NPOI's precompiled NuGet package (2.8.0+) ships under a EULA requiring a paid maintenance
subscription for revenue-generating organizations, so this repo compiles NPOI from its
Apache-2.0 source instead, via the `third_party/npoi` git submodule (pinned to tag
`2.7.6-rc1`) referenced as a `ProjectReference`. Only NPOI's `main` tree is used (gives
`.xls`/HSSF, plus format-agnostic HPSF/POIFS for cross-format legacy metadata and subfile
discovery) — NPOI's `.doc`/`.ppt` readers live only in an unshipped `scratchpad` tree, which
is why those two formats use a different library instead (see ADR-004). `b2xtranslator` is
likewise compiled from source, via the `third_party/b2xtranslator` git submodule.

## Build & test

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download) or later, and the
`third_party/npoi` and `third_party/b2xtranslator` git submodules checked out.

```bash
# Clone with submodules (or run submodule update after a plain clone)
git clone --recurse-submodules <repo-url>
# ...or, if already cloned:
git submodule update --init --recursive

# Build the whole solution
dotnet build ExtractorOle/ExtractorOle.slnx

# Run the test suite
dotnet test ExtractorOle/ExtractorOle.slnx
```

Individual projects can also be built/tested directly, e.g.
`dotnet build ExtractorOle/ExtractorOLE/ExtractorOLE.csproj` or
`dotnet test ExtractorOle/ExtractorOLE.Tests/ExtractorOLE.Tests.csproj`.

The test suite (xUnit) covers mime detection, per-format text extraction, embedded-object/
multi-embedding scenarios, and format-dispatch-registry integration, backed by the curated +
generated sample corpus under `samples/`. See `docs/specs/testing-strategy.md` for the full
testing approach and `docs/specs/dataset-curation.md` for how the corpus itself is built.

## Documentation

- [`docs/specs/overview.md`](docs/specs/overview.md) — problem statement, success criteria, public API, scope
- [`docs/specs/extraction.md`](docs/specs/extraction.md) — extraction contract and result shape
- [`docs/specs/mime-detection.md`](docs/specs/mime-detection.md) — how content-only format detection works
- [`docs/specs/testing-strategy.md`](docs/specs/testing-strategy.md) / [`dataset-curation.md`](docs/specs/dataset-curation.md) — test approach and corpus curation
- [`docs/specs/glossary.md`](docs/specs/glossary.md) — domain terminology
- [`docs/adrs/`](docs/adrs/) — architecture decision records:
  - [ADR-001](docs/adrs/001-extraction-library-stack.md) — extraction library stack (OOXML, legacy OLE, CFB introspection)
  - [ADR-002](docs/adrs/002-profiling-and-stress-testing.md) — profiling and stress-testing approach
  - [ADR-003](docs/adrs/003-manual-spec-review-in-place-of-bedrock.md) — manual LLM spec review in place of `ydk spec verify`
  - [ADR-004](docs/adrs/004-legacy-doc-ppt-parsing.md) — legacy `.doc`/`.ppt` body-text parsing via `b2xtranslator`
  - [ADR-005](docs/adrs/005-legacy-doc-ppt-authoring.md) — authoring legacy `.doc`/`.ppt`/`.xls` test samples in SampleGenerator
- [`docs/project-rules.md`](docs/project-rules.md) — conventions, brownfield constraints, and known gotchas
- [`docs/research/`](docs/research/) — research spikes backing the ADRs above

## Development workflow

This project follows the YDK development lifecycle (brainstorming → ignition → task
management → execution → learning); workflow state (tasks, epics, manifest, reports) lives
under `.ydk/`.
