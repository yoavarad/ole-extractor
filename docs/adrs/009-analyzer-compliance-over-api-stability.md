# ADR-009: Fix analyzer findings instead of suppressing them, even when it breaks the public API

## Status
Accepted. 2026-10-26. Task #259 (adopt ydk 1.7.0 .NET analyzers).

## Context
Turning on the .NET analyzers (#259) flagged two public-API issues:
- **CA1711**: the public enums `DetectedFormatEnum` and `OfficeMimeTypeEnum` end in `Enum`.
- **CA2208**: `MainExtractor.Extract` and `DetectMimeType` threw `ArgumentNullException` with
  `ParamName` set to a request property (`"FileBytes"`, `"FileName"`, `"DetectedMimeType"`),
  which is not a parameter of the method.

The first draft of #259 suppressed both to keep the API unchanged. The project owner rejected that:
breaking the public API is fine if the change is documented and fixed everywhere in the repo
(code, tests, docs). Following the guidelines matters more here than keeping the API stable.

## Decision
- Rename `DetectedFormatEnum` to `DetectedFormat` and `OfficeMimeTypeEnum` to `OfficeMimeType`.
  `MimeDetectionResult.DetectedFormat` is now of type `DetectedFormat`. A property named after its
  type is allowed and recommended by the .NET Framework Design Guidelines.
- An argument exception's `ParamName` must be a real parameter of the method. When a request
  property is null, throw `ArgumentNullException(nameof(request), "<Type>.<Property> must not be null...")`.
  `ParamName` is `"request"` and the message names the property.
- Do not suppress naming or usage analyzer rules in production code. Fix the code instead.
  The only override kept is CA1707 in test projects, for `Method_Scenario_Result` test names.

## Breaking changes and migration
| Before | After | Migration |
|---|---|---|
| `ExtractorOLE.DTOs.DetectedFormatEnum` | `ExtractorOLE.DTOs.DetectedFormat` | Rename the type where you use it. The values are unchanged. |
| `ExtractorOLE.DTOs.OfficeMimeTypeEnum` | `ExtractorOLE.DTOs.OfficeMimeType` | Rename the type where you use it. The values are unchanged. |
| A null `ExtractionRequest.FileBytes`/`FileName`/`DetectedMimeType` threw `ArgumentNullException` with `ParamName` = property name | Same exception type, `ParamName` = `"request"`, and `Message` names the property | Code that branched on `ParamName` should check `Message` instead, or check the request before calling. |
| `DetectMimeType` with no input source threw with `ParamName` = `"FileBytes"` | `ParamName` = `"request"`, and `Message` names `FileBytes` | Same as above. |

The exception types are unchanged, so `catch (ArgumentNullException)` and `catch (ArgumentException)`
still work.

## Consequences
- Callers have to recompile and rename the two enum types.
- New code has to meet the analyzer rules without suppressions. Any future suppression needs its
  own ADR or reviewer sign-off.
