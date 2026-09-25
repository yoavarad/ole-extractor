# ADR-006: GitHub Issues as the YDK task backend

## Status
Accepted (2026-09-25)

## Context
Until now YDK task management ran on the local backend (`project.remote: local`): epics,
stories and tasks were Markdown files under `.ydk/epics`, `.ydk/stories` and `.ydk/tasks`,
plus a `.ydk/manifest.yaml` index and `.ydk/batch-mapping.json` for batch-created IDs. That
store kept drifting from reality:

- `ydk task done` never wrote `status: done` reliably, so `manifest.yaml` and the `.md`
  frontmatter disagreed with each other and with merged-PR state. The dependency gate reads
  `manifest.yaml`, so `ydk task ready`/`start` reported wrong results.
- `batch-mapping.json` got overwritten, not merged, and stale placeholder slugs stayed in
  `manifest.yaml` dependency lists. A hand-written `.ydk/scripts/remediate_task_status.py`
  was needed to repair all of this.
- `.github/workflows/task-sync.yml` ran `ydk task sync` after every PR merge and committed
  `.ydk/` changes straight to main. Before it existed, 37 of the first 228 commits were
  status-sync churn. Even with the workflow, manual `chore(tasks)` commits were still needed
  to fix stale statuses and broken task-file frontmatter.

## Decision
- Set `project.remote: github`. Epics, stories and tasks are GitHub issues on
  `yoavarad/ole-extractor` (labels `epic` / `story` / `task`). IDs are bare issue numbers.
- Fresh start, no import. The old local store is deleted: `.ydk/epics`, `.ydk/stories`,
  `.ydk/tasks`, `.ydk/manifest.yaml`, `.ydk/batch-mapping.json`, `.ydk/batches` and
  `.ydk/scripts/remediate_task_status.py`. Its full history stays in git at commit
  `36d03f9d67c9f916feaee8ef5e82027f6d7be9bf`.
- Remove `.github/workflows/task-sync.yml`. `ydk task done` puts `Closes #N` in the PR body,
  so GitHub closes the issue on merge and no sync step is needed.
- A standing epic #144 "Maintenance" with story #145 "Chores" holds chores and small
  maintenance tasks.
- `ydk task quick` is not allowed until ydk is patched to use the configured backend. It
  always writes local `.ydk/tasks/QD-*.md` files, whatever the backend. Create a task under
  story #145 instead.

## Alternatives Considered
- **Import the open epics/stories as issues**, or import all ~153 closed items too. Rejected:
  a lot of API churn and noise in the issue tracker for items that are already done. Git
  history already preserves them.
- **Hybrid: keep QD quick tasks local, everything else on GitHub.** Rejected: that keeps two
  task stores, and the local one is exactly the part that drifted.
- **Keep `task-sync.yml` and give it `issues: write`.** Rejected: once `Closes #N` closes the
  issue on merge, there is nothing left to sync, and the workflow was the main source of bot
  commits on main.

## Consequences
- One source of truth for task status: the GitHub issue's open/closed state and labels.
  No more manifest/frontmatter drift or sync commits.
- Task IDs share numbering with PRs.
- Known gaps in ydk's github backend (in the ydk repo, not fixed here):
  - `ydk task add-gate` / `check-gates`: gates are never parsed back from the issue.
  - `ydk task tdd`: the TDD stage is dropped.
  - `ydk task archive-done`: rewrites the issue body. Do not use.
  - `ydk task list --status in-progress|done` and `ydk task list --epic` return empty. Use
    `gh issue list --label ...` instead.
  - `ydk task list` shows only `task`-labelled issues (no epics or stories).
  - `ydk task quick` writes local files (see Decision).
- `.claude/hooks/check-task-complete.sh:12` reads `['task_id']`, but `active-task.json` has
  the shape `{"tasks": {...}}`. So the SubagentStop hook never finds a task and is effectively
  dead. This is noted here and not fixed by this change.
- Old `T-xxxxxxxx` / `QD-xxxxxx` IDs in existing docs, reports and ADRs stay as historical
  references. They resolve through git history at the commit above, not to GitHub issues.
