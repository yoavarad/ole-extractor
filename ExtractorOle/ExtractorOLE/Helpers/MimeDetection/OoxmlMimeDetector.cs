using ExtractorOLE.DTOs;
using System;
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

        public MimeDetectionResult Detect(MimeDetectionRequest request)
        {
            if (request?.FileBytes == null || request.FileBytes.Length == 0)
            {
                return UnknownResult();
            }

            try
            {
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
