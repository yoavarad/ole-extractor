using System.Buffers.Binary;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// #406/#407: LegacyDecryption.Decrypt on its own. End-to-end behaviour through MainExtractor is in
// DocDecryptionExtractTests and PptDecryptionExtractTests.
public sealed class LegacyDecryptionTests
{
    private const string DocMime = "application/msword";
    private const string PptMime = "application/vnd.ms-powerpoint";

    private static LegacyPlaintext Decrypt(string file, string mime, OfficeMimeType format, MimeDetectionLimits? limits = null)
    {
        using var input = ExtractionInput.From(new ExtractionRequest { FileBytes = LegacyFixtures.Bytes(file), FileName = file, DetectedMimeType = mime });
        var encrypted = LegacyEncryptionGuard.Inspect(input, format, hasPassword: true, mime)!;
        return LegacyDecryption.Decrypt(input, encrypted, LegacyFixtures.Password(file)!, limits ?? new MimeDetectionLimits(), mime);
    }

    [Theory]
    [InlineData(LegacyFixtures.TikaBinaryRc4Doc)]
    [InlineData(LegacyFixtures.PasswordCryptoApiDoc)]
    [InlineData(LegacyFixtures.Rc4CryptoApiDoc)]
    public void Decrypt_EncryptedDoc_ReturnsPlainCompoundFileWithNoDeferredFailures(string file)
    {
        var plaintext = Decrypt(file, DocMime, OfficeMimeType.WordLegacy);

        Assert.Empty(plaintext.DeferredFieldFailures);
        using var input = ExtractionInput.FromPlaintext(plaintext.Bytes);
        Assert.Null(LegacyEncryptionGuard.Inspect(input, OfficeMimeType.WordLegacy, hasPassword: true, DocMime));
    }

    [Theory]
    [InlineData(LegacyFixtures.HelloPpt)]
    [InlineData(LegacyFixtures.HelloNoPropsPpt)]
    [InlineData(LegacyFixtures.PngEncryptedPpt)]
    public void Decrypt_EncryptedPpt_ReturnsPlainCompoundFileWithNoDeferredFailures(string file)
    {
        var plaintext = Decrypt(file, PptMime, OfficeMimeType.PowerPointLegacy);

        // UserEditAtoms and the CryptSession10Container stay in place (PptDecryptor keeps offsets), so the
        // rebuild is recognised by its plain Current User token and the missing EncryptedSummary.
        Assert.Empty(plaintext.DeferredFieldFailures);
        using var fs = new NPOIFSFileSystem(new MemoryStream(plaintext.Bytes, writable: false));
        var entry = (DocumentEntry)fs.Root.GetEntry("Current User");
        var currentUser = new byte[entry.Size];
        using (var stream = fs.Root.CreateDocumentInputStream(entry))
        {
            stream.ReadFully(currentUser);
        }

        Assert.Equal(PptEncryptionProbe.TokenPlain, BinaryPrimitives.ReadUInt32LittleEndian(currentUser.AsSpan(12)));
        Assert.False(fs.Root.HasEntry(CryptoApiEncryptedSummaryReader.PptStreamName));
    }

    [Fact]
    public void Decrypt_StreamOverPerEntryCap_IsOversizedNestedContent()
    {
        var limits = new MimeDetectionLimits { MaxDeclaredEntryBytes = 1024 };

        Assert.Throws<OversizedNestedContentException>(() =>
            Decrypt(LegacyFixtures.PasswordCryptoApiDoc, DocMime, OfficeMimeType.WordLegacy, limits));
    }

    [Fact]
    public void Decrypt_PptPicturesOverPerEntryCap_IsOversizedNestedContent()
    {
        var limits = new MimeDetectionLimits { MaxDeclaredEntryBytes = 512 * 1024 };

        Assert.Throws<OversizedNestedContentException>(() =>
            Decrypt(LegacyFixtures.CryptoApiProc2356Ppt, PptMime, OfficeMimeType.PowerPointLegacy, limits));
    }
}
