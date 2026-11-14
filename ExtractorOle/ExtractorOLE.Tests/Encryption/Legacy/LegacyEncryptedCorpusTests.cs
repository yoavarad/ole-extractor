using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static ExtractorOLE.Tests.Integration.EncryptedCorpus;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// #408: the encrypted doc/ppt corpus (samples/encrypted/, found by folder; format, password, twin and
// expectations from samples/manifest.json) tested three ways through the shared EncryptedCorpus
// harness: Extract, DetectMimeType, batch (ExtractManyAsync / ExtractManyStreamingAsync) and stream input.
// No sample is named here.
public class LegacyEncryptedCorpusTests
{
    private static readonly string[] LegacyFormats = ["doc", "ppt"];

    // Not the password of any entry, and must never appear in an exception.
    private static readonly string WrongPw = "wrong-" + Guid.NewGuid();

    private static readonly Lazy<MainExtractor> Extractor = new(() =>
    {
        var services = new ServiceCollection();
        ServiceRegistration.Register(services);
        return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
    });

    public static IEnumerable<object[]> All() => Paths(e => LegacyFormats.Contains(e.Format));

    public static IEnumerable<object[]> WithPassword() =>
        Paths(e => LegacyFormats.Contains(e.Format) && e.Password is not null);

    private static IEnumerable<EncryptedCorpusEntry> Legacy() => Entries().Where(e => LegacyFormats.Contains(e.Format));

    [Fact]
    public void Harness_FindsDocAndPptGroups()
    {
        Assert.Contains(Legacy(), e => e.Format == "doc");
        Assert.Contains(Legacy(), e => e.Format == "ppt");
        Assert.Contains(Legacy(), e => e.Password is null);
        Assert.NotEmpty(WithPassword());
    }

    // ---- Extract ----

    [Theory]
    [MemberData(nameof(All))]
    public void Extract_NoPassword_ThrowsPasswordProtected(string path) =>
        AssertExtractNoPasswordRejected(Get(path));

    [Theory]
    [MemberData(nameof(All))]
    public void Extract_WrongPassword_ThrowsInvalidPassword(string path)
    {
        var entry = Get(path);
        var ex = Assert.Throws<InvalidPasswordException>(() => Extract(ExtractRequest(entry, WrongPw)));
        Assert.DoesNotContain(WrongPw, ex.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(WithPassword))]
    public void Extract_CorrectPassword_MatchesTwinOrManifest(string path)
    {
        var entry = Get(path);
        if (entry.TwinFullPath is not null)
        {
            AssertExtractCorrectPasswordEqualsTwin(entry);
            return;
        }

        var result = Extract(ExtractRequest(entry, entry.Password));
        Assert.Equal(entry.InnerMimeType, result.MimeType);
        AssertMatchesManifest(entry, result);
    }

    private static string[] NonEmptyLines(string text) =>
        text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static DateTime Utc(string iso) =>
        DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    private static void AssertMatchesManifest(EncryptedCorpusEntry corpusEntry, DocumentExtractionResult result)
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(EncryptedSamples.FindRepoRootFile("samples/manifest.json")));
        var entry = manifest.RootElement.GetProperty("samples").EnumerateArray()
            .Single(e => string.Equals(e.GetProperty("path").GetString(), corpusEntry.Path, StringComparison.OrdinalIgnoreCase));

        if (entry.TryGetProperty("bodyTextLines", out var lines))
        {
            Assert.Equal(lines.EnumerateArray().SelectMany(l => NonEmptyLines(l.GetString()!)), NonEmptyLines(result.ExtractedText));
        }

        if (entry.TryGetProperty("metadata", out var metadata))
        {
            if (metadata.TryGetProperty("Title", out var title)) Assert.Equal(title.GetString(), result.Metadata.Title);
            if (metadata.TryGetProperty("Author", out var author)) Assert.Equal(author.GetString(), result.Metadata.Creator);
            if (metadata.TryGetProperty("LastModifiedBy", out var lmb)) Assert.Equal(lmb.GetString(), result.Metadata.LastModifiedBy);
            if (metadata.TryGetProperty("ApplicationName", out var app)) Assert.Equal(app.GetString(), result.Metadata.ApplicationName);
            if (metadata.TryGetProperty("RevisionNumber", out var rev)) Assert.Equal(rev.GetInt32(), result.Metadata.RevisionNumber);
            if (metadata.TryGetProperty("Created", out var created)) { Assert.NotNull(result.Metadata.Created); Assert.Equal(Utc(created.GetString()!), result.Metadata.Created.Value.ToUniversalTime()); }
            if (metadata.TryGetProperty("Modified", out var modified)) { Assert.NotNull(result.Metadata.Modified); Assert.Equal(Utc(modified.GetString()!), result.Metadata.Modified.Value.ToUniversalTime()); }
        }

        if (entry.TryGetProperty("expectedFormatMetadata", out var format))
        {
            if (corpusEntry.Format == "doc")
            {
                var word = Assert.IsType<WordFormatMetadata>(result.FormatMetadata);
                Assert.Equal(format.GetProperty("wordCount").GetInt32(), word.WordCount);
            }
            else
            {
                var ppt = Assert.IsType<PowerPointFormatMetadata>(result.FormatMetadata);
                Assert.Equal(format.GetProperty("slideCount").GetInt32(), ppt.SlideCount);
                Assert.Equal(format.GetProperty("notesSlideCount").GetInt32(), ppt.NotesSlideCount);
                Assert.Equal(format.GetProperty("hasMacros").GetBoolean(), ppt.HasMacros);
            }
        }

        Assert.Equal(entry.GetProperty("expectedSubfileCount").GetInt32(), result.EmbeddedFiles.Count);
        foreach (var (expected, actual) in entry.GetProperty("expectedSubfiles").EnumerateArray().Zip(result.EmbeddedFiles))
        {
            Assert.Equal(expected.GetProperty("packagePath").GetString(), actual.PackagePath);
            Assert.Equal(expected.GetProperty("sizeInBytes").GetInt64(), actual.SizeInBytes);
            Assert.Equal(expected.GetProperty("sha256").GetString(), Convert.ToHexString(SHA256.HashData(actual.BinaryData)), ignoreCase: true);
        }

        Assert.Equal(
            entry.GetProperty("expectedFieldErrors").EnumerateArray()
                .Select(e => (e.GetProperty("fieldPath").GetString()!, Enum.Parse<FieldErrorCode>(e.GetProperty("code").GetString()!)))
                .Order(),
            result.FieldErrors.Select(e => (e.FieldPath, e.Code)).Order());
    }

    // ---- DetectMimeType ----

    [Theory]
    [MemberData(nameof(All))]
    public void Detect_EveryPasswordVariant_FormatUnchangedAndNeverThrows(string path)
    {
        var entry = Get(path);
        foreach (var password in new[] { null, string.Empty, WrongPw, entry.Password ?? ArbitraryPassword })
        {
            var result = Detect(entry, password);
            Assert.Equal(entry.ExpectedDetectedMimeType, result.MimeType);
            Assert.True(result.IsSupported);
        }
    }

    // ---- Batch and stream input: same outcome as the single call ----

    private static IEnumerable<string?> Passwords(EncryptedCorpusEntry entry)
    {
        yield return null;
        yield return string.Empty;
        yield return WrongPw;
        if (entry.Password is not null) yield return entry.Password;
    }

    private static string Outcome(Func<DocumentExtractionResult> extract)
    {
        try
        {
            return "OK:" + Leaves(extract());
        }
        catch (Exception ex) when (ex is PasswordProtectedException or InvalidPasswordException)
        {
            return "ERR:" + ex.GetType().FullName;
        }
    }

    private static string Outcome(BatchExtractionResult item) => item.Error switch
    {
        null => "OK:" + Leaves(item.Result!),
        PasswordProtectedException or InvalidPasswordException => "ERR:" + item.Error.GetType().FullName,
        _ => throw new InvalidOperationException("Unexpected batch item error", item.Error),
    };

    [Fact]
    public async Task Batch_MixedPasswords_EachItemEqualsSingleCallOutcome()
    {
        var cases = Legacy().SelectMany(e => Passwords(e).Select(p => (Entry: e, Password: p))).ToList();
        var expected = cases.Select(c => Outcome(() => Extract(ExtractRequest(c.Entry, c.Password)))).ToList();
        Assert.Contains(expected, o => o.StartsWith("OK:", StringComparison.Ordinal));
        Assert.Contains(expected, o => o.Contains(nameof(InvalidPasswordException), StringComparison.Ordinal));
        Assert.Contains(expected, o => o.Contains(nameof(PasswordProtectedException), StringComparison.Ordinal));

        IEnumerable<ExtractionRequest> Requests() => cases.Select(c => ExtractRequest(c.Entry, c.Password));

        var many = await Extractor.Value.ExtractManyAsync(Requests(), 3);
        Assert.Equal(cases.Count, many.Count);
        foreach (var item in many)
        {
            Assert.Equal(expected[item.Index], Outcome(item));
        }

        var streamed = new List<BatchExtractionResult>();
        await foreach (var item in Extractor.Value.ExtractManyStreamingAsync(Requests(), 3)) streamed.Add(item);
        Assert.Equal(cases.Count, streamed.Count);
        Assert.Equal(Enumerable.Range(0, cases.Count), streamed.Select(i => i.Index).Order());
        foreach (var item in streamed)
        {
            Assert.Equal(expected[item.Index], Outcome(item));
        }
    }

    [Theory]
    [MemberData(nameof(All))]
    public void Extract_StreamInput_SameOutcomeAsByteInput(string path)
    {
        var entry = Get(path);
        var bytes = File.ReadAllBytes(entry.FullPath);
        foreach (var password in Passwords(entry))
        {
            var expected = Outcome(() => Extract(ExtractRequest(bytes, entry, password)));

            using var stream = new MemoryStream(bytes, writable: false);
            var request = new ExtractionRequest
            {
                InputStream = stream,
                FileName = System.IO.Path.GetFileName(entry.Path),
                DetectedMimeType = entry.InnerMimeType,
                Password = password,
            };
            var actual = Outcome(() => Extract(request));
            Assert.Equal(expected, actual);
            if (password is not null && password == entry.Password)
            {
                Assert.StartsWith("OK:", actual, StringComparison.Ordinal);
            }
        }
    }
}
