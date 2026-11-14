using ExtractorOLE.Helpers.Input;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Encryption;
using ExtractorOLE.Helpers.FileTypeStrategy;
using OpenMcdf;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;

namespace ExtractorOLE.Helpers.Guardrails
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
            if (CheckNotTruncated(stream, isOoxmlFormat))
            {
                CheckCfbNotEncrypted(stream);
            }
        }

        /// <summary>
        /// <see cref="Preflight(Stream, bool)"/> for a request that carries a password
        /// ([ydk:req:extraction/password-decryption]): an encrypted OOXML package (a CFB holding both
        /// EncryptionInfo and EncryptedPackage) is decrypted instead of rejected, and the decrypted
        /// package is returned in a new stream owned by the caller. Returns null for any other input,
        /// which has passed the same checks as Preflight. Without a password this is Preflight.
        /// A CFB whose streams cannot be read during decryption is corrupt-file; a read fault of the
        /// caller's stream is rethrown as-is.
        /// </summary>
        internal static Stream? PreflightOrDecrypt(Stream stream, bool isOoxmlFormat, string? password, long maxEntryBytes, string detectedMimeType)
        {
            if (!RequestPasswords.HasPassword(password))
            {
                Preflight(stream, isOoxmlFormat);
                return null;
            }

            if (!CheckNotTruncated(stream, isOoxmlFormat)) return null;

            // FaultTrackingStream is read-only and non-closing, so OpenMcdf cannot flush to or close
            // the caller's stream.
            var source = new FaultTrackingStream(stream);
            var decrypting = false;
            try
            {
                source.Position = 0;
                using var root = RootStorage.Open(source, StorageModeFlags.LeaveOpen);
                var names = root.EnumerateEntries().Select(e => e.Name).ToHashSet(StringComparer.Ordinal);
                var hasInfo = names.Contains(EncryptedPackageDecryptor.EncryptionInfoStreamName);
                var hasPackage = names.Contains(EncryptedPackageDecryptor.EncryptedPackageStreamName);
                if (hasInfo && hasPackage)
                {
                    decrypting = true;
                    return EncryptedPackageDecryptor.Decrypt(root, password, maxEntryBytes);
                }

                // Only one of the two streams: an encrypted package we cannot decrypt, as Preflight.
                if (hasInfo || hasPackage) throw new PasswordProtectedException();
                return null;
            }
            catch (Exception) when (source.Fault != null)
            {
                ExceptionDispatchInfo.Throw(source.Fault);
                throw; // unreachable: ExceptionDispatchInfo.Throw never returns; kept for flow analysis
            }
            catch (Exception ex) when (ParseFailure.IsParseFailure(ex, source))
            {
                // Unreadable before an encrypted package was found: defer to the format library, as
                // Preflight does. Unreadable while decrypting: the encrypted package is corrupt.
                if (!decrypting) return null;
                throw new CorruptFileException(detectedMimeType, $"the encrypted package could not be read ({ex.GetType().Name})", ex);
            }
        }

        // Zero-length and truncated containers. Returns true when the input is a CFB container.
        private static bool CheckNotTruncated(Stream stream, bool isOoxmlFormat)
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
                return false;
            }

            if (!signature.StartsWith(CfbSignature)) return false;

            CheckCfbNotTruncated(stream, length);
            return true;
        }

        /// <summary>
        /// Runs the format's open strategy. A format-library parse exception (or a strategy that
        /// yields nothing) is corrupt-file, with the library exception as InnerException; a
        /// library-level encrypted-document exception is password-protected. Anything else
        /// (out-of-memory, a disposed stream, a read fault of the caller's stream) is rethrown
        /// as-is (#287). A supported format with no registered strategy is a registry
        /// misconfiguration and throws <see cref="InvalidOperationException"/>.
        /// </summary>
        public static DocumentExtractionResult OpenOrThrow(IOpenStrategy? strategy, byte[] fileBytes, string detectedMimeType) =>
            OpenOrThrow(strategy, s => s.Open(fileBytes), null, detectedMimeType, hasPassword: false);

        // Byte[] input calls Open(byte[], password); stream/path input calls Open(Stream, password) (#192).
        // Stream input is read through a FaultTrackingStream so a read fault of the caller's
        // stream is told apart from a parse failure (#287). The password is only passed through.
        internal static DocumentExtractionResult OpenOrThrow(IOpenStrategy? strategy, ExtractionInput input, string detectedMimeType, string? password = null)
        {
            var hasPassword = RequestPasswords.HasPassword(password);
            if (input.OriginalBytes != null) return OpenOrThrow(strategy, s => s.Open(input.OriginalBytes, password), null, detectedMimeType, hasPassword);

            var source = new FaultTrackingStream(input.Stream!);
            return OpenOrThrow(strategy, s => s.Open(source, password), source, detectedMimeType, hasPassword);
        }

        // Only a top-level EncryptedDocumentException is a password outcome: PASSWORD_PROTECTED
        // without a password, INVALID_PASSWORD with one. A RecordFormatException wrapping one means
        // the scheme is not supported by this NPOI build and stays CORRUPT_FILE.
        private static DocumentExtractionResult OpenOrThrow(IOpenStrategy? strategy, Func<IOpenStrategy, DocumentExtractionResult?> open, FaultTrackingStream? source, string detectedMimeType, bool hasPassword)
        {
            if (strategy == null)
            {
                throw new InvalidOperationException($"No open strategy is registered for supported format '{detectedMimeType}'.");
            }

            DocumentExtractionResult? result;
            try
            {
                result = open(strategy);
            }
            catch (Exception) when (source?.Fault != null)
            {
                // The caller's stream failed. Rethrow its own exception: a library may have
                // replaced it (System.IO.Packaging throws FileFormatException with no inner).
                ExceptionDispatchInfo.Throw(source.Fault);
                throw; // unreachable: ExceptionDispatchInfo.Throw never returns; kept for flow analysis
            }
            catch (Exception ex) when (IsLibraryEncrypted(ex) && hasPassword)
            {
                throw new InvalidPasswordException();
            }
            catch (Exception ex) when (IsLibraryEncrypted(ex))
            {
                throw new PasswordProtectedException();
            }
            catch (Exception ex) when (ParseFailure.IsParseFailure(ex, source))
            {
                throw new CorruptFileException(detectedMimeType, $"the format library could not parse the container ({ex.GetType().Name})", ex);
            }

            return result ?? throw new CorruptFileException(detectedMimeType, "the container structure could not be opened by the format library");
        }

        // NPOI's or b2xtranslator's top-level encrypted-document exception (b2xtranslator is a
        // defence in depth behind the legacy-encryption guard).
        internal static bool IsLibraryEncrypted(Exception ex) =>
            ex is NPOI.EncryptedDocumentException || ParseFailure.IsB2xEncryptedDocument(ex);

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
            catch (Exception ex) when (ParseFailure.IsParseFailure(ex))
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
