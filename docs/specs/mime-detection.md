# MIME Detection

## Purpose

[ydk:contract:extraction/detect-mime-type] answers one question: given raw bytes, what exact Office format is this — without ever looking at the filename to decide. [ydk:req:extraction/content-only-mime-detection] is the governing rule; filename travels through [ydk:entity:extraction/MimeDetectionRequest] only for logging/traceability.

## How Detection Works

Two independent structural checks, dispatched by the file's leading bytes: one for the ZIP-based OOXML family, one for the compound-file-based legacy family. Neither performs a full document parse — both inspect only the container's own directory structure (which parts/streams it declares) before deciding.

Anything matching neither check returns the Unknown result — this is a normal, expected outcome, not a failure. See [ydk:entity:extraction/MimeDetectionResult].

## Macro-Enabled Variants

A macro-enabled file (docm/xlsm/pptm) is structurally distinguishable from its non-macro sibling — detection can and does see the difference internally. The detected-format value nonetheless stays the same as the base format (a docm detects as Docx, not as a separate enum value) so the six-format contract doesn't grow a parallel macro axis. The distinguishing information isn't discarded: it surfaces as an explicit macro-presence flag on the format-specific metadata produced by extraction, not on the detection result itself.

## Guardrails

Before either check inspects declared internal sizes, [ydk:nfr:extraction/max-file-size] and [ydk:nfr:extraction/nesting-depth-guard] apply — a byte array over the configured limit is rejected as [ydk:error:extraction/file-too-large] before any structural inspection begins, and a structurally-tiny file claiming an implausibly large internal size is rejected as [ydk:error:extraction/oversized-nested-content]. A zero-length input is never rejected by detection itself — it simply produces the Unknown result, since there's nothing to check against either structural signature.

## Extensibility

Detection dispatches through a registry keyed by structural signature, not an enumeration of exactly six formats — see [ydk:req:extraction/format-extensibility]. A future format is added by registering a new detection component, not by editing the core detection method.
