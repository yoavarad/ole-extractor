# ADR-003: Manual LLM spec review in place of `ydk spec verify`

## Status
Accepted

## Context
YDK's Stage 01 exit criteria require `ydk spec verify` (10 Bedrock-backed reviewers, N01-N10) to pass. This environment has no AWS credentials configured, and `ydk spec verify` failed immediately with `NoCredentialsError` for all 10 reviewers — a local environment gap, not a spec problem.

## Decision
Substituted the automated Bedrock pipeline with manual review: an Opus-model subagent read each reviewer's actual rubric YAML (`.ydk/spec-reviewers/n01.yaml`-`n10.yaml`) and graded the real spec artifacts against those exact rubrics and thresholds, plus the Step 19b self-review checklist. Iterated four passes (grade → fix → re-grade) until all 10 criteria cleared their real per-file threshold. Full reports at `.ydk/reports/spec-verify-manual-2026-08-18.md`.

## Alternatives Considered
- **Configure AWS credentials and run the real pipeline**: offered to the user; declined in favor of the subagent approach for this session.
- **Skip the quality gate entirely**: rejected — the gate exists to catch exactly the kind of vague/inconsistent spec content that would otherwise reach implementation; skipping it would violate Stage 01 exit criteria without a substitute.

## Consequences
- Final verdict (10/10, all thresholds cleared) is not machine-reproducible the way a Bedrock run would be — no cached JSON report artifact in YDK's standard format, only the manual markdown report.
- If AWS credentials are configured later, re-running `ydk spec verify --all-files` against the current spec is worth doing once as a cross-check, though not required to unblock Stage 02.
- This substitution pattern (Opus subagent reading the actual reviewer YAML rubrics) is reusable for any future YDK spec-verify gate blocked by missing Bedrock credentials in this environment.
