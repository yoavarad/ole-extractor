using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

public class PptEncryptionProbeTests
{
    private const string CurrentUser = "Current User";
    private const string PowerPointDocument = "PowerPoint Document";
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const uint BogusToken = 0x12345678;
    private static readonly byte[] VersionHeader = [0x02, 0x00, 0x02, 0x00];

    public static TheoryData<string, uint> EncryptedFixtures => new()
    {
        { LegacyFixtures.HelloPpt, 6 },
        { LegacyFixtures.Hello56Ppt, 7 },
        { LegacyFixtures.HelloNoPropsPpt, 7 },
        { LegacyFixtures.CryptoApiProc2356Ppt, 13 },
        { LegacyFixtures.PngEncryptedPpt, 5 },
    };

    public static TheoryData<string> PlainPpts => new()
    {
        "samples/curated/ppt/ppt_with_png.ppt",
        "samples/curated/ppt/quarterly-review-deck.ppt",
        "samples/synthetic/large-body/large-body.ppt",
        "samples/synthetic/macro-embed/sample.ppt",
        "samples/synthetic/multi-embedding/sample.ppt",
    };

    private static PptProbeResult Probe(byte[] cfb)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        return PptEncryptionProbe.Probe(fs.Root);
    }

    private static byte[] Hello() => LegacyFixtures.Bytes(LegacyFixtures.HelloPpt);

    private static byte[] PlainPng() => File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainReferencePptPath));

    private static byte[] ReadStream(byte[] cfb, string name)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var entry = (DocumentEntry)fs.Root.GetEntry(name);
        var data = new byte[entry.Size];
        using var input = fs.Root.CreateDocumentInputStream(entry);
        input.ReadFully(data);
        return data;
    }

    private static uint CurrentEditOffset(byte[] cfb) =>
        BinaryPrimitives.ReadUInt32LittleEndian(ReadStream(cfb, CurrentUser).AsSpan(16));

    private static byte[] SetCurrentUserU32(byte[] cfb, int offset, uint value) =>
        CfbMutator.ReplaceStream(cfb, CurrentUser, b =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(offset), value);
            return b;
        });

    private static byte[] SetToken(byte[] cfb, uint token) => SetCurrentUserU32(cfb, 12, token);

    private static byte[] SetDocumentU32(byte[] cfb, long offset, uint value) =>
        CfbMutator.ReplaceStream(cfb, PowerPointDocument, b =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan((int)offset), value);
            return b;
        });

    // The five structural mutations of the decision table, keyed by name.
    public static TheoryData<string> Mutations => new() { "recLen", "editOffset", "cycle", "badRef", "refAbsent" };

    private static byte[] Mutate(byte[] hello, string mutation)
    {
        var edit = CurrentEditOffset(hello);
        switch (mutation)
        {
            case "recLen":
                return SetDocumentU32(hello, edit + 4, 0x1D);
            case "editOffset":
                return SetCurrentUserU32(hello, 16, 0x7FFFFFF0);
            case "cycle":
                return SetDocumentU32(hello, edit + 16, edit);
            case "badRef":
            {
                var layout = Probe(hello).Layout!;
                var other = layout.PersistOffsets.Keys.First(k => k != layout.CryptSessionPersistId);
                return SetDocumentU32(hello, edit + 36, other);
            }

            case "refAbsent":
                return SetDocumentU32(hello, edit + 36, 0xFFFFF);
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation));
        }
    }

    [Theory]
    [MemberData(nameof(EncryptedFixtures))]
    public void Probe_EncryptedFixture_IsEncryptedWithPersistId(string fileName, uint persistId)
    {
        var result = Probe(LegacyFixtures.Bytes(fileName));

        Assert.Equal(PptProbeOutcome.Encrypted, result.Outcome);
        Assert.NotNull(result.Layout);
        Assert.Equal(persistId, result.Layout.CryptSessionPersistId);
    }

    [Theory]
    [MemberData(nameof(PlainPpts))]
    public void Probe_PlainPpt_IsNotEncrypted(string path)
    {
        var result = Probe(File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(path)));

        Assert.Equal(PptProbeOutcome.NotEncrypted, result.Outcome);
        Assert.Null(result.Layout);
    }

    [Fact]
    public void Probe_PlainTokenWithValidStructure_StructureWins()
    {
        var result = Probe(SetToken(Hello(), PptEncryptionProbe.TokenPlain));

        Assert.Equal(PptProbeOutcome.Encrypted, result.Outcome);
    }

    [Fact]
    public void Probe_EncryptedTokenOnPlainPpt_IsStructureUnreadable()
    {
        var result = Probe(SetToken(PlainPng(), PptEncryptionProbe.TokenEncrypted));

        Assert.Equal(PptProbeOutcome.EncryptedTokenStructureUnreadable, result.Outcome);
        Assert.Null(result.Layout);
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Probe_BrokenStructureWithEncryptedToken_IsStructureUnreadable(string mutation)
    {
        var result = Probe(Mutate(Hello(), mutation));

        Assert.Equal(PptProbeOutcome.EncryptedTokenStructureUnreadable, result.Outcome);
        Assert.Null(result.Layout);
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Probe_BrokenStructureWithPlainToken_IsNotEncrypted(string mutation)
    {
        var result = Probe(SetToken(Mutate(Hello(), mutation), PptEncryptionProbe.TokenPlain));

        Assert.Equal(PptProbeOutcome.NotEncrypted, result.Outcome);
    }

    [Fact]
    public void Probe_UnknownToken_IsNotEncrypted()
    {
        Assert.Equal(PptProbeOutcome.NotEncrypted, Probe(SetToken(Hello(), BogusToken)).Outcome);
    }

    [Fact]
    public void Probe_DamagedStreams_NeverThrow()
    {
        var hello = Hello();
        var truncated = CfbMutator.ReplaceStream(hello, PowerPointDocument, b => b[..64]);
        var shortCurrentUser = CfbMutator.ReplaceStream(hello, CurrentUser, b => b[..10]);
        var nearMax = SetCurrentUserU32(hello, 16, 0xFFFFFFFF);

        Assert.Equal(PptProbeOutcome.EncryptedTokenStructureUnreadable, Probe(truncated).Outcome);
        Assert.Equal(PptProbeOutcome.NotEncrypted, Probe(shortCurrentUser).Outcome);
        Assert.Equal(PptProbeOutcome.EncryptedTokenStructureUnreadable, Probe(nearMax).Outcome);
    }

    [Fact]
    public void Probe_MissingStreams_IsNotEncrypted()
    {
        Assert.Equal(PptProbeOutcome.NotEncrypted, Probe(CfbMutator.RemoveEntry(Hello(), CurrentUser)).Outcome);
        Assert.Equal(PptProbeOutcome.NotEncrypted, Probe(CfbMutator.RemoveEntry(Hello(), PowerPointDocument)).Outcome);
    }

    [Fact]
    public void CryptSessionPayload_Hello_Is190BytesStartingWithVersion2_2()
    {
        var layout = Probe(Hello()).Layout!;

        var payload = PptEncryptionProbe.CryptSessionPayload(layout, PptMime);

        Assert.Equal(4 + 4 + 4 + 118 + 60, payload.Length);
        Assert.True(payload[..4].SequenceEqual(VersionHeader));
    }

    [Fact]
    public void CryptSessionPayload_RecLenPastStreamEnd_IsCorruptFile()
    {
        var hello = Hello();
        var layout = Probe(hello).Layout!;
        var patched = SetDocumentU32(hello, layout.CryptSessionOffset + 4, 0x7FFFFFF0);
        var patchedLayout = Probe(patched).Layout!;

        var ex = Assert.Throws<CorruptFileException>(() => PptEncryptionProbe.CryptSessionPayload(patchedLayout, PptMime));

        Assert.Equal("CORRUPT_FILE", ex.Code);
    }

    [Fact]
    public void Probe_PlainPptWithLargeTrailingStream_DoesNotReadWholeStream()
    {
        var padded = CfbMutator.ReplaceStream(PlainPng(), PowerPointDocument, b => [.. b, .. new byte[5 * 1024 * 1024]]);
        using var fs = new NPOIFSFileSystem(new MemoryStream(padded));
        PptEncryptionProbe.Probe(fs.Root); // warm up JIT and caches outside the measurement

        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = PptEncryptionProbe.Probe(fs.Root);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(PptProbeOutcome.NotEncrypted, result.Outcome);
        Assert.True(allocated < 1024 * 1024, $"allocated {allocated} bytes");
    }
}
