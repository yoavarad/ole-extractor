using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using NPOI.POIFS.FileSystem;
using NPOI.POIFS.NIO;

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
                PresizeBackingStore(destination, sourceRoot, rootStreams);
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

                // WriteFileSystem syncs the in-memory FAT/directory state into the backing array; the
                // copy to Stream.Null is free, and the exact-size copy below replaces a growing MemoryStream.
                destination.WriteFileSystem(Stream.Null);
                if (destination.Data is ByteArrayBackedDataSource backing)
                {
                    return backing.GetBackingArray().AsSpan(0, (int)backing.Size).ToArray();
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

        // A fresh NPOIFSFileSystem starts with a 1536-byte array that grows by only 25% per write, so
        // the discarded intermediates sum to several times the output. Swap in one array sized for the
        // expected result (document bytes padded to blocks, plus slack for FAT, directory and mini
        // stream). An undershoot is harmless: the data source still extends itself when it runs out.
        private static void PresizeBackingStore(
            NPOIFSFileSystem destination,
            DirectoryNode sourceRoot,
            IReadOnlyDictionary<string, byte[]?> rootStreams)
        {
            if (destination.Data is not ByteArrayBackedDataSource initial)
            {
                return;
            }

            const int BlockSize = 512;
            long estimate = 0;
            var entries = sourceRoot.Entries;
            while (entries.MoveNext())
            {
                if (!rootStreams.ContainsKey(entries.Current.Name))
                {
                    estimate += PaddedSize(entries.Current);
                }
            }

            foreach (var bytes in rootStreams.Values)
            {
                if (bytes is not null)
                {
                    estimate += (((long)bytes.Length + BlockSize - 1) / BlockSize) * BlockSize;
                }
            }

            estimate += estimate / 8 + 16 * 1024;
            if (estimate <= initial.Size || estimate > Array.MaxLength)
            {
                return;
            }

            var buffer = new byte[estimate];
            Array.Copy(initial.GetBackingArray(), buffer, (int)initial.Size);
            destination.Data = new ByteArrayBackedDataSource(buffer, (int)initial.Size);
        }

        private static long PaddedSize(Entry entry)
        {
            const int BlockSize = 512;
            if (entry is DocumentNode document)
            {
                return (((long)document.Size + BlockSize - 1) / BlockSize) * BlockSize;
            }

            long total = 0;
            if (entry is DirectoryNode directory)
            {
                var children = directory.Entries;
                while (children.MoveNext())
                {
                    total += PaddedSize(children.Current);
                }
            }

            return total;
        }
    }
}
