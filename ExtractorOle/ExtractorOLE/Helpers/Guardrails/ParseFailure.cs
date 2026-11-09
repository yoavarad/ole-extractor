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
