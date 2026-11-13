using System.Buffers.Binary;
using System.Text;
using ExtractorOLE.Exceptions;
using NPOI.POIFS.Crypt.CryptoAPI;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

internal sealed record DecryptedPropertyStream(string Name, byte[] Data);

// [ydk:req:extraction/legacy-decryption] [ydk:req:extraction/legacy-encryption-header-bounds]
// Bounded reader for the RC4 CryptoAPI encrypted property stream (MS-OFFCRYPTO 2.3.5.3-2.3.5.4):
// "EncryptedSummary" in a ppt, "encryption" in an xls / doc. NPOI's GetDataStream(DirectoryNode) sizes
// an array from the unvalidated descriptor count, so this reader validates every field first.
internal static class CryptoApiEncryptedSummaryReader
{
    internal const string PptStreamName = "EncryptedSummary";
    internal const string DocXlsStreamName = "encryption";

    // 16 fixed bytes + 2-byte name terminator (POI STREAM_DESCRIPTOR_MIN_SIZE).
    internal const int MinDescriptorSize = 18;

    private const int HeaderSize = 8;
    private const int CountSize = 4;
    private const int DescriptorFixedSize = 16;
    private const int StreamFlag = 0x01;

    // [ydk:error:extraction/corrupt-file] Returns only stream descriptors; storage descriptors are skipped.
    internal static IReadOnlyList<DecryptedPropertyStream> Read(
        byte[] encryptedSummary, CryptoAPIDecryptor decryptor, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(encryptedSummary);
        ArgumentNullException.ThrowIfNull(decryptor);
        ArgumentException.ThrowIfNullOrEmpty(detectedMimeType);

        if (encryptedSummary.Length < HeaderSize)
        {
            throw Corrupt(detectedMimeType, "EncryptedSummary header is truncated");
        }

        var header = Decrypt(encryptedSummary, 0, HeaderSize, 0, decryptor);
        var arrayOffset = BinaryPrimitives.ReadUInt32LittleEndian(header);
        var arraySize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
        if (arrayOffset < HeaderSize
            || arraySize < CountSize
            || (ulong)arrayOffset + arraySize > (ulong)encryptedSummary.Length)
        {
            throw Corrupt(detectedMimeType, "EncryptedSummary streamDescriptorArray offset or size is out of range");
        }

        // Rule 2 bounds offset and size by the stream length, so the int casts below cannot truncate.
        var array = Decrypt(encryptedSummary, (int)arrayOffset, (int)arraySize, 0, decryptor);
        var count = BinaryPrimitives.ReadUInt32LittleEndian(array);
        if (count > (arraySize - CountSize) / MinDescriptorSize)
        {
            throw Corrupt(detectedMimeType, "EncryptedSummary streamDescriptorCount exceeds the descriptor array");
        }

        var streams = new List<DecryptedPropertyStream>();
        var position = CountSize;
        var budget = (long)arrayOffset - HeaderSize;
        for (uint i = 0; i < count; i++)
        {
            if (array.Length - position < DescriptorFixedSize)
            {
                throw Corrupt(detectedMimeType, "EncryptedSummary stream descriptor runs past the descriptor array");
            }

            var streamOffset = BinaryPrimitives.ReadUInt32LittleEndian(array.AsSpan(position));
            var streamSize = BinaryPrimitives.ReadUInt32LittleEndian(array.AsSpan(position + 4));
            var block = BinaryPrimitives.ReadUInt16LittleEndian(array.AsSpan(position + 8));
            var nameChars = array[position + 10];
            var flags = array[position + 11];
            position += DescriptorFixedSize;

            var nameBytes = nameChars * 2;
            if (array.Length - position < nameBytes + 2)
            {
                throw Corrupt(detectedMimeType, "EncryptedSummary stream descriptor name runs past the descriptor array");
            }

            var name = Encoding.Unicode.GetString(array, position, nameBytes);
            position += nameBytes + 2;

            if ((flags & StreamFlag) == 0)
            {
                continue;
            }

            // Both values are bounded by arrayOffset (<= stream length) once this check passes, so the int casts are safe.
            if (streamOffset < HeaderSize || (ulong)streamOffset + streamSize > arrayOffset)
            {
                throw Corrupt(detectedMimeType, "EncryptedSummary StreamOffset or StreamSize is out of range");
            }

            budget -= streamSize;
            if (budget < 0)
            {
                throw Corrupt(detectedMimeType, "EncryptedSummary StreamSize values exceed the stream body (overlapping descriptors)");
            }

            streams.Add(new DecryptedPropertyStream(
                name, Decrypt(encryptedSummary, (int)streamOffset, (int)streamSize, block, decryptor)));
        }

        return streams;
    }

    private static byte[] Decrypt(byte[] source, int offset, int length, int block, CryptoAPIDecryptor decryptor)
    {
        var buffer = source.AsSpan(offset, length).ToArray();
        var cipher = decryptor.InitCipherForBlock(null, block);
        cipher.Update(buffer, 0, length, buffer);
        return buffer;
    }

    private static CorruptFileException Corrupt(string mime, string reason) => new(mime, reason);
}
