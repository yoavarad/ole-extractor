using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using NPOI.POIFS.FileSystem;

namespace ExtractorOLE.Helpers.Encryption.Legacy
{
    // [ydk:req:extraction/legacy-plaintext-rebuild]
    // Rebuilds a compound file as a fresh NPOIFSFileSystem copy. A fresh filesystem plus recursive
    // copy is the only writer that works on every fixture: OpenMcdf CopyTo drops CLSIDs and writes
    // no mini stream, and NPOI in-place updates leave a too-long mini-stream FAT chain.
    internal static class PlainCompoundFileBuilder
    {
        // rootStreams: key = root-level stream name; value = new content (replace or add), or null = remove.
        // Every other entry is copied unchanged.
        internal static byte[] Build(
            DirectoryNode sourceRoot,
            IReadOnlyDictionary<string, byte[]?> rootStreams,
            long maxEntryBytes,
            string detectedMimeType)
        {
            ArgumentNullException.ThrowIfNull(sourceRoot);
            ArgumentNullException.ThrowIfNull(rootStreams);

            foreach (var (name, bytes) in rootStreams)
            {
                if (bytes is not null && bytes.Length > maxEntryBytes)
                {
                    throw new OversizedNestedContentException(string.Create(
                        CultureInfo.InvariantCulture,
                        $"rebuilt stream {name} is {bytes.Length} bytes (per-entry limit {maxEntryBytes})"));
                }
            }

            try
            {
                using var destination = new NPOIFSFileSystem();
                destination.Root.StorageClsid = sourceRoot.StorageClsid;

                var entries = sourceRoot.Entries;
                while (entries.MoveNext())
                {
                    ExtractionCancellation.ThrowIfCancellationRequested();
                    var entry = entries.Current;
                    if (rootStreams.ContainsKey(entry.Name))
                    {
                        continue;
                    }

                    EntryUtils.CopyNodeRecursively(entry, destination.Root);
                }

                foreach (var (name, bytes) in rootStreams)
                {
                    if (bytes is not null)
                    {
                        destination.Root.CreateDocument(name, new MemoryStream(bytes, writable: false));
                    }
                }

                using var output = new MemoryStream();
                destination.WriteFileSystem(output);
                return output.ToArray();
            }
            catch (Exception ex) when (ParseFailure.IsParseFailure(ex))
            {
                throw new CorruptFileException(
                    detectedMimeType,
                    $"plain compound file could not be rebuilt ({ex.GetType().Name})",
                    ex);
            }
        }
    }
}
