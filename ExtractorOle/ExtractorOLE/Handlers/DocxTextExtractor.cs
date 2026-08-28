using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.IO;
using System.Linq;

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
                    var body = word.MainDocumentPart?.Document?.Body;
                    if (body != null)
                    {
                        return string.Join(Environment.NewLine, body.Descendants<Paragraph>().Select(p => p.InnerText));
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
