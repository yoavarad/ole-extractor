using System.Xml.Linq;
using NPOI.POIFS.Crypt;
using OpenMcdf;
using Xunit;
using HashAlgorithm = NPOI.POIFS.Crypt.HashAlgorithm;

namespace ExtractorOLE.Tests.Encryption;

// Agile samples are synthetic (msoffcrypto-tool); passwords come from samples/manifest.json.
// samples/encrypted/password-protected.docx is Standard encryption (EncryptionInfo 4.2), not Agile.
internal static class AgileTestSamples
{
    public const string AgilePptx = "agile-password.pptx";
    public const string AgileNonAsciiXlsx = "agile-nonascii-password.xlsx";
    public const string AgileTamperedXlsx = "agile-tampered-package.xlsx";

    public static readonly XNamespace EncryptionNs = "http://schemas.microsoft.com/office/2006/encryption";
    public static readonly XNamespace Password = "http://schemas.microsoft.com/office/2006/keyEncryptor/password";
    public static readonly XNamespace Certificate = "http://schemas.microsoft.com/office/2006/keyEncryptor/certificate";

    public static EncryptedSamples.EncryptedSample Sample(string fileName) =>
        EncryptedSamples.Enumerate().Single(s => string.Equals(Path.GetFileName(s.Path), fileName, StringComparison.OrdinalIgnoreCase));

    public static string PasswordOf(string fileName) => Sample(fileName).Password!;

    public static (byte[] Descriptor, byte[] Package) ReadStreams(string sampleName)
    {
        using var root = RootStorage.Open(new MemoryStream(File.ReadAllBytes(Sample(sampleName).FullPath)));
        var info = ReadStream(root, "EncryptionInfo");
        var package = ReadStream(root, "EncryptedPackage");

        // Agile EncryptionInfo: version 4.4, then 4 reserved bytes, then the XML descriptor.
        Assert.Equal(4, BitConverter.ToUInt16(info, 0));
        Assert.Equal(4, BitConverter.ToUInt16(info, 2));
        return (info[8..], package);
    }

    public static XElement LoadEncryption(byte[] descriptor) => XDocument.Load(new MemoryStream(descriptor)).Root!;

    public static byte[] MutatePasswordKeyEncryptor(Action<XElement> mutate) =>
        MutateDescriptor(document => mutate(document.Descendants(Password + "encryptedKey").Single()));

    public static byte[] MutateKeyData(Action<XElement> mutate) =>
        MutateDescriptor(document => mutate(document.Descendants(EncryptionNs + "keyData").Single()));

    public static byte[] MutateDescriptor(Action<XDocument> mutate)
    {
        var (descriptor, _) = ReadStreams(AgilePptx);
        var document = XDocument.Load(new MemoryStream(descriptor));
        mutate(document);

        using var output = new MemoryStream();
        document.Save(output);
        return output.ToArray();
    }

    private static byte[] ReadStream(RootStorage root, string name)
    {
        using var stream = root.OpenStream(name);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    internal sealed class CountingKeyDerivation
    {
        public int Calls { get; private set; }

        public byte[] Derive(string password, HashAlgorithm hashAlgorithm, byte[] salt, int spinCount)
        {
            Calls++;
            return CryptoFunctions.HashPassword(password, hashAlgorithm, salt, spinCount);
        }
    }
}
