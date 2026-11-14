using System.Buffers.Binary;
using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.Crypt.CryptoAPI;
using NPOI.POIFS.FileSystem;
using NPOI.Util;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// #410 (S-lg-quality): the five known gaps G1..G5 of [ydk:req:extraction/legacy-decryption]. G1, G4 and G5
// run on crafted files through the production DI wiring; G2 has a feasibility test; G3 has no test (no
// fixture). Every gap is also an entry in the "$encryptedTestGaps" list of samples/manifest.json, which
// the schema test below enforces. An entry means the gap is open: it is removed only in the change that
// adds a real-fixture assertion.
public sealed class LegacyKnownGapTests : IDisposable
{
    private const string DocMime = "application/msword";
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const string SummaryName = "\u0005SummaryInformation";
    private const string Password = "password";
    private const int OffsetHeaderToken = 12;
    private const int HeaderStart = 12;
    private const int KeySizeOffset = HeaderStart + 16;
    private const int SaltOffset = 142;
    private const int SaltLength = 16;
    private const int EncryptedVerifierOffset = SaltOffset + SaltLength;
    private const int EncryptedVerifierHashOffset = EncryptedVerifierOffset + 16 + 4;
    private const int EncryptedVerifierHashLength = 20;
    private const uint TokenEncrypted = 0xF3D1C4DF;

    private static readonly byte[] KnownVerifier =
        [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10];

    private static readonly string[] RequiredFields = ["id", "description", "behaviour", "blockedOn"];
    private static readonly string[] KnownGaps = ["G1", "G2", "G3", "G4", "G5"];

    private readonly ServiceProvider _provider;
    private readonly MainExtractor _extractor;

    public LegacyKnownGapTests()
    {
        var services = new ServiceCollection();
        ServiceRegistration.Register(services);
        _provider = services.BuildServiceProvider();
        _extractor = _provider.GetRequiredService<MainExtractor>();
    }

    public void Dispose() => _provider.Dispose();

    private DocumentExtractionResult Extract(byte[] bytes, string? password, string mime, string fileName) =>
        _extractor.Extract(new ExtractionRequest { FileBytes = bytes, FileName = fileName, DetectedMimeType = mime, Password = password });

    private DocumentExtractionResult ExtractDoc(byte[] bytes, string? password) => Extract(bytes, password, DocMime, "gap.doc");

    private DocumentExtractionResult ExtractPpt(byte[] bytes, string? password) => Extract(bytes, password, PptMime, "gap.ppt");

    private static string Json(DocumentExtractionResult result) =>
        JsonSerializer.Serialize(result) + JsonSerializer.Serialize(result.FormatMetadata, result.FormatMetadata?.GetType() ?? typeof(object));

    private static byte[] ReadStream(NPOIFSFileSystem fs, string name)
    {
        var entry = (DocumentEntry)fs.Root.GetEntry(name);
        var data = new byte[entry.Size];
        using var input = fs.Root.CreateDocumentInputStream(entry);
        input.ReadFully(data);
        return data;
    }

    private static byte[] ReadStream(byte[] cfb, string name)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb, writable: false));
        return ReadStream(fs, name);
    }

    // ---- G1: 40-bit CryptoAPI .doc (no Word-authored fixture) ----
    // Self-consistent only: the key, verifier and streams are all produced by the same NPOI code that
    // decrypts them. Real-file proof for the 40-bit derivation comes from the 40-bit ppt fixtures, which
    // share NPOI's CryptoAPIDecryptor.

    [Fact]
    public void Extract_Crafted40BitCryptoApiDoc_WithRightPassword_ReturnsTheFixtureText()
    {
        var fixture = LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc);
        var crafted = Craft40BitDoc(fixture);

        AssertHeaderKeySize(fixture, 128);
        AssertHeaderKeySize(crafted, 40);
        Assert.NotEqual(ReadStream(fixture, "WordDocument"), ReadStream(crafted, "WordDocument"));

        var expected = ExtractDoc(fixture, Password);
        var result = ExtractDoc(crafted, Password);

        Assert.Equal("This is a test", result.ExtractedText.Trim());
        Assert.Empty(result.FieldErrors);
        Assert.Equal(Json(expected), Json(result));
    }

    [Fact]
    public void Extract_Crafted40BitCryptoApiDoc_WithWrongPasswordOrNone_IsRejected()
    {
        var crafted = Craft40BitDoc(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc));

        Assert.Throws<InvalidPasswordException>(() => ExtractDoc(crafted, "not-the-password"));
        Assert.Throws<PasswordProtectedException>(() => ExtractDoc(crafted, null));
    }

    private static void AssertHeaderKeySize(byte[] doc, int expectedBits)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(doc, writable: false));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)!;
        Assert.Equal(expectedBits, DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime).KeySizeBits);
    }

    // The fixture re-encrypted under a 40-bit key: header keySize 40, a verifier pair for a known verifier
    // and the fixture salt, and every encrypted stream re-encrypted per 512-byte block (RC4 is symmetric).
    private static byte[] Craft40BitDoc(byte[] fixture)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(fixture, writable: false));
        var fib = DocEncryptionProbe.ReadFib(fs.Root)!;
        var header = DocEncryptionProbe.ReadHeader(fs.Root, fib, DocMime);
        var plain = DocDecryptor.Decrypt(fs.Root, fib, header, Password, long.MaxValue);

        var payload = WithVerifierFor(header.Payload, 40, out var encryptor);

        var word = ReencryptKeepingPrefix(encryptor, plain.WordDocument, ReadStream(fs, "WordDocument"), DocEncryptionProbe.FibBaseLength);
        var tableOriginal = ReadStream(fs, fib.TableStreamName);
        var table = ReencryptKeepingPrefix(encryptor, plain.Table, tableOriginal, (int)fib.LKey);
        payload.CopyTo(table, 0);

        var result = CfbMutator.ReplaceStream(fixture, "WordDocument", _ => word);
        result = CfbMutator.ReplaceStream(result, fib.TableStreamName, _ => table);
        if (plain.Data is not null)
        {
            var data = ReencryptKeepingPrefix(encryptor, plain.Data, ReadStream(fs, "Data"), 0);
            result = CfbMutator.ReplaceStream(result, "Data", _ => data);
        }

        return result;
    }

    // Copy of payload with keySize set (when different) and the verifier / verifier hash re-encrypted for
    // password "password" under that key size; the fixture salt is kept.
    private static byte[] WithVerifierFor(byte[] payload, uint keySize, out CryptoAPIEncryptor encryptor)
    {
        var patched = (byte[])payload.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(patched.AsSpan(KeySizeOffset), keySize);
        var info = new EncryptionInfo(new LittleEndianInputStream(new ByteArrayInputStream(patched)), null);
        encryptor = (CryptoAPIEncryptor)info.Encryptor;
        var salt = patched.AsSpan(SaltOffset, SaltLength).ToArray();
        encryptor.ConfirmPassword(Password, null, null, KnownVerifier, salt, null);
        info.Verifier.EncryptedVerifier.CopyTo(patched, EncryptedVerifierOffset);
        info.Verifier.EncryptedVerifierHash.AsSpan(0, EncryptedVerifierHashLength).CopyTo(patched.AsSpan(EncryptedVerifierHashOffset));
        return patched;
    }

    private static byte[] ReencryptKeepingPrefix(CryptoAPIEncryptor encryptor, byte[] plain, byte[] original, int clearPrefix)
    {
        var result = (byte[])plain.Clone();
        for (int offset = 0, block = 0; offset < result.Length; offset += DocDecryptor.ChunkSize, block++)
        {
            var length = Math.Min(DocDecryptor.ChunkSize, result.Length - offset);
            encryptor.InitCipherForBlock(null, block).Update(result, offset, length, result, offset);
        }

        original.AsSpan(0, clearPrefix).CopyTo(result);
        return result;
    }

    // ---- G2: encrypted doc properties (the follow-up is feasible) ----
    // The behaviour today (FieldError Metadata CORRUPT_FILE) is covered by
    // DocDecryptionExtractTests.Extract_CryptoApiDocWithEncryptedProperties_KeepsTextAndRecordsOneMetadataFieldError.
    // This test only shows that the stream a doc writes with NPOI's SetSummaryEntries is readable with the
    // existing CryptoApiEncryptedSummaryReader; it does not change the FieldError behaviour.

    [Fact]
    public void Read_DocEncryptionStreamWrittenWithTheFixtureHeader_ReturnsTheSummaryInformation()
    {
        var fixture = LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc);
        using var fixtureFs = new NPOIFSFileSystem(new MemoryStream(fixture, writable: false));
        var fib = DocEncryptionProbe.ReadFib(fixtureFs.Root)!;
        var header = DocEncryptionProbe.ReadHeader(fixtureFs.Root, fib, DocMime);
        var payload = WithVerifierFor(header.Payload, (uint)header.KeySizeBits, out var encryptor);
        var info = new EncryptionInfo(new LittleEndianInputStream(new ByteArrayInputStream(payload)), null);
        var summary = ReadStream(File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainTwinDocPath)), SummaryName);

        byte[] encryption;
        using (var entries = new NPOIFSFileSystem())
        {
            entries.Root.CreateDocument(SummaryName, new MemoryStream(summary));
            encryptor.SetSummaryEntries(entries.Root, CryptoApiEncryptedSummaryReader.DocXlsStreamName, entries);
            encryption = ReadStream(entries, CryptoApiEncryptedSummaryReader.DocXlsStreamName);
        }

        var decryptor = (CryptoAPIDecryptor)info.Decryptor;
        Assert.True(decryptor.VerifyPassword(Password));
        var streams = CryptoApiEncryptedSummaryReader.Read(encryption, decryptor, DocMime);

        var stream = Assert.Single(streams);
        Assert.Equal(SummaryName, stream.Name);
        Assert.Equal(summary, stream.Data);
    }

    // ---- G4: ppt token / structure disagreement ----

    [Fact]
    public void Extract_HelloPptWithPlainTokenButEncryptedStructure_DecryptsLikeHello()
    {
        var hello = LegacyFixtures.Bytes(LegacyFixtures.HelloPpt);
        var crafted = WithToken(hello, PptEncryptionProbe.TokenPlain);
        Assert.Equal(0xE391C05Fu, PptEncryptionProbe.TokenPlain);

        var expected = ExtractPpt(hello, "hello");
        var result = ExtractPpt(crafted, "hello");

        var text = result.ExtractedText.Replace("\r\n", "\n");
        // 145 is the CRLF length recorded for hello; the 4 separators are one char each here.
        Assert.Equal(145, text.Length + 4);
        Assert.StartsWith("This is a protected document\nIt only has the one slide", text, StringComparison.Ordinal);
        Assert.Equal("This is a protected document", result.Metadata.Title);
        Assert.Empty(result.FieldErrors);
        Assert.Equal(Json(expected), Json(result));
    }

    [Fact]
    public void Extract_HelloPptWithPlainTokenButEncryptedStructure_WithoutPassword_IsPasswordProtected()
    {
        var crafted = WithToken(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), PptEncryptionProbe.TokenPlain);

        Assert.Throws<PasswordProtectedException>(() => ExtractPpt(crafted, null));
    }

    [Fact]
    public void Extract_PlainPptWithEncryptedToken_NeedsAPasswordAndIsCorruptWithOne()
    {
        var plain = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainReferencePptPath));
        var crafted = WithToken(plain, TokenEncrypted);

        Assert.Throws<PasswordProtectedException>(() => ExtractPpt(crafted, null));
        Assert.Throws<CorruptFileException>(() => ExtractPpt(crafted, "x"));
    }

    private static byte[] WithToken(byte[] ppt, uint token) =>
        CfbMutator.ReplaceStream(ppt, "Current User", currentUser =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(currentUser.AsSpan(OffsetHeaderToken), token);
            return currentUser;
        });

    // ---- G5: storage descriptor in EncryptedSummary ----

    [Fact]
    public void Extract_HelloPptWithStorageDescriptorInEncryptedSummary_ExtractsLikeHello()
    {
        var hello = LegacyFixtures.Bytes(LegacyFixtures.HelloPpt);
        var decryptor = DecryptorFor(hello, "hello");
        var original = ReadStream(hello, CryptoApiEncryptedSummaryReader.PptStreamName);
        var crafted = CfbMutator.ReplaceStream(hello, CryptoApiEncryptedSummaryReader.PptStreamName,
            summary => AppendStorageDescriptor(summary, decryptor));
        var craftedSummary = ReadStream(crafted, CryptoApiEncryptedSummaryReader.PptStreamName);

        // The crafted stream really carries one more descriptor and a larger array.
        var before = ReadLayout(original, decryptor);
        var after = ReadLayout(craftedSummary, decryptor);
        Assert.Equal(2u, before.Count);
        Assert.Equal(before.Count + 1, after.Count);
        Assert.Equal(before.ArraySize + StorageDescriptorLength, after.ArraySize);
        Assert.Equal(before.ArrayOffset, after.ArrayOffset);
        Assert.Equal(original.Length + StorageDescriptorLength, craftedSummary.Length);

        // The reader skips the storage descriptor: same streams as for hello.
        var expectedStreams = CryptoApiEncryptedSummaryReader.Read(original, decryptor, PptMime);
        var streams = CryptoApiEncryptedSummaryReader.Read(craftedSummary, decryptor, PptMime);
        Assert.Equal(expectedStreams.Select(s => s.Name), streams.Select(s => s.Name));
        Assert.Equal(expectedStreams.Select(s => s.Data), streams.Select(s => s.Data));

        var expected = ExtractPpt(hello, "hello");
        var result = ExtractPpt(crafted, "hello");

        Assert.Empty(result.FieldErrors);
        Assert.Equal(Json(expected), Json(result));
    }

    private static CryptoAPIDecryptor DecryptorFor(byte[] pptBytes, string password)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(pptBytes, writable: false));
        var probe = PptEncryptionProbe.Probe(fs.Root);
        var payload = PptEncryptionProbe.CryptSessionPayload(probe.Layout!, PptMime);
        var header = LegacyEncryptionHeaderReader.Parse(payload, LegacyContainer.Ppt, PptMime);
        var decryptor = LegacyEncryptionHeaderReader.CreateDecryptor(header);
        Assert.True(LegacyEncryptionHeaderReader.VerifyPassword(decryptor, password));
        return (CryptoAPIDecryptor)decryptor;
    }

    // RC4 is symmetric: the same block-0 transform decrypts and re-encrypts.
    private static byte[] Block0(CryptoAPIDecryptor decryptor, byte[] data, int offset, int length)
    {
        var copy = data.AsSpan(offset, length).ToArray();
        decryptor.InitCipherForBlock(null, 0).Update(copy, 0, copy.Length, copy);
        return copy;
    }

    // One storage descriptor: 16 fixed bytes, a 1-char name and its 2-byte terminator.
    private const int StorageDescriptorLength = 16 + 2 + 2;

    private readonly record struct SummaryLayout(int ArrayOffset, int ArraySize, uint Count);

    private static SummaryLayout ReadLayout(byte[] summary, CryptoAPIDecryptor decryptor)
    {
        var header = Block0(decryptor, summary, 0, 8);
        var arrayOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
        var arraySize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
        var array = Block0(decryptor, summary, arrayOffset, arraySize);
        return new SummaryLayout(arrayOffset, arraySize, BinaryPrimitives.ReadUInt32LittleEndian(array));
    }

    // Appends a storage-flagged descriptor (flags 0, name "S") to the descriptor array, which is the last
    // part of the stream, bumps the count and array size, and re-encrypts the header and the array.
    private static byte[] AppendStorageDescriptor(byte[] summary, CryptoAPIDecryptor decryptor)
    {
        var header = Block0(decryptor, summary, 0, 8);
        var arrayOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(header);
        var arraySize = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
        var array = Block0(decryptor, summary, arrayOffset, arraySize);
        Assert.Equal(summary.Length, arrayOffset + arraySize);

        var storage = new byte[StorageDescriptorLength];
        storage[10] = 1;
        storage[16] = (byte)'S';
        var newArray = array.Concat(storage).ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(newArray, BinaryPrimitives.ReadUInt32LittleEndian(array) + 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)newArray.Length);

        var result = summary[..arrayOffset].Concat(new byte[newArray.Length]).ToArray();
        Block0(decryptor, header, 0, 8).CopyTo(result, 0);
        Block0(decryptor, newArray, 0, newArray.Length).CopyTo(result, arrayOffset);
        return result;
    }

    // ---- Manifest: every known gap has an entry ----

    [Fact]
    public void Manifest_EncryptedTestGaps_ListsG1ToG5WithRequiredFields()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(EncryptedSamples.FindRepoRootFile("samples/manifest.json")));

        Assert.True(manifest.RootElement.TryGetProperty("$encryptedTestGaps", out var gaps));
        Assert.Equal(JsonValueKind.Array, gaps.ValueKind);
        var entries = gaps.EnumerateArray().ToList();
        Assert.All(entries, entry =>
        {
            foreach (var field in RequiredFields)
            {
                Assert.True(entry.TryGetProperty(field, out var value), $"missing {field}");
                Assert.Equal(JsonValueKind.String, value.ValueKind);
                Assert.False(string.IsNullOrWhiteSpace(value.GetString()), $"empty {field}");
            }

            Assert.True(entry.TryGetProperty("coveredBy", out var coveredBy), "missing coveredBy");
            Assert.True(coveredBy.ValueKind is JsonValueKind.String or JsonValueKind.Null);
            if (coveredBy.ValueKind == JsonValueKind.String)
            {
                var names = coveredBy.GetString()!.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                Assert.NotEmpty(names);
                foreach (var name in names)
                {
                    AssertTestMethodExists(name);
                }
            }
        });

        var ids = entries.Select(e => e.GetProperty("id").GetString()!).ToList();
        Assert.Equal(ids.Distinct(StringComparer.Ordinal).Count(), ids.Count);
        foreach (var gap in KnownGaps)
        {
            Assert.Contains(gap, ids);
        }
    }

    // "Class.Method" must name a public test method in this test assembly.
    private static void AssertTestMethodExists(string qualifiedName)
    {
        var parts = qualifiedName.Split('.');
        Assert.True(parts.Length == 2 && parts.All(p => p.Length > 0), $"coveredBy '{qualifiedName}' is not Class.Method");
        var types = typeof(LegacyKnownGapTests).Assembly.GetTypes().Where(t => t.Name == parts[0]).ToList();
        var type = Assert.Single(types);
        var method = type.GetMethod(parts[1]);
        Assert.True(method is not null, $"coveredBy '{qualifiedName}' names no public method");
        Assert.True(
            method.GetCustomAttributes(typeof(FactAttribute), inherit: true).Length > 0,
            $"coveredBy '{qualifiedName}' is not a test");
    }
}
