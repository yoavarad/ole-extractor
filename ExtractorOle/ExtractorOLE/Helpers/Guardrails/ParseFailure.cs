using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Input;
using System;

namespace ExtractorOLE.Helpers.Guardrails
{
    // Tells a format library's parse failure (corrupt input, [ydk:error:extraction/corrupt-file])
    // apart from a failure that is not about the bytes (#287): out-of-memory, a disposed or
    // cancelled operation, one of this library's own typed errors, or a read fault of the
    // caller's stream. Only a parse failure may be reported as corrupt-file.
    internal static class ParseFailure
    {
        // b2xtranslator's typed encrypted-document signal (doc and ppt conversion). Defence in depth:
        // the legacy-encryption guard keeps encrypted files away from b2xtranslator, so this only
        // fires if that detection is bypassed. It is a password outcome, never a parse failure or a
        // per-field error.
        public static bool IsB2xEncryptedDocument(Exception ex) =>
            ex is b2xtranslator.StructuredStorage.Common.EncryptedDocumentException;

        public static bool IsParseFailure(Exception ex, FaultTrackingStream? source = null)
        {
            if (source?.Fault != null) return false;

            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is OutOfMemoryException or ObjectDisposedException or OperationCanceledException
                    or InsufficientExecutionStackException or ExtractorOleException)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
