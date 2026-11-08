# Changelog

## Unreleased

### Added
- `DocumentExtractionResult.FieldErrors` (`List<FieldError>`): one entry per result leaf that could not be populated while the call succeeded, with `FieldPath`, `Code` (`FieldErrorCode`: `CorruptFile`, `OversizedNestedContent`, `UnexpectedFieldFailure`) and `Message`. Empty, never null, when every leaf was populated. Additive: nothing existing changes type. See [ADR-010](docs/adrs/010-partial-results-with-per-field-error-reporting.md).

### Breaking changes
- `DetectedFormatEnum` is renamed to `DetectedFormat`, and `OfficeMimeTypeEnum` is renamed to `OfficeMimeType` (CA1711). The values are unchanged. Rename the types where you use them.
- A null required request field now throws `ArgumentNullException` with `ParamName = "request"` (CA2208). Before, `ParamName` was the property name (`"FileBytes"`, `"FileName"` or `"DetectedMimeType"`). The exception message now names the property. This applies to both `Extract` and `DetectMimeType`. Code that read `ParamName` should check `Message` instead.

See [ADR-009](docs/adrs/009-analyzer-compliance-over-api-stability.md) for the reasoning and migration table.
