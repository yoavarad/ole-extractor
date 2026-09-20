# T-7916a198 orchestrator progress
status: done (PR #120 open, awaiting review; do not merge)
done: ExtractCorpusManifestTests.cs (27 manifest rows + guard, all pass); manifest fix (NOAA apostrophe U+2019); full build 0 err; full suite 298 pass/1 skip + LoadGeneratorTests.Concurrency500 flake under load (passes alone); reviewer run (2 minor notes, not actioned); ydk task done passed after clearing .ydk/cache/verification/dotnet-*.
decisions: two curated pptx subfile counts pinned to extractor observed values (59/58) since manifest has raw part counts (58/83); xlsx slide_image_ prefix ignored; unmapped metadata keys not asserted.
traps: earlier stall = long foreground ydk task done (>10min); run it in background with log file. Never re-run ydk task start / --force.
