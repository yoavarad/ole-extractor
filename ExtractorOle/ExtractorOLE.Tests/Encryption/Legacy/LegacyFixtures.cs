namespace ExtractorOLE.Tests.Encryption.Legacy
{
    // Encrypted legacy .doc/.ppt fixtures under samples/encrypted/ (docs/specs/dataset-curation.md).
    // Bytes and passwords come from the EncryptedSamples locator and the manifest, never hard-coded.
    internal static class LegacyFixtures
    {
        public const string TikaBinaryRc4Doc = "password_tika_binaryrc4.doc";
        public const string PasswordCryptoApiDoc = "password_password_cryptoapi.doc";
        public const string Rc4CryptoApiDoc = "rc4cryptoapi_password.doc";
        public const string PasswordProtectedDoc = "PasswordProtected.doc";
        public const string HelloPpt = "Password_Protected-hello.ppt";
        public const string Hello56Ppt = "Password_Protected-56-hello.ppt";
        public const string HelloNoPropsPpt = "Password_Protected-np-hello.ppt";
        public const string CryptoApiProc2356Ppt = "cryptoapi-proc2356.ppt";
        public const string PngEncryptedPpt = "ppt_with_png_encrypted.ppt";

        public const string PlainTwinDocPath = "samples/curated/doc/rc4cryptoapi_password_plain.doc";
        public const string PlainReferencePptPath = "samples/curated/ppt/ppt_with_png.ppt";

        public static IReadOnlyList<string> EncryptedFileNames { get; } = new[]
        {
            TikaBinaryRc4Doc, PasswordCryptoApiDoc, Rc4CryptoApiDoc, PasswordProtectedDoc,
            HelloPpt, Hello56Ppt, HelloNoPropsPpt, CryptoApiProc2356Ppt, PngEncryptedPpt,
        };

        public static byte[] Bytes(string fileName) => EncryptedSamples.ReadBytes(fileName);

        public static string? Password(string fileName) =>
            EncryptedSamples.Enumerate()
                .Single(s => string.Equals(Path.GetFileName(s.Path), fileName, StringComparison.OrdinalIgnoreCase))
                .Password;
    }
}
