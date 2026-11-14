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

Password-protected files are supported with a caller-supplied password (see
[Password-protected files](#password-protected-files)). Out of scope for v1: recursive unpacking
of subfiles nested inside other subfiles (only first-layer embeddings are extracted). See
`docs/specs/overview.md` for the full scope list.

## Password-protected files

Set `Password` (`string?`, init-only) on `ExtractionRequest` and on `MimeDetectionRequest`. Null or
empty means no password. Each request in a batch carries its own password; the `ExtractMany` and
`ExtractManyStreaming` signatures are unchanged.

```csharp
var request = new ExtractionRequest
{
    FileBytes = bytes,
    FileName = "report.docx",
    DetectedMimeType = mimeType,
    Password = "secret"
};
```

The password applies to the supplied container only. Encrypted embedded subfiles are returned opaque.

| Format | Decrypted schemes |
|--------|-------------------|
| `.docx`, `.xlsx`, `.pptx` | Agile (Office 2010+) and Standard (Office 2007) encryption |
| `.xls` | RC4, CryptoAPI RC4 and XOR obfuscation |
| `.doc` | BinaryRC4 and RC4 CryptoAPI |
| `.ppt` | RC4 CryptoAPI |

After decryption the file is extracted exactly like its unencrypted version.

Error codes:

- `PASSWORD_PROTECTED`: no password was supplied, or the encryption is not decrypted by this library.
- `INVALID_PASSWORD` (`InvalidPasswordException`, `StatusCode` 422): the password fails the format's verifier. It never returns a partial result and is never reported as `CORRUPT_FILE`.
- `CORRUPT_FILE`: altered ciphertext with the right password (Agile integrity HMAC mismatch), or crafted encryption headers or a key-derivation cost over the format maximum.

Detection: an encrypted docx, xlsx or pptx (`EncryptionInfo` and `EncryptedPackage` in a CFB wrapper) detects as `Unknown` when no password is supplied, because the inner format cannot be known. This includes Word wrappers with a `WordDocument` stub, which were formerly detected as `Doc`. The correct password gives the inner format; a wrong one throws `INVALID_PASSWORD`. Encrypted doc, xls and ppt are classified from their plain directory, and the password is not used for detection.

A password supplied for an unencrypted file is ignored for all six formats: no error, no warning, identical result. An xls encrypted only with Excel's default VelvetSweatshop key counts as having no password.

Confidentiality: the password, derived keys and decrypted bytes are never logged, never appear in exceptions, `FieldErrors` or results, and are not kept after the call.

Still `PASSWORD_PROTECTED` even with a password: XOR-obfuscated doc (deferred, depends on yoavarad/npoi#198 and a fixture), doc schemes other than RC4 and RC4 CryptoAPI, Extensible encryption, and IRM/rights-management.

Known limitations (legacy doc and ppt):

- A CryptoAPI doc with encrypted document properties keeps its text and reports one `Metadata` `CORRUPT_FILE` field error.
- OLE objects of a CryptoAPI doc are not returned (0 `EmbeddedFiles`, no field error).
- Two ppt samples (`cryptoapi-proc2356.ppt`, `ppt_with_png_encrypted.ppt`) decrypt, but their text converts empty because of unrelated b2xtranslator conversion defects. Pictures and metadata are fine; text parity is pending those fixes.
- Test-coverage gaps G1 to G5 (for example no Word-authored 40-bit CryptoAPI doc fixture) are recorded in `samples/manifest.json` under `$encryptedTestGaps`.

Cost: Agile key derivation runs about 100,000 SHA-512 rounds per verification, so detect-then-extract pays it twice. Encrypted ppt extraction allocates about 23x its input after #465 (it was about 29.6x; the original 8x target was knowingly missed and accepted).

See [ADR-012](docs/adrs/012-password-protected-decryption.md) and [ADR-013](docs/adrs/013-legacy-doc-ppt-decryption.md).

## Project structure

```
ole-extractor/
├── ExtractorOle/                     # .NET solution (ExtractorOle.slnx)
│   ├── ExtractorOLE/                 # Main library
│   │   ├── MainExtractor.cs          # Public extraction entry point
│   │   ├── Abstractions/             # ITextExtractor, IParsedDocumentTextExtractor contracts
│   │   ├── DTOs/                     # Requests/results, detected-format & mime enums
│   │   ├── Handlers/                 # Per-format ITextExtractor implementations
│   │   ├── Helpers/
│   │   │   ├── Guardrails/           # ExtractionGuardrails, ParseTimeErrorGuard
│   │   │   ├── Input/                # ExtractionInput, NonClosingStream
│   │   │   ├── MimeDetection/        # Content-only OOXML/CFB mime detectors
│   │   │   └── FileTypeStrategy/     # Per-format "open" strategies
│   │   ├── Registry/                 # Format-dispatch registry (detection → open → text)
│   │   ├── Configuration/            # Limits (max file size, nesting-depth guard, etc.)
│   │   └── Exceptions/               # Domain exceptions (too-large, oversized-nested, ...)
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
└── .ydk/                             # YDK dev-workflow state (config, reports, proofs, etc.)
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
Apache-2.0 source instead, via the `third_party/npoi` git submodule (pinned to the
`yoavarad/npoi` fork's `main`) referenced as a `ProjectReference`. Only NPOI's `main` tree is used (gives
`.xls`/HSSF, plus format-agnostic HPSF/POIFS for cross-format legacy metadata and subfile
discovery) — NPOI's `.doc`/`.ppt` readers live only in an unshipped `scratchpad` tree, which
is why those two formats use a different library instead (see ADR-004). `b2xtranslator` is
likewise compiled from source, via the `third_party/b2xtranslator` git submodule.

## Build & test

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download) or later, and the
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

### Concurrency

`MainExtractor.Extract()` is safe for concurrent callers: no shared mutable state is touched on the
extraction path (the detectors' test-observability counters are atomic and scoped per async flow).
`ExtractorOle/ExtractorOLE.StressHarness` (50 callers x 200 iterations over `samples/`) reports 0
cross-call mismatches against the single-threaded baseline. Samples that are deliberately rejected
(e.g. oversized-FAT guard fixtures) fail identically in serial and concurrent runs.

For many files, `MainExtractor.ExtractManyAsync(requests, maxDegreeOfParallelism, cancellationToken)`
runs `Extract()` with bounded parallelism (default `Environment.ProcessorCount`). It returns one result per request in
input order. A failing item reports its error on that item and does not fail the batch
([ADR-007](docs/adrs/007-batch-extraction-api.md)). It keeps every result until the batch returns, so its
memory grows with batch size. For large batches, `MainExtractor.ExtractManyStreamingAsync` (same parameters) yields
results as they finish, in completion order with each item's input `Index`, and keeps at most 2 x MaxDOP + 1 results
alive ([ADR-011](docs/adrs/011-streaming-batch-extraction.md)). `StressHarness -- batch [repeat] [samplesDir]`
measures throughput and the peak heap of both methods.
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
  - [ADR-007](docs/adrs/007-batch-extraction-api.md) — batch extraction API with bounded parallelism
  - [ADR-009](docs/adrs/009-analyzer-compliance-over-api-stability.md) — fix analyzer findings instead of suppressing them (breaking enum renames, ParamName contract)
  - [ADR-010](docs/adrs/010-partial-results-with-per-field-error-reporting.md) — partial results with per-field error reporting
  - [ADR-011](docs/adrs/011-streaming-batch-extraction.md) — streaming batch extraction to bound batch memory
  - [ADR-012](docs/adrs/012-password-protected-decryption.md) — decrypt password-protected OOXML and xls with a caller-supplied password
  - [ADR-013](docs/adrs/013-legacy-doc-ppt-decryption.md) — legacy `.doc`/`.ppt` decryption
- [`CHANGELOG.md`](CHANGELOG.md) — breaking changes and migration notes
- [`docs/project-rules.md`](docs/project-rules.md) — conventions, brownfield constraints, and known gotchas
- [`docs/research/`](docs/research/) — research spikes backing the ADRs above

## Development workflow

This project follows the YDK development lifecycle (brainstorming → ignition → task
management → execution → learning); workflow state (config, reports, proofs) lives under
`.ydk/`; epics, stories and tasks are GitHub Issues.
