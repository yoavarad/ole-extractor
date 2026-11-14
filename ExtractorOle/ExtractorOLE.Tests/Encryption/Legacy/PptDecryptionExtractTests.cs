using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
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

// #407 (S-lg-ppt): MainExtractor.Extract decrypts an encrypted .ppt with its password (persist objects,
// Pictures, EncryptedSummary), rebuilds a plain compound file and runs the unchanged ppt pipeline on it.
// Through the production DI wiring; expected values are the T-lg-fixtures entries in samples/manifest.json.
public sealed class PptDecryptionExtractTests : IDisposable
{
    private const string PptMime = "application/vnd.ms-powerpoint";
    private const string WrongPassword = "wrongpw";
    private const string PicturesStream = "Pictures";
    private const int OffsetHeaderToken = 12;

    private static readonly string[] HelloLines =
    [
        "This is a protected document",
        "It only has the one slide",
        "It does have notes",
        "The password is “hello”",
        "Just for the hell of it, we have some notes",
    ];

    // b2xtranslator conversion failures recorded in the manifest (gated: T-lg-ppt-b2x-text turns them
    // into text assertions).
    private static readonly string[] B2xFieldPaths = ["FormatMetadata.SlideCount", "FormatMetadata.NotesSlideCount", "ExtractedText"];

    private readonly ServiceProvider _provider;
    private readonly MainExtractor _extractor;

    public PptDecryptionExtractTests()
    {
        var services = new ServiceCollection();
        ServiceRegistration.Register(services);
        _provider = services.BuildServiceProvider();
        _extractor = _provider.GetRequiredService<MainExtractor>();
    }

    public void Dispose() => _provider.Dispose();

    public static TheoryData<string> PptFixtures => new(LegacyFixtures.EncryptedFileNames.Where(f => f.EndsWith(".ppt", StringComparison.Ordinal)));

    private static ExtractionRequest Request(byte[] bytes, string? password) =>
        new() { FileBytes = bytes, FileName = "fixture.ppt", DetectedMimeType = PptMime, Password = password };

    private DocumentExtractionResult Extract(byte[] bytes, string? password) => _extractor.Extract(Request(bytes, password));

    private DocumentExtractionResult ExtractFixture(string file) =>
        Extract(LegacyFixtures.Bytes(file), LegacyFixtures.Password(file));

    private static string Json(DocumentExtractionResult result) =>
        JsonSerializer.Serialize(result) + JsonSerializer.Serialize(result.FormatMetadata, result.FormatMetadata?.GetType() ?? typeof(object));

    private static string[] NonEmptyLines(string text) =>
        text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static string Sha256(byte[] data) => Convert.ToHexString(SHA256.HashData(data));

    private static JsonElement ManifestEntry(JsonDocument manifest, string file) =>
        manifest.RootElement.GetProperty("samples").EnumerateArray()
            .Single(e => e.GetProperty("path").GetString() == "samples/encrypted/" + file);

    private static JsonDocument Manifest() =>
        JsonDocument.Parse(File.ReadAllText(EncryptedSamples.FindRepoRootFile("samples/manifest.json")));

    private static DateTime Utc(string iso) =>
        DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    // ---- S-lg-ppt#1: exact text, metadata and counts with 0 FieldErrors ----
    // The issue text said 147/177 characters with straight quotes and a break after "we have ". The
    // decrypted PowerPoint Document holds U+201C/U+201D and one TextBytesAtom run "...we have some notes",
    // so the exact texts are 145/175 characters (manifest corrected in #407).

    [Theory]
    [InlineData(LegacyFixtures.HelloPpt, 145, 2)]
    [InlineData(LegacyFixtures.Hello56Ppt, 145, 3)]
    [InlineData(LegacyFixtures.HelloNoPropsPpt, 175, 3)]
    public void Extract_HelloFixture_ReturnsDecryptedTextMetadataAndCounts(string file, int length, int revision)
    {
        Assert.Equal("hello", LegacyFixtures.Password(file));
        var expectedLines = file == LegacyFixtures.HelloNoPropsPpt
            ? HelloLines.Take(3).Append("Properties are not protected").Concat(HelloLines.Skip(3)).ToArray()
            : HelloLines;

        var result = ExtractFixture(file);

        var text = result.ExtractedText.Replace("\r\n", "\n");
        // length is the CRLF length from the manifest; separators are Environment.NewLine (LF on Linux).
        Assert.Equal(length, text.Length + (expectedLines.Length - 1));
        Assert.Equal(string.Join("\n", expectedLines), text);
        Assert.Equal("This is a protected document", result.Metadata.Title);
        Assert.Equal("Hogwarts", result.Metadata.Creator);
        Assert.Equal("Hogwarts", result.Metadata.LastModifiedBy);
        Assert.Equal("Microsoft Office PowerPoint", result.Metadata.ApplicationName);
        Assert.Equal(revision, result.Metadata.RevisionNumber);
        var format = Assert.IsType<PowerPointFormatMetadata>(result.FormatMetadata);
        Assert.Equal(1, format.SlideCount);
        Assert.Equal(1, format.NotesSlideCount);
        Assert.False(format.HasMacros);
        Assert.Empty(result.EmbeddedFiles);
        Assert.Empty(result.FieldErrors);
        AssertMatchesManifest(file, result);
    }

    // The same values as recorded for the fixture in samples/manifest.json (T-lg-fixtures).
    private static void AssertMatchesManifest(string file, DocumentExtractionResult result)
    {
        using var manifest = Manifest();
        var entry = ManifestEntry(manifest, file);

        if (entry.TryGetProperty("bodyTextLines", out var lines))
        {
            Assert.Equal(lines.EnumerateArray().Select(l => l.GetString()!), NonEmptyLines(result.ExtractedText));
        }

        var metadata = entry.GetProperty("metadata");
        Assert.Equal(metadata.TryGetProperty("Title", out var title) ? title.GetString() : null, result.Metadata.Title);
        Assert.Equal(metadata.TryGetProperty("Author", out var author) ? author.GetString() : null, result.Metadata.Creator);
        if (metadata.TryGetProperty("LastModifiedBy", out var lastModifiedBy))
        {
            Assert.Equal(lastModifiedBy.GetString(), result.Metadata.LastModifiedBy);
        }

        if (metadata.TryGetProperty("ApplicationName", out var application))
        {
            Assert.Equal(application.GetString(), result.Metadata.ApplicationName);
        }

        if (metadata.TryGetProperty("RevisionNumber", out var revision))
        {
            Assert.Equal(revision.GetInt32(), result.Metadata.RevisionNumber);
        }

        if (metadata.TryGetProperty("Created", out var created))
        {
            Assert.Equal(Utc(created.GetString()!), result.Metadata.Created!.Value.ToUniversalTime());
        }

        if (metadata.TryGetProperty("Modified", out var modified))
        {
            Assert.Equal(Utc(modified.GetString()!), result.Metadata.Modified!.Value.ToUniversalTime());
        }

        if (entry.TryGetProperty("expectedFormatMetadata", out var format))
        {
            var ppt = Assert.IsType<PowerPointFormatMetadata>(result.FormatMetadata);
            Assert.Equal(format.GetProperty("slideCount").GetInt32(), ppt.SlideCount);
            Assert.Equal(format.GetProperty("notesSlideCount").GetInt32(), ppt.NotesSlideCount);
            Assert.Equal(format.GetProperty("hasMacros").GetBoolean(), ppt.HasMacros);
        }

        Assert.Equal(entry.GetProperty("expectedSubfileCount").GetInt32(), result.EmbeddedFiles.Count);
        foreach (var (expected, actual) in entry.GetProperty("expectedSubfiles").EnumerateArray().Zip(result.EmbeddedFiles))
        {
            Assert.Equal(expected.GetProperty("packagePath").GetString(), actual.PackagePath);
            Assert.Equal(expected.GetProperty("sizeInBytes").GetInt64(), actual.SizeInBytes);
            Assert.Equal(expected.GetProperty("sizeInBytes").GetInt64(), actual.BinaryData.LongLength);
            Assert.Equal(expected.GetProperty("sha256").GetString(), Sha256(actual.BinaryData));
        }

        Assert.Equal(
            entry.GetProperty("expectedFieldErrors").EnumerateArray()
                .Select(e => (e.GetProperty("fieldPath").GetString()!, Enum.Parse<FieldErrorCode>(e.GetProperty("code").GetString()!)))
                .Order(),
            result.FieldErrors.Select(e => (e.FieldPath, e.Code)).Order());
    }

    // ---- S-lg-ppt#2: decrypted Pictures and metadata; FieldErrors are exactly the b2xtranslator ones ----

    [Fact]
    public void Extract_CryptoApiProc2356_ReturnsDecryptedPicturesAndMetadata()
    {
        var result = ExtractFixture(LegacyFixtures.CryptoApiProc2356Ppt);

        Assert.Equal("RC4 CryptoAPI Encryption", result.Metadata.Title);
        Assert.Equal("Dominic Salemno", result.Metadata.Creator);
        var pictures = Assert.Single(result.EmbeddedFiles);
        Assert.Equal(PicturesStream, pictures.PackagePath);
        Assert.Equal(1026524, pictures.BinaryData.Length);
        Assert.StartsWith("EB85DE0ED697EC2F", Sha256(pictures.BinaryData), StringComparison.Ordinal);
        AssertOnlyB2xFieldErrors(result);
        AssertMatchesManifest(LegacyFixtures.CryptoApiProc2356Ppt, result);
    }

    [Fact]
    public void Extract_PptWithPngEncrypted_ReturnsDecryptedPicturesAndNoTitleOrCreator()
    {
        var result = ExtractFixture(LegacyFixtures.PngEncryptedPpt);

        var pictures = Assert.Single(result.EmbeddedFiles);
        Assert.Equal(PicturesStream, pictures.PackagePath);
        Assert.Equal(16588, pictures.BinaryData.Length);
        Assert.StartsWith("8018CAA4D0530831", Sha256(pictures.BinaryData), StringComparison.Ordinal);
        Assert.Null(result.Metadata.Title);
        Assert.Null(result.Metadata.Creator);
        AssertOnlyB2xFieldErrors(result);
        AssertMatchesManifest(LegacyFixtures.PngEncryptedPpt, result);
    }

    private static void AssertOnlyB2xFieldErrors(DocumentExtractionResult result, params string[] extra)
    {
        Assert.Equal(B2xFieldPaths.Concat(extra).Order(), result.FieldErrors.Select(e => e.FieldPath).Order());
        Assert.All(result.FieldErrors.Where(e => B2xFieldPaths.Contains(e.FieldPath)),
            e => Assert.Equal(FieldErrorCode.UnexpectedFieldFailure, e.Code));
    }

    [Theory]
    [MemberData(nameof(PptFixtures))]
    public void Extract_PptFixture_NeverReturnsCiphertextPictures(string file)
    {
        var bytes = LegacyFixtures.Bytes(file);
        var cipher = ReadRootStream(bytes, PicturesStream);

        var result = Extract(bytes, LegacyFixtures.Password(file));

        Assert.Equal(cipher is null ? 0 : 1, result.EmbeddedFiles.Count);
        if (cipher is not null)
        {
            Assert.NotEqual(Sha256(cipher), Sha256(Assert.Single(result.EmbeddedFiles).BinaryData));
        }
    }

    // ---- S-lg-ppt#3: wrong password; no password unchanged ----

    [Theory]
    [MemberData(nameof(PptFixtures))]
    public void Extract_PptFixtureWithWrongPassword_IsInvalidPassword(string file)
    {
        var bytes = LegacyFixtures.Bytes(file);

        var ex = Assert.Throws<InvalidPasswordException>(() => Extract(bytes, WrongPassword));
        Assert.DoesNotContain(WrongPassword, ex.ToString(), StringComparison.Ordinal);
        Assert.Throws<PasswordProtectedException>(() => Extract(bytes, null));
    }

    // ---- S-lg-ppt#4: the rebuilt file has real property streams, no EncryptedSummary, plain token ----

    [Fact]
    public void Extract_HelloPpt_OpenSeesRebuiltPlainFile()
    {
        var spy = new SpyStrategy();
        var extractor = new MainExtractor(
            _provider.GetRequiredService<IExtractionHelper>(),
            new FormatDispatchRegistry(
                new Dictionary<OfficeMimeType, IOpenStrategy> { [OfficeMimeType.PowerPointLegacy] = spy },
                new Dictionary<OfficeMimeType, ITextExtractor>()));

        extractor.Extract(Request(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), "hello"));

        Assert.Equal(1, spy.OpenCalls);
        var rebuilt = spy.OpenBytes!;
        var currentUser = ReadRootStream(rebuilt, "Current User")!;
        Assert.Equal(PptEncryptionProbe.TokenPlain, BinaryPrimitives.ReadUInt32LittleEndian(currentUser.AsSpan(OffsetHeaderToken)));
        Assert.Equal(0xE391C05Fu, PptEncryptionProbe.TokenPlain);
        using var fs = new NPOIFSFileSystem(new MemoryStream(rebuilt, writable: false));
        Assert.False(fs.Root.HasEntry(CryptoApiEncryptedSummaryReader.PptStreamName));
        Assert.True(fs.Root.HasEntry("\u0005SummaryInformation"));
    }

    // ---- S-lg-ppt#5: a truncated Pictures stream costs only the Pictures subfile ----

    [Fact]
    public void Extract_CryptoApiProc2356WithTruncatedPictures_RecordsOnePicturesFieldError()
    {
        var bytes = CfbMutator.ReplaceStream(LegacyFixtures.Bytes(LegacyFixtures.CryptoApiProc2356Ppt), PicturesStream,
            p => p.AsSpan(0, p.Length / 2).ToArray());

        var result = Extract(bytes, LegacyFixtures.Password(LegacyFixtures.CryptoApiProc2356Ppt));

        Assert.Equal("RC4 CryptoAPI Encryption", result.Metadata.Title);
        Assert.Equal("Dominic Salemno", result.Metadata.Creator);
        Assert.Empty(result.EmbeddedFiles);
        var pictures = Assert.Single(result.FieldErrors, e => e.FieldPath == "EmbeddedFiles[Pictures]");
        Assert.Equal(FieldErrorCode.CorruptFile, pictures.Code);
        AssertOnlyB2xFieldErrors(result, "EmbeddedFiles[Pictures]");
    }

    // An unreadable EncryptedSummary is container-level: the whole extraction fails, no partial result.
    [Fact]
    public void Extract_HelloPptWithTruncatedEncryptedSummary_IsCorruptFile()
    {
        var bytes = CfbMutator.ReplaceStream(LegacyFixtures.Bytes(LegacyFixtures.HelloPpt), CryptoApiEncryptedSummaryReader.PptStreamName,
            s => s.AsSpan(0, 4).ToArray());

        Assert.Throws<CorruptFileException>(() => Extract(bytes, "hello"));
    }

    // ---- S-lg-detect#4 (ppt half): stream input equals byte input ----

    [Fact]
    public void Extract_EncryptedPptStreamInput_EqualsByteInput()
    {
        var bytes = LegacyFixtures.Bytes(LegacyFixtures.HelloPpt);
        var expected = Json(Extract(bytes, "hello"));

        using var stream = new MemoryStream(bytes, writable: false);
        Assert.Equal(expected, Json(_extractor.Extract(new ExtractionRequest
        {
            InputStream = stream,
            FileName = "fixture.ppt",
            DetectedMimeType = PptMime,
            Password = "hello",
        })));
    }

    // Null when the compound file has no such root stream.
    private static byte[]? ReadRootStream(byte[] cfb, string name)
    {
        using var fs = new NPOIFSFileSystem(new MemoryStream(cfb, writable: false));
        if (!fs.Root.HasEntry(name))
        {
            return null;
        }

        var entry = (DocumentEntry)fs.Root.GetEntry(name);
        var data = new byte[entry.Size];
        using (var input = fs.Root.CreateDocumentInputStream(entry))
        {
            input.ReadFully(data);
        }

        return data;
    }

    // Open strategy that records the bytes it is given.
    private sealed class SpyStrategy : IOpenStrategy
    {
        public int OpenCalls { get; private set; }
        public byte[]? OpenBytes { get; private set; }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            OpenCalls++;
            OpenBytes = fileBytes;
            return new DocumentExtractionResult();
        }
    }
}
