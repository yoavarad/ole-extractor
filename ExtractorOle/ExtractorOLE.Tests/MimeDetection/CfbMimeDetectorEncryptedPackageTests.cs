using System.Buffers.Binary;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption;
using ExtractorOLE.Helpers.MimeDetection;
using OpenMcdf;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;

namespace ExtractorOLE.Tests.MimeDetection;

// [S-pw-detect] CfbMimeDetector: encrypted package precedence, inner format with the password,
// INVALID_PASSWORD when wrong, Unknown for schemes it cannot decrypt.
public class CfbMimeDetectorEncryptedPackageTests
{
    private const string StandardDocx = "password-protected.docx";

    private static MimeDetectionResult Detect(string sampleName, string? password) =>
        new CfbMimeDetector().Detect(new MimeDetectionRequest
        {
            FileBytes = File.ReadAllBytes(Sample(sampleName).FullPath),
            FileName = sampleName,
            Password = password,
        });

    [Theory]
    [InlineData(StandardDocx)]
    [InlineData(AgilePptx)]
    [InlineData(AgileNonAsciiXlsx)]
    public void Detect_EncryptedPackageWithoutPassword_ReturnsUnknown(string sampleName)
    {
        Assert.Equal(DetectedFormat.Unknown, Detect(sampleName, null).DetectedFormat);
        Assert.Equal(DetectedFormat.Unknown, Detect(sampleName, string.Empty).DetectedFormat);
    }

    [Theory]
    [InlineData(StandardDocx, DetectedFormat.Docx)]
    [InlineData(AgilePptx, DetectedFormat.Pptx)]
    [InlineData(AgileNonAsciiXlsx, DetectedFormat.Xlsx)]
    public void Detect_EncryptedPackageWithCorrectPassword_ReturnsInnerFormat(string sampleName, DetectedFormat expected)
    {
        var result = Detect(sampleName, PasswordOf(sampleName));

        Assert.Equal(expected, result.DetectedFormat);
        Assert.True(result.IsSupported);
    }

    [Theory]
    [InlineData(StandardDocx)]
    [InlineData(AgilePptx)]
    public void Detect_EncryptedPackageWithWrongPassword_ThrowsInvalidPassword(string sampleName)
    {
        Assert.Throws<InvalidPasswordException>(() => Detect(sampleName, "not-the-password"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("password")]
    public void Detect_ExtensibleScheme_ReturnsUnknown(string? password)
    {
        var info = new byte[64];
        BinaryPrimitives.WriteUInt16LittleEndian(info, 4);
        BinaryPrimitives.WriteUInt16LittleEndian(info.AsSpan(2), 3);
        BinaryPrimitives.WriteInt32LittleEndian(info.AsSpan(4), 0x10);

        var result = new CfbMimeDetector().Detect(new MimeDetectionRequest
        {
            FileBytes = BuildCfb(info, new byte[16]),
            Password = password,
        });

        Assert.Equal(DetectedFormat.Unknown, result.DetectedFormat);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong")]
    [InlineData("Rc4Passw0rd")]
    public void Detect_EncryptedXls_ReturnsXlsWhateverThePassword(string? password)
    {
        Assert.Equal(DetectedFormat.Xls, Detect("rc4-password.xls", password).DetectedFormat);
    }

    // Package with both encryption streams plus a legacy stub: the encrypted package wins.
    [Fact]
    public void Detect_EncryptedPackageBesideWordDocumentStub_IgnoresStub()
    {
        var info = new byte[64];
        BinaryPrimitives.WriteUInt16LittleEndian(info, 4);
        BinaryPrimitives.WriteUInt16LittleEndian(info.AsSpan(2), 3);

        var result = new CfbMimeDetector().Detect(new MimeDetectionRequest { FileBytes = BuildCfb(info, new byte[16], withWordStub: true) });

        Assert.Equal(DetectedFormat.Unknown, result.DetectedFormat);
    }

    // A damaged encrypted package is a typed library error (corrupt, truncated, invalid password),
    // never Unknown and never an untyped exception escaping from NPOI or the crypto stack.
    [Theory]
    [InlineData(StandardDocx)]
    [InlineData(AgilePptx)]
    public void Detect_DamagedEncryptionInfo_ThrowsTypedErrorOrDetects(string sampleName)
    {
        var (info, package) = ReadRawStreams(sampleName);
        var password = PasswordOf(sampleName);

        for (var offset = 8; offset < info.Length; offset++)
        {
            foreach (var value in new byte[] { 0x00, 0xFF })
            {
                var mutated = (byte[])info.Clone();
                mutated[offset] = value;
                AssertTypedOutcome(BuildCfb(mutated, package), password, $"info[{offset}]={value}");
            }
        }
    }

    // ProviderType (info offset 32) is validated by NPOI, not by our own header checks.
    [Fact]
    public void Detect_StandardEncryptionInfoRejectedByNpoi_ThrowsCorruptFile()
    {
        var (info, package) = ReadRawStreams(StandardDocx);
        info[32] = 0;

        Assert.Throws<CorruptFileException>(() =>
            new CfbMimeDetector().Detect(new MimeDetectionRequest { FileBytes = BuildCfb(info, package), Password = PasswordOf(StandardDocx) }));
    }

    [Theory]
    [InlineData(StandardDocx)]
    [InlineData(AgilePptx)]
    public void Detect_TruncatedEncryptedPackage_ThrowsTruncatedContainer(string sampleName)
    {
        var (info, package) = ReadRawStreams(sampleName);

        var cfb = BuildCfb(info, package[..(package.Length / 2)]);

        Assert.Throws<TruncatedContainerException>(() =>
            new CfbMimeDetector().Detect(new MimeDetectionRequest { FileBytes = cfb, Password = PasswordOf(sampleName) }));
    }

    private static void AssertTypedOutcome(byte[] cfb, string password, string label)
    {
        try
        {
            var result = new CfbMimeDetector().Detect(new MimeDetectionRequest { FileBytes = cfb, Password = password });
            Assert.True(result.DetectedFormat is DetectedFormat.Docx or DetectedFormat.Pptx, label);
        }
        catch (ExtractorOleException)
        {
            // Typed library error: acceptable.
        }
    }

    private static (byte[] Info, byte[] Package) ReadRawStreams(string sampleName)
    {
        using var root = RootStorage.Open(new MemoryStream(File.ReadAllBytes(Sample(sampleName).FullPath)));
        return (ReadStream(root, EncryptedPackageDecryptor.EncryptionInfoStreamName),
            ReadStream(root, EncryptedPackageDecryptor.EncryptedPackageStreamName));
    }

    private static byte[] ReadStream(RootStorage root, string name)
    {
        using var stream = root.OpenStream(name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static byte[] BuildCfb(byte[] info, byte[] package, bool withWordStub = false)
    {
        using var stream = new MemoryStream();
        using (var root = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
        {
            Write(root, EncryptedPackageDecryptor.EncryptionInfoStreamName, info);
            Write(root, EncryptedPackageDecryptor.EncryptedPackageStreamName, package);
            if (withWordStub)
            {
                Write(root, "WordDocument", [1, 2, 3]);
            }
        }

        return stream.ToArray();
    }

    private static void Write(RootStorage root, string name, byte[] data)
    {
        using var s = root.CreateStream(name);
        s.Write(data);
    }
}
