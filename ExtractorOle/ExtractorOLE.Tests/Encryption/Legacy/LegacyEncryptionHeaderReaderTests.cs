using System.Buffers.Binary;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

public class LegacyEncryptionHeaderReaderTests
{
    private const string DocMime = "application/msword";
    private const string PptMime = "application/vnd.ms-powerpoint";

    // Offsets inside a CryptoAPI payload (little-endian).
    private const int OffVersionMajor = 0;
    private const int OffVersionMinor = 2;
    private const int OffFlags = 4;
    private const int OffHeaderSize = 8;
    private const int OffAlgId = 12 + 8;
    private const int OffAlgIdHash = 12 + 12;
    private const int OffKeySize = 12 + 16;

    private static byte[] ReadStream(byte[] cfb, string name)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var entry = (DocumentEntry)fs.Root.GetEntry(name);
        var data = new byte[entry.Size];
        using var input = fs.Root.CreateDocumentInputStream(entry);
        input.ReadFully(data);
        return data;
    }

    private static byte[] DocPayload(string fileName)
    {
        var cfb = LegacyFixtures.Bytes(fileName);
        var word = ReadStream(cfb, "WordDocument");
        var flags = BinaryPrimitives.ReadUInt16LittleEndian(word.AsSpan(0x0A));
        var lKey = (int)BinaryPrimitives.ReadUInt32LittleEndian(word.AsSpan(0x0E));
        var table = ReadStream(cfb, (flags & 0x0200) != 0 ? "1Table" : "0Table");
        return table[..lKey];
    }

    private static byte[] PptPayload(string fileName)
    {
        var stream = ReadStream(LegacyFixtures.Bytes(fileName), "PowerPoint Document");
        // The record chain past the persist directory is ciphertext, so locate the (unencrypted)
        // CryptSession10Container header by its signature instead of walking record lengths.
        for (var offset = 0; offset + 8 <= stream.Length; offset++)
        {
            var verInstance = BinaryPrimitives.ReadUInt16LittleEndian(stream.AsSpan(offset));
            var type = BinaryPrimitives.ReadUInt16LittleEndian(stream.AsSpan(offset + 2));
            var length = (long)BinaryPrimitives.ReadUInt32LittleEndian(stream.AsSpan(offset + 4));
            if (type == 0x2F14 && verInstance == 0x000F && offset + 8 + length <= stream.Length)
            {
                return stream[(offset + 8)..(offset + 8 + (int)length)];
            }
        }

        throw new InvalidOperationException("No CryptSession10Container in " + fileName);
    }

    private static byte[] CryptoApiDoc() => DocPayload(LegacyFixtures.PasswordCryptoApiDoc);

    private static byte[] With(byte[] payload, Action<byte[]> edit)
    {
        var copy = (byte[])payload.Clone();
        edit(copy);
        return copy;
    }

    private static void SetU16(byte[] b, int offset, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(offset), value);

    private static void SetU32(byte[] b, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(offset), value);

    private static uint GetU32(byte[] b, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(b.AsSpan(offset));

    private static bool Verify(byte[] payload, LegacyContainer container, string password)
    {
        var header = LegacyEncryptionHeaderReader.Parse(payload, container, DocMime);
        return LegacyEncryptionHeaderReader.VerifyPassword(LegacyEncryptionHeaderReader.CreateDecryptor(header), password);
    }

    [Fact]
    public void Parse_TikaDoc_IsBinaryRc4()
    {
        var header = LegacyEncryptionHeaderReader.Parse(DocPayload(LegacyFixtures.TikaBinaryRc4Doc), LegacyContainer.Doc, DocMime);

        Assert.Equal(LegacyCipherScheme.BinaryRc4, header.Scheme);
        Assert.Equal(40, header.KeySizeBits);
        Assert.Equal(52, header.Payload.Length);
    }

    [Fact]
    public void Parse_CryptoApiDoc_Is128BitCryptoApi()
    {
        var header = LegacyEncryptionHeaderReader.Parse(CryptoApiDoc(), LegacyContainer.Doc, DocMime);

        Assert.Equal(LegacyCipherScheme.CryptoApiRc4, header.Scheme);
        Assert.Equal(4, header.VersionMajor);
        Assert.Equal(2, header.VersionMinor);
        Assert.Equal(0x0Cu, header.Flags);
        Assert.Equal(128, header.KeySizeBits);
        Assert.Equal(126, header.HeaderSize);
        Assert.Equal(198, header.Payload.Length);
    }

    [Fact]
    public void Parse_HelloPpt_Is40BitCryptoApi()
    {
        var header = LegacyEncryptionHeaderReader.Parse(PptPayload(LegacyFixtures.HelloPpt), LegacyContainer.Ppt, PptMime);

        Assert.Equal(LegacyCipherScheme.CryptoApiRc4, header.Scheme);
        Assert.Equal(2, header.VersionMajor);
        Assert.Equal(0x04u, header.Flags);
        Assert.Equal(40, header.KeySizeBits);
        Assert.Equal(118, header.HeaderSize);
    }

    [Fact]
    public void Parse_Proc2356Ppt_KeySizeZeroMeans40()
    {
        var header = LegacyEncryptionHeaderReader.Parse(PptPayload(LegacyFixtures.CryptoApiProc2356Ppt), LegacyContainer.Ppt, PptMime);

        Assert.Equal(3, header.VersionMajor);
        Assert.Equal(40, header.KeySizeBits);
    }

    [Fact]
    public void Parse_TrailingBytes_AreNotCopiedIntoPayload()
    {
        var padded = CryptoApiDoc().Concat(new byte[100]).ToArray();

        var header = LegacyEncryptionHeaderReader.Parse(padded, LegacyContainer.Doc, DocMime);

        Assert.Equal(198, header.Payload.Length);
    }

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc, "tika", "wrong")]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc, "password", "passwordx")]
    [InlineData(LegacyFixtures.Rc4CryptoApiDoc, "Password1234_", "password1234_")]
    public void VerifyPassword_Doc_AcceptsRightAndRejectsWrong(string file, string right, string wrong)
    {
        var payload = DocPayload(file);

        Assert.True(Verify(payload, LegacyContainer.Doc, right));
        Assert.False(Verify(payload, LegacyContainer.Doc, wrong));
    }

    [Fact]
    public void VerifyPassword_HelloPpt_AcceptsRightAndRejectsWrong()
    {
        var payload = PptPayload(LegacyFixtures.HelloPpt);

        Assert.True(Verify(payload, LegacyContainer.Ppt, "hello"));
        Assert.False(Verify(payload, LegacyContainer.Ppt, "wrongpw"));
    }

    [Fact]
    public void VerifyPassword_LongPassword_IsTruncatedTo255()
    {
        var header = LegacyEncryptionHeaderReader.Parse(CryptoApiDoc(), LegacyContainer.Doc, DocMime);
        var decryptor = LegacyEncryptionHeaderReader.CreateDecryptor(header);
        var p300 = new string('a', 300);

        Assert.Equal(
            LegacyEncryptionHeaderReader.VerifyPassword(decryptor, p300[..255]),
            LegacyEncryptionHeaderReader.VerifyPassword(decryptor, p300));
    }

    [Fact]
    public void VerifyPassword_NullOrEmpty_Throws()
    {
        var decryptor = LegacyEncryptionHeaderReader.CreateDecryptor(
            LegacyEncryptionHeaderReader.Parse(CryptoApiDoc(), LegacyContainer.Doc, DocMime));

        Assert.Throws<ArgumentNullException>(() => LegacyEncryptionHeaderReader.VerifyPassword(decryptor, null!));
        Assert.Throws<ArgumentException>(() => LegacyEncryptionHeaderReader.VerifyPassword(decryptor, string.Empty));
    }

    public static TheoryData<string> CorruptMutationNames() => new(CorruptMutationMap.Keys);

    private static readonly Dictionary<string, Action<byte[]>> CorruptMutationMap = new()
    {
        ["headerSize 0"] = p => SetU32(p, OffHeaderSize, 0),
        ["headerSize max"] = p => SetU32(p, OffHeaderSize, uint.MaxValue),
        ["keySize 41"] = p => SetU32(p, OffKeySize, 41),
        ["keySize 256"] = p => SetU32(p, OffKeySize, 256),
        ["keySize max"] = p => SetU32(p, OffKeySize, 0x7FFFFFFF),
        ["saltSize 15"] = p => SetU32(p, 12 + (int)GetU32(p, OffHeaderSize), 15),
        ["verifierHashSize 16"] = p => SetU32(p, 12 + (int)GetU32(p, OffHeaderSize) + 36, 16),
        ["no fCryptoAPI"] = p => SetU32(p, OffFlags, GetU32(p, OffFlags) & ~LegacyEncryptionHeaderReader.FlagCryptoApi),
    };

    [Theory]
    [MemberData(nameof(CorruptMutationNames))]
    public void Parse_Doc_MutatedHeader_IsCorrupt(string name)
    {
        var mutated = With(CryptoApiDoc(), CorruptMutationMap[name]);

        var ex = Assert.Throws<CorruptFileException>(() => LegacyEncryptionHeaderReader.Parse(mutated, LegacyContainer.Doc, DocMime));
        Assert.Equal(DocMime, ex.DetectedMimeType);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(3)]
    public void Parse_Doc_TruncatedPayload_IsCorrupt(int length)
    {
        var truncated = CryptoApiDoc()[..length];

        Assert.Throws<CorruptFileException>(() => LegacyEncryptionHeaderReader.Parse(truncated, LegacyContainer.Doc, DocMime));
    }

    [Fact]
    public void Parse_Doc_TruncatedBinaryRc4_IsCorrupt()
    {
        var truncated = DocPayload(LegacyFixtures.TikaBinaryRc4Doc)[..51];

        Assert.Throws<CorruptFileException>(() => LegacyEncryptionHeaderReader.Parse(truncated, LegacyContainer.Doc, DocMime));
    }

    private static readonly Dictionary<string, Action<byte[]>> UnsupportedMutationMap = new()
    {
        ["version 4.4"] = p => { SetU16(p, OffVersionMajor, 4); SetU16(p, OffVersionMinor, 4); },
        ["version 9.9"] = p => { SetU16(p, OffVersionMajor, 9); SetU16(p, OffVersionMinor, 9); },
        ["version 3.3"] = p => { SetU16(p, OffVersionMajor, 3); SetU16(p, OffVersionMinor, 3); },
        ["fExternal"] = p => SetU32(p, OffFlags, GetU32(p, OffFlags) | LegacyEncryptionHeaderReader.FlagExternal),
        ["fAES"] = p => SetU32(p, OffFlags, GetU32(p, OffFlags) | LegacyEncryptionHeaderReader.FlagAes),
        ["AlgID 0x660E"] = p => SetU32(p, OffAlgId, 0x660E),
        ["AlgIDHash 0x8003"] = p => SetU32(p, OffAlgIdHash, 0x8003),
    };

    public static TheoryData<string> UnsupportedMutationNames() => new(UnsupportedMutationMap.Keys);

    [Theory]
    [MemberData(nameof(UnsupportedMutationNames))]
    public void Parse_Doc_UnsupportedScheme_IsPasswordProtected(string name)
    {
        var mutated = With(CryptoApiDoc(), UnsupportedMutationMap[name]);

        Assert.Throws<PasswordProtectedException>(() => LegacyEncryptionHeaderReader.Parse(mutated, LegacyContainer.Doc, DocMime));
    }

    [Theory]
    [MemberData(nameof(UnsupportedMutationNames))]
    public void Parse_Ppt_UnsupportedScheme_IsCorrupt(string name)
    {
        var mutated = With(PptPayload(LegacyFixtures.HelloPpt), UnsupportedMutationMap[name]);

        Assert.Throws<CorruptFileException>(() => LegacyEncryptionHeaderReader.Parse(mutated, LegacyContainer.Ppt, PptMime));
    }

    [Fact]
    public void Parse_Ppt_BinaryRc4Version_IsCorrupt()
    {
        var payload = DocPayload(LegacyFixtures.TikaBinaryRc4Doc);

        Assert.Throws<CorruptFileException>(() => LegacyEncryptionHeaderReader.Parse(payload, LegacyContainer.Ppt, PptMime));
    }

    [Fact]
    public void CorruptFileReasons_NeverContainThePasswords()
    {
        var cases = new (byte[] Payload, LegacyContainer Container)[]
        {
            (With(CryptoApiDoc(), p => SetU32(p, OffKeySize, 41)), LegacyContainer.Doc),
            (With(PptPayload(LegacyFixtures.HelloPpt), p => SetU32(p, OffAlgId, 0x660E)), LegacyContainer.Ppt),
            (DocPayload(LegacyFixtures.TikaBinaryRc4Doc)[..10], LegacyContainer.Doc),
        };

        foreach (var (payload, container) in cases)
        {
            var ex = Assert.Throws<CorruptFileException>(() => LegacyEncryptionHeaderReader.Parse(payload, container, DocMime));
            foreach (var secret in new[] { "tika", "password", "hello" })
            {
                Assert.DoesNotContain(secret, ex.ParseFailureReason, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
