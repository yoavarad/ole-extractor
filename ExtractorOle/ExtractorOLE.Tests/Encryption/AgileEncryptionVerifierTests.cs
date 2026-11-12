using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Agile;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;
using HashAlgorithm = NPOI.POIFS.Crypt.HashAlgorithm;

namespace ExtractorOLE.Tests.Encryption;

public class AgileEncryptionVerifierTests
{
    [Fact]
    public void Parse_SampleDescriptor_ReadsPasswordKeyEncryptor()
    {
        var (descriptor, _) = ReadStreams(AgilePptx);

        var verifier = AgileEncryptionVerifier.Parse(LoadEncryption(descriptor));

        Assert.Same(HashAlgorithm.sha512, verifier.HashAlgorithm);
        Assert.Equal(100_000, verifier.SpinCount);
        Assert.Equal(verifier.BlockSize, verifier.Salt.Length);
    }

    [Fact]
    public void Parse_SpinCountAtMaximum_IsAccepted()
    {
        var descriptor = MutatePasswordKeyEncryptor(encryptedKey => encryptedKey.SetAttributeValue("spinCount", "10000000"));

        var verifier = AgileEncryptionVerifier.Parse(LoadEncryption(descriptor));

        Assert.Equal(AgileEncryptionVerifier.MaxSpinCount, verifier.SpinCount);
    }

    [Theory]
    [InlineData("10000001")]
    [InlineData("-1")]
    [InlineData("4294967296")]
    [InlineData("many")]
    public void Parse_InvalidSpinCount_ThrowsCorruptFile(string spinCount)
    {
        var descriptor = MutatePasswordKeyEncryptor(encryptedKey => encryptedKey.SetAttributeValue("spinCount", spinCount));

        Assert.Throws<CorruptFileException>(() => AgileEncryptionVerifier.Parse(LoadEncryption(descriptor)));
    }

    [Fact]
    public void Parse_NonSha512Hash_ReadsDescriptorHash()
    {
        var descriptor = MutatePasswordKeyEncryptor(encryptedKey =>
        {
            encryptedKey.SetAttributeValue("hashAlgorithm", "SHA1");
            encryptedKey.SetAttributeValue("hashSize", "20");
        });

        var verifier = AgileEncryptionVerifier.Parse(LoadEncryption(descriptor));

        Assert.Same(HashAlgorithm.sha1, verifier.HashAlgorithm);
    }

    [Fact]
    public void Parse_HashSizeMismatch_ThrowsCorruptFile()
    {
        var descriptor = MutatePasswordKeyEncryptor(encryptedKey => encryptedKey.SetAttributeValue("hashSize", "20"));

        Assert.Throws<CorruptFileException>(() => AgileEncryptionVerifier.Parse(LoadEncryption(descriptor)));
    }

    [Fact]
    public void Parse_UnsupportedSpecHash_ThrowsPasswordProtected()
    {
        var descriptor = MutatePasswordKeyEncryptor(encryptedKey =>
        {
            encryptedKey.SetAttributeValue("hashAlgorithm", "MD5");
            encryptedKey.SetAttributeValue("hashSize", "16");
        });

        Assert.Throws<PasswordProtectedException>(() => AgileEncryptionVerifier.Parse(LoadEncryption(descriptor)));
    }

    [Fact]
    public void Parse_TruncatedVerifierHashValue_ThrowsCorruptFileNotWrongPassword()
    {
        var descriptor = MutatePasswordKeyEncryptor(encryptedKey =>
            encryptedKey.SetAttributeValue("encryptedVerifierHashValue", Convert.ToBase64String(new byte[16])));

        Assert.Throws<CorruptFileException>(() => AgileEncryptionVerifier.Parse(LoadEncryption(descriptor)));
    }
}
