using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.FileTypeStrategy;
using OpenMcdf;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;

namespace ExtractorOLE.Helpers
{
    // Parse-time error handling for Extract ([ydk:error:extraction/truncated-container],
    // [ydk:error:extraction/password-protected], [ydk:error:extraction/corrupt-file]).
    // Pure byte-level checks on the container header/tail; the container kind is sniffed
    // from the signature, not from the claimed mime type, because an encrypted OOXML file
    // is a CFB wrapper even when the caller says "docx".
    public static class ParseTimeErrorGuard
    {
        private const int ZipEocdLength = 22;
        private const int CfbHeaderLength = 512;
        private const uint ZipEocdSignature = 0x06054B50;
        private static readonly byte[] ZipLocalHeaderSignature = { 0x50, 0x4B, 0x03, 0x04 };
        private static readonly byte[] CfbSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        /// <summary>
        /// Rejects zero-length and truncated containers, then encrypted (password-protected)
        /// containers, before any format library parses the bytes.
        /// </summary>
        public static void Preflight(byte[] fileBytes, bool isOoxmlFormat)
        {
            if (fileBytes.Length == 0)
            {
                throw new TruncatedContainerException(isOoxmlFormat ? ZipEocdLength : CfbHeaderLength, 0);
            }

            if (fileBytes.AsSpan().StartsWith(ZipLocalHeaderSignature))
            {
                CheckZipNotTruncated(fileBytes);
            }
            else if (fileBytes.AsSpan().StartsWith(CfbSignature))
            {
                CheckCfbNotTruncated(fileBytes);
                CheckCfbNotEncrypted(fileBytes);
            }
        }

        /// <summary>
        /// Runs the format's open strategy. A strategy that yields nothing has failed to parse
        /// the container (open strategies swallow library exceptions and return null), which is
        /// corrupt-file; a library-level encrypted-document exception is password-protected.
        /// </summary>
        public static DocumentExtractionResult OpenOrThrow(IOpenStrategy? strategy, byte[] fileBytes, string detectedMimeType)
        {
            if (strategy == null) return new DocumentExtractionResult();

            DocumentExtractionResult? result;
            try
            {
                result = strategy.Open(fileBytes);
            }
            catch (NPOI.EncryptedDocumentException)
            {
                throw new PasswordProtectedException();
            }

            return result ?? throw new CorruptFileException(detectedMimeType, "the container structure could not be opened by the format library");
        }

        // An EOCD record that declares a central directory ending past where the EOCD itself
        // sits means bytes are missing. An absent EOCD is a destroyed structure, not a
        // shortfall we can measure, so it is left to the format library (corrupt-file).
        private static void CheckZipNotTruncated(byte[] bytes)
        {
            var searchFloor = Math.Max(0, bytes.Length - ZipEocdLength - ushort.MaxValue);
            for (var pos = bytes.Length - ZipEocdLength; pos >= searchFloor; pos--)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pos, 4)) != ZipEocdSignature) continue;

                var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(pos + 20, 2));
                if (pos + ZipEocdLength + commentLength > bytes.Length) continue;

                var centralDirSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pos + 12, 4));
                var centralDirOffset = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(pos + 16, 4));
                if (centralDirOffset == uint.MaxValue) return; // zip64: real offset lives elsewhere

                var declaredEnd = (long)centralDirOffset + centralDirSize;
                if (declaredEnd > pos)
                {
                    throw new TruncatedContainerException(declaredEnd + ZipEocdLength + commentLength, bytes.Length);
                }
                return;
            }
        }

        // MS-CFB header: the directory's first sector and every FAT sector listed in the header
        // DIFAT must lie wholly inside the file. Sector n occupies [(n+1)*size, (n+2)*size).
        private static void CheckCfbNotTruncated(byte[] bytes)
        {
            if (bytes.Length < CfbHeaderLength)
            {
                throw new TruncatedContainerException(CfbHeaderLength, bytes.Length);
            }

            var sectorShift = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(0x1E, 2));
            if (sectorShift != 9 && sectorShift != 12) return; // not a valid CFB header: corrupt-file

            long sectorSize = 1L << sectorShift;
            if (bytes.Length < sectorSize)
            {
                throw new TruncatedContainerException(sectorSize, bytes.Length);
            }

            long expected = 0;
            void Require(uint sectorId)
            {
                if (sectorId >= 0xFFFFFFFA) return; // FREESECT/ENDOFCHAIN/FATSECT/DIFSECT markers
                expected = Math.Max(expected, ((long)sectorId + 2) * sectorSize);
            }

            Require(BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x30, 4)));

            var fatSectorCount = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x2C, 4));
            var listed = (int)Math.Min(fatSectorCount, 109);
            var fatSectorIds = new uint[listed];
            for (var i = 0; i < listed; i++)
            {
                fatSectorIds[i] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x4C + i * 4, 4));
                Require(fatSectorIds[i]);
            }

            // Highest sector the FAT marks as allocated: data sectors must at least start inside
            // the file (a short final sector is tolerated). Only FAT sectors present in the file
            // are read; a missing one was already accounted for above.
            var entriesPerFatSector = (int)(sectorSize / 4);
            long highestAllocated = -1;
            for (var i = 0; i < listed; i++)
            {
                if (fatSectorIds[i] >= 0xFFFFFFFA || ((long)fatSectorIds[i] + 2) * sectorSize > bytes.Length) continue;

                var fatOffset = ((long)fatSectorIds[i] + 1) * sectorSize;
                for (var j = 0; j < entriesPerFatSector; j++)
                {
                    if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan((int)fatOffset + j * 4, 4)) != 0xFFFFFFFF)
                    {
                        highestAllocated = Math.Max(highestAllocated, (long)i * entriesPerFatSector + j);
                    }
                }
            }
            if (highestAllocated >= 0)
            {
                expected = Math.Max(expected, (highestAllocated + 1) * sectorSize + 1);
            }

            if (expected > bytes.Length)
            {
                throw new TruncatedContainerException(expected, bytes.Length);
            }
        }

        // MS-OFFCRYPTO: an encrypted OOXML package is a CFB whose root holds EncryptionInfo and
        // EncryptedPackage streams. If OpenMcdf cannot read the CFB, defer to the format library.
        private static void CheckCfbNotEncrypted(byte[] bytes)
        {
            bool encrypted;
            try
            {
                using var stream = new MemoryStream(bytes);
                using var root = RootStorage.Open(stream);
                encrypted = root.EnumerateEntries()
                    .Any(e => e.Name == "EncryptionInfo" || e.Name == "EncryptedPackage");
            }
            catch (Exception)
            {
                return;
            }

            if (encrypted) throw new PasswordProtectedException();
        }
    }
}
