using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Input;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Packaging;

namespace ExtractorOLE.Helpers.MimeDetection
{
    // Sibling of the legacy CFB check (see ICfbMimeDetector): distinguishes
    // Word/Excel/PowerPoint OOXML packages by declared part-name structure
    // ([Content_Types].xml via Package.PartExists), not a full document parse.
    // Detection is content-only: request.FileName is never read.
    public class OoxmlMimeDetector : IOoxmlMimeDetector, IMimeTypeDetector
    {
        private static readonly Uri WordDocumentPartUri = new("/word/document.xml", UriKind.Relative);
        private static readonly Uri ExcelWorkbookPartUri = new("/xl/workbook.xml", UriKind.Relative);
        private static readonly Uri PowerPointPresentationPartUri = new("/ppt/presentation.xml", UriKind.Relative);

        private readonly MimeDetectionLimits _limits;
        private readonly Action<string>? _logRejection;

        // Test-observability only: proves guards below short-circuit before
        // Package.Open's own ZIP central-directory walk runs.
        private static readonly AttemptCounter s_packageOpenAttempts = new();
        public static int PackageOpenAttemptCount
        {
            get => s_packageOpenAttempts.Value;
            set => s_packageOpenAttempts.Value = value;
        }

        public OoxmlMimeDetector() : this(new MimeDetectionLimits()) { }

        public OoxmlMimeDetector(MimeDetectionLimits limits, Action<string>? logRejection = null)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _logRejection = logRejection;
        }

        public MimeDetectionResult Detect(MimeDetectionRequest request)
        {
            if (request == null || (request.FileBytes == null && request.InputStream == null && request.FilePath == null))
            {
                return UnknownResult();
            }

            using var input = MimeDetectionInput.From(request);
            var stream = input.Stream;
            var length = input.Length;
            if (length == 0)
            {
                return UnknownResult();
            }

            if (length > _limits.MaxFileSizeBytes)
            {
                _logRejection?.Invoke($"[OoxmlMimeDetector] file-too-large: fileName='{request.FileName}', actualBytes={length}, limitBytes={_limits.MaxFileSizeBytes}");
                throw new FileTooLargeException(length, _limits.MaxFileSizeBytes);
            }

            if (TryGetDeclaredZipTotalSize(stream, out var declaredTotalBytes, out var entriesInspected, out _)
                && declaredTotalBytes > (ulong)_limits.MaxDeclaredNestedContentBytes)
            {
                var metric = $"ZIP local file header(s) ({entriesInspected} {(entriesInspected == 1 ? "entry" : "entries")}) declare {declaredTotalBytes} bytes total uncompressed (limit {_limits.MaxDeclaredNestedContentBytes})";
                _logRejection?.Invoke($"[OoxmlMimeDetector] oversized-nested-content: fileName='{request.FileName}', {metric}");
                throw new OversizedNestedContentException(metric);
            }

            try
            {
                // Test-observability only: proves guards above short-circuit before
                // Package.Open's own ZIP central-directory walk runs.
                s_packageOpenAttempts.Increment();

                stream.Position = 0;
                using var package = Package.Open(new NonClosingStream(stream), FileMode.Open, FileAccess.Read);

                if (package.PartExists(WordDocumentPartUri))
                {
                    return BuildResult(DetectedFormat.Docx, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                }

                if (package.PartExists(ExcelWorkbookPartUri))
                {
                    return BuildResult(DetectedFormat.Xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                }

                if (package.PartExists(PowerPointPresentationPartUri))
                {
                    return BuildResult(DetectedFormat.Pptx, "application/vnd.openxmlformats-officedocument.presentationml.presentation");
                }

                return UnknownResult();
            }
            catch (Exception)
            {
                // Not a ZIP/OPC package, or a corrupt/unsupported one -- Unknown, never throw.
                return UnknownResult();
            }
        }

        // Hand-parses raw ZIP local file headers (PKZIP APPNOTE section 4.3.7) to estimate
        // the total declared uncompressed bytes the file's own entries claim, without letting
        // Package.Open (System.IO.Packaging -> System.IO.Compression) walk the structure first.
        internal static bool TryGetDeclaredZipTotalSize(byte[] bytes, out ulong declaredTotalBytes, out int entriesInspected, out ulong largestEntryBytes)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            return TryGetDeclaredZipTotalSize(stream, out declaredTotalBytes, out entriesInspected, out largestEntryBytes);
        }

        // Stream form (#193): seeks to each local header (absolute offsets, 0 = start of content)
        // and reads only its 30-byte fixed part, so the file is never buffered whole.
        internal static bool TryGetDeclaredZipTotalSize(Stream stream, out ulong declaredTotalBytes, out int entriesInspected, out ulong largestEntryBytes)
        {
            declaredTotalBytes = 0;
            entriesInspected = 0;
            largestEntryBytes = 0;

            const int LocalHeaderFixedLength = 30; // through end of extra-field-length at offset 28-29
            ReadOnlySpan<byte> localHeaderSignature = stackalloc byte[] { 0x50, 0x4B, 0x03, 0x04 };
            Span<byte> header = stackalloc byte[LocalHeaderFixedLength];
            long length = stream.Length;

            long offset = 0;
            while (offset + LocalHeaderFixedLength <= length)
            {
                stream.Position = offset;
                stream.ReadExactly(header);
                if (!header.Slice(0, 4).SequenceEqual(localHeaderSignature))
                {
                    break; // no more local file headers -- central directory or end of entries reached
                }

                entriesInspected++;

                uint compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(18, 4));
                uint uncompressedSize = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(22, 4));
                ushort nameLength = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(26, 2));
                ushort extraLength = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(28, 2));

                // 0xFFFFFFFF is the ZIP64 sentinel meaning "real size is in the extra field" --
                // treat as maximally oversized rather than trust a field that's deliberately
                // not the real value, mirroring CfbMimeDetector's invalid-sectorShift clamp.
                if (uncompressedSize == uint.MaxValue || compressedSize == uint.MaxValue)
                {
                    declaredTotalBytes = ulong.MaxValue;
                    largestEntryBytes = ulong.MaxValue;
                    return true;
                }

                declaredTotalBytes = checked(declaredTotalBytes + uncompressedSize);
                largestEntryBytes = Math.Max(largestEntryBytes, uncompressedSize);

                long nextOffset = offset + LocalHeaderFixedLength + nameLength + extraLength + compressedSize;
                if (nextOffset <= offset || nextOffset > length)
                {
                    // Forged/overflowing length fields would walk past the buffer -- stop
                    // scanning here; the total accumulated so far still stands.
                    break;
                }

                offset = nextOffset;
            }

            return entriesInspected > 0;
        }

        private static MimeDetectionResult BuildResult(DetectedFormat format, string mimeType) => new()
        {
            DetectedFormat = format,
            MimeType = mimeType,
            IsSupported = true
        };

        private static MimeDetectionResult UnknownResult() => new()
        {
            DetectedFormat = DetectedFormat.Unknown,
            MimeType = "application/octet-stream",
            IsSupported = false
        };
    }
}
