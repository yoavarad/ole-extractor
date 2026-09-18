using NPOI.POIFS.FileSystem;
using System.Text;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Builds hand-crafted legacy .ppt fixtures by rewriting the text atoms of a genuine,
    /// PowerPoint-authored .ppt (PowerPoint/Fixtures/with_textbox.ppt, from the Apache-2.0 NPOI test
    /// corpus). NPOI has no .ppt writer (ADR-004), and building the master/layout/drawing records b2xtranslator
    /// needs from scratch would be far larger than the behavior under test, so this keeps every
    /// structural record from the real file and swaps only the slide text.
    ///
    /// Each replacement must have the same UTF-16 length as the text it replaces, so the
    /// StyleTextPropAtom/TextSpecialInfoAtom character-run counts that follow each text atom stay
    /// valid. Like PowerPoint itself, replacement text that is entirely within U+0000..U+00FF is
    /// stored as a TextBytesAtom (one ISO-8859-1 byte per char); anything else becomes a
    /// TextCharsAtom (UTF-16LE). Records after a resized atom move, so every container length, the
    /// PersistDirectoryAtom/UserEditAtom offsets and the "Current User" stream's offset to the
    /// current edit are recomputed.
    /// </summary>
    internal static class PptTextFixtureBuilder
    {
        private const ushort TextCharsAtomType = 4000;
        private const ushort TextBytesAtomType = 4008;
        private const ushort UserEditAtomType = 4085;
        private const ushort PersistDirectoryAtomType = 6002;
        private const int HeaderSize = 8;

        private const string DocumentStreamName = "PowerPoint Document";
        private const string CurrentUserStreamName = "Current User";

        // MS-PPT CurrentUserAtom: 8-byte record header, size(4), headerToken(4), offsetToCurrentEdit(4).
        private const int CurrentUserOffsetToCurrentEditPosition = 16;

        // MS-PPT UserEditAtom payload: lastSlideIdRef(4), version(2), minor(1), major(1),
        // offsetLastEdit(4), offsetPersistDirectory(4), ...
        private const int UserEditOffsetLastEditPosition = 8;
        private const int UserEditOffsetPersistDirectoryPosition = 12;

        public static byte[] ReplaceText(byte[] pptBytes, IReadOnlyDictionary<string, string> replacements)
        {
            var fs = new NPOIFSFileSystem(new MemoryStream(pptBytes));
            try
            {
                byte[] document = ReadStream(fs, DocumentStreamName);
                byte[] currentUser = ReadStream(fs, CurrentUserStreamName);

                byte[] newDocument = RewriteDocumentStream(document, replacements, out var offsetMap);

                int oldCurrentEdit = BitConverter.ToInt32(currentUser, CurrentUserOffsetToCurrentEditPosition);
                WriteInt32(currentUser, CurrentUserOffsetToCurrentEditPosition, Remap(offsetMap, oldCurrentEdit));

                ReplaceStream(fs, DocumentStreamName, newDocument);
                ReplaceStream(fs, CurrentUserStreamName, currentUser);

                using var output = new MemoryStream();
                fs.WriteFileSystem(output);
                return output.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }

        private static byte[] ReadStream(NPOIFSFileSystem fs, string name)
        {
            using var stream = fs.Root.CreateDocumentInputStream(name);
            var bytes = new byte[stream.Length];
            stream.ReadFully(bytes);
            return bytes;
        }

        private static void ReplaceStream(NPOIFSFileSystem fs, string name, byte[] bytes)
        {
            fs.Root.GetEntry(name).Delete();
            fs.Root.CreateDocument(name, new MemoryStream(bytes));
        }

        private static byte[] RewriteDocumentStream(
            byte[] document,
            IReadOnlyDictionary<string, string> replacements,
            out Dictionary<int, int> offsetMap)
        {
            offsetMap = new Dictionary<int, int>();
            var output = new MemoryStream();

            int offset = 0;
            while (offset < document.Length)
            {
                offsetMap[offset] = (int)output.Length;
                byte[] record = RewriteRecord(document, offset, replacements, out int oldLength);
                output.Write(record);
                offset += oldLength;
            }

            byte[] result = output.ToArray();

            // Second pass over the rewritten top-level records: fix the absolute stream offsets held
            // by PersistDirectoryAtom entries and UserEditAtom links, which reference top-level
            // records by their pre-rewrite position.
            int position = 0;
            while (position < result.Length)
            {
                ushort type = BitConverter.ToUInt16(result, position + 2);
                int length = BitConverter.ToInt32(result, position + 4);
                int payload = position + HeaderSize;

                if (type == PersistDirectoryAtomType)
                {
                    int cursor = payload;
                    while (cursor < payload + length)
                    {
                        // persistId in the low 20 bits, count of following offsets in the high 12.
                        uint info = BitConverter.ToUInt32(result, cursor);
                        int count = (int)(info >> 20);
                        cursor += 4;
                        for (int i = 0; i < count; i++, cursor += 4)
                        {
                            WriteInt32(result, cursor, Remap(offsetMap, BitConverter.ToInt32(result, cursor)));
                        }
                    }
                }
                else if (type == UserEditAtomType)
                {
                    RemapAt(result, payload + UserEditOffsetLastEditPosition, offsetMap, allowZero: true);
                    RemapAt(result, payload + UserEditOffsetPersistDirectoryPosition, offsetMap, allowZero: false);
                }

                position += HeaderSize + length;
            }

            return result;
        }

        private static void RemapAt(byte[] buffer, int position, Dictionary<int, int> offsetMap, bool allowZero)
        {
            int old = BitConverter.ToInt32(buffer, position);
            if (allowZero && old == 0) return;
            WriteInt32(buffer, position, Remap(offsetMap, old));
        }

        private static int Remap(Dictionary<int, int> offsetMap, int oldOffset)
        {
            if (!offsetMap.TryGetValue(oldOffset, out int newOffset))
            {
                throw new InvalidOperationException(
                    $"Stream offset {oldOffset} is not the start of a top-level record in the source .ppt.");
            }
            return newOffset;
        }

        private static byte[] RewriteRecord(
            byte[] source,
            int offset,
            IReadOnlyDictionary<string, string> replacements,
            out int oldTotalLength)
        {
            ushort verInstance = BitConverter.ToUInt16(source, offset);
            ushort type = BitConverter.ToUInt16(source, offset + 2);
            int length = BitConverter.ToInt32(source, offset + 4);
            int payloadStart = offset + HeaderSize;
            oldTotalLength = HeaderSize + length;

            bool isContainer = (verInstance & 0xF) == 0xF;
            byte[] newPayload;

            if (isContainer)
            {
                using var children = new MemoryStream();
                int childOffset = payloadStart;
                while (childOffset < payloadStart + length)
                {
                    children.Write(RewriteRecord(source, childOffset, replacements, out int childLength));
                    childOffset += childLength;
                }
                newPayload = children.ToArray();
            }
            else if (type == TextBytesAtomType || type == TextCharsAtomType)
            {
                string original = type == TextBytesAtomType
                    ? Encoding.Latin1.GetString(source, payloadStart, length)
                    : Encoding.Unicode.GetString(source, payloadStart, length);

                if (replacements.TryGetValue(original, out string? replacement))
                {
                    if (replacement.Length != original.Length)
                    {
                        throw new ArgumentException(
                            $"Replacement for \"{original}\" must be {original.Length} UTF-16 code units, got {replacement.Length}.");
                    }

                    bool latin1 = replacement.All(c => c <= 0xFF);
                    type = latin1 ? TextBytesAtomType : TextCharsAtomType;
                    newPayload = latin1 ? Encoding.Latin1.GetBytes(replacement) : Encoding.Unicode.GetBytes(replacement);
                }
                else
                {
                    newPayload = source.AsSpan(payloadStart, length).ToArray();
                }
            }
            else
            {
                newPayload = source.AsSpan(payloadStart, length).ToArray();
            }

            var record = new byte[HeaderSize + newPayload.Length];
            BitConverter.GetBytes(verInstance).CopyTo(record, 0);
            BitConverter.GetBytes(type).CopyTo(record, 2);
            BitConverter.GetBytes(newPayload.Length).CopyTo(record, 4);
            newPayload.CopyTo(record, HeaderSize);
            return record;
        }

        private static void WriteInt32(byte[] buffer, int position, int value)
        {
            BitConverter.GetBytes(value).CopyTo(buffer, position);
        }
    }
}
