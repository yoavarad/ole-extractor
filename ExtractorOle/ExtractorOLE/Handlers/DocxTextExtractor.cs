using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.Linq;

namespace ExtractorOLE.Handlers
{
    internal class DocxTextExtractor : IParsedDocumentTextExtractor
    {
        public string ExtractText(byte[] fileBytes)
        {
            try
            {
                using (var word = OpenXmlPackages.OpenWord(fileBytes))
                {
                    return ExtractText(word);
                }
            }
            catch (Exception)
            {
                // swallow and return empty text on failure
            }

            return string.Empty;
        }

        // parsedDocument is the package WordOpenStrategy already opened (#167); caller owns it.
        public string? ExtractText(object parsedDocument)
        {
            if (parsedDocument is not WordprocessingDocument word) return null;
            try
            {
                return ExtractText(word);
            }
            catch (Exception)
            {
                // swallow and return empty text on failure
            }

            return string.Empty;
        }

        private static string ExtractText(WordprocessingDocument word)
        {
            var body = word.MainDocumentPart?.Document?.Body;
            return body != null
                ? string.Join(Environment.NewLine, body.Descendants<Paragraph>().Select(p => p.InnerText))
                : string.Empty;
        }
    }
}
