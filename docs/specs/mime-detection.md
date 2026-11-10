# MIME Detection

## Purpose

[ydk:contract:extraction/detect-mime-type] answers one question: given raw bytes, what exact Office format is this — without ever looking at the filename to decide. [ydk:req:extraction/content-only-mime-detection] is the governing rule; filename travels through [ydk:entity:extraction/MimeDetectionRequest] only for logging/traceability.

## How Detection Works

Two independent structural checks, dispatched by the file's leading bytes: one for the ZIP-based OOXML family, one for the compound-file-based legacy family. Neither performs a full document parse — both inspect only the container's own directory structure (which parts/streams it declares) before deciding.

Anything matching neither check returns the Unknown result — this is a normal, expected outcome, not a failure. See [ydk:entity:extraction/MimeDetectionResult].

## Macro-Enabled Variants

A macro-enabled file (docm/xlsm/pptm) is structurally distinguishable from its non-macro sibling — detection can and does see the difference internally. The detected-format value nonetheless stays the same as the base format (a docm detects as Docx, not as a separate enum value) so the six-format contract doesn't grow a parallel macro axis. The distinguishing information isn't discarded: it surfaces as an explicit macro-presence flag on the format-specific metadata produced by extraction, not on the detection result itself.

## Encrypted Files

An encrypted docx, xlsx or pptx is not a ZIP. It is a CFB file that wraps an encrypted package, so its inner format cannot be seen without decrypting it. Detection therefore follows these rules, which are fixed in [ydk:req:extraction/password-decryption]:

- **A CFB file that holds an encrypted package is treated as an encrypted package**, whatever other entries it carries. Word, for example, writes a legacy compatibility stub beside the encrypted package; that stub no longer makes such a file detect as doc.
- **Without a password** the result is Unknown. Extraction of such a file fails with [ydk:error:extraction/password-protected].
- **With the correct password**, supplied on [ydk:entity:extraction/MimeDetectionRequest], the package is decrypted in memory. The decrypted bytes go through the same OOXML structural check as any unencrypted file, so the result is the inner format, or Unknown if the package matches none.
- **With a wrong password**, detection throws [ydk:error:extraction/invalid-password] rather than returning Unknown, so the caller's mistake is not hidden.
- **An encryption the library cannot decrypt** returns Unknown, with or without a password.
- **Encrypted doc, xls and ppt** keep their directory unencrypted, so they are detected from it exactly as before. The password is neither used nor checked for them; a wrong password surfaces only at extraction.
- **An unencrypted file with a password** is detected exactly as without one. The password is ignored, with no error.

The password is a decryption key, not a classification hint: content-only detection ([ydk:req:extraction/content-only-mime-detection]) is unchanged. It is never logged or echoed, per [ydk:req:extraction/password-confidentiality].

## Guardrails

Before either check inspects declared internal sizes, [ydk:nfr:extraction/max-file-size] and [ydk:nfr:extraction/nesting-depth-guard] apply — a byte array over the configured limit is rejected as [ydk:error:extraction/file-too-large] before any structural inspection begins, and a structurally-tiny file claiming an implausibly large internal size is rejected as [ydk:error:extraction/oversized-nested-content]. The same guards cover a decrypted package: the size it declares after decryption is checked against [ydk:nfr:extraction/nesting-depth-guard] before decryption allocates anything, and [ydk:error:extraction/file-too-large] is judged on the encrypted bytes as supplied. A zero-length input is never rejected by detection itself — it simply produces the Unknown result, since there's nothing to check against either structural signature.

## Extensibility

Detection dispatches through a registry keyed by structural signature, not an enumeration of exactly six formats — see [ydk:req:extraction/format-extensibility]. A future format is added by registering a new detection component, not by editing the core detection method.
