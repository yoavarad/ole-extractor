# Project Conventions — ole-extractor

## Code exploration: use graphify, not raw file reads

This repo has a maintained knowledge graph at `graphify-out/` (built with `/graphify`). For any "where is X defined", "what calls Y", "how does Z fit together" question:

- Use `graphify query "<question>"` or `/graphify explain "<node>"` first.
- Keep the graph current: run `/graphify --update` after code changes land (per task/epic), not just once at project start.
- Fall back to plain Read/Grep only when the graph doesn't have the answer (e.g. content inside a function body it didn't extract).

This applies across sessions — any agent working in this repo should default to the graph, not ad-hoc exploration.

## YDK workflow

This project follows the YDK development lifecycle (Stage 01 brainstorming → 01.5 ignition → 02 task management → 03 execution → 04 learning). See the project plan and `.ydk/` once initialized. Stage 04 (learning) runs after every epic closes, not just at project end.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
