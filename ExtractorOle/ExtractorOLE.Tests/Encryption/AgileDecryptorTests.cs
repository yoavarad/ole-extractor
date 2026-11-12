using System.IO.Compression;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Agile;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;

namespace ExtractorOLE.Tests.Encryption;

public class AgileDecryptorTests
{
    [Theory]
    [InlineData(AgilePptx, "ppt/presentation.xml")]
    [InlineData(AgileNonAsciiXlsx, "xl/workbook.xml")]
    public void DecryptPackage_CorrectPassword_DecryptsToValidOoxmlZip(string sampleName, string mainPart)
    {
        var (descriptor, package) = ReadStreams(sampleName);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));

        using var output = new MemoryStream();
        decryptor.DecryptPackage(PasswordOf(sampleName), new MemoryStream(package), output);

        Assert.Equal(BitConverter.ToInt64(package, 0), output.Length);
        output.Position = 0;
        using var zip = new ZipArchive(output, ZipArchiveMode.Read);
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry(mainPart));
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var (descriptor, _) = ReadStreams(AgilePptx);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));

        Assert.True(decryptor.VerifyPassword(PasswordOf(AgilePptx)));
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalseAndDecryptsNothing()
    {
        var (descriptor, package) = ReadStreams(AgilePptx);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));

        Assert.False(decryptor.VerifyPassword("not-the-password"));

        using var output = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() =>
            decryptor.Decrypt(new MemoryStream(package, 8, package.Length - 8), BitConverter.ToInt64(package, 0), output));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void VerifyPassword_WrongAfterCorrect_ClearsKey()
    {
        var (descriptor, package) = ReadStreams(AgilePptx);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));
        Assert.True(decryptor.VerifyPassword(PasswordOf(AgilePptx)));

        Assert.False(decryptor.VerifyPassword("not-the-password"));

        Assert.Throws<InvalidOperationException>(() => decryptor.VerifyIntegrity(new MemoryStream(package)));
    }

    [Fact]
    public void VerifyPassword_UsesInjectedKeyDerivationOnce()
    {
        var (descriptor, _) = ReadStreams(AgilePptx);
        var counter = new CountingKeyDerivation();
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor), counter.Derive);

        Assert.True(decryptor.VerifyPassword(PasswordOf(AgilePptx)));
        Assert.Equal(1, counter.Calls);
    }

    [Fact]
    public void DecryptPackage_WrongPassword_ThrowsInvalidPasswordAndWritesNothing()
    {
        var (descriptor, package) = ReadStreams(AgilePptx);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));

        using var output = new MemoryStream();
        Assert.Throws<InvalidPasswordException>(() => decryptor.DecryptPackage("not-the-password", new MemoryStream(package), output));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void DecryptPackage_TamperedPackageCorrectPassword_ThrowsCorruptFileAndWritesNothing()
    {
        var (descriptor, package) = ReadStreams(AgileTamperedXlsx);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));

        using var output = new MemoryStream();
        Assert.Throws<CorruptFileException>(() => decryptor.DecryptPackage(PasswordOf(AgileTamperedXlsx), new MemoryStream(package), output));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void DecryptPackage_TamperedPackageWrongPassword_ThrowsInvalidPassword()
    {
        var (descriptor, package) = ReadStreams(AgileTamperedXlsx);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));

        Assert.Throws<InvalidPasswordException>(() => decryptor.DecryptPackage("not-the-password", new MemoryStream(package), Stream.Null));
    }

    [Fact]
    public void FromDescriptor_SpinCountOverMaximum_ThrowsCorruptFileWithoutKeyDerivation()
    {
        var descriptor = MutatePasswordKeyEncryptor(encryptedKey => encryptedKey.SetAttributeValue("spinCount", "10000001"));
        var counter = new CountingKeyDerivation();

        Assert.Throws<CorruptFileException>(() =>
            AgileDecryptor.FromDescriptor(new MemoryStream(descriptor), counter.Derive).VerifyPassword(PasswordOf(AgilePptx)));
        Assert.Equal(0, counter.Calls);
    }

    [Fact]
    public void FromDescriptor_CertificateKeyEncryptorOnly_ThrowsPasswordProtectedWithoutKeyDerivation()
    {
        var descriptor = MutateDescriptor(document =>
        {
            var keyEncryptor = document.Descendants(EncryptionNs + "keyEncryptor").Single();
            keyEncryptor.SetAttributeValue("uri", Certificate.NamespaceName);
            keyEncryptor.ReplaceNodes(new System.Xml.Linq.XElement(
                Certificate + "encryptedKey",
                new System.Xml.Linq.XAttribute("encryptedKeyValue", "AAAA"),
                new System.Xml.Linq.XAttribute("X509Certificate", "AAAA"),
                new System.Xml.Linq.XAttribute("certVerifier", "AAAA")));
        });
        var counter = new CountingKeyDerivation();

        Assert.Throws<PasswordProtectedException>(() =>
            AgileDecryptor.FromDescriptor(new MemoryStream(descriptor), counter.Derive).VerifyPassword("any"));
        Assert.Equal(0, counter.Calls);
    }

    [Fact]
    public void Decrypt_CiphertextShorterThanDeclared_ThrowsTruncatedContainer()
    {
        var (descriptor, package) = ReadStreams(AgilePptx);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor));
        Assert.True(decryptor.VerifyPassword(PasswordOf(AgilePptx)));

        Assert.Throws<TruncatedContainerException>(() =>
            decryptor.Decrypt(new MemoryStream(package, 8, 4096), BitConverter.ToInt64(package, 0), Stream.Null));
    }
}
