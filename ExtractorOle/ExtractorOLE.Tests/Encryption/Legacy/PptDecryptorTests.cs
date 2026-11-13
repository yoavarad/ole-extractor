using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

public class PptDecryptorTests
{
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const ushort RecTypeTextChars = 0x0FA0;
    private const ushort RecTypeTextBytes = 0x0FA8;
    private const int UserEditAtomLength = 8 + 0x20;
    private const uint HugeRecLen = 0xFFFFFF00;

    private static (PptEncryptionLayout Layout, LegacyEncryptionHeader Header) Load(string file)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(LegacyFixtures.Bytes(file)));
        var layout = PptEncryptionProbe.Probe(fs.Root).Layout!;
        var header = LegacyEncryptionHeaderReader.Parse(
            PptEncryptionProbe.CryptSessionPayload(layout, PptMime), LegacyContainer.Ppt, PptMime);
        return (layout, header);
    }

    private static DecryptedPptCore Decrypt(PptEncryptionLayout layout, LegacyEncryptionHeader header, string password) =>
        PptDecryptor.DecryptCore(layout, header, password, PptMime);

    private static DecryptedPptCore DecryptFixture(string file)
    {
        var (layout, header) = Load(file);
        return Decrypt(layout, header, LegacyFixtures.Password(file)!);
    }

    private static IEnumerable<int> ContentObjectOffsets(PptEncryptionLayout layout) =>
        layout.PersistOffsets.Where(p => p.Key != layout.CryptSessionPersistId).Select(p => (int)p.Value);

    private static uint U32(byte[] data, long offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int)offset));

    // SHA-256 of the whole decrypted stream from an independent Python reference (hashlib SHA-1 key
    // derivation + own RC4 per persist object, everything else left as is). It agrees byte for byte with
    // msoffcrypto-tool 6.0.0 ppt97 on every persist object whose length msoffcrypto gets right;
    // msoffcrypto sizes an object as "next persist id's offset - offset", which is wrong whenever ids are
    // not in offset order (hello: id 2 follows id 3), so its whole-stream output is not a usable oracle.
    [Theory]
    [InlineData(LegacyFixtures.HelloPpt, "E645F2100EE7B3F8AA202D8E62587B818B2BAA2D7D9DFEFF20B15615FF5BCAF9")]
    [InlineData(LegacyFixtures.Hello56Ppt, "9644445D30788CB2CD8ED24F049400F575337724819B8BE67E82D90181D388AD")]
    [InlineData(LegacyFixtures.HelloNoPropsPpt, "8822F3075A092C492F79CA0A120DFB649976B91A269855F9F8A9636DEB224465")]
    [InlineData(LegacyFixtures.CryptoApiProc2356Ppt, "8D6910DE50736D0BC09BB16CE4B9335CD67252458BFD7FD36614B54610F7160B")]
    [InlineData(LegacyFixtures.PngEncryptedPpt, "7B510CE248088A3A5AB7B233AF2673E42810EA3EEB58CD3D92A83C6B12398776")]
    public void DecryptCore_PasswordFixture_MatchesReferenceHash(string file, string expected)
    {
        var (layout, header) = Load(file);
        var document = Decrypt(layout, header, LegacyFixtures.Password(file)!).PowerPointDocument;

        Assert.Equal(layout.PowerPointDocument.Length, document.Length);
        Assert.Equal(expected, Convert.ToHexString(SHA256.HashData(document)));
    }

    [Theory]
    [InlineData(LegacyFixtures.HelloPpt)]
    [InlineData(LegacyFixtures.Hello56Ppt)]
    [InlineData(LegacyFixtures.HelloNoPropsPpt)]
    [InlineData(LegacyFixtures.CryptoApiProc2356Ppt)]
    [InlineData(LegacyFixtures.PngEncryptedPpt)]
    public void DecryptCore_PasswordFixture_WalksWithoutStructuralErrors(string file)
    {
        var (layout, header) = Load(file);
        var result = Decrypt(layout, header, LegacyFixtures.Password(file)!);

        var walk = PptRecordWalker.WalkRecords(result.PowerPointDocument, ContentObjectOffsets(layout));

        Assert.Equal(0, walk.StructuralErrors);
        Assert.True(walk.Records > 0);
    }

    [Fact]
    public void DecryptCore_Proc2356_Walks1566RecordsIn12PersistObjects()
    {
        var (layout, header) = Load(LegacyFixtures.CryptoApiProc2356Ppt);
        var result = Decrypt(layout, header, "crypto");

        var offsets = ContentObjectOffsets(layout).ToList();
        var walk = PptRecordWalker.WalkRecords(result.PowerPointDocument, offsets);

        Assert.Equal(12, offsets.Count);
        Assert.Equal(1566, walk.Records);
        Assert.Equal(0, walk.StructuralErrors);
    }

    [Fact]
    public void DecryptCore_Hello_ContainsProtectedDocumentText()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var document = Decrypt(layout, header, "hello").PowerPointDocument;

        var texts = PptRecordWalker.WalkRecords(document, ContentObjectOffsets(layout)).Atoms
            .Where(a => a.RecType is RecTypeTextChars or RecTypeTextBytes)
            .Select(a => a.RecType == RecTypeTextChars
                ? Encoding.Unicode.GetString(document, a.BodyOffset, a.BodyLength)
                : Encoding.Latin1.GetString(document, a.BodyOffset, a.BodyLength));

        Assert.Contains(texts, t => t.Contains("This is a protected document", StringComparison.Ordinal));
    }

    [Fact]
    public void DecryptCore_Hello_RewritesOnlyTheCurrentUserToken()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var original = (byte[])layout.CurrentUser.Clone();

        var currentUser = Decrypt(layout, header, "hello").CurrentUser;

        Assert.Equal(PptEncryptionProbe.TokenEncrypted, U32(original, 12));
        Assert.Equal(0xE391C05Fu, U32(currentUser, 12));
        Assert.Equal(original.Length, currentUser.Length);
        Assert.Equal(original[..12], currentUser[..12]);
        Assert.Equal(original[16..], currentUser[16..]);
    }

    [Fact]
    public void DecryptCore_Hello_LeavesUnencryptedStructuresUntouched()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var document = Decrypt(layout, header, "hello").PowerPointDocument;

        var edit = (int)U32(layout.CurrentUser, 16);
        var directory = (int)U32(layout.PowerPointDocument, edit + 20);
        var crypt = (int)layout.CryptSessionOffset;
        var cryptLength = 8 + (int)U32(layout.PowerPointDocument, crypt + 4);
        Assert.Equal(layout.PowerPointDocument[edit..(edit + UserEditAtomLength)], document[edit..(edit + UserEditAtomLength)]);
        Assert.Equal(layout.PowerPointDocument[directory..edit], document[directory..edit]);
        Assert.Equal(layout.PowerPointDocument[crypt..(crypt + cryptLength)], document[crypt..(crypt + cryptLength)]);
    }

    // G1 evidence: MS-OFFCRYPTO 2.3.5.2 by hand (H0 = SHA1(salt || UTF-16LE password), Hfinal =
    // SHA1(H0 || LE32(block)), 40-bit key = first 5 bytes + 11 zero bytes) plus a hand-written RC4.
    [Fact]
    public void DecryptCore_Hello_PersistObject1EqualsHandWrittenRc4()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var document = Decrypt(layout, header, "hello").PowerPointDocument;

        var offset = (int)layout.PersistOffsets[1];
        var length = 8 + (int)U32(document, offset + 4);
        var salt = header.Payload.AsSpan(12 + header.HeaderSize + 4, 16);
        var h0 = Sha1([.. salt, .. Encoding.Unicode.GetBytes("hello")]);
        var block = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(block, 1);
        var key = new byte[16];
        Sha1([.. h0, .. block]).AsSpan(0, 5).CopyTo(key);

        var reference = Rc4(key, layout.PowerPointDocument.AsSpan(offset, length));

        Assert.Equal(40, header.KeySizeBits);
        Assert.Equal(reference, document[offset..(offset + length)]);
    }

    public static TheoryData<string> WrongPasswordFixtures => new()
    {
        LegacyFixtures.HelloPpt, LegacyFixtures.CryptoApiProc2356Ppt, LegacyFixtures.PngEncryptedPpt,
    };

    [Theory]
    [MemberData(nameof(WrongPasswordFixtures))]
    public void DecryptCore_WrongPassword_ThrowsInvalidPasswordAndLeavesInputsUntouched(string file)
    {
        var (layout, header) = Load(file);
        var document = (byte[])layout.PowerPointDocument.Clone();
        var currentUser = (byte[])layout.CurrentUser.Clone();

        var ex = Assert.Throws<InvalidPasswordException>(() => Decrypt(layout, header, "wrongpw"));

        Assert.Equal("INVALID_PASSWORD", ex.Code);
        Assert.Equal(new InvalidPasswordException().Message, ex.Message);
        Assert.Equal(document, layout.PowerPointDocument);
        Assert.Equal(currentUser, layout.CurrentUser);
    }

    // A persist map that would be corrupt still reports INVALID_PASSWORD: the verifier runs first.
    [Fact]
    public void DecryptCore_WrongPasswordWithCorruptPersistMap_ThrowsInvalidPassword()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var broken = layout with { PersistOffsets = WithEntry(layout, 1, (uint)layout.PowerPointDocument.Length) };

        Assert.Throws<InvalidPasswordException>(() => Decrypt(broken, header, "wrongpw"));
    }

    [Fact]
    public void DecryptCore_PersistOffsetPastStreamEnd_IsCorruptFile()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var broken = layout with { PersistOffsets = WithEntry(layout, 1, (uint)layout.PowerPointDocument.Length - 7) };

        AssertCorrupt(() => Decrypt(broken, header, "hello"));
    }

    [Fact]
    public void DecryptCore_TwoIdsSharingAnOffset_IsCorruptFile()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var broken = layout with { PersistOffsets = WithEntry(layout, 2, layout.PersistOffsets[1]) };

        AssertCorrupt(() => Decrypt(broken, header, "hello"));
    }

    // RC4 is XOR: flipping the ciphertext recLen by (plain ^ HugeRecLen) makes it decrypt to HugeRecLen.
    [Fact]
    public void DecryptCore_DecryptedRecLenPastStreamEnd_IsCorruptFile()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        var offset = (int)layout.PersistOffsets[1];
        var plainRecLen = U32(Decrypt(layout, header, "hello").PowerPointDocument, offset + 4);
        var patched = (byte[])layout.PowerPointDocument.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(patched.AsSpan(offset + 4), U32(patched, offset + 4) ^ plainRecLen ^ HugeRecLen);

        AssertCorrupt(() => Decrypt(layout with { PowerPointDocument = patched }, header, "hello"));
    }

    [Fact]
    public void DecryptCore_CancelledToken_ThrowsAfterVerification()
    {
        var (layout, header) = Load(LegacyFixtures.HelloPpt);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using (ExtractionCancellation.Enter(cts.Token))
        {
            Assert.Throws<OperationCanceledException>(() => Decrypt(layout, header, "hello"));
            Assert.Throws<InvalidPasswordException>(() => Decrypt(layout, header, "wrongpw"));
        }
    }

    [Fact]
    public void DecryptCore_ReturnsThePasswordVerifiedDecryptor()
    {
        var result = DecryptFixture(LegacyFixtures.HelloPpt);

        Assert.NotNull(result.Decryptor.GetSecretKey());
    }

    private static Dictionary<uint, uint> WithEntry(PptEncryptionLayout layout, uint id, uint offset) =>
        new(layout.PersistOffsets) { [id] = offset };

    private static void AssertCorrupt(Action action)
    {
        var ex = Assert.Throws<CorruptFileException>(action);
        Assert.Equal("CORRUPT_FILE", ex.Code);
        Assert.DoesNotContain("hello", ex.Message, StringComparison.Ordinal);
    }

    private static byte[] Sha1(byte[] data) => CryptographicOperations.HashData(HashAlgorithmName.SHA1, data);

    private static byte[] Rc4(byte[] key, ReadOnlySpan<byte> input)
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

        var output = new byte[input.Length];
        for (int k = 0, i = 0, j = 0; k < input.Length; k++)
        {
            i = (i + 1) & 0xFF;
            j = (j + s[i]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
            output[k] = (byte)(input[k] ^ s[(s[i] + s[j]) & 0xFF]);
        }

        return output;
    }
}
