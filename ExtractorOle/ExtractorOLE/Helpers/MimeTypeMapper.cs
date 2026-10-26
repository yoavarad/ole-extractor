using ExtractorOLE.DTOs;

namespace ExtractorOLE.Helpers
{
    /// <summary>MIME string &lt;-&gt; <see cref="OfficeMimeType"/> / file-extension mapping.</summary>
    public interface IMimeTypeMapper
    {
        string MimeFor(OfficeMimeType type);
        OfficeMimeType ParseOfficeMimeType(string? mime);
        string GetExtensionFromContentType(string contentType);
    }

    public class MimeTypeMapper : IMimeTypeMapper
    {
        public string MimeFor(OfficeMimeType type)
        {
            return type switch
            {
                OfficeMimeType.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                OfficeMimeType.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                OfficeMimeType.PowerPoint => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                OfficeMimeType.ExcelLegacy => "application/vnd.ms-excel",
                OfficeMimeType.PowerPointLegacy => "application/vnd.ms-powerpoint",
                OfficeMimeType.WordLegacy => "application/msword",
                _ => "application/vnd.openxmlformats-package",
            };
        }

        public OfficeMimeType ParseOfficeMimeType(string? mime)
        {
            if (string.IsNullOrEmpty(mime)) return OfficeMimeType.OpenXmlUnknown;
            var m = mime.ToLowerInvariant();

            if (m.Contains("word") || m.Contains("wordprocessingml") || m.Contains("/doc")) return OfficeMimeType.Word;
            if (m.Contains("sheet") || m.Contains("spreadsheetml") || m.Contains("excel") || m.Contains("/xl")) return OfficeMimeType.Excel;
            if (m.Contains("presentation") || m.Contains("presentationml") || m.Contains("ppt")) return OfficeMimeType.PowerPoint;
            if (m.Equals("application/zip", StringComparison.Ordinal) || m.Equals("application/vnd.openxmlformats-package", StringComparison.Ordinal)) return OfficeMimeType.OpenXmlUnknown;

            return OfficeMimeType.OpenXmlUnknown;
        }

        public string GetExtensionFromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return ".bin";
            string cType = contentType.ToLowerInvariant();
            if (cType.Contains("pdf")) return ".pdf";
            if (cType.Contains("spreadsheetml") || cType.Contains("excel")) return ".xlsx";
            if (cType.Contains("wordprocessingml")) return ".docx";
            if (cType.Contains("jpeg") || cType.Contains("jpg")) return ".jpg";
            if (cType.Contains("png")) return ".png";
            if (cType.Contains("presentationml")) return ".pptx";
            return ".bin";
        }
    }
}
