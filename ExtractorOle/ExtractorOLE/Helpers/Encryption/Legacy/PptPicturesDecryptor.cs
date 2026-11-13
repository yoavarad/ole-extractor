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
// Ported from Apache POI poi-scratchpad org/apache/poi/hslf/usermodel/HSLFSlideShowEncrypted.java
// decryptPicture, POI trunk, retrieved 2026-11-10. Modified: works on a copy of the whole stream,
// walks every record itself, and throws CorruptFileException where a record or field runs past the
// stream or record end. The RC4 CryptoAPI cipher comes from NPOI main's CryptoAPIDecryptor.

using System.Buffers.Binary;
using System.Security.Cryptography;
using ExtractorOLE.Exceptions;
using NPOI.POIFS.Crypt;
using NPOI.POIFS.Crypt.CryptoAPI;

namespace ExtractorOLE.Helpers.Encryption.Legacy;

// [ydk:req:extraction/legacy-decryption] [ydk:req:extraction/password-decryption]
// Decrypts the Pictures stream of a RC4 CryptoAPI .ppt (MS-PPT 2.3.7): each field of each
// OfficeArtBStoreContainerFileBlock is a separate RC4 run with a fresh block-0 key, so the stream is
// walked record by record. The stream never changes size.
internal static class PptPicturesDecryptor
{
    private const int RecordHeaderSize = 8;
    private const int UidSize = 16;
    private const int MetafileHeaderSize = 34;
    private const int TagSize = 1;
    private const int OffsetFbseCbName = 33;
    private const int CancellationStride = 64;
    private const ushort RecTypeFbse = 0xF007;
    private const ushort RecTypeBlipFirst = 0xF018;
    private const ushort RecTypeBlipLast = 0xF117;
    private const ushort RecTypeEmf = 0xF01A;
    private const ushort RecTypePict = 0xF01C;

    // OfficeArtFBSE fields after its header: btWin32, btMacOS, rgbUid, tag, size, cRef, foDelay,
    // unused1, cbName, unused2, unused3.
    private static readonly int[] FbseFieldSizes = [1, 1, 16, 2, 4, 4, 4, 1, 1, 1, 1];

    // Blip recInstance values whose record carries a second 16-byte rgbUid (MS-ODRAW 2.2.23-2.2.31).
    private static readonly int[] TwoUidInstances = [0x217, 0x3D5, 0x46B, 0x543, 0x6E1, 0x6E3, 0x6E5, 0x7A9];

    // Returns a decrypted copy; pictures is not modified. Throws CorruptFileException when a record or
    // field runs past the stream or record end. A record whose decrypted type is neither FBSE nor a blip
    // stops decryption without error: its 8 header bytes stay decrypted and everything after it stays as
    // stored, exactly as POI does.
    internal static byte[] Decrypt(byte[] pictures, CryptoAPIDecryptor decryptor, string detectedMimeType)
    {
        ArgumentNullException.ThrowIfNull(pictures);
        ArgumentNullException.ThrowIfNull(decryptor);

        var output = (byte[])pictures.Clone();
        try
        {
            DecryptRecords(output, decryptor, detectedMimeType);
        }
        catch
        {
            CryptographicOperations.ZeroMemory(output);
            throw;
        }

        return output;
    }

    private static void DecryptRecords(byte[] data, CryptoAPIDecryptor decryptor, string detectedMimeType)
    {
        Cipher? cipher = null;
        var pos = 0;
        var records = 0;
        while (pos + (long)RecordHeaderSize <= data.Length)
        {
            if (records++ % CancellationStride == 0)
            {
                ExtractionCancellation.ThrowIfCancellationRequested();
            }

            var recordStart = pos;
            DecryptField(RecordHeaderSize, data.Length);
            var (recInstance, recType, recLen) = ReadHeader(data, recordStart);
            if (recType != RecTypeFbse && recType is < RecTypeBlipFirst or > RecTypeBlipLast)
            {
                return;
            }

            var end = recordStart + (long)RecordHeaderSize + recLen;
            if (end > data.Length)
            {
                throw new CorruptFileException(detectedMimeType, $"encrypted presentation: Pictures record at offset {recordStart} runs past the end of the stream");
            }

            if (recType == RecTypeFbse)
            {
                var fbseBody = pos;
                foreach (var size in FbseFieldSizes)
                {
                    DecryptField(size, end);
                }

                DecryptField(data[fbseBody + OffsetFbseCbName], end);
                var blipStart = pos;
                DecryptField(RecordHeaderSize, end);
                (recInstance, recType, _) = ReadHeader(data, blipStart);
            }

            DecryptField(UidSize, end);
            if (Array.IndexOf(TwoUidInstances, recInstance) >= 0)
            {
                DecryptField(UidSize, end);
            }

            DecryptField(recType is >= RecTypeEmf and <= RecTypePict ? MetafileHeaderSize : TagSize, end);
            DecryptField((int)(end - pos), end);
        }

        void DecryptField(int count, long end)
        {
            if (pos + (long)count > end)
            {
                throw new CorruptFileException(detectedMimeType, $"encrypted presentation: Pictures field at offset {pos} runs past the end of its record");
            }

            cipher = decryptor.InitCipherForBlock(cipher, 0);
            cipher.Update(data, pos, count, data, pos);
            pos += count;
        }
    }

    private static (int RecInstance, ushort RecType, uint RecLen) ReadHeader(byte[] data, int offset) =>
        (BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)) >> 4,
         BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + 2)),
         BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4)));
}
