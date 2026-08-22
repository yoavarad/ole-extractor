using ExtractorOLE.DTOs;
using OpenMcdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractorOLE.Helpers.MimeDetection
{
    // Distinguishes doc/xls/ppt by opening FileBytes as a compound file (OpenMcdf)
    // and inspecting root storage/stream names -- no full document parse.
    // Detection is content-only: request.FileName is never read.
    public class CfbMimeDetector : ICfbMimeDetector
    {
        private const string WordDocumentEntryName = "WordDocument";
        private const string ExcelWorkbookEntryName = "Workbook";
        private const string ExcelBookEntryName = "Book";
        private const string PowerPointEntryName = "PowerPoint Document";

        public MimeDetectionResult Detect(MimeDetectionRequest request)
        {
            if (request?.FileBytes == null || request.FileBytes.Length == 0)
            {
                return UnknownResult();
            }

            try
            {
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
