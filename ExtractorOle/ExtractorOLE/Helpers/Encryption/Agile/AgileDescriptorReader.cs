using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using ExtractorOLE.Exceptions;
using NPOI;
using NPOI.POIFS.Crypt;

namespace ExtractorOLE.Helpers.Encryption.Agile;

// Reads the Agile EncryptionInfo XML descriptor (MS-OFFCRYPTO 2.3.4.10) with System.Xml.
// Replaces NPOI ooxml's NPOI.OpenXmlFormats.Encryption binding (ADR-012). Every malformed or
// unsupported value is CORRUPT_FILE: a damaged descriptor must never look like a wrong password.
internal static class AgileDescriptorReader
{
    internal const string EncryptionNamespace = "http://schemas.microsoft.com/office/2006/encryption";
    internal const string PasswordNamespace = "http://schemas.microsoft.com/office/2006/keyEncryptor/password";

    // The encrypted package's inner format is unknown until it is decrypted.
    internal const string ContainerMimeType = "application/octet-stream";

    // A real descriptor is a few kilobytes; this only bounds a hostile one.
    private const long MaxDescriptorCharacters = 1_000_000;

    internal static XElement LoadEncryptionElement(Stream descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxDescriptorCharacters,
        };

        XDocument document;
        try
        {
            using var reader = XmlReader.Create(descriptor, settings);
            document = XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw Corrupt("the EncryptionInfo descriptor is not well-formed XML", ex);
        }

        var root = document.Root;
        if (root is null || root.Name != XName.Get("encryption", EncryptionNamespace))
        {
            throw Corrupt("the EncryptionInfo descriptor has no encryption element");
        }

        return root;
    }

    internal static XElement RequiredChild(XElement parent, XName name) =>
        parent.Element(name) ?? throw Corrupt($"the EncryptionInfo descriptor is missing {name.LocalName}");

    internal static int RequiredInt(XElement element, string attribute)
    {
        var value = RequiredLong(element, attribute);
        if (value is < 0 or > int.MaxValue)
        {
            throw Corrupt($"descriptor attribute {attribute} is out of range");
        }

        return (int)value;
    }

    internal static long RequiredLong(XElement element, string attribute)
    {
        var raw = RequiredString(element, attribute);
        if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw Corrupt($"descriptor attribute {attribute} is not an integer");
        }

        return value;
    }

    internal static byte[] RequiredBase64(XElement element, string attribute)
    {
        var raw = RequiredString(element, attribute);
        try
        {
            return Convert.FromBase64String(raw);
        }
        catch (FormatException ex)
        {
            throw Corrupt($"descriptor attribute {attribute} is not valid base64", ex);
        }
    }

    internal static CipherAlgorithm ReadCipherAlgorithm(XElement element, int keyBits)
    {
        var xmlId = RequiredString(element, "cipherAlgorithm");
        CipherAlgorithm cipher;
        try
        {
            cipher = CipherAlgorithm.FromXmlId(xmlId, keyBits);
        }
        catch (EncryptedDocumentException ex)
        {
            throw Corrupt($"descriptor cipher {xmlId}/{keyBits} is not supported", ex);
        }

        // Agile allows other block ciphers (RC2, DES, 3DES), but only AES is decrypted here: anything
        // else is encryption this library does not decrypt (PASSWORD_PROTECTED, ADR-012 decision 3).
        if (cipher != CipherAlgorithm.aes128 && cipher != CipherAlgorithm.aes192 && cipher != CipherAlgorithm.aes256)
        {
            throw new PasswordProtectedException();
        }

        return cipher;
    }

    internal static HashAlgorithm ReadHashAlgorithm(XElement element)
    {
        var ecmaString = RequiredString(element, "hashAlgorithm");
        HashAlgorithm hash;
        try
        {
            hash = HashAlgorithm.FromEcmaId(ecmaString);
        }
        catch (EncryptedDocumentException ex)
        {
            throw Corrupt($"descriptor hash {ecmaString} is not supported", ex);
        }

        // Only the SHA family is decrypted here; other spec hashes (MD5, RIPEMD, ...) are encryption this
        // library does not decrypt (PASSWORD_PROTECTED).
        if (hash != HashAlgorithm.sha1 && hash != HashAlgorithm.sha256 && hash != HashAlgorithm.sha384 && hash != HashAlgorithm.sha512)
        {
            throw new PasswordProtectedException();
        }

        var hashSize = RequiredInt(element, "hashSize");
        if (hash.hashSize <= 0 || hash.hashSize != hashSize)
        {
            throw Corrupt($"descriptor hash {ecmaString} does not have {hashSize} bytes");
        }

        return hash;
    }

    internal static ChainingMode ReadChainingMode(XElement element)
    {
        var chaining = RequiredString(element, "cipherChaining");
        return chaining switch
        {
            "ChainingModeCBC" => ChainingMode.cbc,
            "ChainingModeCFB" => ChainingMode.cfb,
            _ => throw Corrupt($"descriptor chaining mode {chaining} is not supported"),
        };
    }

    internal static CorruptFileException Corrupt(string reason, Exception? inner = null) =>
        new(ContainerMimeType, reason, inner);

    private static string RequiredString(XElement element, string attribute) =>
        element.Attribute(attribute)?.Value
        ?? throw Corrupt($"descriptor element {element.Name.LocalName} is missing {attribute}");
}
