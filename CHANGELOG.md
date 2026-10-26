# Changelog

## Unreleased

### Breaking changes
- `DetectedFormatEnum` is renamed to `DetectedFormat`, and `OfficeMimeTypeEnum` is renamed to `OfficeMimeType` (CA1711). The values are unchanged. Rename the types where you use them.
- A null required request field now throws `ArgumentNullException` with `ParamName = "request"` (CA2208). Before, `ParamName` was the property name (`"FileBytes"`, `"FileName"` or `"DetectedMimeType"`). The exception message now names the property. This applies to both `Extract` and `DetectMimeType`. Code that read `ParamName` should check `Message` instead.

See [ADR-009](docs/adrs/009-analyzer-compliance-over-api-stability.md) for the reasoning and migration table.
