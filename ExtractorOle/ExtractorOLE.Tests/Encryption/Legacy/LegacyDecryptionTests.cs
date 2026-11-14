using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy;

// #406: LegacyDecryption.Decrypt on its own. End-to-end behaviour through MainExtractor is in
// DocDecryptionExtractTests.
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

    [Fact]
    public void Decrypt_EncryptedPpt_IsPasswordProtected_Interim_UntilDecryptionWired()
    {
        Assert.Throws<PasswordProtectedException>(() => Decrypt(LegacyFixtures.HelloPpt, PptMime, OfficeMimeType.PowerPointLegacy));
    }

    [Fact]
    public void Decrypt_StreamOverPerEntryCap_IsOversizedNestedContent()
    {
        var limits = new MimeDetectionLimits { MaxDeclaredEntryBytes = 1024 };

        Assert.Throws<OversizedNestedContentException>(() =>
            Decrypt(LegacyFixtures.PasswordCryptoApiDoc, DocMime, OfficeMimeType.WordLegacy, limits));
    }
}
