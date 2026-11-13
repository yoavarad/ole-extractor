using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

public class DocEncryptionProbeTests
{
    private const string DocMime = "application/msword";
    private const int OffFlags = 0x0A;
    private const int OffLKey = 0x0E;

    private static byte[] PlainTwin() =>
        File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainTwinDocPath));

    private static T WithRoot<T>(byte[] cfb, Func<DirectoryNode, T> action)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        return action(fs.Root);
    }

    private static byte[] SetLKey(byte[] cfb, uint lKey) =>
        CfbMutator.ReplaceStream(cfb, "WordDocument", w =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(w.AsSpan(OffLKey), lKey);
            return w;
        });

    public static IEnumerable<object[]> EncryptedDocs() => new[]
    {
        new object[] { LegacyFixtures.TikaBinaryRc4Doc, "1Table", 52u },
        new object[] { LegacyFixtures.PasswordCryptoApiDoc, "1Table", 198u },
        new object[] { LegacyFixtures.Rc4CryptoApiDoc, "1Table", 198u },
        new object[] { LegacyFixtures.PasswordProtectedDoc, "1Table", 52u },
    };

    [Theory]
    [MemberData(nameof(EncryptedDocs))]
    public void ReadFib_EncryptedFixture_IsRc4WithTableNameAndLKey(string file, string table, uint lKey)
    {
        var fib = WithRoot(LegacyFixtures.Bytes(file), DocEncryptionProbe.ReadFib);

        Assert.NotNull(fib);
        Assert.Equal(DocEncryptionKind.Rc4, fib.Kind);
        Assert.Equal(table, fib.TableStreamName);
        Assert.Equal(lKey, fib.LKey);
    }

    public static IEnumerable<object[]> PlainDocs()
    {
        var samples = Path.GetDirectoryName(EncryptedSamples.FindRepoRootFile("samples/manifest.json"))!;
        foreach (var folder in new[] { "curated", "synthetic" })
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(samples, folder), "*.doc", SearchOption.AllDirectories)
                         .OrderBy(f => f, StringComparer.Ordinal))
            {
                yield return new object[] { Path.GetRelativePath(samples, file).Replace('\\', '/') };
            }
        }
    }

    [Theory]
    [MemberData(nameof(PlainDocs))]
    public void ReadFib_PlainDoc_IsNotEncrypted(string relativePath)
    {
        var bytes = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile("samples/" + relativePath));

        var fib = WithRoot(bytes, DocEncryptionProbe.ReadFib);

        Assert.NotNull(fib);
        Assert.Equal(DocEncryptionKind.NotEncrypted, fib.Kind);
    }

    [Fact]
    public void ReadFib_EncryptedAndObfuscatedFlags_IsXor()
    {
        var cfb = CfbMutator.ReplaceStream(PlainTwin(), "WordDocument", w =>
        {
            var flags = BinaryPrimitives.ReadUInt16LittleEndian(w.AsSpan(OffFlags));
            BinaryPrimitives.WriteUInt16LittleEndian(w.AsSpan(OffFlags), (ushort)(flags | 0x8100));
            return w;
        });

        Assert.Equal(DocEncryptionKind.Xor, WithRoot(cfb, DocEncryptionProbe.ReadFib)!.Kind);
    }

    [Fact]
    public void ReadFib_WordDocumentTruncatedTo40Bytes_ReturnsNull()
    {
        var cfb = CfbMutator.ReplaceStream(PlainTwin(), "WordDocument", w => w[..40]);

        Assert.Null(WithRoot(cfb, DocEncryptionProbe.ReadFib));
    }

    [Fact]
    public void ReadFib_WrongWIdent_ReturnsNull()
    {
        var cfb = CfbMutator.ReplaceStream(PlainTwin(), "WordDocument", w =>
        {
            w[0] ^= 0xFF;
            return w;
        });

        Assert.Null(WithRoot(cfb, DocEncryptionProbe.ReadFib));
    }

    [Fact]
    public void ReadFib_WordDocumentRemoved_ReturnsNull()
    {
        var cfb = CfbMutator.RemoveEntry(PlainTwin(), "WordDocument");

        Assert.Null(WithRoot(cfb, DocEncryptionProbe.ReadFib));
    }

    [Fact]
    public void ReadFib_LargeWordDocument_ReadsOnlyFibBase()
    {
        var cfb = CfbMutator.ReplaceStream(PlainTwin(), "WordDocument", w => [.. w, .. new byte[5 * 1024 * 1024]]);

        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var fib = DocEncryptionProbe.ReadFib(fs.Root);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(DocEncryptionKind.NotEncrypted, fib!.Kind);
        Assert.True(allocated < 1024 * 1024, $"allocated {allocated} bytes");
    }

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc, nameof(LegacyCipherScheme.BinaryRc4), 40)]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc, nameof(LegacyCipherScheme.CryptoApiRc4), 128)]
    [InlineData(LegacyFixtures.Rc4CryptoApiDoc, nameof(LegacyCipherScheme.CryptoApiRc4), 128)]
    public void ReadHeader_PasswordFixture_ParsesScheme(string file, string scheme, int keyBits)
    {
        var header = WithRoot(LegacyFixtures.Bytes(file), root =>
            DocEncryptionProbe.ReadHeader(root, DocEncryptionProbe.ReadFib(root)!, DocMime));

        Assert.Equal(scheme, header.Scheme.ToString());
        Assert.Equal(keyBits, header.KeySizeBits);
    }

    private static void AssertReadHeaderCorrupt(byte[] cfb) =>
        Assert.Throws<CorruptFileException>(() =>
            WithRoot(cfb, root => DocEncryptionProbe.ReadHeader(root, DocEncryptionProbe.ReadFib(root)!, DocMime)));

    [Theory]
    [InlineData(0u)]
    [InlineData(20u)]
    [InlineData(0xFFFFFFFFu)]
    [InlineData(4097u)]
    public void ReadHeader_LKeyOutOfBounds_IsCorrupt(uint lKey)
    {
        AssertReadHeaderCorrupt(SetLKey(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc), lKey));
    }

    [Fact]
    public void ReadHeader_LKeyBeyondTableLength_IsCorrupt()
    {
        var cfb = LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc);
        var tableSize = WithRoot(cfb, r => ((DocumentEntry)r.GetEntry("1Table")).Size);

        AssertReadHeaderCorrupt(SetLKey(cfb, (uint)tableSize + 1));
    }

    [Fact]
    public void ReadHeader_LKeyAboveMaxWithLargeTable_IsCorrupt()
    {
        var cfb = CfbMutator.ReplaceStream(
            SetLKey(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc), 4097), "1Table", _ => new byte[8192]);

        AssertReadHeaderCorrupt(cfb);
    }

    [Fact]
    public void ReadHeader_NonRc4Probe_Throws()
    {
        var bytes = PlainTwin();
        Assert.Throws<ArgumentException>(() => WithRoot(bytes, root =>
            DocEncryptionProbe.ReadHeader(root, DocEncryptionProbe.ReadFib(root)!, DocMime)));
    }

    [Fact]
    public void ReadHeader_TableStreamDeleted_IsCorrupt()
    {
        AssertReadHeaderCorrupt(CfbMutator.RemoveEntry(LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc), "1Table"));
    }

    [Fact]
    public void ReadHeader_BinaryRc4WithLKey40_IsCorrupt()
    {
        AssertReadHeaderCorrupt(SetLKey(LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc), 40));
    }

    [Fact]
    public void ReadHeader_HugeLKeySmallTable_AllocatesNoLKeySizedBuffer()
    {
        var cfb = CfbMutator.ReplaceStream(PlainTwin(), "WordDocument", w =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(w.AsSpan(OffLKey), 0xFFFFFFFF);
            BinaryPrimitives.WriteUInt16LittleEndian(w.AsSpan(OffFlags),
                (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(w.AsSpan(OffFlags)) | 0x0100));
            return w;
        });
        cfb = CfbMutator.ReplaceStream(cfb, "1Table", _ => new byte[1024]);

        long allocated = 0;
        WithRoot(cfb, root =>
        {
            var fib = DocEncryptionProbe.ReadFib(root)!;
            Assert.Equal("1Table", fib.TableStreamName);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Assert.Throws<CorruptFileException>(() => DocEncryptionProbe.ReadHeader(root, fib, DocMime));
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            return 0;
        });

        Assert.True(allocated < 64 * 1024, $"allocated {allocated} bytes");
    }
}
