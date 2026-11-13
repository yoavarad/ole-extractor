using System.Buffers.Binary;

namespace ExtractorOLE.Tests.Encryption.Legacy;

internal sealed record PptAtom(ushort RecType, int BodyOffset, int BodyLength);

internal sealed record PptWalkResult(int Records, int StructuralErrors, IReadOnlyList<PptAtom> Atoms);

// Test-side MS-PPT record walker (port of the research prototype's walk): a record is an 8-byte header
// (recVer/recInstance u16, recType u16, recLen u32); a container (recVer 0xF) must be filled exactly
// by its children. Any header or body that does not fit counts as one structural error.
internal static class PptRecordWalker
{
    private const int HeaderSize = 8;
    private const int ContainerVersion = 0xF;

    internal static PptWalkResult WalkRecords(byte[] stream, IEnumerable<int> recordOffsets)
    {
        var records = 0;
        var errors = 0;
        var atoms = new List<PptAtom>();
        foreach (var offset in recordOffsets)
        {
            Walk(stream, offset, stream.Length, ref records, ref errors, atoms);
        }

        return new PptWalkResult(records, errors, atoms);
    }

    // Returns the end of the record at offset, or -1 when it does not fit inside limit.
    private static long Walk(byte[] data, long offset, long limit, ref int records, ref int errors, List<PptAtom> atoms)
    {
        if (offset + HeaderSize > limit)
        {
            errors++;
            return -1;
        }

        var verInstance = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan((int)offset));
        var recType = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan((int)offset + 2));
        var recLen = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)offset + 4));
        var body = offset + HeaderSize;
        var end = body + recLen;
        if (end > limit)
        {
            errors++;
            return -1;
        }

        records++;
        if ((verInstance & 0xF) != ContainerVersion)
        {
            atoms.Add(new PptAtom(recType, (int)body, (int)recLen));
            return end;
        }

        var position = body;
        while (position < end)
        {
            var next = Walk(data, position, end, ref records, ref errors, atoms);
            if (next < 0)
            {
                return end;
            }

            position = next;
        }

        return end;
    }
}
