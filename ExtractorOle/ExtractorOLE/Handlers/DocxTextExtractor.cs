using DocumentFormat.OpenXml.Packaging;
using System;
using System.IO;

namespace ExtractorOLE.Handlers
{
    internal class DocxTextExtractor : ITextExtractor
    {
        public string ExtractText(byte[] fileBytes)
        {
            try
            {
                using (var ms = new MemoryStream(fileBytes))
                using (var word = WordprocessingDocument.Open(ms, false))
                {
                    if (word.MainDocumentPart?.Document?.Body != null)
                    {
                        return word.MainDocumentPart.Document.Body.InnerText ?? string.Empty;
                    }
                }
            }
            catch (Exception)
            {
                // swallow and return empty text on failure
            }

            return string.Empty;
        }
    }
}
