using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class ExcelOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public ExcelOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            try
            {
                var result = new DocumentExtractionResult();
                using (var ms = new MemoryStream(fileBytes))
                using (var excel = SpreadsheetDocument.Open(ms, false))
                {
                    _helper.ExtractMetadataAndEmbedded(excel, excel.WorkbookPart, result);
                    if (string.IsNullOrEmpty(result.MimeType) && excel.CoreFilePropertiesPart != null)
                    {
                        result.MimeType = excel.CoreFilePropertiesPart.ContentType ?? string.Empty;
                    }
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
