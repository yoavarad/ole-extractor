# Spec verify baseline on main (2026-11-10)

Evidence that the five reviewers failing on `spec/password-decryption` already fail on `main`.

- main commit: `dc7ce12` (temp worktree, `ydk spec verify --all-files`)
- Reviewer responses: 5 layer-b responses were reused from byte-identical earlier requests. The other 11 (10 spec-review, 1 layer-b) were written by the same Claude agent that wrote the branch responses (`ai.provider: subagent`). This run is therefore not an independent judge. As independent corroboration, `.ydk/spec-check-results.json` from the 2026-11-08 run on main recorded the same failing set: N02 7, N04 7, N05 6, N08 3, N10 6.

| Reviewer | main | spec/password-decryption |
|---|---|---|
| N01 Problem Statement | 8/10 PASS | 8/10 PASS |
| N02 Success Criteria | 7/10 FAIL | 7/10 FAIL |
| N03 Scope Boundaries | 8/10 PASS | 9/10 PASS |
| N04 Terminology Consistency | 7/10 FAIL | 7/10 FAIL |
| N05 Ambiguity | 6/10 FAIL | 6/10 FAIL |
| N06 Flow Completeness | 8/10 PASS | 8/10 PASS |
| N07 Information Density | 8/10 PASS | 8/10 PASS |
| N08 No Technical Specs in Prose | 3/10 FAIL | 3/10 FAIL |
| N09 Component References | 9/10 PASS | 9/10 PASS |
| N10 YAGNI | 6/10 FAIL | 6/10 FAIL |

Both runs: 5 failed | 4 passed | 1 deterministic passed. Same failing set (N02, N04, N05, N08, N10) with the same scores. Only N03 differs, at 8 on main and 9 on the branch.

## Raw output (main)

```

===============================================================
 YDK Spec Verification Report
 Project: ole-extractor | Files: 6 | Components: 28 | Duration: 0s
===============================================================

-- Summary --

  Criterion                          Score     Threshold    Status        Time  
  N01 Problem Statement                  8             8    PASS          0.0s  
  N02 Success Criteria                   7             8    FAIL          0.0s  
  N03 Scope Boundaries                   8             8    PASS          0.0s  
  N04 Terminology Consistency            7             8    FAIL          0.0s  
  N05 Ambiguity                          6             8    FAIL          0.0s  
  N06 Flow Completeness                  8             8    PASS          0.0s  
  N07 Information Density                8             8    PASS          0.0s  
  N08 No Technical Specs in              3             8    FAIL          0.0s  
  Prose                                                                         
  N09 Component References               9             8    PASS          0.0s  
  N10 YAGNI                              6             8    FAIL          0.0s  
  Component Checks                                 24/28    FAIL                
  Reference Integrity                              28/28    PASS                

 RESULT: 5 failed, 4 passed, 1 deterministic passed

-- Deterministic Tool Findings --

  Reviewer      Tool                                Findings  
  N09           unlinked_entity                           17  

 Total deterministic findings: 17

-- N01 Problem Statement -- 8/10 PASS --

 LLM Assessment:
   overview.md states WHO (systems ingesting untrusted Office files), WHAT 
(filename cannot be trusted, paid SDKs not always an option), WHY (silent 
misprocessing or rejection, re-implemented parsing) and SUCCESS (Success 
Criteria section). WHO is a category rather than a persona, WHY is qualitative, 
and the 'We are building' paragraph mixes solution into the problem.

 Suggestions:
   1. Name a concrete consumer persona.
   2. Quantify the impact.
   3. Separate solution from problem.

 Tool Findings (1):
   Line 206: Systems that ingest arbitrary Office files from untrusted sources

-- N02 Success Criteria -- 7/10 FAIL --

 LLM Assessment:
   Classification, extraction and adversarial criteria are quantified (100%, 
exact manifest match, specific error). The latency/resource criterion only names
NFR ids in backticks without numbers, and the 7th-format criterion is 
hypothetical with no verification method.

 Suggestions:
   1. State latency/memory figures or link the NFRs.
   2. Make the 7th-format criterion verifiable.

 Tool Findings (2):
   Line 215: Latency and resource targets in 
`ydk:nfr:extraction/extraction-latency`
   Line 216: A hypothetical 7th format can be added

-- N03 Scope Boundaries -- 8/10 PASS --

 LLM Assessment:
   overview.md has explicit IN, OUT and DEFERRED lists, including decryption as 
OUT and recursive unpacking as DEFERRED. Boundaries are restated per narrative. 
Minor: some OUT items lack the reason they are excluded.

 Suggestions:
   1. Give a one-line reason for each OUT item.

-- N04 Terminology Consistency -- 7/10 FAIL --

 LLM Assessment:
   The glossary locks terms with DO NOT USE lists and most prose follows it. 
mime-detection.md says 'compound-file-based legacy family' although the glossary
prescribes CFB, and extraction.md mixes entity names and C# DTO names for the 
same fields (Author/Creator, CreatedAt/Created).

 Suggestions:
   1. Use 'CFB' consistently.
   2. Keep the entity/DTO name mapping in manifests.

 Tool Findings (2):
   Line 181: one for the compound-file-based legacy family
   Line 44: the C# `FileMetadata` DTO uses `Creator` (entity `Author`)

-- N05 Ambiguity -- 6/10 FAIL --

 LLM Assessment:
   Most rules are precise, but 'format-agnostic where possible' leaves sample 
targeting to judgement, 'configurable higher for extended runs' is unbounded, 
and extraction.md calls legacy text decoding 'unverified' while project-rules 
records it as verified.

 Suggestions:
   1. State per adversarial sample which formats it targets.
   2. Reconcile the 'unverified' sentence.
   3. Bound the stress-test concurrency.

 Tool Findings (3):
   Line 17: since these scenarios are format-agnostic where possible
   Line 58: exact text-decoding behavior for mixed-encoding legacy fields is 
unverified
   Line 287: configurable higher for extended runs

-- N06 Flow Completeness -- 8/10 PASS --

 LLM Assessment:
   Detection and extraction flows cover the happy path, the enumerated fatal 
set, per-field isolation, cancellation and batch behaviour, and the adversarial 
review maps each edge case to an error. Detection-time cancellation is not 
described.

 Suggestions:
   1. Describe cancellation for detection.

-- N07 Information Density -- 8/10 PASS --

 LLM Assessment:
   Prose is dense and specific with little padding. A few rhetorical asides add 
length without information.

 Suggestions:
   1. Trim the Purpose sentence in dataset-curation.md.

 Tool Findings (1):
   Line 9: not a folded-in afterthought of "testing"

-- N08 No Technical Specs in Prose -- 3/10 FAIL --

 LLM Assessment:
   Zero-tolerance check. The narratives carry backticked C# DTO/property names 
and an entity-to-DTO mapping, a filename template with placeholder syntax, OOXML
element names, the manifest field `expectedSubfileCount`, enum values 
`LegacyOle`/`Ooxml`, and an exception type with its ParamName.

 Suggestions:
   1. Move the DTO naming bridge, filename template and manifest field names 
into manifests.
   2. Reference ADR-009 instead of exception details.

 Tool Findings (5):
   Line 44: the C# `FileMetadata` DTO uses `Creator` (entity `Author`)
   Line 66: subfile `FileName` is `<prefix>_<N><ext>`
   Line 60: text inside tracked deletions (`w:delText`)
   Line 133: one of `LegacyOle` or `Ooxml`
   Line 270: `ArgumentNullException` whose `ParamName` is the method parameter

-- N09 Component References -- 9/10 PASS --

 LLM Assessment:
   Components are referenced with  links throughout. Overview Success Criteria 
names three NFRs in backticks instead of links, and testing-strategy lists 
container-open error names as bare words.

 Suggestions:
   1. Convert backticked NFR ids to links.
   2. Link the error names in testing-strategy.

 Tool Findings (19):
   Line 215: `ydk:nfr:extraction/extraction-latency`
   Line 292: The container-open cases (file-too-large, password-protected, 
unsupported-format
   Line 14: LibreOffice
   Line 42: FileMetadata
   Line 42: CreatedAt
   Line 42: ModifiedAt
   Line 42: EditingDurationMinutes
   Line 42: EditTime
   Line 42: TotalTime
   Line 46: PowerPoint
   Line 64: FileName
   Line 89: PowerPoint
   Line 103: SummaryInformation
   Line 131: LegacyOle
   Line 131: FormatFamily
   Line 134: PowerPoint
   Line 134: PowerPoint
   Line 268: ArgumentNullException
   Line 268: ParamName

-- N10 YAGNI -- 6/10 FAIL --

 LLM Assessment:
   The extensibility success criterion and registry-dispatch requirement design 
for a hypothetical 7th format, the architecture section prescribes DI/registry 
patterns beyond what six formats need, and dataset-curation demands a 
100,000-word synthetic sample per format with no tied NFR.

 Suggestions:
   1. Tie the 100,000-word samples to the latency NFR or drop them.
   2. Keep the 7th-format criterion only if a next format is planned.

 Tool Findings (2):
   Line 216: A hypothetical 7th format can be added
   Line 15: a file with a text body exceeding 100,000 words

===============================================================
 5 failed | 4 passed | 1 deterministic passed | 0s total
===============================================================

 Reports written to .ydk\reports/
   spec-verify-20261110-054733.txt
   spec-verify-20261110-054733.json

```
