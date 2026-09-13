using System.Buffers.Binary;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Builds the corrupt-container and truncated-container members of
    /// docs/specs/dataset-curation.md's shared adversarial set, derived from
    /// an existing valid zip-based (docx/xlsx/pptx) sample - per T-3bddbe75's
    /// "derived from valid synthetic samples where practical" scope.
    /// </summary>
    public static class CorruptedContainerSampleGenerator
    {
        private const uint EndOfCentralDirectorySignature = 0x06054b50;

        /// <summary>
        /// Bitwise-complements every byte from the start of the ZIP Central
        /// Directory to the end of the file, destroying both the Central
        /// Directory and the End Of Central Directory (EOCD) record while
        /// leaving every local file header and compressed part byte before
        /// that offset untouched. The result still starts with the
        /// PK\x03\x04 local file header signature (passes a lightweight
        /// structural sniff) but has no locatable Central Directory/EOCD, so
        /// full parsing fails - ydk:error:extraction/corrupt-file.
        /// </summary>
        public static byte[] CorruptCentralDirectory(byte[] validZipBytes)
        {
            var centralDirectoryOffset = (int)ReadCentralDirectoryOffset(validZipBytes);

            var bytes = (byte[])validZipBytes.Clone();
            for (var i = centralDirectoryOffset; i < bytes.Length; i++)
            {
                bytes[i] = (byte)~bytes[i];
            }

            return bytes;
        }

        /// <summary>
        /// Keeps only the first <paramref name="headBytes"/> bytes plus the
        /// original, unmodified End Of Central Directory (EOCD) record,
        /// dropping everything in between. The EOCD still declares the
        /// original Central Directory offset/size, which now point past the
        /// truncated file's actual length -
        /// ydk:error:extraction/truncated-container.
        /// </summary>
        public static byte[] TruncateKeepingEocd(byte[] validZipBytes, int headBytes)
        {
            var eocdOffset = FindEndOfCentralDirectory(validZipBytes);
            var head = validZipBytes[..headBytes];
            var eocd = validZipBytes[eocdOffset..];
            return [.. head, .. eocd];
        }

        private static uint ReadCentralDirectoryOffset(byte[] bytes)
        {
            var eocdOffset = FindEndOfCentralDirectory(bytes);
            return BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(eocdOffset + 16, 4));
        }

        private static int FindEndOfCentralDirectory(byte[] bytes)
        {
            // EOCD is at the very end, save for an optional trailing comment
            // (max 65535 bytes), so scan backward from the tail.
            var minOffset = Math.Max(0, bytes.Length - 22 - 65535);
            for (var i = bytes.Length - 22; i >= minOffset; i--)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i, 4)) == EndOfCentralDirectorySignature)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("End of Central Directory record not found - not a valid zip.");
        }
    }
}
