using ExtractorOLE.Abstractions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.Linq;
using System.Text;

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
                ? string.Join(Environment.NewLine, body.Descendants<Paragraph>()
                    // Text-box paragraphs are nested in an outer paragraph; ParagraphText folds them in.
                    .Where(p => !p.Ancestors<Paragraph>().Any())
                    .Select(ParagraphText))
                : string.Empty;
        }

        // InnerText minus DeletedText (w:delText) and DeletedFieldCode (w:delInstrText), so output matches the
        // document with tracked edits accepted. Fallback copies of text boxes (mc:Fallback) are skipped, and
        // nested (text-box) paragraphs are separated by a newline.
        private static string ParagraphText(Paragraph outer)
        {
            var sb = new StringBuilder();
            Paragraph? previous = null;
            foreach (var leaf in outer.Descendants<OpenXmlLeafTextElement>())
            {
                if (leaf is DeletedText or DeletedFieldCode || leaf.Ancestors<AlternateContentFallback>().Any()) continue;
                var current = leaf.Ancestors<Paragraph>().First();
                if (previous != null && !ReferenceEquals(previous, current)) sb.Append(Environment.NewLine);
                previous = current;
                sb.Append(leaf.Text);
            }

            return sb.ToString();
        }
    }
}
