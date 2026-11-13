using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.HPSF;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.Crypt.CryptoAPI;
using NPOI.POIFS.FileSystem;
using NPOI.Util;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

public class CryptoApiEncryptedSummaryReaderTests
{
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const string SummaryName = "\u0005SummaryInformation";
    private const string DocSummaryName = "\u0005DocumentSummaryInformation";

    public static TheoryData<string, string> OtherPpts => new()
    {
        { LegacyFixtures.Hello56Ppt, "hello" },
        { LegacyFixtures.CryptoApiProc2356Ppt, "crypto" },
        { LegacyFixtures.PngEncryptedPpt, "password" },
    };

    private static byte[] ReadStream(byte[] cfb, string name)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var entry = (DocumentEntry)fs.Root.GetEntry(name);
        var data = new byte[entry.Size];
        using var input = fs.Root.CreateDocumentInputStream(entry);
        input.ReadFully(data);
        return data;
    }

    private static CryptoAPIDecryptor DecryptorFor(byte[] pptBytes, string password)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(pptBytes));
        var probe = PptEncryptionProbe.Probe(fs.Root);
        var payload = PptEncryptionProbe.CryptSessionPayload(probe.Layout!, PptMime);
        var header = LegacyEncryptionHeaderReader.Parse(payload, LegacyContainer.Ppt, PptMime);
        var decryptor = LegacyEncryptionHeaderReader.CreateDecryptor(header);
        Assert.True(LegacyEncryptionHeaderReader.VerifyPassword(decryptor, password));
        return (CryptoAPIDecryptor)decryptor;
    }

    private static (byte[] Summary, CryptoAPIDecryptor Decryptor) Hello()
    {
        var bytes = LegacyFixtures.Bytes(LegacyFixtures.HelloPpt);
        return (ReadStream(bytes, CryptoApiEncryptedSummaryReader.PptStreamName), DecryptorFor(bytes, "hello"));
    }

    private static string Sha1Prefix(byte[] data) => Convert.ToHexString(CryptoFunctions.GetMessageDigest(HashAlgorithm.sha1).Digest(data))[..8];

    // RC4 is symmetric: the same block-0 transform decrypts and re-encrypts.
    private static byte[] Block0(CryptoAPIDecryptor decryptor, byte[] data, int offset, int length)
    {
        var copy = data.AsSpan(offset, length).ToArray();
        decryptor.InitCipherForBlock(null, 0).Update(copy, 0, copy.Length, copy);
        return copy;
    }

    // Decrypts header and descriptor array, lets the caller edit the plaintext, then re-encrypts both
    // in place (the array at its original position).
    private static byte[] Rebuild(byte[] summary, CryptoAPIDecryptor decryptor, Action<byte[], byte[]> edit)
    {
        var header = Block0(decryptor, summary, 0, 8);
        var arrayOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
        var arraySize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
        var array = Block0(decryptor, summary, arrayOffset, arraySize);

        edit(header, array);

        var result = (byte[])summary.Clone();
        Block0(decryptor, header, 0, 8).CopyTo(result, 0);
        Block0(decryptor, array, 0, array.Length).CopyTo(result, arrayOffset);
        return result;
    }

    private static void AssertCorrupt(byte[] summary, CryptoAPIDecryptor decryptor, string field)
    {
        var ex = Assert.Throws<CorruptFileException>(() => CryptoApiEncryptedSummaryReader.Read(summary, decryptor, PptMime));
        Assert.Contains(field, ex.Message, StringComparison.Ordinal);
    }

    private static PropertySet ParsePropertySet(byte[] data) => new(new ByteArrayInputStream(data));

    // Offsets inside the plaintext descriptor array of a two-descriptor stream.
    private const int Desc0 = 4;

    private static int Desc1(byte[] array) => Desc0 + 16 + (array[Desc0 + 10] * 2) + 2;

    [Fact]
    public void Read_HelloPpt_YieldsBothPropertyStreams()
    {
        var (summary, decryptor) = Hello();

        var streams = CryptoApiEncryptedSummaryReader.Read(summary, decryptor, PptMime);

        Assert.Equal(2, streams.Count);
        Assert.Equal(SummaryName, streams[0].Name);
        Assert.Equal(DocSummaryName, streams[1].Name);
        Assert.Equal("F1FEF6B5", Sha1Prefix(streams[0].Data));
        Assert.Equal("D03E04BB", Sha1Prefix(streams[1].Data));
        Assert.Equal("This is a protected document", new SummaryInformation(ParsePropertySet(streams[0].Data)).Title);
    }

    [Theory]
    [MemberData(nameof(OtherPpts))]
    public void Read_OtherPpts_YieldsTwoParsablePropertySets(string fileName, string password)
    {
        var bytes = LegacyFixtures.Bytes(fileName);
        var summary = ReadStream(bytes, CryptoApiEncryptedSummaryReader.PptStreamName);

        var streams = CryptoApiEncryptedSummaryReader.Read(summary, DecryptorFor(bytes, password), PptMime);

        Assert.Equal([SummaryName, DocSummaryName], streams.Select(s => s.Name));
        Assert.All(streams, s => Assert.True(ParsePropertySet(s.Data).SectionCount > 0));
    }

    [Theory]
    [InlineData(0x7FFFFFFFu)]
    [InlineData(0xFFFFFFFFu)]
    public void Read_HugeCount_IsCorruptNotOutOfMemory(uint count)
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (_, array) => BinaryPrimitives.WriteUInt32LittleEndian(array, count));

        AssertCorrupt(mutated, decryptor, "streamDescriptorCount");
    }

    [Fact]
    public void Read_CountOneOverBound_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (_, array) =>
            BinaryPrimitives.WriteUInt32LittleEndian(
                array, (uint)((array.Length - 4) / CryptoApiEncryptedSummaryReader.MinDescriptorSize) + 1));

        AssertCorrupt(mutated, decryptor, "streamDescriptorCount");
    }

    [Fact]
    public void Read_StreamRangeReachesArray_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (header, array) =>
        {
            var arrayOffset = BinaryPrimitives.ReadUInt32LittleEndian(header);
            BinaryPrimitives.WriteUInt32LittleEndian(array.AsSpan(Desc0), 8);
            BinaryPrimitives.WriteUInt32LittleEndian(array.AsSpan(Desc0 + 4), arrayOffset - 7); // ends one byte past the array start
        });

        AssertCorrupt(mutated, decryptor, "StreamOffset or StreamSize");
    }

    [Fact]
    public void Read_StreamOffsetBelowHeader_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (_, array) => BinaryPrimitives.WriteUInt32LittleEndian(array.AsSpan(Desc0), 4));

        AssertCorrupt(mutated, decryptor, "StreamOffset or StreamSize");
    }

    [Fact]
    public void Read_StreamRangeOverflowsUint_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (_, array) =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(array.AsSpan(Desc0), 0xFFFFFFFEu);
            BinaryPrimitives.WriteUInt32LittleEndian(array.AsSpan(Desc0 + 4), 4);
        });

        AssertCorrupt(mutated, decryptor, "StreamOffset or StreamSize");
    }

    [Fact]
    public void Read_OverlappingDescriptors_ExceedCumulativeCap()
    {
        var (summary, decryptor) = Hello();

        // Both descriptors point at the larger range, so the summed sizes exceed the stream body.
        var mutated = Rebuild(summary, decryptor, (_, array) =>
        {
            var second = Desc1(array);
            var firstSize = BinaryPrimitives.ReadUInt32LittleEndian(array.AsSpan(Desc0 + 4));
            var secondSize = BinaryPrimitives.ReadUInt32LittleEndian(array.AsSpan(second + 4));
            var (from, to) = firstSize >= secondSize ? (Desc0, second) : (second, Desc0);
            array.AsSpan(from, 8).CopyTo(array.AsSpan(to));
        });

        AssertCorrupt(mutated, decryptor, "overlapping descriptors");
    }

    [Fact]
    public void Read_DescriptorPastArrayEnd_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        // The plaintext array is not shrunk; only the declared size is, so the second descriptor is cut off.
        var mutated = Rebuild(summary, decryptor, (header, array) =>
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)Desc1(array) + 8));

        AssertCorrupt(mutated, decryptor, "descriptor runs past");
    }

    [Fact]
    public void Read_NamePastArrayEnd_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (header, array) =>
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)Desc1(array) + 16 + 4));

        AssertCorrupt(mutated, decryptor, "name runs past");
    }

    [Fact]
    public void Read_ArrayOffsetBelowHeader_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (header, _) => BinaryPrimitives.WriteUInt32LittleEndian(header, 4));

        AssertCorrupt(mutated, decryptor, "streamDescriptorArray offset or size");
    }

    [Fact]
    public void Read_ArrayOffsetMax_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (header, _) => BinaryPrimitives.WriteUInt32LittleEndian(header, 0xFFFFFFFFu));

        AssertCorrupt(mutated, decryptor, "streamDescriptorArray offset or size");
    }

    [Fact]
    public void Read_ArraySizeBelowCount_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        var mutated = Rebuild(summary, decryptor, (header, _) => BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), 3));

        AssertCorrupt(mutated, decryptor, "streamDescriptorArray offset or size");
    }

    [Fact]
    public void Read_SevenByteStream_IsCorrupt()
    {
        var (summary, decryptor) = Hello();

        AssertCorrupt(summary[..7], decryptor, "header is truncated");
    }

    [Fact]
    public void Read_StorageDescriptor_IsSkipped()
    {
        var (summary, decryptor) = Hello();
        var header = Block0(decryptor, summary, 0, 8);
        var arrayOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
        var arraySize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
        var array = Block0(decryptor, summary, arrayOffset, arraySize);
        Assert.Equal(summary.Length, arrayOffset + arraySize); // the array is last, so nothing trails it

        // Third descriptor: flags 0 (storage), name "S" plus terminator.
        var storage = new byte[16 + 2 + 2];
        storage[10] = 1;
        storage[16] = (byte)'S';
        var newArray = array.Concat(storage).ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(newArray, 3);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)newArray.Length);

        var result = summary[..arrayOffset].Concat(new byte[newArray.Length]).ToArray();
        Block0(decryptor, header, 0, 8).CopyTo(result, 0);
        Block0(decryptor, newArray, 0, newArray.Length).CopyTo(result, arrayOffset);

        var streams = CryptoApiEncryptedSummaryReader.Read(result, decryptor, PptMime);

        Assert.Equal(2, streams.Count);
    }

    [Fact]
    public void Read_EncryptionStreamWrittenByNpoi_RoundTrips()
    {
        var (helloSummary, helloDecryptor) = Hello();
        var plain = CryptoApiEncryptedSummaryReader.Read(helloSummary, helloDecryptor, PptMime)[0].Data;

        var info = new EncryptionInfo(EncryptionMode.CryptoAPI);
        info.Encryptor.ConfirmPassword("pw");
        var decryptor = (CryptoAPIDecryptor)info.Decryptor;
        Assert.True(decryptor.VerifyPassword("pw"));

        using var source = new NPOIFSFileSystem();
        source.Root.CreateDocument(SummaryName, new MemoryStream(plain));
        using var target = new NPOIFSFileSystem();
        ((CryptoAPIEncryptor)info.Encryptor).SetSummaryEntries(
            target.Root, CryptoApiEncryptedSummaryReader.DocXlsStreamName, source);
        using var written = new MemoryStream();
        target.WriteFileSystem(written);

        var encrypted = ReadStream(written.ToArray(), CryptoApiEncryptedSummaryReader.DocXlsStreamName);
        var streams = CryptoApiEncryptedSummaryReader.Read(encrypted, decryptor, "application/msword");

        var single = Assert.Single(streams);
        Assert.Equal(SummaryName, single.Name);
        Assert.Equal(plain, single.Data);
    }
}
