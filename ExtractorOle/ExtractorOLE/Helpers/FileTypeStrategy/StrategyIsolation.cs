using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.Guardrails;
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

        // A library rejection of the container bytes that may still leave part of the file readable
        // (the .xls fallback). Reuses ParseFailure so a caller-stream or resource fault (out-of-memory,
        // disposed, cancelled, own typed errors) is never absorbed; an encrypted document is left to
        // ParseTimeErrorGuard (password-protected).
        internal static bool IsOpenFailure(Exception ex) =>
            ex is not NPOI.EncryptedDocumentException && !ParseFailure.IsB2xEncryptedDocument(ex) && ParseFailure.IsParseFailure(ex);
    }
}
