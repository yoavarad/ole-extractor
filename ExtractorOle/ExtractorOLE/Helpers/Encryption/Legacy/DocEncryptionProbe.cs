using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using NPOI.POIFS.FileSystem;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

internal enum DocEncryptionKind { NotEncrypted, Xor, Rc4 }

internal sealed record DocFibProbe(DocEncryptionKind Kind, string TableStreamName, uint LKey);

// [ydk:req:extraction/legacy-decryption] [ydk:req:extraction/legacy-encryption-header-bounds]
// Classifies a legacy .doc from FibBase (MS-DOC 2.5.2) and reads the table-stream encryption header
// within bounds. Never reads more than the 68 clear FibBase bytes and lKey header bytes.
internal static class DocEncryptionProbe
{
    internal const int FibBaseLength = 68;
    internal const uint MaxLKey = 4096;

    private const string WordDocumentStream = "WordDocument";
    private const ushort WordIdent = 0xA5EC;
    private const int OffFlags = 0x0A;
    private const int OffLKey = 0x0E;
    private const ushort FlagEncrypted = 0x0100;
    private const ushort FlagWhichTableStream = 0x0200;
    private const ushort FlagObfuscated = 0x8000;
    private const uint MinLKey = 8;

    internal static DocFibProbe? ReadFib(DirectoryNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (!root.HasEntry(WordDocumentStream) || root.GetEntry(WordDocumentStream) is not DocumentEntry entry
            || entry.Size < FibBaseLength)
        {
            return null;
        }

        var fib = new byte[FibBaseLength];
        using (var input = root.CreateDocumentInputStream(entry))
        {
            input.ReadFully(fib);
        }

        if (BinaryPrimitives.ReadUInt16LittleEndian(fib) != WordIdent)
        {
            return null;
        }

        var flags = BinaryPrimitives.ReadUInt16LittleEndian(fib.AsSpan(OffFlags));
        var kind = (flags & FlagEncrypted) == 0
            ? DocEncryptionKind.NotEncrypted
            : (flags & FlagObfuscated) != 0 ? DocEncryptionKind.Xor : DocEncryptionKind.Rc4;
        var tableName = (flags & FlagWhichTableStream) != 0 ? "1Table" : "0Table";

        return new DocFibProbe(kind, tableName, BinaryPrimitives.ReadUInt32LittleEndian(fib.AsSpan(OffLKey)));
    }

    internal static LegacyEncryptionHeader ReadHeader(DirectoryNode root, DocFibProbe fib, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(fib);
        if (fib.Kind != DocEncryptionKind.Rc4)
        {
            throw new ArgumentException("ReadHeader requires an Rc4 probe.", nameof(fib));
        }

        if (!root.HasEntry(fib.TableStreamName) || root.GetEntry(fib.TableStreamName) is not DocumentEntry entry)
        {
            throw new CorruptFileException(detectedMimeType, "legacy encryption header: table stream missing");
        }

        if (fib.LKey < MinLKey || fib.LKey > entry.Size || fib.LKey > MaxLKey)
        {
            throw new CorruptFileException(
                detectedMimeType,
                $"legacy encryption header: lKey {fib.LKey} is outside the table stream bounds");
        }

        var header = new byte[fib.LKey];
        using (var input = root.CreateDocumentInputStream(entry))
        {
            input.ReadFully(header);
        }

        return LegacyEncryptionHeaderReader.Parse(header, LegacyContainer.Doc, detectedMimeType);
    }
}
