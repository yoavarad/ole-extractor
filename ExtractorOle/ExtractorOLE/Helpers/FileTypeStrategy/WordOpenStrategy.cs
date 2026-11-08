using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System;
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

        public DocumentExtractionResult? Open(byte[] fileBytes) => Open(new MemoryStream(fileBytes, false));

        // Reads the seekable stream lazily (#192, ADR-008); the caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input)
        {
            WordprocessingDocument word;
            try
            {
                word = OpenXmlPackages.OpenWord(input);
            }
            catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
            {
                return null;
            }

            try
            {
                var result = new DocumentExtractionResult();
                StrategyIsolation.PopulateEmbedded(result, () => _helper.ExtractMetadataAndEmbedded(word, word.MainDocumentPart, result));
                if (string.IsNullOrEmpty(result.MimeType) && word.CoreFilePropertiesPart != null)
                {
                    result.MimeType = word.CoreFilePropertiesPart.ContentType ?? string.Empty;
                }

                result.FormatMetadata = BuildWordFormatMetadata(word, result);

                // Kept open for DocxTextExtractor (#167); MainExtractor disposes it.
                result.ParsedDocument = word;
                return result;
            }
            catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
            {
                word.Dispose();
                return null;
            }
            catch
            {
                word.Dispose();
                throw;
            }
        }

        // Page/word/character/paragraph/line counts and company/manager/template come
        // from OOXML Extended (app.xml) Properties - they're recorded values from the
        // producing application, never recomputed here. A field is left null when its
        // element is absent from app.xml (ExtendedFilePropertiesPart itself may also be
        // absent entirely, e.g. for a minimally-authored docx). HasMacros is true when
        // the package carries a VBA project part (docm), which is how the OOXML SDK
        // exposes the macro storage regardless of the document's file extension.
        // Each field is its own leaf at FormatMetadata.<Name>; a failed leaf keeps its default.
        private static WordFormatMetadata BuildWordFormatMetadata(WordprocessingDocument word, DocumentExtractionResult result)
        {
            // Read once; a part that cannot be parsed fails every leaf it sources.
            var props = new Lazy<DocumentFormat.OpenXml.ExtendedProperties.Properties?>(() => word.ExtendedFilePropertiesPart?.Properties);
            var fm = new WordFormatMetadata();

            void Leaf(string name, Action populate) => StrategyIsolation.PopulateLeaf(result, name, populate);

            Leaf(nameof(WordFormatMetadata.PageCount), () => fm.PageCount = ParseNullableInt(props.Value?.Pages?.Text));
            Leaf(nameof(WordFormatMetadata.WordCount), () => fm.WordCount = ParseNullableInt(props.Value?.Words?.Text));
            Leaf(nameof(WordFormatMetadata.CharacterCount), () => fm.CharacterCount = ParseNullableInt(props.Value?.Characters?.Text));
            Leaf(nameof(WordFormatMetadata.ParagraphCount), () => fm.ParagraphCount = ParseNullableInt(props.Value?.Paragraphs?.Text));
            Leaf(nameof(WordFormatMetadata.LineCount), () => fm.LineCount = ParseNullableInt(props.Value?.Lines?.Text));
            Leaf(nameof(WordFormatMetadata.Company), () => fm.Company = props.Value?.Company?.Text);
            Leaf(nameof(WordFormatMetadata.Manager), () => fm.Manager = props.Value?.Manager?.Text);
            Leaf(nameof(WordFormatMetadata.Template), () => fm.Template = props.Value?.Template?.Text);
            Leaf(nameof(WordFormatMetadata.HasMacros), () => fm.HasMacros = word.MainDocumentPart?.VbaProjectPart != null);
            return fm;
        }

        private static int? ParseNullableInt(string? text) =>
            int.TryParse(text, out var value) ? value : null;
    }
}
