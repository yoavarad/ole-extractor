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
            using var stream = new MemoryStream(fileBytes, writable: false);
            Preflight(stream, isOoxmlFormat);
        }

        // Stream form (#193): reads only the signature, the ZIP tail, or the CFB header and its
        // listed FAT sectors, at absolute offsets (0 = start of content); never buffers the whole
        // file. The stream is left open; its position afterwards is unspecified.
        internal static void Preflight(Stream stream, bool isOoxmlFormat)
        {
            var length = stream.Length;
            if (length == 0)
            {
                throw new TruncatedContainerException(isOoxmlFormat ? ZipEocdLength : CfbHeaderLength, 0);
            }

            Span<byte> signature = stackalloc byte[CfbSignature.Length];
            signature = signature.Slice(0, (int)Math.Min(signature.Length, length));
            ReadAt(stream, 0, signature);

            if (signature.StartsWith(ZipLocalHeaderSignature))
            {
                CheckZipNotTruncated(stream, length);
            }
            else if (signature.StartsWith(CfbSignature))
            {
                CheckCfbNotTruncated(stream, length);
                CheckCfbNotEncrypted(stream);
            }
        }

        /// <summary>
        /// Runs the format's open strategy. A strategy that yields nothing has failed to parse
        /// the container (open strategies swallow library exceptions and return null), which is
        /// corrupt-file; a library-level encrypted-document exception is password-protected.
        /// </summary>
        public static DocumentExtractionResult OpenOrThrow(IOpenStrategy? strategy, byte[] fileBytes, string detectedMimeType) =>
            OpenOrThrow(strategy, s => s.Open(fileBytes), detectedMimeType);

        // Byte[] input keeps calling Open(byte[]); stream/path input calls Open(Stream) (#192).
        internal static DocumentExtractionResult OpenOrThrow(IOpenStrategy? strategy, ExtractionInput input, string detectedMimeType) =>
            OpenOrThrow(strategy, s => input.OriginalBytes != null ? s.Open(input.OriginalBytes) : s.Open(input.Stream!), detectedMimeType);

        private static DocumentExtractionResult OpenOrThrow(IOpenStrategy? strategy, Func<IOpenStrategy, DocumentExtractionResult?> open, string detectedMimeType)
        {
            if (strategy == null) return new DocumentExtractionResult();

            DocumentExtractionResult? result;
            try
            {
                result = open(strategy);
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
        // Only the tail that can hold the EOCD (22 bytes + max comment length) is read.
        private static void CheckZipNotTruncated(Stream stream, long length)
        {
            if (length < ZipEocdLength) return;

            var tailLength = (int)Math.Min(length, ZipEocdLength + ushort.MaxValue);
            var tailStart = length - tailLength;
            var tail = new byte[tailLength];
            ReadAt(stream, tailStart, tail);

            // pos is an absolute offset; its index in tail is pos - tailStart.
            for (var pos = length - ZipEocdLength; pos >= tailStart; pos--)
            {
                var i = (int)(pos - tailStart);
                if (BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i, 4)) != ZipEocdSignature) continue;

                var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(i + 20, 2));
                if (pos + ZipEocdLength + commentLength > length) continue;

                var centralDirSize = BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i + 12, 4));
                var centralDirOffset = BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i + 16, 4));
                if (centralDirOffset == uint.MaxValue) return; // zip64: real offset lives elsewhere

                var declaredEnd = (long)centralDirOffset + centralDirSize;
                if (declaredEnd > pos)
                {
                    throw new TruncatedContainerException(declaredEnd + ZipEocdLength + commentLength, length);
                }
                return;
            }
        }

        // MS-CFB header: the directory's first sector and every FAT sector listed in the header
        // DIFAT must lie wholly inside the file. Sector n occupies [(n+1)*size, (n+2)*size).
        // Reads the 512-byte header, then each listed FAT sector that lies inside the file.
        private static void CheckCfbNotTruncated(Stream stream, long length)
        {
            if (length < CfbHeaderLength)
            {
                throw new TruncatedContainerException(CfbHeaderLength, length);
            }

            var header = new byte[CfbHeaderLength];
            ReadAt(stream, 0, header);

            var sectorShift = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(0x1E, 2));
            if (sectorShift != 9 && sectorShift != 12) return; // not a valid CFB header: corrupt-file

            long sectorSize = 1L << sectorShift;
            if (length < sectorSize)
            {
                throw new TruncatedContainerException(sectorSize, length);
            }

            long expected = 0;
            void Require(uint sectorId)
            {
                if (sectorId >= 0xFFFFFFFA) return; // FREESECT/ENDOFCHAIN/FATSECT/DIFSECT markers
                expected = Math.Max(expected, ((long)sectorId + 2) * sectorSize);
            }

            Require(BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x30, 4)));

            var fatSectorCount = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x2C, 4));
            var listed = (int)Math.Min(fatSectorCount, 109);
            var fatSectorIds = new uint[listed];
            for (var i = 0; i < listed; i++)
            {
                fatSectorIds[i] = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0x4C + i * 4, 4));
                Require(fatSectorIds[i]);
            }

            // Highest sector the FAT marks as allocated: data sectors must at least start inside
            // the file (a short final sector is tolerated). Only FAT sectors present in the file
            // are read; a missing one was already accounted for above.
            var entriesPerFatSector = (int)(sectorSize / 4);
            var fatSector = new byte[sectorSize];
            long highestAllocated = -1;
            for (var i = 0; i < listed; i++)
            {
                if (fatSectorIds[i] >= 0xFFFFFFFA || ((long)fatSectorIds[i] + 2) * sectorSize > length) continue;

                ReadAt(stream, ((long)fatSectorIds[i] + 1) * sectorSize, fatSector);
                for (var j = 0; j < entriesPerFatSector; j++)
                {
                    if (BinaryPrimitives.ReadUInt32LittleEndian(fatSector.AsSpan(j * 4, 4)) != 0xFFFFFFFF)
                    {
                        highestAllocated = Math.Max(highestAllocated, (long)i * entriesPerFatSector + j);
                    }
                }
            }
            if (highestAllocated >= 0)
            {
                expected = Math.Max(expected, (highestAllocated + 1) * sectorSize + 1);
            }

            if (expected > length)
            {
                throw new TruncatedContainerException(expected, length);
            }
        }

        // MS-OFFCRYPTO: an encrypted OOXML package is a CFB whose root holds EncryptionInfo and
        // EncryptedPackage streams. If OpenMcdf cannot read the CFB, defer to the format library.
        // The stream is the caller's (or ExtractionInput's) and is read again on open. OpenMcdf
        // flushes to a writable stream on dispose, so it gets a read-only view that it cannot
        // write to or close.
        private static void CheckCfbNotEncrypted(Stream stream)
        {
            bool encrypted;
            try
            {
                stream.Position = 0;
                using var root = RootStorage.Open(new ReadOnlyView(stream), StorageModeFlags.LeaveOpen);
                encrypted = root.EnumerateEntries()
                    .Any(e => e.Name == "EncryptionInfo" || e.Name == "EncryptedPackage");
            }
            catch (Exception)
            {
                return;
            }

            if (encrypted) throw new PasswordProtectedException();
        }

        // Read-only, non-owning view of a seekable stream.
        private sealed class ReadOnlyView : Stream
        {
            private readonly Stream _inner;
            public ReadOnlyView(Stream inner) => _inner = inner;
            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
            public override int Read(Span<byte> buffer) => _inner.Read(buffer);
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void Flush() { }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private static void ReadAt(Stream stream, long offset, Span<byte> buffer)
        {
            stream.Position = offset;
            stream.ReadExactly(buffer);
        }
    }
}
