using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using OpenMcdf;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractorOLE.Helpers.MimeDetection
{
    // Distinguishes doc/xls/ppt by opening FileBytes as a compound file (OpenMcdf)
    // and inspecting root storage/stream names -- no full document parse.
    // Detection is content-only: request.FileName is never read to decide the
    // result -- it is only used inside the diagnostic rejection log message.
    public class CfbMimeDetector : ICfbMimeDetector
    {
        private const string WordDocumentEntryName = "WordDocument";
        private const string ExcelWorkbookEntryName = "Workbook";
        private const string ExcelBookEntryName = "Book";
        private const string PowerPointEntryName = "PowerPoint Document";

        private readonly MimeDetectionLimits _limits;
        private readonly Action<string>? _logRejection;

        // Test-observability only: proves guards below short-circuit before
        // OpenMcdf's own FAT/directory walk runs.
        public static int OpenMcdfParseAttemptCount;

        public CfbMimeDetector() : this(new MimeDetectionLimits()) { }

        public CfbMimeDetector(MimeDetectionLimits limits, Action<string>? logRejection = null)
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
                _logRejection?.Invoke($"[CfbMimeDetector] file-too-large: fileName='{request.FileName}', actualBytes={request.FileBytes.Length}, limitBytes={_limits.MaxFileSizeBytes}");
                throw new FileTooLargeException(request.FileBytes.Length, _limits.MaxFileSizeBytes);
            }

            if (TryGetDeclaredCfbTotalSize(request.FileBytes, out var declaredTotalBytes, out var fatSectorCount, out var sectorSize)
                && declaredTotalBytes > (ulong)_limits.MaxDeclaredNestedContentBytes)
            {
                var metric = $"CFB FAT sector count {fatSectorCount} at sector size {sectorSize} bytes implies {declaredTotalBytes} bytes total (limit {_limits.MaxDeclaredNestedContentBytes})";
                _logRejection?.Invoke($"[CfbMimeDetector] oversized-nested-content: fileName='{request.FileName}', {metric}");
                throw new OversizedNestedContentException(metric);
            }

            try
            {
                // Test-observability only: proves guards above short-circuit before
                // OpenMcdf's own FAT/directory walk runs.
                OpenMcdfParseAttemptCount++;

                using var stream = new MemoryStream(request.FileBytes);
                using var root = RootStorage.Open(stream);

                HashSet<string> entryNames = root.EnumerateEntries()
                    .Select(entry => entry.Name)
                    .ToHashSet(StringComparer.Ordinal);

                if (entryNames.Contains(WordDocumentEntryName))
                {
                    return BuildResult(DetectedFormatEnum.Doc, "application/msword");
                }

                if (entryNames.Contains(ExcelWorkbookEntryName) || entryNames.Contains(ExcelBookEntryName))
                {
                    return BuildResult(DetectedFormatEnum.Xls, "application/vnd.ms-excel");
                }

                if (entryNames.Contains(PowerPointEntryName))
                {
                    return BuildResult(DetectedFormatEnum.Ppt, "application/vnd.ms-powerpoint");
                }

                return UnknownResult();
            }
            catch (Exception)
            {
                // Not a CFB file, or a corrupt/unsupported one -- Unknown, never throw.
                return UnknownResult();
            }
        }

        // Hand-parses the raw CFB header (MS-CFB section 2.2) to estimate the total
        // bytes the file's own FAT claims to describe, without letting OpenMcdf walk
        // the structure first. OpenMcdf's header type is internal, so this reads the
        // documented fixed-offset fields directly.
        private static bool TryGetDeclaredCfbTotalSize(byte[] bytes, out ulong declaredTotalBytes, out uint fatSectorCount, out int sectorSize)
        {
            declaredTotalBytes = 0;
            fatSectorCount = 0;
            sectorSize = 0;

            const int HeaderMinLength = 0x30; // through end of FAT-sector-count field at 0x2C-0x2F
            if (bytes.Length < HeaderMinLength) return false;

            ReadOnlySpan<byte> cfbSignature = stackalloc byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
            if (!bytes.AsSpan(0, 8).SequenceEqual(cfbSignature)) return false;

            ushort sectorShift = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(0x1E, 2));
            fatSectorCount = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(0x2C, 4));

            // MS-CFB only defines sector shift 9 (512B, v3) or 12 (4096B, v4). A forged/out-of-range
            // value could otherwise wrap the shift math, so treat anything implausible as maximally
            // oversized rather than risk a wraparound silently bypassing the guard.
            if (sectorShift > 20)
            {
                declaredTotalBytes = ulong.MaxValue;
                sectorSize = int.MaxValue;
                return true;
            }

            sectorSize = 1 << sectorShift;
            ulong entriesPerFatSector = (ulong)sectorSize / 4;
            declaredTotalBytes = (ulong)fatSectorCount * entriesPerFatSector * (ulong)sectorSize;
            return true;
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
