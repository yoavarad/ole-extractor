/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */
// Ported from NPOI third_party/npoi/ooxml/POIFS/Crypt/Agile/AgileDecryptor.cs
// at submodule commit 72ff7c756fc5bb52ec86848c3724556171bc4b23 (ADR-012). Decrypt side only:
// password verification and segment-wise decryption of EncryptedPackage. Password key encryptor
// only: certificate (KeyPair / X509) decryption and the reflection-based EncryptionInfoBuilder are
// not ported. Added: the data-integrity HMAC check NPOI omits. Crypto primitives come from NPOI
// main's CryptoFunctions/CipherAlgorithm/HashAlgorithm.

using System.Buffers.Binary;
using System.Security.Cryptography;
using ExtractorOLE.Exceptions;
using NPOI.POIFS.Crypt;
using HashAlgorithm = NPOI.POIFS.Crypt.HashAlgorithm;

namespace ExtractorOLE.Helpers.Encryption.Agile;

// Derives H_n from the password (MS-OFFCRYPTO 2.3.4.11). Injectable so tests can count calls.
internal delegate byte[] AgileKeyDerivation(string password, HashAlgorithm hashAlgorithm, byte[] salt, int spinCount);

internal sealed class AgileDecryptor : IDisposable
{
    // MS-OFFCRYPTO 2.3.4.15: EncryptedPackage is encrypted in 4096-byte segments.
    private const int SegmentLength = 4096;

    // blockKey constants, MS-OFFCRYPTO 2.3.4.13.
    private static readonly byte[] VerifierInputBlockKey = [0xfe, 0xa7, 0xd2, 0x76, 0x3b, 0x4b, 0x9e, 0x79];
    private static readonly byte[] HashedVerifierBlockKey = [0xd7, 0xaa, 0x0f, 0x6d, 0x30, 0x61, 0x34, 0x4e];
    private static readonly byte[] CryptoKeyBlockKey = [0x14, 0x6e, 0x0b, 0xe7, 0xab, 0xac, 0xd0, 0xd6];

    // blockKey constants, MS-OFFCRYPTO 2.3.4.14 (data integrity).
    private static readonly byte[] IntegrityKeyBlockKey = [0x5f, 0xb2, 0xad, 0x01, 0x0c, 0xb9, 0xe1, 0xf6];
    private static readonly byte[] IntegrityValueBlockKey = [0xa0, 0x67, 0x7f, 0x02, 0xb2, 0x2c, 0x84, 0x33];

    private readonly AgileKeyDerivation _keyDerivation;
    private byte[]? _secretKey;
    private bool _disposed;

    internal AgileDecryptor(AgileEncryptionHeader header, AgileEncryptionVerifier verifier, AgileKeyDerivation? keyDerivation = null)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(verifier);
        Header = header;
        Verifier = verifier;
        _keyDerivation = keyDerivation ?? CryptoFunctions.HashPassword;
    }

    public AgileEncryptionHeader Header { get; }

    public AgileEncryptionVerifier Verifier { get; }

    // Parses the XML descriptor that follows the 8-byte version header of an Agile (4.4)
    // EncryptionInfo stream. Throws CorruptFileException for a malformed descriptor or a spin
    // count over the MS-OFFCRYPTO maximum, before any key derivation.
    public static AgileDecryptor FromDescriptor(Stream descriptor, AgileKeyDerivation? keyDerivation = null)
    {
        var encryption = AgileDescriptorReader.LoadEncryptionElement(descriptor);
        var verifier = AgileEncryptionVerifier.Parse(encryption);
        var header = AgileEncryptionHeader.Parse(encryption);
        if (header.BlockSize > SegmentLength || SegmentLength % header.BlockSize != 0)
        {
            throw AgileDescriptorReader.Corrupt("keyData blockSize does not divide the segment length");
        }

        // encryptedKeyValue decrypts to the package key, sized by keyData.keyBits (MS-OFFCRYPTO 2.3.4.13).
        if (verifier.EncryptedKeyValue.Length < header.KeyBits / 8)
        {
            throw AgileDescriptorReader.Corrupt("encryptedKeyValue is shorter than the keyData key");
        }

        return new AgileDecryptor(header, verifier, keyDerivation);
    }

    // True when the password matches the verifier; the intermediate key is then kept for Decrypt.
    // A wrong password returns false and decrypts nothing.
    public bool VerifyPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        ObjectDisposedException.ThrowIf(_disposed, this);
        ClearSecretKey();
        _secretKey = null;

        var passwordHash = _keyDerivation(password, Verifier.HashAlgorithm, Verifier.Salt, Verifier.SpinCount);
        byte[]? verifierInput = null;
        byte[]? keySpec = null;
        try
        {
            verifierInput = HashInput(passwordHash, VerifierInputBlockKey, Verifier.EncryptedVerifierHashInput);
            var verifierHash = CryptoFunctions.GetMessageDigest(Verifier.HashAlgorithm).Digest(verifierInput);

            var verifierHashDecrypted = CryptoFunctions.GetBlock0(
                HashInput(passwordHash, HashedVerifierBlockKey, Verifier.EncryptedVerifierHashValue),
                Verifier.HashAlgorithm.hashSize);

            keySpec = CryptoFunctions.GetBlock0(
                HashInput(passwordHash, CryptoKeyBlockKey, Verifier.EncryptedKeyValue),
                Header.KeyBits / 8);

            if (!CryptographicOperations.FixedTimeEquals(verifierHashDecrypted, verifierHash))
            {
                return false;
            }

            _secretKey = keySpec;
            keySpec = null;
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordHash);
            if (verifierInput is not null)
            {
                CryptographicOperations.ZeroMemory(verifierInput);
            }

            if (keySpec is not null)
            {
                CryptographicOperations.ZeroMemory(keySpec);
            }
        }
    }

    // Decrypts the EncryptedPackage payload (the stream positioned just after its 8-byte StreamSize
    // field) into output, writing exactly decryptedLength bytes. Size guards against the declared
    // length belong to the caller and run before this is called. No integrity check here, and on
    // truncated ciphertext earlier segments are already written: use DecryptPackage for the
    // all-or-nothing path.
    public void Decrypt(Stream ciphertext, long decryptedLength, Stream output)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentOutOfRangeException.ThrowIfNegative(decryptedLength);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var secretKey = _secretKey
            ?? throw new InvalidOperationException("The password has not been verified.");

        var key = new SecretKeySpec(secretKey, Header.CipherAlgorithm.jceId);
        var cipher = CryptoFunctions.GetCipher(key, Header.CipherAlgorithm, Header.ChainingMode, Header.KeySalt, Cipher.DECRYPT_MODE);
        var segment = new byte[SegmentLength];
        var plain = new byte[SegmentLength];
        var blockKey = new byte[4];
        try
        {
            long remaining = decryptedLength;
            long consumed = 0;
            for (var index = 0; remaining > 0; index++)
            {
                var plainLength = (int)Math.Min(SegmentLength, remaining);
                var cipherLength = RoundUpToBlock(plainLength, Header.BlockSize);
                var read = ciphertext.ReadAtLeast(segment.AsSpan(0, cipherLength), cipherLength, throwOnEndOfStream: false);
                if (read < cipherLength)
                {
                    throw new TruncatedContainerException(consumed + cipherLength, consumed + read);
                }

                BinaryPrimitives.WriteInt32LittleEndian(blockKey, index);
                var iv = CryptoFunctions.GenerateIv(Header.HashAlgorithm, Header.KeySalt, blockKey, Header.BlockSize);
                cipher.Init(Cipher.DECRYPT_MODE, key, new IvParameterSpec(iv));
                cipher.DoFinal(segment, 0, cipherLength, plain);
                output.Write(plain, 0, plainLength);

                consumed += cipherLength;
                remaining -= plainLength;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    // Verifies the password, checks data integrity, then decrypts. encryptedPackage is the whole
    // seekable EncryptedPackage stream (StreamSize field included). Wrong password ->
    // InvalidPasswordException; tampered ciphertext -> CorruptFileException. Nothing is written to
    // output unless both checks pass. Size guards on the declared length belong to the caller.
    public void DecryptPackage(string password, Stream encryptedPackage, Stream output)
    {
        ArgumentNullException.ThrowIfNull(encryptedPackage);
        if (!encryptedPackage.CanSeek)
        {
            throw new ArgumentException("The EncryptedPackage stream must be seekable.", nameof(encryptedPackage));
        }

        if (!VerifyPassword(password))
        {
            throw new InvalidPasswordException();
        }

        var start = encryptedPackage.Position;
        VerifyIntegrity(encryptedPackage);
        encryptedPackage.Position = start;

        Span<byte> streamSize = stackalloc byte[8];
        var read = encryptedPackage.ReadAtLeast(streamSize, streamSize.Length, throwOnEndOfStream: false);
        if (read < streamSize.Length)
        {
            throw new TruncatedContainerException(streamSize.Length, read);
        }

        var decryptedLength = BinaryPrimitives.ReadInt64LittleEndian(streamSize);
        if (decryptedLength < 0)
        {
            throw CorruptPackage("EncryptedPackage declares a negative size");
        }

        Decrypt(encryptedPackage, decryptedLength, output);
    }

    // MS-OFFCRYPTO 2.3.4.14: HMAC (descriptor keyData hash, RFC 2104) over the entire
    // EncryptedPackage stream, StreamSize field included, read from its current position to the end.
    // A mismatch after a verified password means altered ciphertext: CORRUPT_FILE, never
    // INVALID_PASSWORD (ADR-012 decision 2). NPOI reads these values but never checks them.
    public void VerifyIntegrity(Stream encryptedPackage)
    {
        ArgumentNullException.ThrowIfNull(encryptedPackage);
        ObjectDisposedException.ThrowIf(_disposed, this);
        var secretKey = _secretKey
            ?? throw new InvalidOperationException("The password has not been verified.");

        var key = new SecretKeySpec(secretKey, Header.CipherAlgorithm.jceId);
        byte[]? hmacKey = null;
        byte[]? expected = null;
        var buffer = new byte[SegmentLength];
        var mac = CryptoFunctions.GetMac(Header.HashAlgorithm);
        try
        {
            hmacKey = DecryptIntegrityValue(key, IntegrityKeyBlockKey, Header.EncryptedHmacKey);
            expected = DecryptIntegrityValue(key, IntegrityValueBlockKey, Header.EncryptedHmacValue);
            mac.Init(new SecretKeySpec(hmacKey, Header.HashAlgorithm.jceHmacId));
            int read;
            while ((read = encryptedPackage.Read(buffer, 0, buffer.Length)) > 0)
            {
                mac.Update(buffer, 0, read);
            }

            var actual = mac.DoFinal();
            if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            {
                throw CorruptPackage("the EncryptedPackage data-integrity check failed");
            }
        }
        finally
        {
            mac.Dispose();
            if (hmacKey is not null)
            {
                CryptographicOperations.ZeroMemory(hmacKey);
            }

            if (expected is not null)
            {
                CryptographicOperations.ZeroMemory(expected);
            }
        }
    }

    public void Dispose()
    {
        ClearSecretKey();
        _secretKey = null;
        _disposed = true;
    }

    // MS-OFFCRYPTO 2.3.4.11/2.3.4.13: decrypt one password-key-encryptor value with the key derived
    // from H_n and blockKey, using the encryptor's salt as IV. Every parameter comes from the
    // PasswordKeyEncryptor, not keyData: 2.3.4.11 says H() is "determined by the
    // PasswordKeyEncryptor.hashAlgorithm element" and pads/truncates to "PasswordKeyEncryptor.keyBits",
    // and 2.3.4.13 defines saltValue/blockSize on that element and uses its hashAlgorithm "to obtain
    // the encryptedVerifierHashValue". Only the decrypted encryptedKeyValue is sized by keyData.keyBits.
    // (NPOI takes key/block size from keyData here; Office writes equal values in both.)
    private byte[] HashInput(byte[] passwordHash, byte[] blockKey, byte[] input)
    {
        var intermediateKey = CryptoFunctions.GenerateKey(passwordHash, Verifier.HashAlgorithm, blockKey, Verifier.KeyBits / 8);
        try
        {
            var key = new SecretKeySpec(intermediateKey, Verifier.CipherAlgorithm.jceId);
            var iv = CryptoFunctions.GenerateIv(Verifier.HashAlgorithm, Verifier.Salt, null, Verifier.BlockSize);
            var cipher = CryptoFunctions.GetCipher(key, Verifier.CipherAlgorithm, Verifier.ChainingMode, iv, Cipher.DECRYPT_MODE);
            var padded = CryptoFunctions.GetBlock0(input, RoundUpToBlock(input.Length, Verifier.BlockSize));
            return cipher.DoFinal(padded);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(intermediateKey);
        }
    }

    // MS-OFFCRYPTO 2.3.4.14: encryptedHmacKey/encryptedHmacValue are encrypted with the package key,
    // IV = H(keyData.saltValue + blockKey); the plaintext is the first hashSize bytes.
    private byte[] DecryptIntegrityValue(SecretKeySpec key, byte[] blockKey, byte[] encrypted)
    {
        if (encrypted.Length == 0 || encrypted.Length % Header.BlockSize != 0)
        {
            throw CorruptPackage("dataIntegrity value length is not a multiple of the block size");
        }

        var iv = CryptoFunctions.GenerateIv(Header.HashAlgorithm, Header.KeySalt, blockKey, Header.BlockSize);
        var cipher = CryptoFunctions.GetCipher(key, Header.CipherAlgorithm, Header.ChainingMode, iv, Cipher.DECRYPT_MODE);
        return CryptoFunctions.GetBlock0(cipher.DoFinal(encrypted), Header.HashAlgorithm.hashSize);
    }

    private static CorruptFileException CorruptPackage(string reason) => AgileDescriptorReader.Corrupt(reason);

    private static int RoundUpToBlock(int length, int blockSize) =>
        Math.Max(blockSize, (length + blockSize - 1) / blockSize * blockSize);

    private void ClearSecretKey()
    {
        if (_secretKey is not null)
        {
            CryptographicOperations.ZeroMemory(_secretKey);
        }
    }
}
