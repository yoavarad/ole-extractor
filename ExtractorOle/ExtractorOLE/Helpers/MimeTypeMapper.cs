using ExtractorOLE.DTOs;

namespace ExtractorOLE.Helpers
{
    /// <summary>MIME string &lt;-&gt; <see cref="OfficeMimeTypeEnum"/> / file-extension mapping.</summary>
    public interface IMimeTypeMapper
    {
        string MimeFor(OfficeMimeTypeEnum type);
        OfficeMimeTypeEnum ParseOfficeMimeType(string? mime);
        string GetExtensionFromContentType(string contentType);
    }

    public class MimeTypeMapper : IMimeTypeMapper
    {
        public string MimeFor(OfficeMimeTypeEnum type)
        {
            return type switch
            {
                OfficeMimeTypeEnum.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                OfficeMimeTypeEnum.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                OfficeMimeTypeEnum.PowerPoint => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                OfficeMimeTypeEnum.ExcelLegacy => "application/vnd.ms-excel",
                OfficeMimeTypeEnum.PowerPointLegacy => "application/vnd.ms-powerpoint",
                OfficeMimeTypeEnum.WordLegacy => "application/msword",
                _ => "application/vnd.openxmlformats-package",
            };
        }

        public OfficeMimeTypeEnum ParseOfficeMimeType(string? mime)
        {
            if (string.IsNullOrEmpty(mime)) return OfficeMimeTypeEnum.OpenXmlUnknown;
            var m = mime.ToLowerInvariant();

            if (m.Contains("word") || m.Contains("wordprocessingml") || m.Contains("/doc")) return OfficeMimeTypeEnum.Word;
            if (m.Contains("sheet") || m.Contains("spreadsheetml") || m.Contains("excel") || m.Contains("/xl")) return OfficeMimeTypeEnum.Excel;
            if (m.Contains("presentation") || m.Contains("presentationml") || m.Contains("ppt")) return OfficeMimeTypeEnum.PowerPoint;
            if (m.Equals("application/zip", StringComparison.Ordinal) || m.Equals("application/vnd.openxmlformats-package", StringComparison.Ordinal)) return OfficeMimeTypeEnum.OpenXmlUnknown;

            return OfficeMimeTypeEnum.OpenXmlUnknown;
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
