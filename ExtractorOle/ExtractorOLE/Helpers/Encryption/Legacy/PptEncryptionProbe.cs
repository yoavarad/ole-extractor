using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using NPOI.POIFS.FileSystem;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

internal enum PptProbeOutcome { NotEncrypted, Encrypted, EncryptedTokenStructureUnreadable }

internal sealed record PptEncryptionLayout(
    byte[] CurrentUser,
    byte[] PowerPointDocument,
    IReadOnlyDictionary<uint, uint> PersistOffsets,
    uint CryptSessionPersistId,
    uint CryptSessionOffset);

internal sealed record PptProbeResult(PptProbeOutcome Outcome, PptEncryptionLayout? Layout);

// [ydk:req:extraction/legacy-decryption] Decides whether a .ppt is encrypted from the UserEditAtom /
// persist-directory structure (the Current User token only decides how a structural failure is
// classified). The record stream after the persist directory is ciphertext, so it is never scanned.
internal static class PptEncryptionProbe
{
    internal const uint TokenEncrypted = 0xF3D1C4DF;
    internal const uint TokenPlain = 0xE391C05F;
    internal const int MaxEditChain = 10_000;

    private const string CurrentUserStream = "Current User";
    private const string PowerPointStream = "PowerPoint Document";
    private const int MaxCurrentUserSize = 64 * 1024;
    private const int MinCurrentUserSize = 20;
    private const int RecordHeaderSize = 8;
    private const int PlainUserEditAtomSize = RecordHeaderSize + 0x1C;
    private const ushort RecTypeCurrentUser = 0x0FF6;
    private const ushort RecTypeUserEditAtom = 0x0FF5;
    private const ushort RecTypePersistDirectory = 0x1772;
    private const ushort RecTypeCryptSession = 0x2F14;
    private const ushort RecVerInstanceContainer = 0x000F;
    private const uint UserEditLenShort = 0x1C;
    private const uint UserEditLenLong = 0x20;
    private const int OffsetLastEditField = 16;
    private const int OffsetPersistDirectoryField = 20;
    private const int OffsetEncryptRefField = 36;

    internal static PptProbeResult Probe(DirectoryNode root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var notEncrypted = new PptProbeResult(PptProbeOutcome.NotEncrypted, null);
        try
        {
            if (!TryGetStream(root, CurrentUserStream, out var currentUserEntry)
                || !TryGetStream(root, PowerPointStream, out var documentEntry))
            {
                return notEncrypted;
            }

            if (currentUserEntry.Size is > MaxCurrentUserSize or < MinCurrentUserSize)
            {
                return notEncrypted;
            }

            var currentUser = ReadRange(root, currentUserEntry, 0, currentUserEntry.Size);
            if (U16(currentUser, 2) != RecTypeCurrentUser)
            {
                return notEncrypted;
            }

            var token = U32(currentUser, 12);
            if (token is not (TokenEncrypted or TokenPlain))
            {
                return notEncrypted;
            }

            var offsetToCurrentEdit = U32(currentUser, 16);
            var structureFailure = token == TokenPlain
                ? notEncrypted
                : new PptProbeResult(PptProbeOutcome.EncryptedTokenStructureUnreadable, null);

            try
            {
                if (token == TokenPlain && IsPlainCurrentEdit(root, documentEntry, offsetToCurrentEdit))
                {
                    return notEncrypted;
                }

                var document = ReadRange(root, documentEntry, 0, documentEntry.Size);
                return Resolve(currentUser, document, offsetToCurrentEdit, structureFailure);
            }
            catch (Exception ex) when (ex is PptStructureException || ParseFailure.IsParseFailure(ex))
            {
                return structureFailure;
            }
        }
        catch (Exception ex) when (ex is not PptStructureException && ParseFailure.IsParseFailure(ex))
        {
            // NPOI could not read a stream of a damaged compound file: not a ppt this path handles.
            return notEncrypted;
        }
    }

    // [ydk:error:extraction/corrupt-file] The container body, valid once a password is present.
    internal static ReadOnlySpan<byte> CryptSessionPayload(PptEncryptionLayout layout, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var document = layout.PowerPointDocument;
        var offset = (long)layout.CryptSessionOffset;
        if (offset + RecordHeaderSize > document.Length)
        {
            throw new CorruptFileException(detectedMimeType, "CryptSession10Container header runs past the PowerPoint Document stream");
        }

        var length = U32(document, offset + 4);
        if (offset + RecordHeaderSize + length > document.Length)
        {
            throw new CorruptFileException(detectedMimeType, "CryptSession10Container runs past the PowerPoint Document stream");
        }

        return document.AsSpan((int)offset + RecordHeaderSize, (int)length);
    }

    private static PptProbeResult Resolve(byte[] currentUser, byte[] document, uint offsetToCurrentEdit, PptProbeResult structureFailure)
    {
        var persist = new Dictionary<uint, uint>();
        var seen = new HashSet<uint>();
        var mergedDirectories = new HashSet<uint>();
        uint? encryptRef = null;
        var offset = offsetToCurrentEdit;

        while (true)
        {
            if (seen.Count >= MaxEditChain || !seen.Add(offset))
            {
                throw new PptStructureException("UserEditAtom chain is cyclic or too long");
            }

            RequireRecord(document, offset, RecTypeUserEditAtom);
            var recLen = U32(document, offset + 4);
            if (recLen is not (UserEditLenShort or UserEditLenLong) || offset + RecordHeaderSize + recLen > document.Length)
            {
                throw new PptStructureException("UserEditAtom has an invalid length");
            }

            if (seen.Count == 1 && recLen == UserEditLenLong)
            {
                encryptRef = U32(document, offset + OffsetEncryptRefField);
            }

            var directoryOffset = U32(document, offset + OffsetPersistDirectoryField);
            if (mergedDirectories.Add(directoryOffset))
            {
                MergePersistDirectory(document, directoryOffset, persist);
            }

            var lastEdit = U32(document, offset + OffsetLastEditField);
            if (lastEdit == 0)
            {
                break;
            }

            offset = lastEdit;
        }

        if (encryptRef is not uint id || !persist.TryGetValue(id, out var cryptOffset))
        {
            return structureFailure;
        }

        if (cryptOffset + (long)RecordHeaderSize > document.Length
            || U16(document, cryptOffset) != RecVerInstanceContainer
            || U16(document, cryptOffset + 2) != RecTypeCryptSession)
        {
            return structureFailure;
        }

        var layout = new PptEncryptionLayout(currentUser, document, persist, id, cryptOffset);
        return new PptProbeResult(PptProbeOutcome.Encrypted, layout);
    }

    private static void MergePersistDirectory(byte[] document, uint directoryOffset, Dictionary<uint, uint> persist)
    {
        RequireRecord(document, directoryOffset, RecTypePersistDirectory);
        var end = (long)directoryOffset + RecordHeaderSize + U32(document, directoryOffset + 4);
        if (end > document.Length)
        {
            throw new PptStructureException("PersistDirectoryAtom runs past the stream");
        }

        var position = (long)directoryOffset + RecordHeaderSize;
        while (position < end)
        {
            if (position + 4 > end)
            {
                throw new PptStructureException("PersistDirectoryEntry is truncated");
            }

            var header = U32(document, position);
            var startId = header & 0xFFFFF;
            var count = header >> 20;
            if (position + 4 + (4L * count) > end)
            {
                throw new PptStructureException("PersistDirectoryEntry runs past the atom");
            }

            for (uint i = 0; i < count; i++)
            {
                persist.TryAdd(startId + i, U32(document, position + 4 + (4L * i)));
            }

            position += 4 + (4L * count);
        }
    }

    private static bool IsPlainCurrentEdit(DirectoryNode root, DocumentEntry documentEntry, uint offset)
    {
        if (offset + (long)PlainUserEditAtomSize > documentEntry.Size)
        {
            return false;
        }

        var head = ReadRange(root, documentEntry, offset, PlainUserEditAtomSize);
        return U16(head, 2) == RecTypeUserEditAtom && U32(head, 4) == UserEditLenShort;
    }

    private static void RequireRecord(byte[] data, uint offset, ushort recType)
    {
        if (offset + (long)RecordHeaderSize > data.Length || U16(data, offset + 2) != recType)
        {
            throw new PptStructureException($"expected record 0x{recType:X4} at {offset}");
        }
    }

    private static bool TryGetStream(DirectoryNode root, string name, out DocumentEntry entry)
    {
        if (root.HasEntry(name) && root.GetEntry(name) is DocumentEntry document)
        {
            entry = document;
            return true;
        }

        entry = null!;
        return false;
    }

    private static byte[] ReadRange(DirectoryNode root, DocumentEntry entry, long offset, int count)
    {
        var buffer = new byte[count];
        using var input = root.CreateDocumentInputStream(entry);
        input.Skip(offset);
        input.ReadFully(buffer, 0, count);
        return buffer;
    }

    private static ushort U16(byte[] data, long offset) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan((int)offset));

    private static uint U32(byte[] data, long offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)offset));

    private sealed class PptStructureException(string message) : Exception(message);
}
