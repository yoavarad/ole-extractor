using System.Security.Cryptography;
using System.Xml.Linq;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption.Agile;
using Xunit;
using static ExtractorOLE.Tests.Encryption.AgileTestSamples;
using HashAlgorithm = NPOI.POIFS.Crypt.HashAlgorithm;

namespace ExtractorOLE.Tests.Encryption;

// Known-answer tests for the MS-OFFCRYPTO 2.3.4.14 data-integrity check with a SHA-1 / AES-128-CBC
// descriptor. The package and its HMAC-SHA1 are built here with System.Security.Cryptography only,
// independently of the production code. Password hashing is injected so no spin loop runs.
public class AgileDecryptorSha1IntegrityTests
{
    private const string CorrectPassword = "sha1-integrity";
    private const int Block = 16;
    private const int Segment = 4096;

    private static readonly byte[] PasswordHash = Enumerable.Range(1, 20).Select(i => (byte)i).ToArray();
    private static readonly byte[] WrongPasswordHash = Enumerable.Range(21, 20).Select(i => (byte)i).ToArray();
    private static readonly byte[] KeySalt = Enumerable.Range(100, Block).Select(i => (byte)i).ToArray();
    private static readonly byte[] VerifierSalt = Enumerable.Range(200, Block).Select(i => (byte)i).ToArray();
    private static readonly byte[] PackageKey = Enumerable.Range(50, Block).Select(i => (byte)i).ToArray();
    private static readonly byte[] VerifierInput = Enumerable.Range(150, Block).Select(i => (byte)i).ToArray();
    private static readonly byte[] HmacKey = Enumerable.Range(10, 20).Select(i => (byte)i).ToArray();

    [Fact]
    public void DecryptPackage_Sha1Descriptor_IntegrityPassesAndRoundTrips()
    {
        var plain = Plaintext();
        var (descriptor, package) = Build(plain);
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor), DerivePasswordHash);

        using var output = new MemoryStream();
        decryptor.DecryptPackage(CorrectPassword, new MemoryStream(package), output);

        Assert.Equal(plain, output.ToArray());
    }

    [Fact]
    public void VerifyIntegrity_Sha1Descriptor_AcceptsIndependentHmacSha1()
    {
        var (descriptor, package) = Build(Plaintext());
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor), DerivePasswordHash);
        Assert.True(decryptor.VerifyPassword(CorrectPassword));

        Assert.Null(Record.Exception(() => decryptor.VerifyIntegrity(new MemoryStream(package))));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(5000)]
    public void DecryptPackage_Sha1FlippedByteCorrectPassword_ThrowsCorruptFileAndWritesNothing(int offset)
    {
        var (descriptor, package) = Build(Plaintext());
        package[offset] ^= 0x01;
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor), DerivePasswordHash);

        using var output = new MemoryStream();
        Assert.Throws<CorruptFileException>(() => decryptor.DecryptPackage(CorrectPassword, new MemoryStream(package), output));
        Assert.Equal(0, output.Length);
    }

    [Fact]
    public void DecryptPackage_Sha1TamperedPackageWrongPassword_ThrowsInvalidPassword()
    {
        var (descriptor, package) = Build(Plaintext());
        package[8] ^= 0x01;
        using var decryptor = AgileDecryptor.FromDescriptor(new MemoryStream(descriptor), DerivePasswordHash);

        Assert.Throws<InvalidPasswordException>(() => decryptor.DecryptPackage("wrong", new MemoryStream(package), Stream.Null));
    }

    private static byte[] Sha1(byte[] data) => CryptographicOperations.HashData(HashAlgorithmName.SHA1, data);

    private static byte[] HmacSha1(byte[] key, byte[] data) => CryptographicOperations.HmacData(HashAlgorithmName.SHA1, key, data);

    private static byte[] Plaintext() => Enumerable.Range(0, 5000).Select(i => (byte)(i * 7)).ToArray();

    private static byte[] DerivePasswordHash(string password, HashAlgorithm hashAlgorithm, byte[] salt, int spinCount) =>
        password == CorrectPassword ? PasswordHash : WrongPasswordHash;

    private static (byte[] Descriptor, byte[] Package) Build(byte[] plain)
    {
        // EncryptedPackage: 8-byte StreamSize, then 4096-byte segments, each AES-CBC with IV = SHA1(keySalt + index)[..16].
        var padded = ZeroPad(plain, (plain.Length + Block - 1) / Block * Block);
        using var stream = new MemoryStream();
        stream.Write(BitConverter.GetBytes((long)plain.Length));
        for (var index = 0; index * Segment < padded.Length; index++)
        {
            var chunk = padded.AsSpan(index * Segment, Math.Min(Segment, padded.Length - (index * Segment))).ToArray();
            var iv = Sha1([.. KeySalt, .. BitConverter.GetBytes(index)])[..Block];
            stream.Write(Encrypt(PackageKey, iv, chunk));
        }

        var package = stream.ToArray();
        var hmac = HmacSha1(HmacKey, package);

        var descriptor = MutateDescriptor(document =>
        {
            var keyData = document.Descendants(EncryptionNs + "keyData").Single();
            UseSha1Aes128(keyData);
            keyData.SetAttributeValue("saltValue", Convert.ToBase64String(KeySalt));

            var encryptedKey = document.Descendants(Password + "encryptedKey").Single();
            UseSha1Aes128(encryptedKey);
            encryptedKey.SetAttributeValue("spinCount", "1");
            encryptedKey.SetAttributeValue("saltValue", Convert.ToBase64String(VerifierSalt));
            encryptedKey.SetAttributeValue("encryptedVerifierHashInput", Convert.ToBase64String(
                Encrypt(IntermediateKey([0xfe, 0xa7, 0xd2, 0x76, 0x3b, 0x4b, 0x9e, 0x79]), VerifierSalt, VerifierInput)));
            encryptedKey.SetAttributeValue("encryptedVerifierHashValue", Convert.ToBase64String(
                Encrypt(IntermediateKey([0xd7, 0xaa, 0x0f, 0x6d, 0x30, 0x61, 0x34, 0x4e]), VerifierSalt, ZeroPad(Sha1(VerifierInput), 32))));
            encryptedKey.SetAttributeValue("encryptedKeyValue", Convert.ToBase64String(
                Encrypt(IntermediateKey([0x14, 0x6e, 0x0b, 0xe7, 0xab, 0xac, 0xd0, 0xd6]), VerifierSalt, PackageKey)));

            var integrity = document.Descendants(EncryptionNs + "dataIntegrity").Single();
            integrity.SetAttributeValue("encryptedHmacKey", Convert.ToBase64String(IntegrityValue([0x5f, 0xb2, 0xad, 0x01, 0x0c, 0xb9, 0xe1, 0xf6], HmacKey)));
            integrity.SetAttributeValue("encryptedHmacValue", Convert.ToBase64String(IntegrityValue([0xa0, 0x67, 0x7f, 0x02, 0xb2, 0x2c, 0x84, 0x33], hmac)));
        });

        return (descriptor, package);
    }

    private static void UseSha1Aes128(XElement element)
    {
        element.SetAttributeValue("cipherAlgorithm", "AES");
        element.SetAttributeValue("cipherChaining", "ChainingModeCBC");
        element.SetAttributeValue("hashAlgorithm", "SHA1");
        element.SetAttributeValue("hashSize", "20");
        element.SetAttributeValue("keyBits", "128");
        element.SetAttributeValue("blockSize", "16");
        element.SetAttributeValue("saltSize", "16");
    }

    private static byte[] IntermediateKey(byte[] blockKey) => Sha1([.. PasswordHash, .. blockKey])[..Block];

    private static byte[] IntegrityValue(byte[] blockKey, byte[] value) =>
        Encrypt(PackageKey, Sha1([.. KeySalt, .. blockKey])[..Block], ZeroPad(value, 32));

    private static byte[] ZeroPad(byte[] value, int length)
    {
        var padded = new byte[length];
        value.CopyTo(padded, 0);
        return padded;
    }

    private static byte[] Encrypt(byte[] key, byte[] iv, byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        return aes.EncryptCbc(data, iv, PaddingMode.None);
    }
}
