using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Input;
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
    public class CfbMimeDetector : ICfbMimeDetector, IMimeTypeDetector
    {
        private const string WordDocumentEntryName = "WordDocument";
        private const string ExcelWorkbookEntryName = "Workbook";
        private const string ExcelBookEntryName = "Book";
        private const string PowerPointEntryName = "PowerPoint Document";

        private readonly MimeDetectionLimits _limits;
        private readonly Action<string>? _logRejection;

        // Test-observability only: proves guards below short-circuit before
        // OpenMcdf's own FAT/directory walk runs.
        private static readonly AttemptCounter s_openMcdfParseAttempts = new();
        public static int OpenMcdfParseAttemptCount
        {
            get => s_openMcdfParseAttempts.Value;
            set => s_openMcdfParseAttempts.Value = value;
        }

        public CfbMimeDetector() : this(new MimeDetectionLimits()) { }

        public CfbMimeDetector(MimeDetectionLimits limits, Action<string>? logRejection = null)
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
                _logRejection?.Invoke($"[CfbMimeDetector] file-too-large: fileName='{request.FileName}', actualBytes={length}, limitBytes={_limits.MaxFileSizeBytes}");
                throw new FileTooLargeException(length, _limits.MaxFileSizeBytes);
            }

            if (TryGetDeclaredCfbTotalSize(stream, out var declaredTotalBytes, out var fatSectorCount, out var sectorSize)
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
                s_openMcdfParseAttempts.Increment();

                stream.Position = 0;
                using var root = RootStorage.Open(new NonClosingStream(stream));

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
        internal static bool TryGetDeclaredCfbTotalSize(byte[] bytes, out ulong declaredTotalBytes, out uint fatSectorCount, out int sectorSize)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            return TryGetDeclaredCfbTotalSize(stream, out declaredTotalBytes, out fatSectorCount, out sectorSize);
        }

        // Stream form (#193): reads only the first 0x30 header bytes from position 0.
        internal static bool TryGetDeclaredCfbTotalSize(Stream stream, out ulong declaredTotalBytes, out uint fatSectorCount, out int sectorSize)
        {
            declaredTotalBytes = 0;
            fatSectorCount = 0;
            sectorSize = 0;

            const int HeaderMinLength = 0x30; // through end of FAT-sector-count field at 0x2C-0x2F
            if (stream.Length < HeaderMinLength) return false;

            Span<byte> header = stackalloc byte[HeaderMinLength];
            stream.Position = 0;
            stream.ReadExactly(header);

            ReadOnlySpan<byte> cfbSignature = stackalloc byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
            if (!header.Slice(0, 8).SequenceEqual(cfbSignature)) return false;

            ushort sectorShift = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(0x1E, 2));
            fatSectorCount = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(0x2C, 4));

            // MS-CFB only defines sector shift 9 (512B, v3) or 12 (4096B, v4). Any other
            // value is forged/invalid -- treat it as maximally oversized rather than let
            // it flow into the size multiplication below.
            if (sectorShift != 9 && sectorShift != 12)
            {
                declaredTotalBytes = ulong.MaxValue;
                sectorSize = int.MaxValue;
                return true;
            }

            sectorSize = 1 << sectorShift;
            ulong entriesPerFatSector = (ulong)sectorSize / 4;
            // checked() as defense-in-depth: sectorShift is now clamped to 9/12 above, so this
            // can't overflow for any in-range fatSectorCount, but guard against wraparound anyway.
            declaredTotalBytes = checked((ulong)fatSectorCount * entriesPerFatSector * (ulong)sectorSize);
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
