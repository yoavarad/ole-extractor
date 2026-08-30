using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
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
        public static int PackageOpenAttemptCount;

        public OoxmlMimeDetector() : this(new MimeDetectionLimits()) { }

        public OoxmlMimeDetector(MimeDetectionLimits limits, Action<string>? logRejection = null)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _logRejection = logRejection;
        }

        public MimeDetectionResult Detect(MimeDetectionRequest request)
        {
            if (request?.FileBytes == null || request.FileBytes.Length == 0)
            {
                return UnknownResult();
            }

            if (request.FileBytes.Length > _limits.MaxFileSizeBytes)
            {
                _logRejection?.Invoke($"[OoxmlMimeDetector] file-too-large: fileName='{request.FileName}', actualBytes={request.FileBytes.Length}, limitBytes={_limits.MaxFileSizeBytes}");
                throw new FileTooLargeException(request.FileBytes.Length, _limits.MaxFileSizeBytes);
            }

            if (TryGetDeclaredZipTotalSize(request.FileBytes, out var declaredTotalBytes, out var entriesInspected)
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
                PackageOpenAttemptCount++;

                using var stream = new MemoryStream(request.FileBytes);
                using var package = Package.Open(stream, FileMode.Open, FileAccess.Read);

                if (package.PartExists(WordDocumentPartUri))
                {
                    return BuildResult(DetectedFormatEnum.Docx, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                }

                if (package.PartExists(ExcelWorkbookPartUri))
                {
                    return BuildResult(DetectedFormatEnum.Xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                }

                if (package.PartExists(PowerPointPresentationPartUri))
                {
                    return BuildResult(DetectedFormatEnum.Pptx, "application/vnd.openxmlformats-officedocument.presentationml.presentation");
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
        private static bool TryGetDeclaredZipTotalSize(byte[] bytes, out ulong declaredTotalBytes, out int entriesInspected)
        {
            declaredTotalBytes = 0;
            entriesInspected = 0;

            const int LocalHeaderFixedLength = 30; // through end of extra-field-length at offset 28-29
            ReadOnlySpan<byte> localHeaderSignature = stackalloc byte[] { 0x50, 0x4B, 0x03, 0x04 };

            long offset = 0;
            while (offset + LocalHeaderFixedLength <= bytes.Length)
            {
                if (!bytes.AsSpan((int)offset, 4).SequenceEqual(localHeaderSignature))
                {
                    break; // no more local file headers -- central directory or end of entries reached
                }

                entriesInspected++;

                uint compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan((int)offset + 18, 4));
                uint uncompressedSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan((int)offset + 22, 4));
                ushort nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan((int)offset + 26, 2));
                ushort extraLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan((int)offset + 28, 2));

                // 0xFFFFFFFF is the ZIP64 sentinel meaning "real size is in the extra field" --
                // treat as maximally oversized rather than trust a field that's deliberately
                // not the real value, mirroring CfbMimeDetector's invalid-sectorShift clamp.
                if (uncompressedSize == uint.MaxValue || compressedSize == uint.MaxValue)
                {
                    declaredTotalBytes = ulong.MaxValue;
                    return true;
                }

                declaredTotalBytes = checked(declaredTotalBytes + uncompressedSize);

                long nextOffset = offset + LocalHeaderFixedLength + nameLength + extraLength + compressedSize;
                if (nextOffset <= offset || nextOffset > bytes.Length)
                {
                    // Forged/overflowing length fields would walk past the buffer -- stop
                    // scanning here; the total accumulated so far still stands.
                    break;
                }

                offset = nextOffset;
            }

            return entriesInspected > 0;
        }

        private static MimeDetectionResult BuildResult(DetectedFormatEnum format, string mimeType) => new()
        {
            DetectedFormat = format,
            MimeType = mimeType,
            IsSupported = true
        };

        private static MimeDetectionResult UnknownResult() => new()
        {
            DetectedFormat = DetectedFormatEnum.Unknown,
            MimeType = "application/octet-stream",
            IsSupported = false
        };
    }
}
