using System.Buffers.Binary;
using System.Security.Cryptography;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// Expected hashes: msoffcrypto-tool 6.0.0 output, from the end of each stream's clear prefix (#403).
public class DocDecryptorTests
{
    private const string DocMime = "application/msword";
    private const long NoCap = long.MaxValue;
    private const int OffFlags = 0x0A;
    private const int OffLKey = 0x0E;
    private const string HovercraftXorDoc = "Test-weak-XOR_myhovercraftisfullofeels_.doc";
    private const string XorMe1234Doc = "xor_me-1234.doc";

    private static DecryptedDocStreams DecryptFixture(string file, string password, long maxEntryBytes = NoCap) =>
        DecryptBytes(LegacyFixtures.Bytes(file), password, maxEntryBytes);

    private static DecryptedDocStreams DecryptBytes(byte[] cfb, string password, long maxEntryBytes = NoCap)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)!;
        var header = DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime);
        return DocDecryptor.Decrypt(fs.Root, fib, header, password, maxEntryBytes);
    }

    private static string Sha256(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc, 52,
        "F54D3EB1D63365E9ADBDD4F06AB55190C55151E73E6014E315722308F280F870",
        "291BCDA9A7B95B36F4937819775A09FDB2768FDCEE7CD67B547DF1E553603F2D", null)]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc, 198,
        "15A4056311A45FE670A2A3D7DAEB5D85B0F23D0B9F7BCC39EFD47774EA404E51",
        "A64D304A931233EDB1872FD97892FCEF583E03D122950B671A93ADE56CA6C9C7",
        "CE9062485860C877C5174155E759F5AF64C3D8D8E7D109588CD48C8CF3B056AD")]
    [InlineData(LegacyFixtures.Rc4CryptoApiDoc, 198,
        "C4ED076BF41A9D3DCC2E83F3CC776D70C7A07CEF73DA26C63B798880817AA3F0",
        "9E97F8FC3FE1239B42929C62971087426520294420119FD96FB31A155CCBAD24", null)]
    public void Decrypt_PasswordFixture_MatchesMsoffcryptoHashes(
        string file, int lKey, string wordHash, string tableHash, string? dataHash)
    {
        var streams = DecryptFixture(file, LegacyFixtures.Password(file)!);

        Assert.Equal("1Table", streams.TableStreamName);
        Assert.Equal(wordHash, Sha256(streams.WordDocument.AsSpan(DocEncryptionProbe.FibBaseLength)));
        Assert.Equal(tableHash, Sha256(streams.Table.AsSpan(lKey)));
        if (dataHash is null)
        {
            Assert.Null(streams.Data);
        }
        else
        {
            Assert.Equal(dataHash, Sha256(streams.Data));
        }
    }

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc)]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc)]
    [InlineData(LegacyFixtures.Rc4CryptoApiDoc)]
    public void Decrypt_PasswordFixture_PreservesLengthsAndClearPrefixes(string file)
    {
        var cfb = LegacyFixtures.Bytes(file);
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)!;
        var original = ReadAll(fs.Root, "WordDocument");
        var originalTable = ReadAll(fs.Root, fib.TableStreamName);

        var streams = DocDecryptor.Decrypt(
            fs.Root, fib, DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime), LegacyFixtures.Password(file)!, NoCap);

        Assert.Equal(original.Length, streams.WordDocument.Length);
        Assert.Equal(originalTable.Length, streams.Table.Length);
        Assert.Equal(originalTable.AsSpan(0, (int)fib.LKey).ToArray(), streams.Table.AsSpan(0, (int)fib.LKey).ToArray());
        Assert.Equal(original.AsSpan(0, OffFlags).ToArray(), streams.WordDocument.AsSpan(0, OffFlags).ToArray());
        Assert.Equal(
            original.AsSpan(OffLKey + 4, DocEncryptionProbe.FibBaseLength - OffLKey - 4).ToArray(),
            streams.WordDocument.AsSpan(OffLKey + 4, DocEncryptionProbe.FibBaseLength - OffLKey - 4).ToArray());
        if (fs.Root.HasEntry("Data"))
        {
            Assert.Equal(((DocumentEntry)fs.Root.GetEntry("Data")).Size, streams.Data!.Length);
        }
    }

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc)]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc)]
    [InlineData(LegacyFixtures.Rc4CryptoApiDoc)]
    public void Decrypt_PasswordFixture_ClearsFibMarkersOnly(string file)
    {
        var cfb = LegacyFixtures.Bytes(file);
        ushort originalFlags;
        using (var fs = new NPOIFSFileSystem(new MemoryStream(cfb)))
        {
            originalFlags = BinaryPrimitives.ReadUInt16LittleEndian(ReadAll(fs.Root, "WordDocument").AsSpan(OffFlags));
        }

        var word = DecryptBytes(cfb, LegacyFixtures.Password(file)!).WordDocument;

        var flags = BinaryPrimitives.ReadUInt16LittleEndian(word.AsSpan(OffFlags));
        Assert.Equal(0, flags & 0x0100);
        Assert.Equal(0, flags & 0x8000);
        Assert.Equal(originalFlags & ~0x8100, flags);
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(word.AsSpan(OffLKey)));
    }

    [Fact]
    public void Decrypt_Tika_FlagsGoFrom13F0To12F0()
    {
        var word = DecryptFixture(LegacyFixtures.TikaBinaryRc4Doc, "tika").WordDocument;

        Assert.Equal(0x12F0, BinaryPrimitives.ReadUInt16LittleEndian(word.AsSpan(OffFlags)));
    }

    public static IEnumerable<object[]> WrongPasswords() => new[]
    {
        new object[] { LegacyFixtures.TikaBinaryRc4Doc, "wrong" },
        new object[] { LegacyFixtures.PasswordCryptoApiDoc, "passwordx" },
        new object[] { LegacyFixtures.Rc4CryptoApiDoc, "password1234_" },
        new object[] { LegacyFixtures.PasswordProtectedDoc, "wrong" },
        new object[] { LegacyFixtures.PasswordProtectedDoc, "password" },
        new object[] { LegacyFixtures.PasswordProtectedDoc, "Password" },
        new object[] { LegacyFixtures.PasswordProtectedDoc, "test" },
        new object[] { LegacyFixtures.PasswordProtectedDoc, "1234" },
        new object[] { LegacyFixtures.PasswordProtectedDoc, "secret" },
    };

    [Theory]
    [MemberData(nameof(WrongPasswords))]
    public void Decrypt_WrongPassword_ThrowsInvalidPasswordWithoutPasswordInMessage(string file, string password)
    {
        var ex = Assert.Throws<InvalidPasswordException>(() => DecryptFixture(file, password));

        Assert.Equal("INVALID_PASSWORD", ex.Code);
        Assert.Equal(new InvalidPasswordException().Message, ex.Message);
    }

    // The fib and header come from the intact file; the root handed to Decrypt has every encrypted
    // stream removed. Any stream access would surface as CORRUPT_FILE, so INVALID_PASSWORD proves the
    // verifier rejects the password before a stream is read.
    [Theory]
    [MemberData(nameof(WrongPasswords))]
    public void Decrypt_WrongPassword_RejectsBeforeReadingAnyStream(string file, string password)
    {
        var cfb = LegacyFixtures.Bytes(file);
        DocFibProbe fib;
        LegacyEncryptionHeader header;
        using (var fs = new NPOIFSFileSystem(new MemoryStream(cfb)))
        {
            fib = DocEncryptionProbe.ReadFib(fs.Root)!;
            header = DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime);
        }

        var stripped = CfbMutator.RemoveEntry(CfbMutator.RemoveEntry(cfb, "WordDocument"), fib.TableStreamName);
        using (var check = new NPOIFSFileSystem(new MemoryStream(stripped)))
        {
            if (check.Root.HasEntry("Data"))
            {
                stripped = CfbMutator.RemoveEntry(stripped, "Data");
            }
        }

        using var strippedFs = new NPOIFSFileSystem(new MemoryStream(stripped));

        Assert.Throws<InvalidPasswordException>(() =>
            DocDecryptor.Decrypt(strippedFs.Root, fib, header, password, NoCap));
    }

    [Fact]
    public void Decrypt_StrippedRootWithRightPassword_IsCorrupt()
    {
        var cfb = LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc);
        DocFibProbe fib;
        LegacyEncryptionHeader header;
        using (var fs = new NPOIFSFileSystem(new MemoryStream(cfb)))
        {
            fib = DocEncryptionProbe.ReadFib(fs.Root)!;
            header = DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime);
        }

        using var strippedFs = new NPOIFSFileSystem(new MemoryStream(CfbMutator.RemoveEntry(cfb, "WordDocument")));

        Assert.Throws<CorruptFileException>(() => DocDecryptor.Decrypt(strippedFs.Root, fib, header, "tika", NoCap));
    }

    [Fact]
    public void Decrypt_StreamOverPerEntryCap_ThrowsOversized()
    {
        var ex = Assert.Throws<OversizedNestedContentException>(() =>
            DecryptFixture(LegacyFixtures.PasswordCryptoApiDoc, "password", 1000));

        Assert.Equal("OVERSIZED_NESTED_CONTENT", ex.Code);
        Assert.DoesNotContain("password", ex.Message, StringComparison.Ordinal);
    }

    // WordDocument grown to 4 MiB of trailing bytes (verification only reads the header): the cap
    // must fire before a stream-sized buffer exists.
    [Fact]
    public void Decrypt_StreamOverPerEntryCap_ThrowsBeforeAllocation()
    {
        const int Grown = 4 * 1024 * 1024;
        var cfb = CfbMutator.ReplaceStream(
            LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc), "WordDocument", w => [.. w, .. new byte[Grown]]);
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)!;
        var header = DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime);

        var before = GC.GetAllocatedBytesForCurrentThread();
        Assert.Throws<OversizedNestedContentException>(() =>
            DocDecryptor.Decrypt(fs.Root, fib, header, "password", Grown));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(allocated < 1024 * 1024, $"allocated {allocated} bytes");
    }

    [Fact]
    public void Decrypt_CancelledToken_ThrowsAfterVerificationBeforeStreams()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using (ExtractionCancellation.Enter(cts.Token))
        {
            Assert.Throws<OperationCanceledException>(() => DecryptFixture(LegacyFixtures.TikaBinaryRc4Doc, "tika"));
            Assert.Throws<InvalidPasswordException>(() => DecryptFixture(LegacyFixtures.TikaBinaryRc4Doc, "wrong"));
        }
    }

    [Fact]
    public void Decrypt_WordDocumentShorterThanFibBase_IsCorrupt()
    {
        var cfb = LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc);
        DocFibProbe fib;
        LegacyEncryptionHeader header;
        using (var fs = new NPOIFSFileSystem(new MemoryStream(cfb)))
        {
            fib = DocEncryptionProbe.ReadFib(fs.Root)!;
            header = DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime);
        }

        var truncated = CfbMutator.ReplaceStream(cfb, "WordDocument", w => w[..40]);
        using var truncatedFs = new NPOIFSFileSystem(new MemoryStream(truncated));

        Assert.Throws<CorruptFileException>(() => DocDecryptor.Decrypt(truncatedFs.Root, fib, header, "tika", NoCap));
    }

    // The twin is msoffcrypto-tool's decrypted output (manifest unencryptedTwin). WordDocument is
    // byte-identical, FIB included: msoffcrypto-tool clears the same markers. The table stream differs
    // only in its first lKey bytes: msoffcrypto-tool runs the keystream over the encryption header too,
    // while #403 step 4 keeps those clear bytes as they are (no FIB fc points below lKey).
    [Fact]
    public void Decrypt_Rc4CryptoApi_MatchesUnencryptedTwinStreams()
    {
        var cfb = LegacyFixtures.Bytes(LegacyFixtures.Rc4CryptoApiDoc);
        byte[] encryptedTable;
        using (var fs = new NPOIFSFileSystem(new MemoryStream(cfb)))
        {
            encryptedTable = ReadAll(fs.Root, "1Table");
        }

        var streams = DecryptBytes(cfb, LegacyFixtures.Password(LegacyFixtures.Rc4CryptoApiDoc)!);
        using var twin = new NPOIFSFileSystem(new MemoryStream(
            File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainTwinDocPath))));
        const int LKey = 198;

        Assert.Equal(ReadAll(twin.Root, "WordDocument"), streams.WordDocument);
        var twinTable = ReadAll(twin.Root, streams.TableStreamName);
        Assert.Equal(twinTable.Length, streams.Table.Length);
        Assert.Equal(twinTable.AsSpan(LKey).ToArray(), streams.Table.AsSpan(LKey).ToArray());
        Assert.Equal(encryptedTable.AsSpan(0, LKey).ToArray(), streams.Table.AsSpan(0, LKey).ToArray());
        Assert.Equal(twin.Root.HasEntry("Data"), streams.Data is not null);
    }

    private static DecryptedDocStreams DecryptXorBytes(byte[] cfb, string password)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)!;
        return DocDecryptor.DecryptXor(fs.Root, fib, password, NoCap);
    }

    [Theory]
    [InlineData(HovercraftXorDoc, true)]
    [InlineData(XorMe1234Doc, false)]
    public void DecryptXor_RightPassword_PreservesLengthsAndClearsFibMarkersOnly(string file, bool hasData)
    {
        var cfb = LegacyFixtures.Bytes(file);
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)!;
        var original = ReadAll(fs.Root, "WordDocument");
        var originalTable = ReadAll(fs.Root, fib.TableStreamName);

        var streams = DocDecryptor.DecryptXor(fs.Root, fib, LegacyFixtures.Password(file)!, NoCap);

        Assert.Equal(original.Length, streams.WordDocument.Length);
        Assert.Equal(originalTable.Length, streams.Table.Length);
        Assert.Equal(original.AsSpan(0, OffFlags).ToArray(), streams.WordDocument.AsSpan(0, OffFlags).ToArray());
        var originalFlags = BinaryPrimitives.ReadUInt16LittleEndian(original.AsSpan(OffFlags));
        Assert.Equal(originalFlags & ~0x8100, BinaryPrimitives.ReadUInt16LittleEndian(streams.WordDocument.AsSpan(OffFlags)));
        Assert.Equal(original.AsSpan(OffFlags + 2, 2).ToArray(), streams.WordDocument.AsSpan(OffFlags + 2, 2).ToArray());
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(streams.WordDocument.AsSpan(OffLKey)));
        Assert.Equal(
            original.AsSpan(OffLKey + 4, DocEncryptionProbe.FibBaseLength - OffLKey - 4).ToArray(),
            streams.WordDocument.AsSpan(OffLKey + 4, DocEncryptionProbe.FibBaseLength - OffLKey - 4).ToArray());
        Assert.Equal(hasData, streams.Data is not null);
    }

    [Theory]
    [InlineData(HovercraftXorDoc, "wrong")]
    [InlineData(XorMe1234Doc, "12345")]
    public void DecryptXor_WrongPassword_ThrowsInvalidPassword(string file, string password)
    {
        Assert.Throws<InvalidPasswordException>(() => DecryptXorBytes(LegacyFixtures.Bytes(file), password));
    }

    [Fact]
    public void DecryptXor_OnlyFirst15PasswordCharactersCount()
    {
        var cfb = LegacyFixtures.Bytes(HovercraftXorDoc);

        var full = DecryptXorBytes(cfb, LegacyFixtures.Password(HovercraftXorDoc)!);
        var truncated = DecryptXorBytes(cfb, "myhovercraftisf");

        Assert.Equal(full.WordDocument, truncated.WordDocument);
        Assert.Equal(full.Table, truncated.Table);
    }

    // Euro sign: NPOI's low-byte rule gives 0xAC, Windows-1252 gives 0x80, so only the 1252 path matches lKey.
    [Fact]
    public void DecryptXor_Windows1252StandIn_MatchesWhenRawConversionDoesNot()
    {
        var euroX = new string([(char)0x20AC, 'x']);
        var lKey = unchecked((uint)CryptoFunctions.CreateXorVerifier2("\u0080x"));
        var cfb = CfbMutator.ReplaceStream(LegacyFixtures.Bytes(XorMe1234Doc), "WordDocument", w =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(w.AsSpan(OffLKey), lKey);
            return w;
        });

        Assert.NotEqual(lKey, unchecked((uint)CryptoFunctions.CreateXorVerifier2(euroX)));
        var streams = DecryptXorBytes(cfb, euroX);
        Assert.NotNull(streams.WordDocument);
    }

    [Fact]
    public void DecryptXor_NonXorProbe_ThrowsArgumentException()
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(LegacyFixtures.Bytes(XorMe1234Doc)));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)! with { Kind = DocEncryptionKind.Rc4 };

        Assert.Throws<ArgumentException>(() => DocDecryptor.DecryptXor(fs.Root, fib, "1234", NoCap));
    }

    private static byte[] ReadAll(DirectoryNode root, string name)
    {
        var entry = (DocumentEntry)root.GetEntry(name);
        var bytes = new byte[entry.Size];
        using var input = root.CreateDocumentInputStream(entry);
        input.ReadFully(bytes);
        return bytes;
    }
}
