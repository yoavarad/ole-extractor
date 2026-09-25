"""Compare BenchmarkDotNet P95 results against the committed baseline.

Usage: python compare_benchmarks.py <baseline_dir> <results_dir>

Implements the rule in docs/benchmarks/README.md (P95_new > 1.20 * P95_baseline, per Sample row
of the *-report.csv files) but only emits GitHub ::warning annotations: hosted runners differ from
the machine the baseline was captured on, so a regression here is a signal, not a verdict.
"""

import csv
import os
import sys
from pathlib import Path

THRESHOLD = 1.20
UNITS = {"ns": 1e-6, "μs": 1e-3, "us": 1e-3, "ms": 1.0, "s": 1000.0}


def to_ms(value: str) -> float | None:
    parts = value.replace(",", "").split()
    if len(parts) != 2 or parts[1] not in UNITS:
        return None
    try:
        return float(parts[0]) * UNITS[parts[1]]
    except ValueError:
        return None


def load(directory: Path) -> dict[str, float]:
    p95: dict[str, float] = {}
    for report in directory.glob("*-report.csv"):
        with report.open(encoding="utf-8-sig", newline="") as fh:
            for row in csv.DictReader(fh):
                ms = to_ms(row.get("P95", ""))
                if row.get("Sample") and ms is not None:
                    p95[row["Sample"]] = ms
    return p95


def main() -> int:
    baseline, current = load(Path(sys.argv[1])), load(Path(sys.argv[2]))
    if not current:
        print("::warning::No benchmark results found to compare.")
        return 0
    lines = ["| Sample | Baseline P95 (ms) | Current P95 (ms) | Ratio |", "|---|---|---|---|"]
    for sample in sorted(current):
        now = current[sample]
        base = baseline.get(sample)
        if base is None:
            lines.append(f"| {sample} | n/a | {now:.2f} | n/a |")
            continue
        ratio = now / base
        flag = " ⚠️" if ratio > THRESHOLD else ""
        lines.append(f"| {sample} | {base:.2f} | {now:.2f} | {ratio:.2f}{flag} |")
        if ratio > THRESHOLD:
            print(f"::warning::{sample}: P95 {now:.2f} ms vs baseline {base:.2f} ms ({ratio:.2f}x > {THRESHOLD}x)")
    table = "\n".join(lines)
    print(table)
    summary = Path(os.environ.get("GITHUB_STEP_SUMMARY", ""))
    if summary.name:
        with summary.open("a", encoding="utf-8") as fh:
            fh.write("## Benchmark P95 vs baseline\n\n" + table + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
