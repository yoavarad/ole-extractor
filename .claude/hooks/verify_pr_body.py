#!/usr/bin/env python3
"""Block `gh pr create/edit` when the PR body would fail CI's pr-body check.

Runs the same `ydk verify pr-body` the YDK PR Body workflow runs, against the
--body-file before it is sent. Inline --body text can't be checked reliably,
so it is refused in favour of --body-file.
"""

import json
import os
import re
import subprocess
import sys
from pathlib import Path

try:
    data = json.loads(sys.stdin.read() or "{}")
except (json.JSONDecodeError, ValueError):
    sys.exit(0)  # Bad data = fail open

command = data.get("tool_input", {}).get("command", "")
# Only a real invocation (line start or after ; & | ( ), not the words inside a
# commit message or heredoc. Flags are read from that command's line only.
gh_call = re.search(r"(?m)(?:^|[;&|(])\s*gh\s+pr\s+(?:create|edit)\b(.*)$", command)
if not gh_call:
    sys.exit(0)
command = gh_call.group(1)


def block(msg):
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    print(f"PR body check: {msg}", file=sys.stderr)
    sys.exit(2)


body_file = re.search(r"(?:--body-file|-F)[\s=]+(?:\"([^\"]+)\"|'([^']+)'|(\S+))", command)
if not body_file:
    if re.search(r"(?:--body|-b)[\s=]", command):
        block("write the body to a file and pass --body-file, so it can be validated before sending.")
    sys.exit(0)  # No body change (e.g. title/label-only edit)

path = next(g for g in body_file.groups() if g)
if "$" in path or "%" in path:
    block(f"--body-file must be a literal path, not a variable ({path}).")

# Git Bash style /c/Users/... -> C:/Users/...
path = re.sub(r"^/([a-zA-Z])/", r"\1:/", path)
body_path = Path(path)
if not body_path.is_absolute():
    body_path = Path(data.get("cwd", ".")) / body_path
if not body_path.is_file():
    block(f"body file not found: {body_path}")

if body_path.read_bytes().startswith(b"\xef\xbb\xbf"):
    block(
        f"{body_path} starts with a UTF-8 BOM, which hides the first '## ' heading from CI. "
        "Write it without a BOM (PowerShell 5.1: [IO.File]::WriteAllText(path, text, (New-Object Text.UTF8Encoding $false)))."
    )

project_dir = os.environ.get("CLAUDE_PROJECT_DIR", data.get("cwd", "."))
result = subprocess.run(
    ["uv", "run", "ydk", "verify", "pr-body", "--body-file", str(body_path), "--base-ref", "origin/main"],
    cwd=project_dir,
    capture_output=True,
    encoding="utf-8",
    errors="replace",
)
if result.returncode != 0:
    block("ydk verify pr-body failed:\n" + (result.stdout + result.stderr).strip())
sys.exit(0)
