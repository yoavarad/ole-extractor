## Summary

Wired the public `DetectMimeType(MimeDetectionRequest)` facade in `IExtractionHelper`/`ExtractionHelper` to resolve and dispatch to the OOXML and CFB detection components via the existing DI-registered `_detectionRegistry`. It iterates the registered `IMimeTypeDetector` instances (OOXML then CFB, per existing registration order) and returns the first non-Unknown `MimeDetectionResult`, else an Unknown fallback (`MimeType=application/octet-stream`, `IsSupported=false`). Throws `ArgumentNullException` immediately for a null request or null `request.FileBytes`. The facade contains no format-specific parsing logic — pure orchestration, a thin non-DI-aware entry point. Followed TDD (guard.py enforced): added 5 new xUnit tests first (red — compile failure since the method didn't exist), then implemented (green).

## Test Plan

- `dotnet test` on `ExtractorOle.slnx` — 134 tests run, 134 passed, 0 failed, 0 skipped, including the 8 tests in `ExtractionHelperDetectMimeTypeTests` (OOXML match, XLS/CFB match, garbage bytes -> Unknown, null request throws, null FileBytes throws).
- Verified independently: developer run, separate unit-tester subagent run, and `ydk verify run`'s `dotnet-test` plugin — all green.

```console
dotnet test ExtractorOle/ExtractorOle.slnx
Passed!  - Failed: 0, Passed: 134, Skipped: 0, Total: 134
```