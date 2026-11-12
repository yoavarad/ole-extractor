using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy
{
    public class LegacyFixtureSmokeTests
    {
        private static readonly Dictionary<string, string> Sha256ByPath = new(StringComparer.Ordinal)
        {
            ["samples/encrypted/password_tika_binaryrc4.doc"] = "9231E724BB17A2E5F74815728D90B06E15684CF5FB2443A6FA24DEEBD33BE952",
            ["samples/encrypted/password_password_cryptoapi.doc"] = "F2D0DC59AD7EC2356695AD5DC550057052A4017D5F1EB46E887297F5089896FB",
            ["samples/encrypted/rc4cryptoapi_password.doc"] = "9C5217BEA80FB1A7811AD7709DD9BB35F6C639BAC7154580DD4C99D54E1FC0E0",
            ["samples/encrypted/PasswordProtected.doc"] = "863D2216A6F5BB4CEC0EA4131A05AE0822DBCE347E2940155EF638C6D64935FE",
            ["samples/curated/doc/rc4cryptoapi_password_plain.doc"] = "65EB884BE862337C0E51E78BF57BE6CB6601040A16055A87D1E4DD3DE4C04303",
            ["samples/encrypted/Password_Protected-hello.ppt"] = "8D0DD9F53317A720687CF5234AF0C021FAB665194B09B9BB56A3F71B495BF45A",
            ["samples/encrypted/Password_Protected-56-hello.ppt"] = "B2F7F4881AF9B3AC11A941B4496478FF4B219288774A268741785F88A29B6CB1",
            ["samples/encrypted/Password_Protected-np-hello.ppt"] = "C722ABEB35A155040E49DD0963A360928050ACA2419D46E3210E7FBD18F120D3",
            ["samples/encrypted/cryptoapi-proc2356.ppt"] = "C97E74D7137915251DF14CD5937B97DE490F12FA83A46D5AE570900DEC590F4D",
            ["samples/encrypted/ppt_with_png_encrypted.ppt"] = "35DC3CBF31660872A3C1BB26F4200DFB94736A7BC95E72AC626B2D033C250EB3",
            ["samples/curated/ppt/ppt_with_png.ppt"] = "92865C21E43AA3A69A61371FED9EA50873A66E4A5EE5A24EDE2AF58DB66AEEB3",
        };

        private static readonly Dictionary<string, string?> PasswordByFile = new(StringComparer.Ordinal)
        {
            [LegacyFixtures.TikaBinaryRc4Doc] = "tika",
            [LegacyFixtures.PasswordCryptoApiDoc] = "password",
            [LegacyFixtures.Rc4CryptoApiDoc] = "Password1234_",
            [LegacyFixtures.PasswordProtectedDoc] = null,
            [LegacyFixtures.HelloPpt] = "hello",
            [LegacyFixtures.Hello56Ppt] = "hello",
            [LegacyFixtures.HelloNoPropsPpt] = "hello",
            [LegacyFixtures.CryptoApiProc2356Ppt] = "crypto",
            [LegacyFixtures.PngEncryptedPpt] = "password",
        };

        private static JsonElement ManifestEntry(string path)
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(EncryptedSamples.FindRepoRootFile("samples/manifest.json")));
            return doc.RootElement.GetProperty("samples").EnumerateArray()
                .Single(e => e.GetProperty("path").GetString() == path).Clone();
        }

        public static IEnumerable<object[]> Paths() => Sha256ByPath.Keys.Select(p => new object[] { p });

        [Theory]
        [MemberData(nameof(Paths))]
        public void Fixture_ExistsWithListedSha256_AndHasManifestEntry(string path)
        {
            var bytes = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(path));

            Assert.Equal(Sha256ByPath[path], Convert.ToHexString(SHA256.HashData(bytes)));
            Assert.Equal(path, ManifestEntry(path).GetProperty("path").GetString());
        }

        [Fact]
        public void Passwords_MatchTheListedValues_AndTheNegativeOnlySampleHasNone()
        {
            Assert.Equal(LegacyFixtures.EncryptedFileNames.Count, PasswordByFile.Count);
            foreach (var (file, expected) in PasswordByFile)
            {
                Assert.Equal(expected, LegacyFixtures.Password(file));
                Assert.NotEmpty(LegacyFixtures.Bytes(file));
            }
        }

        [Fact]
        public void PasswordProtectedDoc_IsNegativeOnly_WithoutDecryptionExpectations()
        {
            var entry = ManifestEntry("samples/encrypted/PasswordProtected.doc");

            Assert.True(entry.GetProperty("negativeOnly").GetBoolean());
            foreach (var field in new[] { "password", "unencryptedTwin", "bodyText", "bodyTextLines", "metadata", "expectedFormatMetadata", "expectedSubfileCount", "expectedSubfiles" })
            {
                Assert.False(entry.TryGetProperty(field, out _), field);
            }

            Assert.Equal(0, entry.GetProperty("expectedFieldErrors").GetArrayLength());
        }

        [Fact]
        public void OnlyTheRc4CryptoApiDoc_HasAnUnencryptedTwin()
        {
            var legacy = EncryptedSamples.Enumerate()
                .Where(s => s.Path.EndsWith(".doc", StringComparison.Ordinal) || s.Path.EndsWith(".ppt", StringComparison.Ordinal));
            foreach (var sample in legacy)
            {
                var expected = sample.Path == "samples/encrypted/rc4cryptoapi_password.doc" ? LegacyFixtures.PlainTwinDocPath : null;
                Assert.Equal(expected, sample.UnencryptedTwin);
            }
        }

        [Fact]
        public void CfbMutator_AddReplaceRemove_RoundTripsThroughNpoi()
        {
            var cfb = LegacyFixtures.Bytes(LegacyFixtures.TikaBinaryRc4Doc);

            var added = CfbMutator.AddStream(cfb, "ProbeStream", new byte[] { 1, 2, 3 });
            var replaced = CfbMutator.ReplaceStream(added, "ProbeStream", b => b.Concat(new byte[] { 4 }).ToArray());
            byte[]? seen = null;
            CfbMutator.ReplaceStream(replaced, "ProbeStream", b => { seen = b; return b; });
            var removed = CfbMutator.RemoveEntry(replaced, "ProbeStream");

            Assert.Equal(new byte[] { 1, 2, 3, 4 }, seen);
            Assert.Throws<FileNotFoundException>(() => CfbMutator.ReplaceStream(removed, "ProbeStream", b => b));
        }
    }
}
