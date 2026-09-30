import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).with_name("compare_benchmarks.py")


def write_report(directory: Path, sample: str, p95: str) -> None:
    (directory / "x-report.csv").write_text(f"Sample,P95\n{sample},{p95}\n", encoding="utf-8")


def run(baseline_p95: str, current_p95: str):
    with tempfile.TemporaryDirectory() as tmp:
        root = Path(tmp)
        (root / "b").mkdir()
        (root / "c").mkdir()
        write_report(root / "b", "doc-10mb", baseline_p95)
        write_report(root / "c", "doc-10mb", current_p95)
        summary, issue = root / "summary.md", root / "issue.md"
        env = {**os.environ, "GITHUB_STEP_SUMMARY": str(summary)}
        subprocess.run([sys.executable, str(SCRIPT), str(root / "b"), str(root / "c"), str(issue)],
                       check=True, env=env, capture_output=True)
        return summary.read_text(encoding="utf-8"), issue.exists()


class CompareBenchmarksTests(unittest.TestCase):
    def test_pass_has_table_and_no_issue_file(self):
        summary, issue = run("100 ms", "110 ms")
        self.assertIn("| doc-10mb | 100.00 | 110.00 | 1.10 | pass |", summary)
        self.assertFalse(issue)

    def test_regression_flagged_and_issue_file_written(self):
        summary, issue = run("100 ms", "130 ms")
        self.assertIn("| doc-10mb | 100.00 | 130.00 | 1.30 | regress |", summary)
        self.assertTrue(issue)


if __name__ == "__main__":
    unittest.main()
