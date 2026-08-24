#!/usr/bin/env python3
"""One-time (re-runnable) remediation for ydk task-state drift in this repo.

Fixes three things, all rooted in ydk-core bugs documented in
docs/project-rules.md's Known Gotchas (ydk task done doesn't sync status;
create-batch overwrites batch-mapping.json instead of merging; dependency
resolution updates a task's own key but not other tasks' references to it):

1. manifest.yaml `tasks[id].status` is corrected to reflect TRUE state,
   determined from `gh pr list` (merged -> done, open -> in-progress,
   no PR -> open). This is the field ydk's own check_dependencies() reads.
2. .ydk/tasks/<id>.md frontmatter `status` field is synced to match, for
   human/agent readability (not load-bearing for the dependency gate, but
   avoids future confusion). `labels` is intentionally left untouched --
   it is not consistently a status mirror across existing task files.
3. Stale slug references in manifest.yaml `tasks[*].dependencies` are
   resolved to real task IDs, using .ydk/batch-mapping.json plus a
   supplementary mapping recovered from .ydk/reports/*.md for slugs that
   batch-mapping.json never captured (see SUPPLEMENTARY_SLUG_MAP below).

Safe to re-run: it only ever computes status from real git/gh state and
only ever resolves slugs to IDs that already exist in manifest.yaml's
tasks: dict.
"""

import json
import re
import subprocess
import sys
from pathlib import Path
from typing import Any

import yaml

REPO_ROOT = Path(__file__).resolve().parents[2]
MANIFEST_PATH = REPO_ROOT / ".ydk" / "manifest.yaml"
BATCH_MAPPING_PATH = REPO_ROOT / ".ydk" / "batch-mapping.json"
TASKS_DIR = REPO_ROOT / ".ydk" / "tasks"

# Slugs used in manifest.yaml dependency lists that were never captured in
# batch-mapping.json. Recovered by cross-referencing task titles/placeholder
# tables in .ydk/reports/stage-02-planning-sprint23-2026-08-19.md (the
# Sprint 2/3 planning report), since create-batch's placeholder->real-ID
# mapping for that batch was never persisted anywhere else.
SUPPLEMENTARY_SLUG_MAP = {
    "task-cfb-detect-core": "T-fd49ef39",
    "task-cfb-detect-guardrails": "T-f667e540",
    "task-cfb-detect-registry": "T-0a81d133",
    "task-doc-open": "T-c4123b53",
    "task-xls-open": "T-c2a60f06",
    "task-ppt-open": "T-517f89c5",
    "task-ooxml-detect-harden": "T-44812a03",
    "task-ooxml-detect-macro-variants": "T-ef11e30d",
    "task-ooxml-detect-zerobyte": "T-1653b76b",
    "task-detect-facade-wire": "T-ef73861f",
    "task-doc-subfiles": "T-1df33d6c",
    "task-xls-subfiles": "T-9e6bfce8",
    "task-ppt-subfiles": "T-12dfd045",
    "task-docx-subfiles": "T-29fbb7cf",
    "task-xlsx-subfiles": "T-30f2c1b8",
    "task-pptx-subfiles": "T-c3f1ec05",
    "task-extract-facade-wire": "T-3b4e70f4",
}

BRANCH_TASK_RE = re.compile(r"^(?:task|quickdev)/((?:T|QD)-[0-9a-f]+)-")


def gh_pr_list(state: str) -> list[dict[str, Any]]:
    """Return `gh pr list --state <state>` results as parsed JSON."""
    out = subprocess.run(
        [
            "gh",
            "pr",
            "list",
            "--state",
            state,
            "--json",
            "number,title,headRefName,mergedAt",
            "--limit",
            "200",
        ],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=True,
    )
    return json.loads(out.stdout)


def compute_true_status_by_task_id() -> dict[str, str]:
    """Compute {task_id: 'done'|'in-progress'} for every task with a PR.

    Absence from this dict means 'open' (no PR exists).
    """
    status = {}
    for pr in gh_pr_list("open"):
        m = BRANCH_TASK_RE.match(pr["headRefName"])
        if m:
            status[m.group(1)] = "in-progress"
    for pr in gh_pr_list("merged"):
        m = BRANCH_TASK_RE.match(pr["headRefName"])
        if m:
            status[m.group(1)] = "done"  # merged wins even if also seen open
    return status


def resolve_dependencies(
    manifest: dict[str, Any], slug_map: dict[str, str]
) -> tuple[int, list[str]]:
    """Resolve stale slugs in manifest['tasks'][*]['dependencies'] in place.

    Returns (num_resolved, sorted list of still-unresolved slugs).
    """
    tasks = manifest.get("tasks", {})
    resolved_count = 0
    unresolved = set()
    for task in tasks.values():
        deps = task.get("dependencies") or []
        new_deps = []
        for dep in deps:
            if dep in tasks:  # already a real ID
                new_deps.append(dep)
                continue
            real_id = slug_map.get(dep)
            if real_id and real_id in tasks:
                new_deps.append(real_id)
                resolved_count += 1
            else:
                new_deps.append(dep)
                unresolved.add(dep)
        task["dependencies"] = new_deps
    return resolved_count, sorted(unresolved)


def correct_manifest_statuses(
    manifest: dict[str, Any], true_status_by_id: dict[str, str]
) -> list[tuple[str, str, str]]:
    """Correct manifest['tasks'][*]['status'] in place to match true state.

    true_status_by_id defaults to 'open' for tasks with no PR. Returns a
    list of (task_id, old_status, new_status) corrections.
    """
    tasks = manifest.get("tasks", {})
    corrections = []
    for task_id, task in tasks.items():
        true_status = true_status_by_id.get(task_id, "open")
        if task.get("status") != true_status:
            corrections.append((task_id, task.get("status"), true_status))
            task["status"] = true_status
    return corrections


def correct_task_md_frontmatter(task_id: str, true_status: str) -> bool:
    """Rewrite the frontmatter `status` field of .ydk/tasks/<id>.md.

    Does not touch `labels` -- that field is not consistently a status
    mirror across existing task files (some carry a status-name label,
    most don't), so leaving it alone avoids inventing a convention that
    wasn't asked for. Returns True if changed.
    """
    path = TASKS_DIR / f"{task_id}.md"
    if not path.exists():
        return False
    text = path.read_text(encoding="utf-8")
    if not text.startswith("---\n"):
        return False
    end = text.index("\n---\n", 4)
    fm_text = text[4:end]
    body = text[end + 5 :]
    data = yaml.safe_load(fm_text)
    if data.get("status") == true_status:
        return False
    data["status"] = true_status
    new_fm = yaml.dump(data, default_flow_style=False, sort_keys=False)
    path.write_text(f"---\n{new_fm}---\n{body}", encoding="utf-8")
    return True


def main() -> None:
    """Run the full remediation pass and print a summary."""
    manifest = yaml.safe_load(MANIFEST_PATH.read_text(encoding="utf-8"))
    batch_mapping = json.loads(BATCH_MAPPING_PATH.read_text(encoding="utf-8"))
    slug_map = {**batch_mapping, **SUPPLEMENTARY_SLUG_MAP}

    true_status_by_id = compute_true_status_by_task_id()

    dep_resolved, unresolved = resolve_dependencies(manifest, slug_map)
    status_corrections = correct_manifest_statuses(manifest, true_status_by_id)

    MANIFEST_PATH.write_text(
        yaml.dump(manifest, default_flow_style=False, sort_keys=False),
        encoding="utf-8",
    )

    md_corrections = []
    for task_id, task in manifest.get("tasks", {}).items():
        if correct_task_md_frontmatter(task_id, task["status"]):
            md_corrections.append(task_id)

    print(f"manifest.yaml status corrections ({len(status_corrections)}):")
    for task_id, old, new in status_corrections:
        print(f"  {task_id}: {old} -> {new}")

    print(f"\ntask .md frontmatter status corrections ({len(md_corrections)}):")
    for task_id in md_corrections:
        print(f"  {task_id}")

    print(f"\nstale dependency slugs resolved: {dep_resolved}")
    if unresolved:
        print(f"UNRESOLVED slugs (no mapping found, left as-is): {unresolved}")
    else:
        print("no unresolved slugs remaining")


if __name__ == "__main__":
    sys.exit(main())
