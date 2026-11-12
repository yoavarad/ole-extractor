using System.Buffers.Binary;
using System.IO.Compression;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.FileSystem;
using OpenMcdf;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;

namespace ExtractorOLE.Tests.Encryption;

public class EncryptedPackageDecryptorTests
{
    private const string StandardDocx = "password-protected.docx";
    private const long DefaultCap = 1_073_741_824;

    [Theory]
    [InlineData(AgilePptx, "ppt/presentation.xml")]
    [InlineData(AgileNonAsciiXlsx, "xl/workbook.xml")]
    [InlineData(StandardDocx, "word/document.xml")]
    public void Decrypt_CorrectPassword_ReturnsOoxmlPackage(string sampleName, string mainPart)
    {
        using var root = OpenSample(sampleName);

        using var output = EncryptedPackageDecryptor.Decrypt(root, PasswordOf(sampleName), DefaultCap);

        Assert.Equal(0, output.Position);
        using var zip = new ZipArchive(output, ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry(mainPart));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4096)]
    [InlineData(10_000)]
    [InlineData(10_001)]
    public void Decrypt_StandardPackageBuiltWithNpoiEncryptor_RoundTrips(int length)
    {
        var payload = new byte[length];
        new Random(378).NextBytes(payload);
        using var root = RootStorage.Open(new MemoryStream(EncryptStandard(payload, "s3cret")));

        using var output = EncryptedPackageDecryptor.Decrypt(root, "s3cret", DefaultCap);

        Assert.Equal(payload, ((MemoryStream)output).ToArray());
    }

    [Theory]
    [InlineData(AgilePptx)]
    [InlineData(StandardDocx)]
    public void Decrypt_WrongPassword_ThrowsInvalidPasswordWithoutAllocating(string sampleName)
    {
        using var root = OpenSample(sampleName);
        var spy = new AllocationSpy();

        var ex = Assert.Throws<InvalidPasswordException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, "not-the-password", DefaultCap, spy.Allocate));
        Assert.Equal(0, spy.Calls);
        Assert.DoesNotContain("not-the-password", ex.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AgilePptx)]
    [InlineData(StandardDocx)]
    public void Decrypt_DeclaredSizeEqualToCap_Decrypts(string sampleName)
    {
        var (_, package) = ReadRawStreams(sampleName);
        var declared = BinaryPrimitives.ReadInt64LittleEndian(package);
        using var root = OpenSample(sampleName);

        using var output = EncryptedPackageDecryptor.Decrypt(root, PasswordOf(sampleName), declared);

        Assert.Equal(declared, output.Length);
    }

    [Fact]
    public void Decrypt_CiphertextShortOfBlockPadding_ThrowsTruncatedBeforeAllocating()
    {
        // 17 plaintext bytes need two 16-byte AES blocks; the stream holds the declared 17 but not 32.
        var payload = new byte[17];
        using var full = RootStorage.Open(new MemoryStream(EncryptStandard(payload, "pw")));
        var info = ReadStream(full, EncryptedPackageDecryptor.EncryptionInfoStreamName);
        var package = ReadStream(full, EncryptedPackageDecryptor.EncryptedPackageStreamName)[..(8 + 17)];
        using var root = BuildCfb(info, package);
        var spy = new AllocationSpy();

        Assert.Throws<TruncatedContainerException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, "pw", DefaultCap, spy.Allocate));
        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public void Decrypt_NegativeDeclaredSize_ThrowsCorruptFile()
    {
        var (info, package) = ReadRawStreams(AgilePptx);
        BinaryPrimitives.WriteInt64LittleEndian(package, -1);
        using var root = BuildCfb(info, package);

        Assert.Throws<CorruptFileException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(AgilePptx), DefaultCap));
    }

    [Fact]
    public void Decrypt_EncryptionInfoShorterThanVersionHeader_ThrowsCorruptFile()
    {
        using var root = BuildCfb([4, 0, 4, 0], new byte[16]);

        Assert.Throws<CorruptFileException>(() => EncryptedPackageDecryptor.Decrypt(root, "pw", DefaultCap));
    }

    [Fact]
    public void Decrypt_RoundTripStandardWrongPassword_ThrowsInvalidPassword()
    {
        using var root = RootStorage.Open(new MemoryStream(EncryptStandard([1, 2, 3], "right")));

        Assert.Throws<InvalidPasswordException>(() => EncryptedPackageDecryptor.Decrypt(root, "wrong", DefaultCap));
    }

    [Fact]
    public void Decrypt_TamperedAgilePackage_ThrowsCorruptFile()
    {
        using var root = OpenSample(AgileTamperedXlsx);

        Assert.Throws<CorruptFileException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(AgileTamperedXlsx), DefaultCap));
    }

    [Theory]
    [InlineData(AgilePptx)]
    [InlineData(StandardDocx)]
    public void Decrypt_DeclaredSizeOverCap_ThrowsOversizedBeforeAllocating(string sampleName)
    {
        var (info, package) = ReadRawStreams(sampleName);
        var declared = BinaryPrimitives.ReadInt64LittleEndian(package);
        using var root = BuildCfb(info, package);
        var spy = new AllocationSpy();

        Assert.Throws<OversizedNestedContentException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(sampleName), declared - 1, spy.Allocate));
        Assert.Equal(0, spy.Calls);
    }

    [Theory]
    [InlineData(AgilePptx)]
    [InlineData(StandardDocx)]
    public void Decrypt_DeclaredSizeOverStreamLength_ThrowsTruncatedBeforeAllocating(string sampleName)
    {
        var (info, package) = ReadRawStreams(sampleName);
        BinaryPrimitives.WriteInt64LittleEndian(package, package.Length - 8 + 1);
        using var root = BuildCfb(info, package);
        var spy = new AllocationSpy();

        Assert.Throws<TruncatedContainerException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(sampleName), DefaultCap, spy.Allocate));
        Assert.Equal(0, spy.Calls);
    }

    [Fact]
    public void Decrypt_PackageShorterThanSizeField_ThrowsTruncated()
    {
        var (info, _) = ReadRawStreams(AgilePptx);
        using var root = BuildCfb(info, [1, 2, 3]);

        Assert.Throws<TruncatedContainerException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(AgilePptx), DefaultCap));
    }

    [Theory]
    [InlineData(4, 3, 0x10)] // Extensible
    [InlineData(3, 3, 0x10)] // Extensible
    [InlineData(4, 2, 0x04)] // CryptoAPI RC4 (no fAES)
    [InlineData(4, 2, 0x34)] // fExternal set alongside the Standard flags
    [InlineData(2, 2, 0x24)] // Standard flags at version 2.2: no NPOI decryptor
    [InlineData(1, 1, 0x00)] // BinaryRC4 version
    [InlineData(9, 9, 0x00)] // unknown
    public void Decrypt_UnsupportedScheme_ThrowsPasswordProtectedWithAndWithoutPassword(int major, int minor, int flags)
    {
        var info = new byte[64];
        BinaryPrimitives.WriteUInt16LittleEndian(info, (ushort)major);
        BinaryPrimitives.WriteUInt16LittleEndian(info.AsSpan(2), (ushort)minor);
        BinaryPrimitives.WriteInt32LittleEndian(info.AsSpan(4), flags);
        using var root = BuildCfb(info, new byte[16]);

        Assert.Throws<PasswordProtectedException>(() => EncryptedPackageDecryptor.Decrypt(root, null, DefaultCap));
        Assert.Throws<PasswordProtectedException>(() => EncryptedPackageDecryptor.Decrypt(root, "password", DefaultCap));
    }

    [Theory]
    [InlineData(AgilePptx, null)]
    [InlineData(StandardDocx, "")]
    public void Decrypt_SupportedSchemeWithoutPassword_ThrowsPasswordProtected(string sampleName, string? password)
    {
        using var root = OpenSample(sampleName);
        var spy = new AllocationSpy();

        Assert.Throws<PasswordProtectedException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, password, DefaultCap, spy.Allocate));
        Assert.Equal(0, spy.Calls);
    }

    [Theory]
    [InlineData(12 + 8, 0x1234)]   // AlgID not AES
    [InlineData(12 + 8, 0x6610)]   // AES-256 AlgID with the sample's 128-bit KeySize
    [InlineData(12 + 16, 100)]     // KeySize does not match the AES AlgID
    [InlineData(12 + 12, 0x8003)]  // AlgIDHash not SHA-1
    [InlineData(8, 0x7FFF)]        // headerSize past the end of the stream
    [InlineData(8, 4)]             // headerSize shorter than the fixed header
    public void Decrypt_CorruptStandardHeader_ThrowsCorruptFileNotInvalidPassword(int offset, int value)
    {
        var (info, package) = ReadRawStreams(StandardDocx);
        BinaryPrimitives.WriteInt32LittleEndian(info.AsSpan(offset), value);
        using var root = BuildCfb(info, package);

        Assert.Throws<CorruptFileException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(StandardDocx), DefaultCap));
    }

    [Fact]
    public void Decrypt_StandardCspNameTerminatedAtHeaderEnd_Decrypts()
    {
        var (info, package) = ReadRawStreams(StandardDocx);
        using var root = BuildCfb(WithCspName(info, "Provider\0"), package);

        using var output = EncryptedPackageDecryptor.Decrypt(root, PasswordOf(StandardDocx), DefaultCap);

        Assert.Equal(BinaryPrimitives.ReadInt64LittleEndian(package), output.Length);
    }

    [Theory]
    [InlineData("Provider")]        // no NUL inside headerSize
    [InlineData("Prov\0ider\0")]    // NUL before the end of the header: NPOI would misalign
    [InlineData("\u0010\0")]        // NPOI reads a leading int32 of 16 as "no CSP name"
    public void Decrypt_StandardCspNameNotTerminatedAtHeaderEnd_ThrowsCorruptFile(string cspName)
    {
        var (info, package) = ReadRawStreams(StandardDocx);
        using var root = BuildCfb(WithCspName(info, cspName), package);

        Assert.Throws<CorruptFileException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(StandardDocx), DefaultCap));
    }

    // Inserts a UTF-16LE CSPName after the 32-byte fixed header and grows headerSize to cover it.
    private static byte[] WithCspName(byte[] info, string cspName)
    {
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(info.AsSpan(8));
        var csp = System.Text.Encoding.Unicode.GetBytes(cspName);
        var fixedEnd = 12 + 32;
        var tail = info.AsSpan(12 + headerSize);
        var result = new byte[fixedEnd + csp.Length + tail.Length];
        info.AsSpan(0, fixedEnd).CopyTo(result);
        csp.CopyTo(result, fixedEnd);
        tail.CopyTo(result.AsSpan(fixedEnd + csp.Length));
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8), 32 + csp.Length);
        return result;
    }

    [Fact]
    public void Decrypt_StandardSaltSizeNot16_ThrowsCorruptFile()
    {
        var (info, package) = ReadRawStreams(StandardDocx);
        var headerSize = BinaryPrimitives.ReadInt32LittleEndian(info.AsSpan(8));
        BinaryPrimitives.WriteInt32LittleEndian(info.AsSpan(12 + headerSize), 8);
        using var root = BuildCfb(info, package);

        Assert.Throws<CorruptFileException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(StandardDocx), DefaultCap));
    }

    [Fact]
    public void Decrypt_MissingEncryptedPackage_ThrowsCorruptFile()
    {
        var (info, _) = ReadRawStreams(AgilePptx);
        using var stream = new MemoryStream();
        using (var create = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
        {
            using var s = create.CreateStream(EncryptedPackageDecryptor.EncryptionInfoStreamName);
            s.Write(info);
        }

        stream.Position = 0;
        using var root = RootStorage.Open(stream);

        Assert.Throws<CorruptFileException>(() =>
            EncryptedPackageDecryptor.Decrypt(root, PasswordOf(AgilePptx), DefaultCap));
    }

    private static RootStorage OpenSample(string sampleName) =>
        RootStorage.Open(new MemoryStream(File.ReadAllBytes(Sample(sampleName).FullPath)));

    private static (byte[] Info, byte[] Package) ReadRawStreams(string sampleName)
    {
        using var root = OpenSample(sampleName);
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

    private static RootStorage BuildCfb(byte[] info, byte[] package)
    {
        var stream = new MemoryStream();
        using (var root = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
        {
            using (var s = root.CreateStream(EncryptedPackageDecryptor.EncryptionInfoStreamName))
            {
                s.Write(info);
            }

            using (var s = root.CreateStream(EncryptedPackageDecryptor.EncryptedPackageStreamName))
            {
                s.Write(package);
            }
        }

        stream.Position = 0;
        return RootStorage.Open(stream);
    }

    // [S-pw-ooxml#1] Standard package built in-test with NPOI main's Standard encryptor.
    private static byte[] EncryptStandard(byte[] payload, string password)
    {
        var info = new EncryptionInfo(EncryptionMode.Standard);
        var encryptor = info.Encryptor;
        encryptor.ConfirmPassword(password);

        var fs = new NPOIFSFileSystem();
        try
        {
            var os = encryptor.GetDataStream(fs);
            os.Write(payload, 0, payload.Length);
            os.Close();

            using var output = new MemoryStream();
            fs.WriteFileSystem(output);
            return output.ToArray();
        }
        finally
        {
            fs.Close();
        }
    }

    private sealed class AllocationSpy
    {
        public int Calls { get; private set; }

        public MemoryStream Allocate(long size)
        {
            Calls++;
            return new MemoryStream((int)size);
        }
    }
}
