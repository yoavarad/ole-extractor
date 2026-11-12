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
// Ported from NPOI third_party/npoi/ooxml/POIFS/Crypt/Agile/AgileEncryptionVerifier.cs
// at submodule commit 72ff7c756fc5bb52ec86848c3724556171bc4b23 (ADR-012). Decrypt side only;
// the descriptor is read with System.Xml instead of NPOI.OpenXmlFormats, and certificate key
// encryptors are not read (only password decryption is supported).

using System.Xml.Linq;
using ExtractorOLE.Exceptions;
using NPOI.POIFS.Crypt;

namespace ExtractorOLE.Helpers.Encryption.Agile;

// The password key encryptor (p:encryptedKey) of the descriptor: everything needed to check a
// password and recover the intermediate key.
internal sealed class AgileEncryptionVerifier
{
    // MS-OFFCRYPTO 2.3.4.10 caps PasswordKeyEncryptor.spinCount at 10,000,000. A larger value is a
    // hostile file built to burn CPU in key derivation (ADR-012 decision 6).
    internal const int MaxSpinCount = 10_000_000;

    private AgileEncryptionVerifier(
        CipherAlgorithm cipherAlgorithm,
        HashAlgorithm hashAlgorithm,
        ChainingMode chainingMode,
        int keyBits,
        int blockSize,
        int spinCount,
        byte[] salt,
        byte[] encryptedVerifierHashInput,
        byte[] encryptedVerifierHashValue,
        byte[] encryptedKeyValue)
    {
        CipherAlgorithm = cipherAlgorithm;
        HashAlgorithm = hashAlgorithm;
        ChainingMode = chainingMode;
        KeyBits = keyBits;
        BlockSize = blockSize;
        SpinCount = spinCount;
        Salt = salt;
        EncryptedVerifierHashInput = encryptedVerifierHashInput;
        EncryptedVerifierHashValue = encryptedVerifierHashValue;
        EncryptedKeyValue = encryptedKeyValue;
    }

    public CipherAlgorithm CipherAlgorithm { get; }

    public HashAlgorithm HashAlgorithm { get; }

    public ChainingMode ChainingMode { get; }

    public int KeyBits { get; }

    public int BlockSize { get; }

    public int SpinCount { get; }

    public byte[] Salt { get; }

    public byte[] EncryptedVerifierHashInput { get; }

    public byte[] EncryptedVerifierHashValue { get; }

    public byte[] EncryptedKeyValue { get; }

    internal static AgileEncryptionVerifier Parse(XElement encryption)
    {
        var keyEncryptors = AgileDescriptorReader.RequiredChild(encryption, XName.Get("keyEncryptors", AgileDescriptorReader.EncryptionNamespace));
        var encryptors = keyEncryptors.Elements(XName.Get("keyEncryptor", AgileDescriptorReader.EncryptionNamespace)).ToList();
        if (encryptors.Count == 0)
        {
            throw AgileDescriptorReader.Corrupt("the descriptor has no key encryptor");
        }

        // Only a password key encryptor is supported. A file that can only be opened with a
        // certificate is encryption this library does not decrypt: PASSWORD_PROTECTED (ADR-012
        // decision 3), whether or not a password was supplied.
        var keyData = encryptors
            .Elements(XName.Get("encryptedKey", AgileDescriptorReader.PasswordNamespace))
            .FirstOrDefault()
            ?? throw new PasswordProtectedException();

        // Checked first, before anything else is read or derived.
        var spinCount = AgileDescriptorReader.RequiredLong(keyData, "spinCount");
        if (spinCount is < 0 or > MaxSpinCount)
        {
            throw AgileDescriptorReader.Corrupt($"spinCount is outside 0..{MaxSpinCount}");
        }

        var keyBits = AgileDescriptorReader.RequiredInt(keyData, "keyBits");
        var blockSize = AgileDescriptorReader.RequiredInt(keyData, "blockSize");
        var cipher = AgileDescriptorReader.ReadCipherAlgorithm(keyData, keyBits);
        if (blockSize != cipher.blockSize)
        {
            throw AgileDescriptorReader.Corrupt("encryptedKey blockSize does not match its cipher");
        }

        var chaining = AgileDescriptorReader.ReadChainingMode(keyData);
        var hash = AgileDescriptorReader.ReadHashAlgorithm(keyData);

        var saltSize = AgileDescriptorReader.RequiredInt(keyData, "saltSize");
        var salt = AgileDescriptorReader.RequiredBase64(keyData, "saltValue");
        if (salt.Length != saltSize || salt.Length != blockSize)
        {
            throw AgileDescriptorReader.Corrupt("encryptedKey salt length is invalid");
        }

        // Damaged encrypted values must fail as CORRUPT_FILE here, not later as a verifier mismatch
        // that would look like a wrong password.
        var verifierHashInput = ReadEncryptedValue(keyData, "encryptedVerifierHashInput", blockSize, 1);
        var verifierHashValue = ReadEncryptedValue(keyData, "encryptedVerifierHashValue", blockSize, hash.hashSize);
        var keyValue = ReadEncryptedValue(keyData, "encryptedKeyValue", blockSize, 1);

        return new AgileEncryptionVerifier(
            cipher,
            hash,
            chaining,
            keyBits,
            blockSize,
            (int)spinCount,
            salt,
            verifierHashInput,
            verifierHashValue,
            keyValue);
    }

    private static byte[] ReadEncryptedValue(XElement keyData, string attribute, int blockSize, int minimumLength)
    {
        var value = AgileDescriptorReader.RequiredBase64(keyData, attribute);
        if (value.Length < minimumLength || value.Length % blockSize != 0)
        {
            throw AgileDescriptorReader.Corrupt($"descriptor attribute {attribute} has an invalid length");
        }

        return value;
    }
}
