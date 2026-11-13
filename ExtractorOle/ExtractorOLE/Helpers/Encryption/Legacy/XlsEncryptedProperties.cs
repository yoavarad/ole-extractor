using System.Buffers.Binary;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using NPOI.HPSF;
using NPOI.POIFS.Crypt.CryptoAPI;
using NPOI.POIFS.FileSystem;
using NPOI.Util;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

// [ydk:req:extraction/legacy-decryption]
// An xls saved with RC4 CryptoAPI and "encrypt document properties" keeps its summary property sets in the
// "encryption" stream (MS-OFFCRYPTO 2.3.5.4) instead of the plain "\u0005SummaryInformation" stream, so NPOI
// leaves SummaryInformation null. This reads the SummaryInformation back out of it.
internal static class XlsEncryptedProperties
{
    private const string SummaryStreamName = "\u0005SummaryInformation";
    private const ushort FilePassSid = 0x002F;
    private const ushort EofSid = 0x000A;
    private const ushort EncryptionTypeOther = 1;
    private const ushort MinCryptoApiMajor = 2;
    private const int RecordHeaderSize = 4;
    private const int MaxRecordsScanned = 8;
    private const int MaxRecordDataSize = 8224;
    private static readonly string[] WorkbookStreamNames = { "Workbook", "Book" };

    // True when the file has encrypted properties and no plain SummaryInformation: the only case this serves.
    internal static bool AppliesTo(DirectoryNode root) =>
        root.HasEntry(CryptoApiEncryptedSummaryReader.DocXlsStreamName) && !root.HasEntry(SummaryStreamName);

    // Returns null when the workbook is not RC4 CryptoAPI encrypted (no FILEPASS, XOR, or binary RC4).
    // Throws CorruptFileException for a CryptoAPI header NPOI cannot use.
    internal static LegacyEncryptionHeader? ReadFilePassHeader(DirectoryNode root, string detectedMimeType)
    {
        var data = ReadFilePassData(root);
        if (data is null
            || data.Length < 6
            || BinaryPrimitives.ReadUInt16LittleEndian(data) != EncryptionTypeOther
            || BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(2)) < MinCryptoApiMajor)
        {
            return null;
        }

        // The xls FILEPASS payload is the EncryptionInfo after the 2-byte type. Container Ppt makes an
        // unsupported variant a CorruptFileException, which the caller isolates, instead of a password error.
        return LegacyEncryptionHeaderReader.Parse(data.AsSpan(2), LegacyContainer.Ppt, detectedMimeType);
    }

    // Applies the decrypted SummaryInformation to result's Metadata. Returns without effect when the
    // password does not verify: the workbook was already opened with it, so this is not expected, and
    // metadata is best effort. Unreadable encrypted streams throw CorruptFileException.
    internal static void Apply(
        DirectoryNode root, LegacyEncryptionHeader header, string password, string detectedMimeType, DocumentExtractionResult result)
    {
        if (LegacyEncryptionHeaderReader.CreateDecryptor(header) is not CryptoAPIDecryptor decryptor)
        {
            throw new CorruptFileException(detectedMimeType, "FILEPASS header is not a CryptoAPI encryption header");
        }

        if (!LegacyEncryptionHeaderReader.VerifyPassword(decryptor, password))
        {
            return;
        }

        var encrypted = ReadStream(root, CryptoApiEncryptedSummaryReader.DocXlsStreamName);
        var streams = CryptoApiEncryptedSummaryReader.Read(encrypted, decryptor, detectedMimeType);
        var summaryStream = streams.FirstOrDefault(s => string.Equals(s.Name, SummaryStreamName, StringComparison.Ordinal));
        if (summaryStream is null)
        {
            return;
        }

        SummaryInformation summary;
        try
        {
            summary = PropertySetFactory.Create(new ByteArrayInputStream(summaryStream.Data)) as SummaryInformation
                ?? throw new InvalidDataException("The encrypted SummaryInformation stream is not a summary property set.");
        }
        catch (Exception ex) when (ParseFailure.IsParseFailure(ex))
        {
            throw new CorruptFileException(
                detectedMimeType,
                $"encrypted SummaryInformation stream is unreadable ({ex.GetType().Name})",
                ex);
        }

        MetadataConversions.Apply(summary, result);
    }

    private static byte[] ReadStream(DirectoryNode root, string name)
    {
        var entry = (DocumentEntry)root.GetEntry(name);
        var buffer = new byte[entry.Size];
        using var input = root.CreateDocumentInputStream(entry);
        input.ReadFully(buffer);
        return buffer;
    }

    // The FILEPASS record directly follows the BOF record of the workbook globals; only the first few records are read.
    private static byte[]? ReadFilePassData(DirectoryNode root)
    {
        var name = WorkbookStreamNames.FirstOrDefault(root.HasEntry);
        if (name is null)
        {
            return null;
        }

        using var input = root.CreateDocumentInputStream(name);
        var header = new byte[RecordHeaderSize];
        for (var i = 0; i < MaxRecordsScanned && input.Available() >= RecordHeaderSize; i++)
        {
            input.ReadFully(header);
            var sid = BinaryPrimitives.ReadUInt16LittleEndian(header);
            var length = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(2));
            if (length > MaxRecordDataSize || length > input.Available())
            {
                return null;
            }

            var data = new byte[length];
            input.ReadFully(data);
            if (sid == FilePassSid)
            {
                return data;
            }

            if (sid == EofSid)
            {
                return null;
            }
        }

        return null;
    }
}
