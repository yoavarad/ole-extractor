using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ExtractorOLE.Helpers
{
    // [ydk:req:extraction/per-field-error-isolation]: the leaf-isolation primitive. Runs one
    // leaf's populate action; on failure it records one FieldError on the result and sends the
    // exception to ExtractionTelemetry only, so the result never carries exception details.
    internal static class FieldIsolation
    {
        internal const string EmbeddedFilesPath = nameof(DocumentExtractionResult.EmbeddedFiles);

        // "EmbeddedFiles[<package path>]", or the bare collection name when the path is unknown.
        internal static string SubfilePath(string? packagePath) =>
            string.IsNullOrEmpty(packagePath) ? EmbeddedFilesPath : $"{EmbeddedFilesPath}[{packagePath}]";

        // Returns true when populate completed. On failure the leaf is left as populate left it;
        // callers must not write a placeholder. OutOfMemoryException is never caught, and
        // cancellation (OperationCanceledException) propagates instead of becoming a field error,
        // also when wrapped (see IsIsolatable).
        internal static bool TryPopulate(DocumentExtractionResult result, string fieldPath, Action populate)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentException.ThrowIfNullOrEmpty(fieldPath);
            ArgumentNullException.ThrowIfNull(populate);

            try
            {
                populate();
                return true;
            }
            catch (Exception ex) when (IsIsolatable(ex))
            {
                var code = Classify(ex);

                // One entry per distinct path (first failure wins); only the bare collection name
                // may repeat. Recorded before telemetry so a sink failure cannot lose the entry.
                // Telemetry still sees every failure, including a deduplicated one.
                if (fieldPath == EmbeddedFilesPath || !result.FieldErrors.Exists(e => e.FieldPath == fieldPath))
                {
                    result.FieldErrors.Add(new FieldError(fieldPath, code, MessageFor(fieldPath, code)));
                }
                ExtractionTelemetry.RecordFieldFailure(fieldPath, code, ex);
                return false;
            }
        }

        // A failure that may cost one leaf (or one part of a leaf) instead of the call: everything
        // except out-of-memory and cancellation, including when either is wrapped anywhere in the
        // exception's inner chain or in an AggregateException's inner exceptions (#288).
        internal static bool IsIsolatable(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            // A password outcome escapes as PASSWORD_PROTECTED, never a FieldError.
            if (Guardrails.ParseFailure.IsB2xEncryptedDocument(exception)) return false;

            var pending = new Stack<Exception>();
            pending.Push(exception);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current is OutOfMemoryException or OperationCanceledException) return false;
                if (current is AggregateException aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions) pending.Push(inner);
                }
                else if (current.InnerException != null)
                {
                    pending.Push(current.InnerException);
                }
            }

            return true;
        }

        // Chosen by cause, not by leaf. UnexpectedFieldFailure only when neither other code fits.
        // The CorruptFile set is deliberately conservative: exceptions that signal a malformed
        // record by type. FormatException is included because populate code parses source
        // values; generic faults (IndexOutOfRange, ArgumentOutOfRange, ...) stay residual since
        // they equally signal a bug in our own code.
        internal static FieldErrorCode Classify(Exception exception) => exception switch
        {
            OversizedNestedContentException => FieldErrorCode.OversizedNestedContent,
            CorruptFileException
                or InvalidDataException
                or EndOfStreamException
                or FormatException
                or XmlException
                or OpenXmlPackageException
                or OpenMcdf.FileFormatException
                or NPOI.Util.RecordFormatException => FieldErrorCode.CorruptFile,
            _ => FieldErrorCode.UnexpectedFieldFailure,
        };

        private static string MessageFor(string fieldPath, FieldErrorCode code) => code switch
        {
            FieldErrorCode.CorruptFile => $"{fieldPath} could not be read: its source record is unreadable or malformed.",
            FieldErrorCode.OversizedNestedContent => $"{fieldPath} was skipped: it exceeds the configured size or nesting limits.",
            _ => $"{fieldPath} could not be read because of an unexpected failure.",
        };
    }
}
