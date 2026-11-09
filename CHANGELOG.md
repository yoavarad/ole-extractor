# Changelog

## Unreleased

### Added
- `DocumentExtractionResult.FieldErrors` (`List<FieldError>`): one entry per result leaf that could not be populated while the call succeeded, with `FieldPath`, `Code` (`FieldErrorCode`: `CorruptFile`, `OversizedNestedContent`, `UnexpectedFieldFailure`) and `Message`. Empty, never null, when every leaf was populated. Additive: nothing existing changes type. See [ADR-010](docs/adrs/010-partial-results-with-per-field-error-reporting.md).
- `MainExtractor.Extract(ExtractionRequest, CancellationToken)`: a new overload; `Extract(ExtractionRequest)` is unchanged and calls it with `CancellationToken.None`. A cancelled token throws `OperationCanceledException` before the input is opened. A token cancelled during the call is checked at each stage boundary and in the built-in sheet, slide and embedded-file loops. Cancellation is never recorded as a field error. The legacy .doc/.ppt conversion (b2xtranslator) has no cancellation hook, so the token is checked only before and after each conversion stage. Custom `IOpenStrategy`/`ITextExtractor` implementations are not given the token; it is checked before and after them.

### Changed
- `ExtractManyAsync` now passes its cancellation token to each in-flight `Extract`. Cancelling it fails the batch with `OperationCanceledException`; an item that observes the cancellation is not reported as a `BatchExtractionResult.Error`.

### Breaking changes
- `DetectedFormatEnum` is renamed to `DetectedFormat`, and `OfficeMimeTypeEnum` is renamed to `OfficeMimeType` (CA1711). The values are unchanged. Rename the types where you use them.
- A null required request field now throws `ArgumentNullException` with `ParamName = "request"` (CA2208). Before, `ParamName` was the property name (`"FileBytes"`, `"FileName"` or `"DetectedMimeType"`). The exception message now names the property. This applies to both `Extract` and `DetectMimeType`. Code that read `ParamName` should check `Message` instead.
- `ExtractionGuardrails` now takes an optional `ILogger<ExtractionGuardrails>` instead of an `Action<string>` rejection callback (#280). Guardrail rejections no longer write to the console: with no logger they are silent, and `ServiceRegistration.Register` uses the host's `ILogger<ExtractionGuardrails>` when logging is registered. Rejection log entries are `Warning` level with structured sizes and limits and no longer include the caller's `FileName`. Pass an `ILogger` where you passed a callback.

See [ADR-009](docs/adrs/009-analyzer-compliance-over-api-stability.md) for the reasoning and migration table.
