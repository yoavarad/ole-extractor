using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class WordOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public WordOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            try
            {
                var result = new DocumentExtractionResult();
                using (var ms = new MemoryStream(fileBytes))
                using (var word = WordprocessingDocument.Open(ms, false))
                {
                    _helper.ExtractMetadataAndEmbedded(word, word.MainDocumentPart, result);
                    if (string.IsNullOrEmpty(result.MimeType) && word.CoreFilePropertiesPart != null)
                    {
                        result.MimeType = word.CoreFilePropertiesPart.ContentType ?? string.Empty;
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
