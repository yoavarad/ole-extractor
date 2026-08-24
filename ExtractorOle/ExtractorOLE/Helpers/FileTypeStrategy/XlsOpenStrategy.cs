using ExtractorOLE.DTOs;
using NPOI.HSSF.UserModel;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class XlsOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public XlsOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            try
            {
                var result = new DocumentExtractionResult();
                using (var ms = new MemoryStream(fileBytes))
                {
                    var workbook = new HSSFWorkbook(ms);
                    var summary = workbook.SummaryInformation;
                    if (summary != null)
                    {
                        result.Metadata.Title = summary.Title;
                        result.Metadata.Creator = summary.Author;
                        result.Metadata.Created = summary.CreateDateTime;
                        result.Metadata.Modified = summary.LastSaveDateTime;
                        result.Metadata.LastModifiedBy = summary.LastAuthor;
                    }
                }
                result.MimeType = _helper.MimeFor(OfficeMimeTypeEnum.ExcelLegacy);
                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
