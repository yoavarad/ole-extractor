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
