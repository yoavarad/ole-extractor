using System.Diagnostics.Metrics;
using System.IO.Compression;
using System.Text.Json;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;

namespace ExtractorOLE.Tests.Encryption;

/// <summary>
/// #379: Extract decrypts an encrypted OOXML package with the request password, then runs the
/// unchanged OOXML path and guards on the decrypted package. Samples, passwords and twins come
/// from samples/manifest.json through <see cref="EncryptedSamples"/>.
/// </summary>
public class OoxmlPasswordExtractionTests
{
    private const string StandardDocx = "password-protected.docx";
    private const string EmbeddingDocx = "embeds-encrypted-package.docx";
    private const string WrongPassword = "not-the-password";

    private static readonly Lazy<MainExtractor> Extractor = new(() =>
    {
        var services = new ServiceCollection();
        ServiceRegistration.Register(services);
        return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
    });

    private static string MimeOf(string sampleName) => new ExtractionHelper().MimeFor(Path.GetExtension(sampleName) switch
    {
        ".docx" => OfficeMimeType.Word,
        ".xlsx" => OfficeMimeType.Excel,
        ".pptx" => OfficeMimeType.PowerPoint,
        var other => throw new InvalidOperationException($"Unmapped sample extension '{other}'"),
    });

    private static ExtractionRequest Request(string sampleName, string? password) => new()
    {
        FileBytes = File.ReadAllBytes(Sample(sampleName).FullPath),
        FileName = sampleName,
        DetectedMimeType = MimeOf(sampleName),
        Password = password,
    };

    private static byte[] TwinBytes(EncryptedSamples.EncryptedSample sample)
    {
        var repoRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(sample.FullPath)))!;
        return File.ReadAllBytes(Path.Combine(repoRoot, sample.UnencryptedTwin!));
    }

    // Every public leaf, the embedded bytes included; FormatMetadata by its runtime type.
    private static string Leaves(DocumentExtractionResult result) =>
        JsonSerializer.Serialize(result) + JsonSerializer.Serialize(result.FormatMetadata, result.FormatMetadata?.GetType() ?? typeof(object));

    // [S-pw-ooxml#1]
    [Theory]
    [InlineData(AgileNonAsciiXlsx)]
    [InlineData(AgilePptx)]
    [InlineData(StandardDocx)]
    public void Extract_CorrectPassword_EqualsUnencryptedTwinLeafForLeaf(string sampleName)
    {
        var sample = Sample(sampleName);
        var request = Request(sampleName, sample.Password);

        var decrypted = Extractor.Value.Extract(request);
        var twin = Extractor.Value.Extract(new ExtractionRequest
        {
            FileBytes = TwinBytes(sample),
            FileName = sampleName,
            DetectedMimeType = request.DetectedMimeType,
        });

        Assert.Equal(Leaves(twin), Leaves(decrypted));
        Assert.Empty(decrypted.FieldErrors);
    }

    [Fact]
    public void Extract_CorrectPassword_StreamInput_EqualsByteInput()
    {
        var byteRequest = Request(AgilePptx, PasswordOf(AgilePptx));
        using var stream = new MemoryStream(byteRequest.FileBytes, writable: false);

        var fromStream = Extractor.Value.Extract(new ExtractionRequest
        {
            InputStream = stream,
            FileName = AgilePptx,
            DetectedMimeType = byteRequest.DetectedMimeType,
            Password = byteRequest.Password,
        });

        Assert.Equal(Leaves(Extractor.Value.Extract(byteRequest)), Leaves(fromStream));
        Assert.True(stream.CanRead); // the caller's stream is not closed
    }

    // [S-pw-ooxml#1] The size recorded for the request is the encrypted input's, not the decrypted package's.
    [Fact]
    public void Extract_CorrectPassword_RecordsEncryptedInputSize()
    {
        var request = Request(AgilePptx, PasswordOf(AgilePptx));
        long total = 0;
        var inFlow = new AsyncLocal<bool>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (i, l) => { if (i.Meter.Name == ExtractionTelemetry.MeterName) l.EnableMeasurementEvents(i); },
        };
        listener.SetMeasurementEventCallback<long>((inst, m, _, _) =>
        {
            if (inFlow.Value && inst.Name == "extractor.bytes.processed") Interlocked.Add(ref total, m);
        });
        listener.Start();

        inFlow.Value = true;
        Extractor.Value.Extract(request);

        Assert.Equal(request.FileBytes.Length, Interlocked.Read(ref total));
    }

    // [S-pw-ooxml#2]
    [Theory]
    [InlineData(AgileNonAsciiXlsx)]
    [InlineData(AgilePptx)]
    [InlineData(StandardDocx)]
    public void Extract_NoPassword_ThrowsPasswordProtected(string sampleName)
    {
        Assert.Throws<PasswordProtectedException>(() => Extractor.Value.Extract(Request(sampleName, null)));
        Assert.Throws<PasswordProtectedException>(() => Extractor.Value.Extract(Request(sampleName, string.Empty)));
    }

    // [S-pw-ooxml#2]
    [Theory]
    [InlineData(AgileNonAsciiXlsx)]
    [InlineData(AgilePptx)]
    [InlineData(StandardDocx)]
    public void Extract_WrongPassword_ThrowsInvalidPassword(string sampleName)
    {
        Assert.Throws<InvalidPasswordException>(() => Extractor.Value.Extract(Request(sampleName, WrongPassword)));
    }

    // [S-pw-ooxml#4]
    [Fact]
    public void Extract_TamperedPackageWithCorrectPassword_ThrowsCorruptFile()
    {
        Assert.Throws<CorruptFileException>(() =>
            Extractor.Value.Extract(Request(AgileTamperedXlsx, PasswordOf(AgileTamperedXlsx))));
    }

    // [S-pw-ooxml#5] The decrypted package is held to the inflation caps: its local headers claim
    // zero bytes, so only the existing ZipInflationGuard can see that it inflates past the total cap.
    [Fact]
    public void Extract_DecryptedPackageInflatingPastCap_ThrowsOversizedNestedContent()
    {
        const string password = "bomb-pw";
        var bomb = ForgeLocalHeaders(BuildZip(entries: 5, bytesPerEntry: 1_000_000));
        var encrypted = EncryptedPackageDecryptorTests.EncryptStandard(bomb, password);
        var request = new ExtractionRequest
        {
            FileBytes = encrypted,
            FileName = "bomb.docx",
            DetectedMimeType = MimeOf(".docx"),
            Password = password,
        };
        var limits = new MimeDetectionLimits { MaxDeclaredEntryBytes = 2_000_000, MaxDeclaredNestedContentBytes = 3_000_000 };

        // The encrypted CFB itself is within every cap: the rejection comes from the decrypted package.
        new ExtractionGuardrails(limits).Enforce(new ExtractionRequest { FileBytes = encrypted, FileName = "bomb.docx" });
        var ex = Assert.Throws<OversizedNestedContentException>(() => Build(limits).Extract(request));
        Assert.Contains("inflate", ex.DeclaredMetric, StringComparison.OrdinalIgnoreCase);

        // Control: under the default caps the same decrypted package passes every guard and extracts
        // (it has no document part, so there is no text), so the caps above are what rejected it.
        Assert.Empty(Extractor.Value.Extract(request).ExtractedText);
    }

    // [S-pw-ooxml#8] An encrypted package embedded in an unencrypted document is one opaque subfile,
    // with or without a request password (which applies only to the container).
    [Theory]
    [InlineData(null)]
    [InlineData("container-has-no-password")]
    public void Extract_EmbeddedEncryptedPackage_ReturnedOpaqueWithoutFieldError(string? password)
    {
        var embeddedBytes = File.ReadAllBytes(Sample(AgilePptx).FullPath);

        var result = Extractor.Value.Extract(Request(EmbeddingDocx, password));

        var item = Assert.Single(result.EmbeddedFiles, f => f.PackagePath == "/word/embeddings/oleObject1.bin");
        Assert.Equal(embeddedBytes, item.BinaryData);
        Assert.Empty(result.FieldErrors);
    }

    private static MainExtractor Build(MimeDetectionLimits limits)
    {
        var services = new ServiceCollection();
        ServiceRegistration.Register(services);
        var provider = services.BuildServiceProvider();
        return new MainExtractor(provider.GetRequiredService<IExtractionHelper>(), provider.GetRequiredService<IFormatDispatchRegistry>(), limits);
    }

    private static byte[] BuildZip(int entries, int bytesPerEntry)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < entries; i++)
            {
                using var s = zip.CreateEntry($"e{i}.bin", CompressionLevel.Optimal).Open();
                s.Write(new byte[bytesPerEntry]);
            }
        }

        return ms.ToArray();
    }

    // Zero the sizes in every local file header; the central directory keeps the real data.
    private static byte[] ForgeLocalHeaders(byte[] zip)
    {
        var forged = (byte[])zip.Clone();
        for (var i = 0; i + 30 < forged.Length; i++)
        {
            if (forged[i] == 0x50 && forged[i + 1] == 0x4B && forged[i + 2] == 3 && forged[i + 3] == 4)
            {
                forged.AsSpan(i + 18, 8).Clear();
            }
        }

        return forged;
    }
}
