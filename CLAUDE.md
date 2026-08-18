# Project Conventions — ole-extractor

## Code exploration: use graphify, not raw file reads

This repo has a maintained knowledge graph at `graphify-out/` (built with `/graphify`). For any "where is X defined", "what calls Y", "how does Z fit together" question:

- Use `graphify query "<question>"` or `/graphify explain "<node>"` first.
- Keep the graph current: run `/graphify --update` after code changes land (per task/epic), not just once at project start.
- Fall back to plain Read/Grep only when the graph doesn't have the answer (e.g. content inside a function body it didn't extract).

This applies across sessions — any agent working in this repo should default to the graph, not ad-hoc exploration.

## YDK workflow

This project follows the YDK development lifecycle (Stage 01 brainstorming → 01.5 ignition → 02 task management → 03 execution → 04 learning). See the project plan and `.ydk/` once initialized. Stage 04 (learning) runs after every epic closes, not just at project end.
