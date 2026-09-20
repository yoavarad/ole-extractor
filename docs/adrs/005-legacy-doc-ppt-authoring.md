# ADR-005: Authoring legacy .doc/.ppt (and .xls) test samples in SampleGenerator

## Status
Accepted

## Context
`SampleGenerator` must be able to author valid legacy `.doc`, `.xls` and `.ppt` files so the
dataset-curation corpus (docs/specs/dataset-curation.md) can cover all six formats (T-b856b14e,
which unblocks T-07571b32). ADR-001 chose NPOI for legacy OLE and ADR-004 narrowed that to
`.xls` (NPOI HSSF/HPSF/POIFS) plus `b2xtranslator` for `.doc`/`.ppt` body *reading*. Neither ADR
says how to *write* `.doc`/`.ppt` body content, and `DocSampleGenerator`/`PptSampleGenerator`
(and `XlsSampleGenerator`) were `NotSupportedException` placeholders. T-f0963fc3 had to work
around the gap for the benchmark samples by padding existing Word-/PowerPoint-authored fixtures
with an unreferenced stream, which cannot produce arbitrary body text.

Spike (time-boxed, T-b856b14e step 1) - which library can WRITE legacy .doc / .ppt body content:

- **b2xtranslator** (BSD-3-Clause, `third_party/b2xtranslator`): verified a reader/converter, not a
  body writer. Its `Doc` and `Ppt` modules parse the binary formats (`WordDocument`,
  `PowerpointDocument`) and map them to OOXML; there is no code that emits `.doc`/`.ppt`
  records. The only binary writer in the tree is `Common/StructuredStorage/Writer` - a generic CFB
  container writer, i.e. the same job NPOI POIFS / OpenMcdf already do.
- **NPOI** (self-compiled, `third_party/npoi`): `main/` (HSSF, HPSF, POIFS) can write `.xls`,
  property sets and CFB containers, and is already referenced by `SampleGenerator`. HWPF
  (`.doc`) exists only in `scratchpad/`, which ADR-004 established is not compiled here; its
  `HWPFDocument.Write` merely re-serialises an already-parsed document (there is no
  create-from-scratch path). HSLF (`.ppt`) in the C# port has only low-level Record/Model
  classes and no slide-show assemble/write entry point at all.
- **OpenMcdf** (already a dependency): generic CFB only; no Word/PowerPoint knowledge, and
  redundant with POIFS for our purposes.
- **Other OSS**: Apache POI (Java) has the same limits (HWPF cannot create from scratch, HSLF
  creates from a bundled template) and is the wrong runtime. LibreOffice headless
  (`soffice --convert-to doc|ppt`) would write real files but is the same 500MB+
  runtime dependency ADR-004 already rejected, and is not available in this environment.
  Commercial SDKs (Aspose, GemBox, Syncfusion) are rejected for the same reason as in
  ADR-001/ADR-004.

Conclusion: no OSS library available to this project writes `.doc` or `.ppt` body content.

## Decision
- **.xls**: author with NPOI `HSSFWorkbook` (ADR-001/ADR-004 unchanged; the same path the
  `.xls` Unicode-fidelity baseline of 15/15 already used).
- **.doc and .ppt**: write minimal, valid binary files ourselves, per [MS-DOC] and [MS-PPT]
  (with [MS-ODRAW] for shapes), inside `SampleGenerator`. NPOI's POIFS (already a
  `SampleGenerator` reference since T-f0963fc3) provides the CFB container and HPSF the
  `SummaryInformation` stream; the two body streams are byte-built:
  - `.doc`: `WordDocument` + `1Table` streams - FIB (Word 97, `nFib` 0x00C1), one UTF-16LE
    piece in a Clx piece table, one paragraph per body line (PAPX FKPs), one CHPX run, the
    "Normal" style, a font table, one section and a default DOP.
  - `.ppt`: `PowerPoint Document` + `Current User` streams - Document (DocumentAtom,
    Environment with the default text style, drawing group, doc-info list, slide lists), one
    main master, one slide with one text box per body line (`TextCharsAtom`, UTF-16LE), and
    the PersistDirectory / UserEdit records.
  - Embeddings become opaque CFB storages (`ObjectPool/*` for `.doc`, root storages for
    `.ppt`), a `SampleSpec.VbaProject` becomes the `Macros` storage (`.doc`/`.ppt`) or
    `_VBA_PROJECT_CUR` (`.xls`) - the locations ExtractorOLE's open strategies read.
- The reader side is unchanged: b2xtranslator for `.doc`/`.ppt` body text, NPOI for `.xls`,
  HPSF/POIFS for metadata and subfile discovery (ADR-004).

## Alternatives Considered
- **Template rewrite of a real Office-authored file** (as `PptTextFixtureBuilder` and the
  benchmark padding do): rejected as the general mechanism - the template's text length,
  slide count and structure are fixed, so arbitrary `SampleSpec.BodyText`/multi-line bodies and
  the 100,000-word large-body sample cannot be expressed.
- **Port scratchpad HWPF/HSLF to net8.0 and use it to write** (ADR-004's `.doc` fallback):
  rejected - neither can create a document from scratch, so this still ends in hand-built
  structures plus a second self-compiled dependency.
- **LibreOffice / commercial SDKs**: see Context; rejected for the reasons ADR-004 gives.

## Consequences
- The generated `.doc`/`.ppt` are **minimal** (no fields, tables, lists, headers/footers,
  images, notes or animations). They are verified by *reading them back*, not by an
  independent implementation:
  `ExtractorOLE.Tests/SampleGeneration/{Doc,Xls,Ppt}SampleGeneratorTests.cs` generate each
  format and read it through `MainExtractor.Extract` (open strategy + text extractor +
  mime detection). Real Microsoft Word / PowerPoint / LibreOffice were not available when this
  was written, so **openability in the real applications is unverified**; the structures
  follow the specs and the fields the b2xtranslator/NPOI readers require, and should be
  spot-checked in real Office before the samples are used to make claims about Office
  behaviour.
- Reader quirks the writer accommodates (each is a b2xtranslator expectation, not a spec
  requirement): the Word FIB's legacy `fcMin`/`fcMac` fields must be set (the piece table's
  last position is taken from `fcMac`); every ClientTextbox shape needs an OfficeArt Opt record;
  a `.ppt` needs a `DocInfoList` > `SlideViewInfo` > `ZoomViewInfoAtom` and an
  `Environment` > `TextMasterStyleAtom` before b2xtranslator can convert it.
- Unicode/RTL/CJK/emoji fidelity is now verified for generated `.doc` and `.ppt` (11 of 11
  `MultilingualFixtures` strings each - Hebrew, Arabic, Persian, Russian, Latin, Cjk, the four
  emoji fixtures incl. ZWJ family / skin-tone / ZWJ flag, and a decomposed combining
  sequence - exact string equality, no normalization), plus `.xls` through
  `XlsSampleGenerator` (same 11), on top of the existing 15/15 `.xls` baseline. This
  exercises the write side and the b2xtranslator read path together; it does not add
  coverage of older single-byte code-page runs.
- `.ppt` embedding count quirk: `PptOpenStrategy` reports every top-level storage as an
  embedded object, so a macro-enabled `.ppt` reports its `Macros` storage as one extra
  embedded file. `.xls` OLE-object embeddings also register one shared icon picture, which is
  reported as a picture. T-07571b32's expected-subfile fields must account for both.
- Curated legacy samples (with 2+ levels of nested embedding, authorship chains) and the
  remaining 10 legacy corpus samples are T-07571b32's job; this ADR only provides the
  generators.
