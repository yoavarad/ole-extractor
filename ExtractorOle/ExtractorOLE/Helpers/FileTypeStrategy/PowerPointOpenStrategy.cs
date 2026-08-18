using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class PowerPointOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public PowerPointOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            try
            {
                var result = new DocumentExtractionResult();
                using (var ms = new MemoryStream(fileBytes))
                using (var pres = PresentationDocument.Open(ms, false))
                {
                    _helper.ExtractMetadataAndEmbedded(pres, pres.PresentationPart, result);
                    if (string.IsNullOrEmpty(result.MimeType) && pres.CoreFilePropertiesPart != null)
                    {
                        result.MimeType = pres.CoreFilePropertiesPart.ContentType ?? string.Empty;
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
