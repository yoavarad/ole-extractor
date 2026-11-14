using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Legacy;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.Input;
using System.Runtime.ExceptionServices;
using NPOI.POIFS.FileSystem;

namespace ExtractorOLE.Helpers.Guardrails;

internal abstract record LegacyEncryptedFile;

internal sealed record EncryptedDoc(DocFibProbe Fib, LegacyEncryptionHeader Header) : LegacyEncryptedFile;

internal sealed record EncryptedPpt(PptEncryptionLayout Layout, LegacyEncryptionHeader Header) : LegacyEncryptedFile;

// XOR obfuscation Method 2 (MS-DOC 2.2.6.1): no encryption header; FibBase lKey is the password verifier.
internal sealed record XorObfuscatedDoc(DocFibProbe Fib) : LegacyEncryptedFile;

// [ydk:req:extraction/legacy-decryption] [ydk:error:extraction/password-protected]
// [ydk:error:extraction/corrupt-file] Stops an encrypted .doc/.ppt before its open strategy (and
// b2xtranslator) parses it. Reads only the FibBase / table-stream header of a .doc and the
// Current User / edit chain of a .ppt.
internal static class LegacyEncryptionGuard
{
    // Returns null for a plain file, a format other than WordLegacy/PowerPointLegacy, or a compound
    // file NPOI cannot read (the open strategy then reports it as before). Throws
    // PasswordProtectedException when the file is encrypted and cannot be decrypted with what the
    // request carries, and CorruptFileException when its encryption structure is malformed.
    internal static LegacyEncryptedFile? Inspect(ExtractionInput input, OfficeMimeType format, bool hasPassword, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (format is not (OfficeMimeType.WordLegacy or OfficeMimeType.PowerPointLegacy))
        {
            return null;
        }

        // NonClosingStream makes NPOI read the input block by block instead of buffering it. The
        // caller's stream is read through a FaultTrackingStream so that a read fault of that stream
        // is rethrown as-is, never taken for a damaged file (#287).
        using var bytesView = input.OriginalBytes != null ? new MemoryStream(input.OriginalBytes, writable: false) : null;
        var tracked = bytesView == null ? new FaultTrackingStream(input.Stream!) : null;
        var source = new NonClosingStream(bytesView ?? (Stream)tracked!);
        try
        {
            NPOIFSFileSystem fs;
            try
            {
                fs = CfbFileSystems.Open(source);
            }
            catch (Exception ex) when (ParseFailure.IsParseFailure(ex, tracked))
            {
                return null;
            }

            using (fs)
            {
                var result = format == OfficeMimeType.WordLegacy
                    ? (LegacyEncryptedFile?)InspectDoc(fs.Root, hasPassword, detectedMimeType, tracked)
                    : InspectPpt(fs.Root, hasPassword, detectedMimeType);
                // The probes classify unreadable streams themselves; a fault behind that is not a result.
                ThrowIfFaulted(tracked);
                return result;
            }
        }
        catch (Exception) when (tracked?.Fault != null)
        {
            ThrowIfFaulted(tracked);
            throw; // unreachable: ThrowIfFaulted throws when a fault is recorded; kept for flow analysis
        }
    }

    private static void ThrowIfFaulted(FaultTrackingStream? tracked)
    {
        if (tracked?.Fault != null)
        {
            ExceptionDispatchInfo.Throw(tracked.Fault);
        }
    }

    private static LegacyEncryptedFile? InspectDoc(DirectoryNode root, bool hasPassword, string detectedMimeType, FaultTrackingStream? tracked)
    {
        DocFibProbe? fib;
        try
        {
            fib = DocEncryptionProbe.ReadFib(root);
        }
        catch (Exception ex) when (ParseFailure.IsParseFailure(ex, tracked))
        {
            // An unreadable WordDocument stream is left to the open strategy (corrupt-file).
            return null;
        }

        if (fib == null || fib.Kind == DocEncryptionKind.NotEncrypted)
        {
            return null;
        }

        if (!hasPassword)
        {
            throw new PasswordProtectedException();
        }

        if (fib.Kind == DocEncryptionKind.Xor)
        {
            return new XorObfuscatedDoc(fib);
        }

        return new EncryptedDoc(fib, DocEncryptionProbe.ReadHeader(root, fib, detectedMimeType));
    }

    private static EncryptedPpt? InspectPpt(DirectoryNode root, bool hasPassword, string detectedMimeType)
    {
        var probe = PptEncryptionProbe.Probe(root);
        switch (probe.Outcome)
        {
            case PptProbeOutcome.NotEncrypted:
                return null;
            case PptProbeOutcome.EncryptedTokenStructureUnreadable when hasPassword:
                throw new CorruptFileException(detectedMimeType, "ppt encryption structure unreadable");
            case PptProbeOutcome.EncryptedTokenStructureUnreadable:
                throw new PasswordProtectedException();
        }

        if (!hasPassword)
        {
            throw new PasswordProtectedException();
        }

        var layout = probe.Layout!;
        var header = LegacyEncryptionHeaderReader.Parse(
            PptEncryptionProbe.CryptSessionPayload(layout, detectedMimeType), LegacyContainer.Ppt, detectedMimeType);
        return new EncryptedPpt(layout, header);
    }
}
