using System.Xml.Linq;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Agile;
using NPOI.POIFS.Crypt;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;
using HashAlgorithm = NPOI.POIFS.Crypt.HashAlgorithm;

namespace ExtractorOLE.Tests.Encryption;

public class AgileEncryptionHeaderTests
{
    [Fact]
    public void Parse_SampleDescriptor_ReadsKeyDataAndDataIntegrity()
    {
        var (descriptor, _) = ReadStreams(AgilePptx);

        var header = AgileEncryptionHeader.Parse(LoadEncryption(descriptor));

        Assert.Same(CipherAlgorithm.aes256, header.CipherAlgorithm);
        Assert.Same(HashAlgorithm.sha512, header.HashAlgorithm);
        Assert.Same(ChainingMode.cbc, header.ChainingMode);
        Assert.Equal(16, header.KeySalt.Length);
        Assert.NotEmpty(header.EncryptedHmacKey);
        Assert.NotEmpty(header.EncryptedHmacValue);
    }

    [Fact]
    public void Parse_SaltLengthMismatch_ThrowsCorruptFile()
    {
        var descriptor = MutateKeyData(keyData => keyData.SetAttributeValue("saltValue", Convert.ToBase64String(new byte[8])));

        Assert.Throws<CorruptFileException>(() => AgileEncryptionHeader.Parse(LoadEncryption(descriptor)));
    }

    [Fact]
    public void Parse_UnknownChainingMode_ThrowsCorruptFile()
    {
        var descriptor = MutateKeyData(keyData => keyData.SetAttributeValue("cipherChaining", "ChainingModeECB"));

        Assert.Throws<CorruptFileException>(() => AgileEncryptionHeader.Parse(LoadEncryption(descriptor)));
    }

    [Fact]
    public void Parse_NonAesSpecCipher_ThrowsPasswordProtected()
    {
        var descriptor = MutateKeyData(keyData =>
        {
            keyData.SetAttributeValue("cipherAlgorithm", "3DES");
            keyData.SetAttributeValue("keyBits", "192");
            keyData.SetAttributeValue("blockSize", "8");
        });

        Assert.Throws<PasswordProtectedException>(() => AgileEncryptionHeader.Parse(LoadEncryption(descriptor)));
    }

    [Fact]
    public void Parse_MissingDataIntegrity_ThrowsCorruptFile()
    {
        var descriptor = MutateDescriptor(document => document.Descendants(EncryptionNs + "dataIntegrity").Remove());

        Assert.Throws<CorruptFileException>(() => AgileEncryptionHeader.Parse(LoadEncryption(descriptor)));
    }
}
