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
// Ported from NPOI third_party/npoi/ooxml/POIFS/Crypt/Agile/AgileEncryptionHeader.cs
// at submodule commit 72ff7c756fc5bb52ec86848c3724556171bc4b23 (ADR-012). Decrypt side only;
// the descriptor is read with System.Xml instead of NPOI.OpenXmlFormats.

using System.Xml.Linq;
using NPOI.POIFS.Crypt;

namespace ExtractorOLE.Helpers.Encryption.Agile;

// The descriptor's keyData (package cipher, hash and key salt) and dataIntegrity elements.
internal sealed class AgileEncryptionHeader
{
    private AgileEncryptionHeader(
        CipherAlgorithm cipherAlgorithm,
        HashAlgorithm hashAlgorithm,
        ChainingMode chainingMode,
        int keyBits,
        int blockSize,
        byte[] keySalt,
        byte[] encryptedHmacKey,
        byte[] encryptedHmacValue)
    {
        CipherAlgorithm = cipherAlgorithm;
        HashAlgorithm = hashAlgorithm;
        ChainingMode = chainingMode;
        KeyBits = keyBits;
        BlockSize = blockSize;
        KeySalt = keySalt;
        EncryptedHmacKey = encryptedHmacKey;
        EncryptedHmacValue = encryptedHmacValue;
    }

    public CipherAlgorithm CipherAlgorithm { get; }

    public HashAlgorithm HashAlgorithm { get; }

    public ChainingMode ChainingMode { get; }

    public int KeyBits { get; }

    public int BlockSize { get; }

    public byte[] KeySalt { get; }

    // Read here, checked by the data-integrity step (ADR-012 decision 2), which NPOI omits.
    public byte[] EncryptedHmacKey { get; }

    public byte[] EncryptedHmacValue { get; }

    internal static AgileEncryptionHeader Parse(XElement encryption)
    {
        var keyData = AgileDescriptorReader.RequiredChild(encryption, XName.Get("keyData", AgileDescriptorReader.EncryptionNamespace));

        var keyBits = AgileDescriptorReader.RequiredInt(keyData, "keyBits");
        var blockSize = AgileDescriptorReader.RequiredInt(keyData, "blockSize");
        var cipher = AgileDescriptorReader.ReadCipherAlgorithm(keyData, keyBits);
        if (blockSize != cipher.blockSize)
        {
            throw AgileDescriptorReader.Corrupt("keyData blockSize does not match its cipher");
        }

        var chaining = AgileDescriptorReader.ReadChainingMode(keyData);
        var hash = AgileDescriptorReader.ReadHashAlgorithm(keyData);

        var saltSize = AgileDescriptorReader.RequiredInt(keyData, "saltSize");
        var keySalt = AgileDescriptorReader.RequiredBase64(keyData, "saltValue");
        if (keySalt.Length != saltSize || keySalt.Length != blockSize)
        {
            throw AgileDescriptorReader.Corrupt("keyData salt length is invalid");
        }

        var dataIntegrity = AgileDescriptorReader.RequiredChild(encryption, XName.Get("dataIntegrity", AgileDescriptorReader.EncryptionNamespace));
        var hmacKey = AgileDescriptorReader.RequiredBase64(dataIntegrity, "encryptedHmacKey");
        var hmacValue = AgileDescriptorReader.RequiredBase64(dataIntegrity, "encryptedHmacValue");

        return new AgileEncryptionHeader(cipher, hash, chaining, keyBits, blockSize, keySalt, hmacKey, hmacValue);
    }
}
