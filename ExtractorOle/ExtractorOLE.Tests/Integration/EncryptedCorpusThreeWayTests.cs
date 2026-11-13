using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using Xunit;
using static ExtractorOLE.Tests.Integration.EncryptedCorpus;

namespace ExtractorOLE.Tests.Integration
{
    // Manifest-driven three-way tests (no / wrong / correct password) over samples/encrypted/ for
    // Extract and DetectMimeType ([ydk:req:extraction/password-decryption], testing-strategy.md).
    // Samples, passwords and twins come from the manifest through the EncryptedCorpus harness.
    //
    // Encrypted doc and ppt entries are excluded here by manifest format: they are covered by the
    // legacy batch's corpus tests, which plug their own theory data (EncryptedCorpus.DecryptablePaths
    // for "doc"/"ppt") into the same harness assertions.
    public class EncryptedCorpusThreeWayTests
    {
        private static readonly string[] OoxmlFormats = { "docx", "xlsx", "pptx" };
        private static readonly string[] XlsFormats = { "xls" };

        // Cryptographic-API xls: encrypted document properties are not read yet, so Metadata stays empty.
        private const string CryptoApiXls = "samples/encrypted/cryptoapi-encrypted-properties.xls";

        public static IEnumerable<object[]> OoxmlDecryptable() => DecryptablePaths(OoxmlFormats);

        public static IEnumerable<object[]> OoxmlTampered() => RejectedWithCorrectPasswordPaths(OoxmlFormats);

        public static IEnumerable<object[]> XlsDecryptable() => DecryptablePaths(XlsFormats);

        public static IEnumerable<object[]> XlsDefaultKey() =>
            Paths(e => e.Format == "xls" && e.Password is null && !e.NegativeOnly);

        public static IEnumerable<object[]> EmbeddingDocument() =>
            Paths(e => e.Format == "docx" && e.Password is null);

        [Fact]
        public void Harness_FindsEverySampleGroupThisSuiteCovers()
        {
            Assert.NotEmpty(OoxmlDecryptable());
            Assert.NotEmpty(OoxmlTampered());
            Assert.NotEmpty(XlsDecryptable());
            Assert.NotEmpty(XlsDefaultKey());
            Assert.NotEmpty(EmbeddingDocument());
        }

        [Fact]
        public void Harness_CoversOnlyNonLegacyDocPptEntries_EveryEncryptedEntryAccountedFor()
        {
            var covered = new[] { OoxmlDecryptable(), OoxmlTampered(), XlsDecryptable(), XlsDefaultKey(), EmbeddingDocument() }
                .SelectMany(g => g).Select(o => (string)o[0]).ToHashSet();

            foreach (var entry in Entries())
            {
                var excluded = entry.Format is "doc" or "ppt";
                Assert.Equal(!excluded, covered.Contains(entry.Path));
            }
        }

        // ---- Extract ----

        [Theory]
        [MemberData(nameof(OoxmlDecryptable))]
        [MemberData(nameof(XlsDecryptable))]
        [MemberData(nameof(OoxmlTampered))]
        public void Extract_NoPassword_ThrowsPasswordProtected(string path) =>
            AssertExtractNoPasswordRejected(Get(path));

        [Theory]
        [MemberData(nameof(OoxmlDecryptable))]
        [MemberData(nameof(XlsDecryptable))]
        [MemberData(nameof(OoxmlTampered))]
        public void Extract_WrongPassword_ThrowsInvalidPassword(string path) =>
            AssertExtractWrongPasswordRejected(Get(path));

        [Theory]
        [MemberData(nameof(OoxmlDecryptable))]
        [MemberData(nameof(XlsDecryptable))]
        public void Extract_CorrectPassword_EqualsTwinLeafForLeaf(string path)
        {
            var entry = Get(path);
            if (entry.TwinFullPath is null)
            {
                Assert.Equal("xls", entry.Format);
                // XOR-obfuscated xls entries record no twin; their decrypted cells are asserted by
                // XlsPasswordDecryptionTests. Here: the correct password yields a clean result.
                var result = Extract(ExtractRequest(entry, entry.Password));
                Assert.Equal(entry.InnerMimeType, result.MimeType);
                Assert.Empty(result.FieldErrors);
                return;
            }

            AssertExtractCorrectPasswordEqualsTwin(entry, unreadableMetadata: entry.Path == CryptoApiXls);
        }

        [Theory]
        [MemberData(nameof(OoxmlTampered))]
        public void Extract_TamperedPackageWithCorrectPassword_ThrowsRecordedCorruptFile(string path) =>
            AssertExtractCorrectPasswordRejectedAsRecorded(Get(path));

        [Theory]
        [MemberData(nameof(XlsDefaultKey))]
        public void Extract_DefaultKeyXls_IdenticalWithNoPasswordAndAnArbitraryPassword(string path)
        {
            var entry = Get(path);

            var none = Extract(ExtractRequest(entry, null));
            var arbitrary = Extract(ExtractRequest(entry, ArbitraryPassword));

            Assert.Equal(Leaves(none), Leaves(arbitrary));
            Assert.Empty(none.FieldErrors);
            Assert.Empty(arbitrary.FieldErrors);
            Assert.NotEmpty(none.ExtractedText);
        }

        [Theory]
        [MemberData(nameof(EmbeddingDocument))]
        public void Extract_EmbeddingDocument_ReturnsEncryptedSubfileOpaque(string path)
        {
            var entry = Get(path);
            var encryptedBytes = Entries().Where(e => e.Password is not null && e.Path != path).Select(e => File.ReadAllBytes(e.FullPath)).ToList();

            var none = Extract(ExtractRequest(entry, null));
            var withPassword = Extract(ExtractRequest(entry, ArbitraryPassword));

            Assert.Equal(Leaves(none), Leaves(withPassword));
            Assert.Empty(none.FieldErrors);
            var subfile = Assert.Single(none.EmbeddedFiles, f => encryptedBytes.Any(bytes => bytes.AsSpan().SequenceEqual(f.BinaryData)));
            Assert.Equal(subfile.BinaryData.Length, subfile.SizeInBytes);
        }

        // ---- DetectMimeType ----

        [Theory]
        [MemberData(nameof(OoxmlDecryptable))]
        public void Detect_EncryptedPackage_UnknownInvalidPasswordThenInnerFormat(string path) =>
            AssertDetectEncryptedPackageThreeWays(Get(path));

        [Theory]
        [MemberData(nameof(OoxmlTampered))]
        public void Detect_TamperedPackage_UnknownThenInvalidPassword_ThenCorruptWithCorrectPassword(string path)
        {
            var entry = Get(path);
            Assert.Equal(DetectedFormat.Unknown, Detect(entry, null).DetectedFormat);
            Assert.Throws<InvalidPasswordException>(() => Detect(entry, WrongPassword));
            var ex = Assert.Throws<CorruptFileException>(() => Detect(entry, entry.Password));
            Assert.Equal(entry.CorrectPasswordErrorCode, ex.Code);
            Assert.DoesNotContain(entry.Password!, ex.ToString(), StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(XlsDecryptable))]
        [MemberData(nameof(XlsDefaultKey))]
        public void Detect_EncryptedXls_UnchangedLegacyFormatWithEveryPassword(string path)
        {
            var entry = Get(path);
            AssertDetectLegacyUnchangedThreeWays(entry);
            if (entry.Password is null)
            {
                Assert.Equal(entry.ExpectedDetectedMimeType, Detect(entry, ArbitraryPassword).MimeType);
            }
        }

        [Theory]
        [MemberData(nameof(EmbeddingDocument))]
        public void Detect_EmbeddingDocument_SameWithAndWithoutPassword(string path)
        {
            var entry = Get(path);
            Assert.Equal(entry.ExpectedDetectedMimeType, Detect(entry, null).MimeType);
            Assert.Equal(entry.ExpectedDetectedMimeType, Detect(entry, ArbitraryPassword).MimeType);
        }
    }
}
