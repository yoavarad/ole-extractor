using ExtractorOLE.DTOs;
using System;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Shared by the Word/Excel open strategies ([ydk:req:extraction/per-field-error-isolation]).
    internal static class StrategyIsolation
    {
        // Field path of one FormatMetadata leaf, e.g. "FormatMetadata.SheetCount".
        internal static string FormatMetadataPath(string leaf) =>
            $"{nameof(DocumentExtractionResult.FormatMetadata)}.{leaf}";

        internal static bool PopulateLeaf(DocumentExtractionResult result, string leaf, Action populate) =>
            FieldIsolation.TryPopulate(result, FormatMetadataPath(leaf), populate);

        // The embedded walk is one leaf at the bare collection name; an unexpected throw costs
        // only the entries not yet added.
        internal static bool PopulateEmbedded(DocumentExtractionResult result, Action walk) =>
            FieldIsolation.TryPopulate(result, FieldIsolation.EmbeddedFilesPath, walk);

        // Declared container-open failures: the library rejected the bytes as malformed or
        // unsupported (maps to CorruptFile). Anything else at open is undeclared and propagates.
        // NullReferenceException is included because NPOI surfaces some garbage compound-file
        // directories that way. Encrypted-document exceptions are not in this set; they map to password-protected.
        internal static bool IsOpenFailure(Exception ex) =>
            FieldIsolation.Classify(ex) == FieldErrorCode.CorruptFile
            || ex is IOException
            || ex is ArgumentException
            || ex is NPOI.HSSF.OldExcelFormatException
            || ex is NullReferenceException;
    }
}
