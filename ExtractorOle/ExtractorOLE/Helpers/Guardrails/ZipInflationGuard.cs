using ExtractorOLE.Exceptions;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace ExtractorOLE.Helpers.Guardrails
{
    /// <summary>
    /// Enforces the entry-count and inflated-byte caps on ZIP/OOXML input against what the
    /// archive ACTUALLY produces, not what attacker-written local headers declare. The EOCD
    /// entry count is checked before any ZipArchive open; then each entry is inflated through a
    /// counting loop that aborts as soon as the per-entry or total cap is crossed, so work and
    /// allocation stay bounded by the cap (nothing is retained).
    /// </summary>
    internal static class ZipInflationGuard
    {
        private const int EocdFixedLength = 22;
        private static readonly byte[] EocdSignature = { 0x50, 0x4B, 0x05, 0x06 };

        /// <summary>No-op when the stream is not a ZIP or is too corrupt to inspect (downstream handles it).</summary>
        internal static void Enforce(Stream stream, long maxEntries, long maxEntryBytes, long maxTotalBytes)
        {
            if (!stream.CanSeek) return;
            bool isZip = TryReadEntryCount(stream, out var entryCount);
            if (!isZip)
            {
                stream.Position = 0;
                return;
            }

            if (entryCount > maxEntries)
            {
                stream.Position = 0;
                throw new OversizedNestedContentException($"ZIP end-of-central-directory declares {entryCount} entries (limit {maxEntries})");
            }

            try
            {
                stream.Position = 0;
                using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                var buffer = new byte[81920];
                long total = 0;
                foreach (var entry in zip.Entries)
                {
                    long entryBytes = 0;
                    using var s = entry.Open();
                    int read;
                    while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        entryBytes += read;
                        total += read;
                        if (entryBytes > maxEntryBytes)
                            throw new OversizedNestedContentException($"ZIP entry inflated past {maxEntryBytes} bytes (per-entry limit {maxEntryBytes})");
                        if (total > maxTotalBytes)
                            throw new OversizedNestedContentException($"ZIP entries inflated past {maxTotalBytes} bytes total (limit {maxTotalBytes})");
                    }
                }
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or NotSupportedException or ArgumentException)
            {
                // Corrupt/unsupported archive: not a bomb we can measure; downstream open reports it.
            }
            finally
            {
                stream.Position = 0;
            }
        }

        private static bool TryReadEntryCount(Stream stream, out int entryCount)
        {
            entryCount = 0;
            long length = stream.Length;
            if (length < EocdFixedLength) return false;

            int tailLength = (int)Math.Min(length, EocdFixedLength + ushort.MaxValue);
            var tail = new byte[tailLength];
            stream.Position = length - tailLength;
            stream.ReadExactly(tail);

            for (int i = tailLength - EocdFixedLength; i >= 0; i--)
            {
                if (!tail.AsSpan(i, 4).SequenceEqual(EocdSignature)) continue;
                entryCount = BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(i + 10, 2));
                return true;
            }
            return false;
        }
    }
}
