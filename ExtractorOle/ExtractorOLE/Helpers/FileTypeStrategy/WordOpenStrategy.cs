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
            WordprocessingDocument? word = null;
            try
            {
                var result = new DocumentExtractionResult();
                word = OpenXmlPackages.OpenWord(fileBytes);
                _helper.ExtractMetadataAndEmbedded(word, word.MainDocumentPart, result);
                if (string.IsNullOrEmpty(result.MimeType) && word.CoreFilePropertiesPart != null)
                {
                    result.MimeType = word.CoreFilePropertiesPart.ContentType ?? string.Empty;
                }

                result.FormatMetadata = BuildWordFormatMetadata(word);

                // Kept open for DocxTextExtractor (#167); MainExtractor disposes it.
                result.ParsedDocument = word;
                return result;
            }
            catch
            {
                word?.Dispose();
                return null;
            }
        }

        // Page/word/character/paragraph/line counts and company/manager/template come
        // from OOXML Extended (app.xml) Properties - they're recorded values from the
        // producing application, never recomputed here. A field is left null when its
        // element is absent from app.xml (ExtendedFilePropertiesPart itself may also be
        // absent entirely, e.g. for a minimally-authored docx). HasMacros is true when
        // the package carries a VBA project part (docm), which is how the OOXML SDK
        // exposes the macro storage regardless of the document's file extension.
        private static WordFormatMetadata BuildWordFormatMetadata(WordprocessingDocument word)
        {
            var props = word.ExtendedFilePropertiesPart?.Properties;

            return new WordFormatMetadata
            {
                PageCount = ParseNullableInt(props?.Pages?.Text),
                WordCount = ParseNullableInt(props?.Words?.Text),
                CharacterCount = ParseNullableInt(props?.Characters?.Text),
                ParagraphCount = ParseNullableInt(props?.Paragraphs?.Text),
                LineCount = ParseNullableInt(props?.Lines?.Text),
                Company = props?.Company?.Text,
                Manager = props?.Manager?.Text,
                Template = props?.Template?.Text,
                HasMacros = word.MainDocumentPart?.VbaProjectPart != null,
            };
        }

        private static int? ParseNullableInt(string? text) =>
            int.TryParse(text, out var value) ? value : null;
    }
}
