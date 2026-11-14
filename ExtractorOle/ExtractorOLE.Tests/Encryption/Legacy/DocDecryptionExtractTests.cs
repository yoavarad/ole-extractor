using System.Buffers.Binary;
using System.Text.Json;
using ExtractorOLE.Abstractions;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// #406 (S-lg-doc): MainExtractor.Extract decrypts an encrypted .doc with its password, rebuilds a
// plain compound file and runs the unchanged doc pipeline on it. Through the production DI wiring.
public sealed class DocDecryptionExtractTests : IDisposable
{
    private const string DocMime = "application/msword";
    private const string WrongPassword = "not-the-password";
    private const int OffFlags = 0x0A;
    private const ushort FlagEncrypted = 0x0100;
    private static readonly byte[] CfbSignature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private readonly ServiceProvider _provider;
    private readonly MainExtractor _extractor;

    public DocDecryptionExtractTests()
    {
        var services = new ServiceCollection();
        ServiceRegistration.Register(services);
        _provider = services.BuildServiceProvider();
        _extractor = _provider.GetRequiredService<MainExtractor>();
    }

    public void Dispose() => _provider.Dispose();

    public static TheoryData<string> DocFixtures => new(LegacyFixtures.EncryptedFileNames.Where(f => f.EndsWith(".doc", StringComparison.Ordinal)));

    private static ExtractionRequest Request(byte[] bytes, string? password) =>
        new() { FileBytes = bytes, FileName = "fixture.doc", DetectedMimeType = DocMime, Password = password };

    private DocumentExtractionResult Extract(byte[] bytes, string? password) => _extractor.Extract(Request(bytes, password));

    private static string Json(DocumentExtractionResult result) =>
        JsonSerializer.Serialize(result) + JsonSerializer.Serialize(result.FormatMetadata, result.FormatMetadata?.GetType() ?? typeof(object));

    // ---- S-lg-doc#1: exact text and metadata; the CryptoAPI fixture equals its plain twin ----

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc, "tika", "This is an encrypted Word 2007 File.", "Antoni", "Microsoft Office Word", 2, 5)]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc, "password", "This is a test", "kiwiwings", null, 1, 2)]
    public void Extract_PasswordDocFixture_ReturnsDecryptedTextAndMetadata(
        string file, string password, string text, string creator, string? application, int revision, int wordCount)
    {
        Assert.Equal(password, LegacyFixtures.Password(file));

        var result = Extract(LegacyFixtures.Bytes(file), password);

        Assert.Equal(text, result.ExtractedText.Trim());
        Assert.Equal(creator, result.Metadata.Creator);
        if (application != null)
        {
            Assert.Equal(application, result.Metadata.ApplicationName);
        }

        Assert.Equal(revision, result.Metadata.RevisionNumber);
        Assert.Equal(wordCount, Assert.IsType<WordFormatMetadata>(result.FormatMetadata).WordCount);
        Assert.Empty(result.EmbeddedFiles);
        Assert.Empty(result.FieldErrors);
        AssertMatchesManifest(file, result);
    }

    // The same values as recorded for the fixture in samples/manifest.json (T-lg-fixtures).
    private static void AssertMatchesManifest(string file, DocumentExtractionResult result)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(EncryptedSamples.FindRepoRootFile("samples/manifest.json")));
        var entry = manifest.RootElement.GetProperty("samples").EnumerateArray()
            .Single(e => e.GetProperty("path").GetString() == "samples/encrypted/" + file);

        Assert.Equal(entry.GetProperty("bodyTextLines").EnumerateArray().Select(l => l.GetString()!), new[] { result.ExtractedText.Trim() });
        var metadata = entry.GetProperty("metadata");
        Assert.Equal(metadata.GetProperty("Author").GetString(), result.Metadata.Creator);
        Assert.Equal(metadata.GetProperty("RevisionNumber").GetInt32(), result.Metadata.RevisionNumber);
        if (metadata.TryGetProperty("ApplicationName", out var application))
        {
            Assert.Equal(application.GetString(), result.Metadata.ApplicationName);
        }

        Assert.Equal(entry.GetProperty("expectedFormatMetadata").GetProperty("wordCount").GetInt32(),
            ((WordFormatMetadata)result.FormatMetadata!).WordCount);
        Assert.Equal(entry.GetProperty("expectedSubfileCount").GetInt32(), result.EmbeddedFiles.Count);
    }

    [Fact]
    public void Extract_Rc4CryptoApiDoc_EqualsItsPlainTwin()
    {
        var twin = Extract(File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainTwinDocPath)), null);

        var decrypted = Extract(LegacyFixtures.Bytes(LegacyFixtures.Rc4CryptoApiDoc), LegacyFixtures.Password(LegacyFixtures.Rc4CryptoApiDoc));

        Assert.Equal("lorem ipsum", decrypted.ExtractedText.Trim());
        Assert.Empty(decrypted.FieldErrors);
        Assert.Equal(Json(twin), Json(decrypted));
    }

    // ---- S-lg-doc#2: wrong password; no password unchanged ----

    [Theory]
    [MemberData(nameof(DocFixtures))]
    public void Extract_DocFixtureWithWrongPassword_IsInvalidPassword(string file)
    {
        var bytes = LegacyFixtures.Bytes(file);

        var ex = Assert.Throws<InvalidPasswordException>(() => Extract(bytes, WrongPassword));
        Assert.DoesNotContain(WrongPassword, ex.ToString(), StringComparison.Ordinal);
        Assert.Throws<PasswordProtectedException>(() => Extract(bytes, null));
    }

    // ---- S-lg-doc#4: open and the text fallback see only the rebuilt bytes; stream/path input ----

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc)]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc)]
    public void Extract_EncryptedDoc_OpenAndTextFallbackSeeOnlyRebuiltPlainBytes(string file)
    {
        var encrypted = LegacyFixtures.Bytes(file);
        var spy = new SpyStrategy();
        var extractor = new MainExtractor(
            _provider.GetRequiredService<IExtractionHelper>(),
            new FormatDispatchRegistry(
                new Dictionary<OfficeMimeType, IOpenStrategy> { [OfficeMimeType.WordLegacy] = spy },
                new Dictionary<OfficeMimeType, ITextExtractor> { [OfficeMimeType.WordLegacy] = spy }));

        extractor.Extract(Request(encrypted, LegacyFixtures.Password(file)));

        Assert.Equal(1, spy.OpenCalls);
        Assert.Equal(1, spy.TextCalls);
        foreach (var seen in new[] { spy.OpenBytes!, spy.TextBytes! })
        {
            Assert.False(seen.AsSpan().SequenceEqual(encrypted));
            Assert.Equal(CfbSignature, seen.Take(8));
            Assert.Equal(0, WordDocumentFlags(seen) & FlagEncrypted);
        }
    }

    private static ushort WordDocumentFlags(byte[] cfb)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb, writable: false));
        var entry = (DocumentEntry)fs.Root.GetEntry("WordDocument");
        var word = new byte[entry.Size];
        using (var input = fs.Root.CreateDocumentInputStream(entry))
        {
            input.ReadFully(word);
        }

        return BinaryPrimitives.ReadUInt16LittleEndian(word.AsSpan(OffFlags));
    }

    [Fact]
    public void Extract_EncryptedDocStreamAndPathInput_EqualsByteInput()
    {
        var bytes = LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc);
        var password = LegacyFixtures.Password(LegacyFixtures.TikaBinaryRc4Doc);
        var expected = Json(Extract(bytes, password));

        using (var stream = new MemoryStream(bytes, writable: false))
        {
            Assert.Equal(expected, Json(_extractor.Extract(new ExtractionRequest
            {
                InputStream = stream,
                FileName = "fixture.doc",
                DetectedMimeType = DocMime,
                Password = password,
            })));
        }

        var path = Path.Combine(Path.GetTempPath(), $"doc-decryption-{Guid.NewGuid():N}.doc");
        File.WriteAllBytes(path, bytes);
        try
        {
            Assert.Equal(expected, Json(_extractor.Extract(new ExtractionRequest
            {
                FilePath = path,
                FileName = "fixture.doc",
                DetectedMimeType = DocMime,
                Password = password,
            })));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- S-lg-doc#5: encrypted document properties (gap G2) ----

    [Fact]
    public void Extract_CryptoApiDocWithEncryptedProperties_KeepsTextAndRecordsOneMetadataFieldError()
    {
        var bytes = LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc);
        // The fixture keeps its encryption header in 1Table; Flags (offset 4) is in its plain region.
        bytes = CfbMutator.ReplaceStream(bytes, "1Table", t =>
        {
            t[4] &= unchecked((byte)~LegacyEncryptionHeaderReader.FlagDocProps);
            return t;
        });
        bytes = CfbMutator.RemoveEntry(bytes, "\u0005SummaryInformation");
        var encryptionStream = new byte[64];
        new Random(406).NextBytes(encryptionStream);
        bytes = CfbMutator.AddStream(bytes, "encryption", encryptionStream);

        var result = Extract(bytes, "password");

        Assert.Equal("This is a test", result.ExtractedText.Trim());
        var error = Assert.Single(result.FieldErrors);
        Assert.Equal("Metadata", error.FieldPath);
        Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
        Assert.Null(result.Metadata.Creator);
    }

    // ---- Cancellation once DocDecryptor has started ----

    [Fact]
    public void Extract_CancelledWhileDecrypting_ThrowsOperationCanceledWithNoResult()
    {
        var spy = new SpyStrategy();
        var extractor = new MainExtractor(
            _provider.GetRequiredService<IExtractionHelper>(),
            new FormatDispatchRegistry(
                new Dictionary<OfficeMimeType, IOpenStrategy> { [OfficeMimeType.WordLegacy] = spy },
                new Dictionary<OfficeMimeType, ITextExtractor> { [OfficeMimeType.WordLegacy] = spy }));
        using var cts = new CancellationTokenSource();
        LegacyDecryption.BeforeDocDecryptForTest.Value = cts.Cancel;
        try
        {
            var ex = Assert.ThrowsAny<OperationCanceledException>(() => extractor.Extract(
                Request(LegacyFixtures.Bytes(LegacyFixtures.PasswordCryptoApiDoc), "password"), cts.Token));
            // Observed inside DocDecryptor (its per-stream check), not at a later stage boundary.
            Assert.Contains(nameof(DocDecryptor), ex.StackTrace, StringComparison.Ordinal);
        }
        finally
        {
            LegacyDecryption.BeforeDocDecryptForTest.Value = null;
        }

        Assert.Equal(0, spy.OpenCalls);
    }

    // Open strategy and plain-bytes text extractor that record what they are given.
    private sealed class SpyStrategy : IOpenStrategy, ITextExtractor
    {
        public int OpenCalls { get; private set; }
        public int TextCalls { get; private set; }
        public byte[]? OpenBytes { get; private set; }
        public byte[]? TextBytes { get; private set; }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            OpenCalls++;
            OpenBytes = fileBytes;
            return new DocumentExtractionResult();
        }

        public string ExtractText(byte[] fileBytes)
        {
            TextCalls++;
            TextBytes = fileBytes;
            return string.Empty;
        }
    }
}
