using System.Text;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Corrupts exactly one named entry's compressed data inside an
    /// otherwise-valid zip (docx/xlsx/pptx) package, in place, without
    /// touching any other byte's offset. Used to build test fixtures for
    /// "one embedded part is unreadable but the rest of the package opens
    /// fine" scenarios: <see cref="System.IO.Packaging"/> (what
    /// DocumentFormat.OpenXml's WordprocessingDocument.Open uses internally)
    /// only reads Central Directory metadata eagerly at open time - it
    /// doesn't decompress/CRC-validate an entry until that entry's stream is
    /// actually read to completion - so the corrupted entry's part still
    /// enumerates fine but throws when its stream is read.
    /// </summary>
    public static class ZipEntryCorruptor
    {
        private const uint EndOfCentralDirectorySignature = 0x06054b50;
        private const uint CentralDirectoryFileHeaderSignature = 0x02014b50;
        private const uint LocalFileHeaderSignature = 0x04034b50;

        /// <summary>
        /// Returns a copy of <paramref name="zipBytes"/> with every byte of
        /// the compressed data for the entry named <paramref name="entryName"/>
        /// bitwise-complemented, leaving all other bytes (including the
        /// Central Directory) untouched.
        /// </summary>
        public static byte[] CorruptEntryData(byte[] zipBytes, string entryName)
        {
            ArgumentNullException.ThrowIfNull(zipBytes);
            ArgumentException.ThrowIfNullOrEmpty(entryName);

            var bytes = (byte[])zipBytes.Clone();

            var eocdOffset = FindEndOfCentralDirectory(bytes);
            var centralDirectoryOffset = ReadUInt32(bytes, eocdOffset + 16);
            var totalEntries = ReadUInt16(bytes, eocdOffset + 10);

            var (compressedSize, localHeaderOffset) = FindCentralDirectoryEntry(bytes, (int)centralDirectoryOffset, totalEntries, entryName);

            var dataStart = ComputeLocalFileDataStart(bytes, (int)localHeaderOffset);

            for (var i = 0; i < compressedSize; i++)
            {
                var pos = dataStart + i;
                bytes[pos] = (byte)~bytes[pos];
            }

            return bytes;
        }

        private static int FindEndOfCentralDirectory(byte[] bytes)
        {
            // EOCD is at the very end, save for an optional trailing comment
            // (max 65535 bytes), so scan backward from the tail.
            var minOffset = Math.Max(0, bytes.Length - 22 - 65535);
            for (var i = bytes.Length - 22; i >= minOffset; i--)
            {
                if (ReadUInt32(bytes, i) == EndOfCentralDirectorySignature)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("End of Central Directory record not found - not a valid zip.");
        }

        private static (uint compressedSize, uint localHeaderOffset) FindCentralDirectoryEntry(byte[] bytes, int centralDirectoryOffset, int totalEntries, string entryName)
        {
            var offset = centralDirectoryOffset;
            for (var i = 0; i < totalEntries; i++)
            {
                if (ReadUInt32(bytes, offset) != CentralDirectoryFileHeaderSignature)
                {
                    throw new InvalidOperationException($"Expected Central Directory File Header at offset {offset}.");
                }

                var compressedSize = ReadUInt32(bytes, offset + 20);
                var filenameLength = ReadUInt16(bytes, offset + 28);
                var extraFieldLength = ReadUInt16(bytes, offset + 30);
                var commentLength = ReadUInt16(bytes, offset + 32);
                var localHeaderOffset = ReadUInt32(bytes, offset + 42);

                var filename = Encoding.UTF8.GetString(bytes, offset + 46, filenameLength);

                if (filename == entryName)
                {
                    return (compressedSize, localHeaderOffset);
                }

                offset += 46 + filenameLength + extraFieldLength + commentLength;
            }

            throw new InvalidOperationException($"Zip entry '{entryName}' not found in Central Directory.");
        }

        private static int ComputeLocalFileDataStart(byte[] bytes, int localHeaderOffset)
        {
            if (ReadUInt32(bytes, localHeaderOffset) != LocalFileHeaderSignature)
            {
                throw new InvalidOperationException($"Expected Local File Header at offset {localHeaderOffset}.");
            }

            var filenameLength = ReadUInt16(bytes, localHeaderOffset + 26);
            var extraFieldLength = ReadUInt16(bytes, localHeaderOffset + 28);

            return localHeaderOffset + 30 + filenameLength + extraFieldLength;
        }

        private static uint ReadUInt32(byte[] bytes, int offset) =>
            (uint)(bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24));

        private static ushort ReadUInt16(byte[] bytes, int offset) =>
            (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
    }
}
