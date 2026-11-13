using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.Crypt.CryptoAPI;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

public class PptPicturesDecryptorTests
{
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const string PicturesStream = "Pictures";

    // samples/manifest.json "Pictures" sha256 of the decrypted fixtures (msoffcrypto-tool key derivation
    // + per-record Pictures decryption, docs/research/legacy-encryption/ppt-encryption.md 2.2).
    private const string Proc2356PicturesSha256 = "EB85DE0ED697EC2FE8F52C8279AD1A36FE5BE6CCD51FC43E48DAA363451508AA";
    private const string PngPicturesSha256 = "8018CAA4D05308312FAD6AED55EC6648DFD0CB7BD2DCC75B8731D9E2B50A72B4";

    private const ushort RecTypeBlipJpeg = 0xF01D;
    private const ushort RecTypeBlipPng = 0xF01E;

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly int[] TwoUidInstances = [0x217, 0x3D5, 0x46B, 0x543, 0x6E1, 0x6E3, 0x6E5, 0x7A9];

    private sealed record Fixture(byte[] Pictures, CryptoAPIDecryptor Decryptor, LegacyEncryptionHeader Header, string Password);

    private static Fixture Load(string file)
    {
        var password = LegacyFixtures.Password(file)!;
        using var fs = new NPOIFSFileSystem(new MemoryStream(LegacyFixtures.Bytes(file)));
        var layout = PptEncryptionProbe.Probe(fs.Root).Layout!;
        var header = LegacyEncryptionHeaderReader.Parse(
            PptEncryptionProbe.CryptSessionPayload(layout, PptMime), LegacyContainer.Ppt, PptMime);
        var decryptor = PptDecryptor.DecryptCore(layout, header, password, PptMime).Decryptor;
        return new Fixture(ReadStream(fs.Root, PicturesStream), decryptor, header, password);
    }

    private static byte[] Decrypt(Fixture fixture, byte[]? pictures = null) =>
        PptPicturesDecryptor.Decrypt(pictures ?? fixture.Pictures, fixture.Decryptor, PptMime);

    [Fact]
    public void Decrypt_Proc2356_MatchesManifestHashAndLength()
    {
        var output = Decrypt(Load(LegacyFixtures.CryptoApiProc2356Ppt));

        Assert.Equal(1026524, output.Length);
        Assert.Equal(Proc2356PicturesSha256, Convert.ToHexString(SHA256.HashData(output)));
    }

    [Fact]
    public void Decrypt_Proc2356_SevenBlipsStartWithTheirImageSignature()
    {
        var output = Decrypt(Load(LegacyFixtures.CryptoApiProc2356Ppt));

        var blips = WalkBlips(output);

        Assert.Equal(7, blips.Count);
        Assert.Equal(5, blips.Count(b => b.RecType == RecTypeBlipPng));
        Assert.Equal(2, blips.Count(b => b.RecType == RecTypeBlipJpeg));
        Assert.All(blips, blip =>
        {
            var signature = blip.RecType == RecTypeBlipPng ? PngSignature : JpegSignature;
            Assert.Equal(signature, output[blip.DataOffset..(blip.DataOffset + signature.Length)]);
        });
    }

    [Fact]
    public void Decrypt_PngFixture_MatchesManifestHashAndLength()
    {
        var output = Decrypt(Load(LegacyFixtures.PngEncryptedPpt));

        Assert.Equal(16588, output.Length);
        Assert.Equal(PngPicturesSha256, Convert.ToHexString(SHA256.HashData(output)));
    }

    [Fact]
    public void Decrypt_PngFixture_SecondPngEqualsTheUnencryptedTwinsPng()
    {
        var output = Decrypt(Load(LegacyFixtures.PngEncryptedPpt));
        using var twin = new NPOIFSFileSystem(new MemoryStream(File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainReferencePptPath))));
        var twinPictures = ReadStream(twin.Root, PicturesStream);

        var second = PngAt(output, IndexOf(output, PngSignature, IndexOf(output, PngSignature, 0) + 1));
        var twinPng = PngAt(twinPictures, IndexOf(twinPictures, PngSignature, 0));

        Assert.StartsWith("711F23417E83917F", Convert.ToHexString(Sha1(second)), StringComparison.Ordinal);
        Assert.Equal(twinPng, second);
    }

    [Theory]
    [InlineData(LegacyFixtures.CryptoApiProc2356Ppt)]
    [InlineData(LegacyFixtures.PngEncryptedPpt)]
    public void Decrypt_Fixture_EqualsOwnCipherReference(string file)
    {
        var fixture = Load(file);

        var output = Decrypt(fixture);

        Assert.NotEqual(fixture.Pictures, output);
        Assert.Equal(ReferenceDecrypt(fixture), output);
    }

    [Fact]
    public void Decrypt_LeavesTheInputUntouched()
    {
        var fixture = Load(LegacyFixtures.PngEncryptedPpt);
        var original = (byte[])fixture.Pictures.Clone();

        var output = Decrypt(fixture);

        Assert.Equal(original, fixture.Pictures);
        Assert.NotSame(fixture.Pictures, output);
    }

    [Fact]
    public void Decrypt_CiphertextTruncatedInsideARecord_IsCorruptFile()
    {
        var fixture = Load(LegacyFixtures.PngEncryptedPpt);

        AssertCorrupt(() => Decrypt(fixture, fixture.Pictures[..^10]));
    }

    // RC4 is XOR: flipping ciphertext bits flips the same plaintext bits. A first record of recLen 4
    // cannot hold its 16-byte rgbUid.
    [Fact]
    public void Decrypt_FieldPastRecordEnd_IsCorruptFile()
    {
        var fixture = Load(LegacyFixtures.PngEncryptedPpt);
        var patched = PatchFirstHeader(fixture, recType: null, recLen: 4);

        AssertCorrupt(() => Decrypt(fixture, patched));
    }

    [Fact]
    public void Decrypt_UnknownRecordType_StopsAndLeavesTheRestAsStored()
    {
        var fixture = Load(LegacyFixtures.PngEncryptedPpt);
        var patched = PatchFirstHeader(fixture, recType: 0x1234, recLen: null);

        var output = Decrypt(fixture, patched);

        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(2)));
        Assert.Equal(patched[8..], output[8..]);
    }

    // Crafted stream: an FBSE (cbName 4) embedding an EMF blip with recInstance 0x3D5 (two rgbUids,
    // 34-byte metafile header), then a record of unknown type, then a 3-byte tail. The field partition
    // below is written out by hand from MS-PPT 2.3.7 / MS-ODRAW, independently of the walker.
    [Fact]
    public void Decrypt_CraftedFbseWithEmfBlip_DecryptsEveryFieldAndStopsAtTheUnknownRecord()
    {
        var fixture = Load(LegacyFixtures.PngEncryptedPpt);
        const int NameLength = 4;
        const int BlipDataLength = 20;
        const int BlipLength = 16 + 16 + 34 + BlipDataLength;
        const int FbseLength = 36 + NameLength + 8 + BlipLength;
        var plain = new byte[8 + FbseLength + 8 + 5 + 3];
        for (var i = 0; i < plain.Length; i++)
        {
            plain[i] = (byte)(i * 7);
        }

        WriteHeader(plain, 0, 0x002F, 0xF007, FbseLength);
        plain[8 + 33] = NameLength;
        WriteHeader(plain, 8 + 36 + NameLength, 0x3D50, 0xF01A, BlipLength);
        var unknown = 8 + FbseLength;
        WriteHeader(plain, unknown, 0, 0x1234, 5);

        int[] fields = [8, 1, 1, 16, 2, 4, 4, 4, 1, 1, 1, 1, NameLength, 8, 16, 16, 34, BlipDataLength, 8];
        var key = ReferenceKey(fixture);
        var cipher = (byte[])plain.Clone();
        var pos = 0;
        foreach (var size in fields)
        {
            Rc4(key, cipher.AsSpan(pos, size));
            pos += size;
        }

        Assert.Equal(unknown + 8, pos);
        var output = Decrypt(fixture, cipher);

        Assert.Equal(plain[..pos], output[..pos]);
        Assert.Equal(cipher[pos..], output[pos..]);
    }

    [Fact]
    public void Decrypt_EmptyStream_ReturnsEmptyCopy()
    {
        var fixture = Load(LegacyFixtures.PngEncryptedPpt);

        Assert.Empty(Decrypt(fixture, []));
    }

    [Fact]
    public void Decrypt_CancelledToken_Throws()
    {
        var fixture = Load(LegacyFixtures.PngEncryptedPpt);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using (ExtractionCancellation.Enter(cts.Token))
        {
            Assert.Throws<OperationCanceledException>(() => Decrypt(fixture));
        }
    }

    // Patches the ciphertext of the first record header so it decrypts to the given type and/or length.
    private static byte[] PatchFirstHeader(Fixture fixture, ushort? recType, uint? recLen)
    {
        var plain = Decrypt(fixture);
        var patched = (byte[])fixture.Pictures.Clone();
        if (recType is { } type)
        {
            var flip = (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(plain.AsSpan(2)) ^ type);
            BinaryPrimitives.WriteUInt16LittleEndian(patched.AsSpan(2), (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(patched.AsSpan(2)) ^ flip));
        }

        if (recLen is { } length)
        {
            var flip = BinaryPrimitives.ReadUInt32LittleEndian(plain.AsSpan(4)) ^ length;
            BinaryPrimitives.WriteUInt32LittleEndian(patched.AsSpan(4), BinaryPrimitives.ReadUInt32LittleEndian(patched.AsSpan(4)) ^ flip);
        }

        return patched;
    }

    // Independent re-implementation of the prototype's own-cipher path: MS-OFFCRYPTO 2.3.5.2 key
    // derivation by hand (SHA-1, block 0) and a hand-written RC4 restarted for every field.
    private static byte[] ReferenceDecrypt(Fixture fixture)
    {
        var key = ReferenceKey(fixture);
        var data = (byte[])fixture.Pictures.Clone();
        var pos = 0;
        void Field(int count)
        {
            Rc4(key, data.AsSpan(pos, count));
            pos += count;
        }

        while (pos + 8 <= data.Length)
        {
            var start = pos;
            Field(8);
            var instance = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(start)) >> 4;
            var type = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(start + 2));
            var end = start + 8 + (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(start + 4));
            if (type != 0xF007 && (type < 0xF018 || type > 0xF117))
            {
                break;
            }

            if (type == 0xF007)
            {
                var body = pos;
                foreach (var size in new[] { 1, 1, 16, 2, 4, 4, 4, 1, 1, 1, 1 })
                {
                    Field(size);
                }

                Field(data[body + 33]);
                var blip = pos;
                Field(8);
                instance = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(blip)) >> 4;
                type = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(blip + 2));
            }

            Field(16);
            if (Array.IndexOf(TwoUidInstances, instance) >= 0)
            {
                Field(16);
            }

            Field(type is 0xF01A or 0xF01B or 0xF01C ? 34 : 1);
            Field(end - pos);
        }

        return data;
    }

    // Block-0 RC4 key: H0 = SHA1(salt || UTF-16LE password), Hfinal = SHA1(H0 || LE32(0)); a 40-bit
    // key is padded with zeros to 16 bytes.
    private static byte[] ReferenceKey(Fixture fixture)
    {
        var header = fixture.Header;
        var salt = header.Payload.AsSpan(12 + header.HeaderSize + 4, 16);
        var h0 = Sha1([.. salt, .. Encoding.Unicode.GetBytes(fixture.Password)]);
        var hFinal = Sha1([.. h0, 0, 0, 0, 0]);
        var key = new byte[Math.Max(16, header.KeySizeBits / 8)];
        hFinal.AsSpan(0, header.KeySizeBits / 8).CopyTo(key);
        return key;
    }

    private static void WriteHeader(byte[] data, int offset, ushort verInstance, ushort recType, int recLen)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), verInstance);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset + 2), recType);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset + 4), recLen);
    }

    private static void Rc4(byte[] key, Span<byte> data)
    {
        var s = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }

        for (int i = 0, j = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        for (int k = 0, i = 0, j = 0; k < data.Length; k++)
        {
            i = (i + 1) & 0xFF;
            j = (j + s[i]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
            data[k] ^= s[(s[i] + s[j]) & 0xFF];
        }
    }

    // Top-level blip records of a decrypted Pictures stream and the offset of each blip's image data.
    private static List<(ushort RecType, int DataOffset)> WalkBlips(byte[] data)
    {
        var blips = new List<(ushort, int)>();
        for (var pos = 0; pos + 8 <= data.Length;)
        {
            var instance = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(pos)) >> 4;
            var type = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(pos + 2));
            var uids = Array.IndexOf(TwoUidInstances, instance) >= 0 ? 2 : 1;
            var extra = type is 0xF01A or 0xF01B or 0xF01C ? 34 : 1;
            blips.Add((type, pos + 8 + (16 * uids) + extra));
            pos += 8 + (int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(pos + 4));
        }

        return blips;
    }

    private static byte[] PngAt(byte[] data, int start)
    {
        var iend = IndexOf(data, "IEND"u8.ToArray(), start);
        return data[start..(iend + 8)];
    }

    private static int IndexOf(byte[] data, byte[] pattern, int from)
    {
        var index = data.AsSpan(from).IndexOf(pattern);
        Assert.True(index >= 0, "pattern not found");
        return from + index;
    }

    private static byte[] ReadStream(DirectoryNode root, string name)
    {
        var entry = (DocumentEntry)root.GetEntry(name);
        using var input = root.CreateDocumentInputStream(entry);
        var bytes = new byte[entry.Size];
        input.ReadExactly(bytes);
        return bytes;
    }

    private static byte[] Sha1(byte[] data) => CryptographicOperations.HashData(HashAlgorithmName.SHA1, data);

    private static void AssertCorrupt(Action action)
    {
        var ex = Assert.Throws<CorruptFileException>(action);
        Assert.Equal("CORRUPT_FILE", ex.Code);
    }
}
