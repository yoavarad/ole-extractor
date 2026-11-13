using System.Buffers.Binary;
using System.Text.Json;
using ExtractorOLE.Abstractions;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// #400 (S-lg-detect): MainExtractor.Extract rejects an encrypted .doc/.ppt at preflight, before the
// Doc/Ppt open strategy (and b2xtranslator) runs. Through the production DI wiring; every rejection
// runs against an extractor whose Doc/Ppt open strategies are spies, to prove they are never called.
public sealed class LegacyEncryptionGuardTests : IDisposable
{
    private const string DocMime = "application/msword";
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const int OffFlags = 0x0A;
    private const int OffLKey = 0x0E;
    private const int OffKeySize = 12 + 16;

    private readonly ServiceProvider _provider;
    private readonly MainExtractor _extractor;
    private readonly SpyOpenStrategy _spy = new();
    private readonly MainExtractor _spyExtractor;

    public LegacyEncryptionGuardTests()
    {
        var services = new ServiceCollection();
        ServiceRegistration.Register(services);
        _provider = services.BuildServiceProvider();
        _extractor = _provider.GetRequiredService<MainExtractor>();
        _spyExtractor = new MainExtractor(
            _provider.GetRequiredService<IExtractionHelper>(),
            new FormatDispatchRegistry(
                new Dictionary<OfficeMimeType, IOpenStrategy> { [OfficeMimeType.WordLegacy] = _spy, [OfficeMimeType.PowerPointLegacy] = _spy },
                new Dictionary<OfficeMimeType, ITextExtractor>()));
    }

    public void Dispose() => _provider.Dispose();

    public static TheoryData<string> EncryptedFixtures => new(LegacyFixtures.EncryptedFileNames);

    public static TheoryData<string> PasswordFixtures =>
        new(LegacyFixtures.EncryptedFileNames.Where(f => LegacyFixtures.Password(f) != null));

    private static string MimeOf(string fileName) =>
        fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ? DocMime : PptMime;

    private static ExtractionRequest Request(byte[] bytes, string mime, string? password) =>
        new() { FileBytes = bytes, FileName = "fixture", DetectedMimeType = mime, Password = password };

    private DocumentExtractionResult Extract(byte[] bytes, string mime, string? password) =>
        _extractor.Extract(Request(bytes, mime, password));

    // Extract throws T, and no Doc/Ppt open strategy (so no b2xtranslator conversion) ran.
    private void AssertRejected<T>(ExtractionRequest request) where T : Exception
    {
        Assert.Throws<T>(() => _spyExtractor.Extract(request));
        Assert.Equal(0, _spy.Calls);
    }

    private void AssertRejected<T>(byte[] bytes, string mime, string? password) where T : Exception =>
        AssertRejected<T>(Request(bytes, mime, password));

    // ---- S-lg-detect#1: every encrypted fixture without a password, no open strategy runs ----

    [Theory]
    [MemberData(nameof(EncryptedFixtures))]
    public void Extract_EncryptedFixtureWithoutPassword_IsPasswordProtected_AndNeverOpens(string file)
    {
        var bytes = LegacyFixtures.Bytes(file);

        AssertRejected<PasswordProtectedException>(bytes, MimeOf(file), null);
        AssertRejected<PasswordProtectedException>(bytes, MimeOf(file), string.Empty);
    }

    [Theory]
    [MemberData(nameof(PasswordFixtures))]
    public void Extract_EncryptedFixtureWithCorrectPassword_IsPasswordProtected_Interim_UntilDecryptionWired(string file)
    {
        AssertRejected<PasswordProtectedException>(LegacyFixtures.Bytes(file), MimeOf(file), LegacyFixtures.Password(file));
    }

    [Theory]
    [InlineData(LegacyFixtures.HelloPpt, null)]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc, null)]
    [InlineData(LegacyFixtures.HelloPpt, "hello")]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc, "tika")]
    public void Extract_EncryptedStreamInput_IsPasswordProtected(string file, string? password)
    {
        using var stream = new MemoryStream(LegacyFixtures.Bytes(file), writable: false);

        AssertRejected<PasswordProtectedException>(new ExtractionRequest
        {
            InputStream = stream, FileName = "fixture", DetectedMimeType = MimeOf(file), Password = password,
        });
    }

    // ---- S-lg-detect#2: XOR and unsupported doc schemes ----

    [Theory]
    [InlineData(null)]
    [InlineData("x")]
    public void Extract_XorObfuscatedDoc_IsPasswordProtected(string? password)
    {
        var xor = MutateWordDocument(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc),
            w => SetU16(w, OffFlags, (ushort)(BinaryPrimitives.ReadUInt16LittleEndian(w.AsSpan(OffFlags)) | 0x8000)));

        AssertRejected<PasswordProtectedException>(xor, DocMime, password);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("x")]
    public void Extract_DocHeaderVersion44_IsPasswordProtected(string? password)
    {
        var mutated = MutateTable(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc), t =>
        {
            SetU16(t, 0, 4);
            SetU16(t, 2, 4);
        });

        AssertRejected<PasswordProtectedException>(mutated, DocMime, password);
    }

    // ---- S-lg-detect#3: malformed doc headers, unreadable encrypted ppt structure ----

    private static byte[] DocWithLKeyMax() =>
        MutateWordDocument(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc),
            w => BinaryPrimitives.WriteUInt32LittleEndian(w.AsSpan(OffLKey), 0xFFFFFFFF));

    private static byte[] DocWithKeySize41() =>
        MutateTable(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc),
            t => BinaryPrimitives.WriteUInt32LittleEndian(t.AsSpan(OffKeySize), 41));

    [Fact]
    public void Extract_DocLKeyMax_IsCorruptWithPassword_AndPasswordProtectedWithout()
    {
        AssertRejected<CorruptFileException>(DocWithLKeyMax(), DocMime, "x");
        // Without a password the table stream is never read, so the bad header is not seen.
        AssertRejected<PasswordProtectedException>(DocWithLKeyMax(), DocMime, null);
    }

    [Fact]
    public void Extract_DocKeySize41_IsCorruptWithPassword_AndPasswordProtectedWithout()
    {
        AssertRejected<CorruptFileException>(DocWithKeySize41(), DocMime, "x");
        AssertRejected<PasswordProtectedException>(DocWithKeySize41(), DocMime, null);
    }

    [Fact]
    public void Extract_PlainPptWithEncryptedToken_IsCorruptWithPassword_AndPasswordProtectedWithout()
    {
        var plain = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainReferencePptPath));
        var mutated = CfbMutator.ReplaceStream(plain, "Current User", b =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(12), PptEncryptionProbe.TokenEncrypted);
            return b;
        });

        AssertRejected<CorruptFileException>(mutated, PptMime, "x");
        AssertRejected<PasswordProtectedException>(mutated, PptMime, null);
    }

    [Fact]
    public void Extract_PptHeaderKeySize41_IsCorruptWithPassword_AndPasswordProtectedWithout()
    {
        var hello = LegacyFixtures.Bytes(LegacyFixtures.HelloPpt);
        PptEncryptionLayout layout;
        using (var fs = new NPOIFSFileSystem(new MemoryStream(hello)))
        {
            layout = PptEncryptionProbe.Probe(fs.Root).Layout!;
        }

        var keySizeOffset = (int)layout.CryptSessionOffset + 8 + OffKeySize;
        var mutated = CfbMutator.ReplaceStream(hello, "PowerPoint Document", d =>
        {
            BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(keySizeOffset), 41);
            return d;
        });

        AssertRejected<CorruptFileException>(mutated, PptMime, "x");
        AssertRejected<PasswordProtectedException>(mutated, PptMime, null);
    }

    // ---- S-lg-detect#4: unencrypted corpus unchanged; DetectMimeType unchanged ----

    public static TheoryData<string> PlainCorpus()
    {
        var data = new TheoryData<string>();
        using var manifest = JsonDocument.Parse(File.ReadAllText(EncryptedSamples.FindRepoRootFile("samples/manifest.json")));
        foreach (var entry in manifest.RootElement.GetProperty("samples").EnumerateArray())
        {
            var path = entry.GetProperty("path").GetString()!;
            if ((path.EndsWith(".doc", StringComparison.Ordinal) || path.EndsWith(".ppt", StringComparison.Ordinal))
                && !path.StartsWith("samples/encrypted/", StringComparison.Ordinal)
                && !entry.TryGetProperty("expectedBehavior", out _))
            {
                data.Add(path);
            }
        }

        return data;
    }

    // The values themselves are pinned against the manifest (password null) by
    // Integration.ExtractCorpusManifestTests; here a password must not change any of them, for byte
    // and stream input alike.
    [Theory]
    [MemberData(nameof(PlainCorpus))]
    public void Extract_UnencryptedCorpusSample_IsTheSameWithAndWithoutPassword(string path)
    {
        var bytes = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(path));
        var mime = MimeOf(path);
        var expected = Json(Extract(bytes, mime, null));

        Assert.Equal(expected, Json(Extract(bytes, mime, "pw")));
        using var stream = new MemoryStream(bytes, writable: false);
        Assert.Equal(expected, Json(_extractor.Extract(
            new ExtractionRequest { InputStream = stream, FileName = "fixture", DetectedMimeType = mime, Password = "pw" })));
    }

    [Theory]
    [MemberData(nameof(EncryptedFixtures))]
    public void DetectMimeType_EncryptedFixture_IsDocOrPptForAnyPassword(string file)
    {
        var helper = _provider.GetRequiredService<IExtractionHelper>();
        var bytes = LegacyFixtures.Bytes(file);
        var expected = file.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ? DetectedFormat.Doc : DetectedFormat.Ppt;

        foreach (var password in new[] { null, "wrong", LegacyFixtures.Password(file) })
        {
            var result = helper.DetectMimeType(new MimeDetectionRequest { FileBytes = bytes, FileName = file, Password = password });
            Assert.Equal(expected, result.DetectedFormat);
        }
    }

    // ---- Inspect: the records the decryption wiring tasks consume, and stream faults ----

    private static LegacyEncryptedFile? Inspect(Stream stream, OfficeMimeType format, bool hasPassword)
    {
        using var input = ExtractionInput.From(
            new ExtractionRequest { InputStream = stream, FileName = "fixture", DetectedMimeType = PptMime });
        return LegacyEncryptionGuard.Inspect(input, format, hasPassword, PptMime);
    }

    [Theory]
    [InlineData(OfficeMimeType.Word)]
    [InlineData(OfficeMimeType.ExcelLegacy)]
    public void Inspect_OtherFormat_IsNullWithoutReading(OfficeMimeType format)
    {
        using var stream = new FaultingStream(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), faultFrom: 0);

        Assert.Null(Inspect(stream, format, hasPassword: false));
        Assert.Equal(0, stream.Reads);
    }

    [Fact]
    public void Inspect_Rc4DocWithPassword_ReturnsEncryptedDoc()
    {
        using var stream = new MemoryStream(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc), writable: false);

        var doc = Assert.IsType<EncryptedDoc>(Inspect(stream, OfficeMimeType.WordLegacy, hasPassword: true));
        Assert.Equal(DocEncryptionKind.Rc4, doc.Fib.Kind);
        Assert.Equal(LegacyCipherScheme.CryptoApiRc4, doc.Header.Scheme);
        Assert.True(stream.CanRead); // the caller's stream is left open
    }

    [Fact]
    public void Inspect_EncryptedPptWithPassword_ReturnsEncryptedPpt()
    {
        using var stream = new MemoryStream(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), writable: false);

        var ppt = Assert.IsType<EncryptedPpt>(Inspect(stream, OfficeMimeType.PowerPointLegacy, hasPassword: true));
        Assert.Equal(6u, ppt.Layout.CryptSessionPersistId);
        Assert.True(stream.CanRead);
    }

    [Fact]
    public void Inspect_UnreadableCompoundFile_IsNull()
    {
        var garbage = new byte[4096];
        new Random(400).NextBytes(garbage);
        using var stream = new MemoryStream(garbage, writable: false);

        Assert.Null(Inspect(stream, OfficeMimeType.PowerPointLegacy, hasPassword: true));
    }

    // A read fault of the caller's stream is rethrown as-is: never null (plain), never corrupt-file.
    [Theory]
    [InlineData(0L)]
    [InlineData(4096L)]
    public void Inspect_CallerStreamFault_IsRethrown(long faultFrom)
    {
        using var stream = new FaultingStream(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), faultFrom);

        var ex = Assert.Throws<IOException>(() => Inspect(stream, OfficeMimeType.PowerPointLegacy, hasPassword: true));
        Assert.Same(FaultingStream.Fault, ex);
    }

    private static string Json(DocumentExtractionResult result) =>
        JsonSerializer.Serialize(result) + JsonSerializer.Serialize(result.FormatMetadata, result.FormatMetadata?.GetType() ?? typeof(object));

    private static void SetU16(byte[] data, int offset, ushort value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);

    private static byte[] MutateWordDocument(byte[] cfb, Action<byte[]> edit) =>
        CfbMutator.ReplaceStream(cfb, "WordDocument", w =>
        {
            edit(w);
            return w;
        });

    // The fixtures used here keep their encryption header in 1Table.
    private static byte[] MutateTable(byte[] cfb, Action<byte[]> edit) =>
        CfbMutator.ReplaceStream(cfb, "1Table", t =>
        {
            edit(t);
            return t;
        });

    private sealed class SpyOpenStrategy : IOpenStrategy
    {
        public int Calls { get; private set; }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            Calls++;
            return new DocumentExtractionResult();
        }
    }

    // Seekable read-only stream whose reads at or past faultFrom throw the same IOException.
    private sealed class FaultingStream(byte[] data, long faultFrom) : MemoryStream(data, writable: false)
    {
        public static readonly IOException Fault = new("simulated read fault");

        public int Reads { get; private set; }

        // MemoryStream routes a derived type's span reads through this overload.
        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads++;
            if (Position + count > faultFrom)
            {
                throw Fault;
            }

            return base.Read(buffer, offset, count);
        }

        public override int ReadByte() => throw new NotSupportedException();
    }
}
