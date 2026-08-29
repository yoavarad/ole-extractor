using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.IO;
using Xunit;
using ExtProps = DocumentFormat.OpenXml.ExtendedProperties;

namespace ExtractorOLE.Tests.Docx
{
    /// <summary>
    /// Covers ydk:entity:extraction/WordFormatMetadata parity for docx/docm:
    /// page/word/character/paragraph/line counts, company/manager/template, and
    /// the macro-presence flag, all sourced from OOXML Extended (app.xml)
    /// Properties, plus the "absent fields are null, not placeholder strings"
    /// acceptance criterion.
    /// </summary>
    public class WordOpenStrategyTests
    {
        private static byte[] BuildDocxFixtureBytes(
            bool macroEnabled,
            int? pages = null,
            int? words = null,
            int? characters = null,
            int? paragraphs = null,
            int? lines = null,
            string? company = null,
            string? manager = null,
            string? template = null,
            bool includeExtendedProperties = true,
            string? title = null,
            string? creator = null)
        {
            var documentType = macroEnabled
                ? WordprocessingDocumentType.MacroEnabledDocument
                : WordprocessingDocumentType.Document;

            using var ms = new MemoryStream();
            using (var document = WordprocessingDocument.Create(ms, documentType))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text("hello")))));
                mainPart.Document.Save();

                if (title != null) document.PackageProperties.Title = title;
                if (creator != null) document.PackageProperties.Creator = creator;

                if (includeExtendedProperties)
                {
                    var extendedPart = document.AddExtendedFilePropertiesPart();
                    var properties = new ExtProps.Properties();
                    if (pages.HasValue) properties.Append(new ExtProps.Pages(pages.Value.ToString()));
                    if (words.HasValue) properties.Append(new ExtProps.Words(words.Value.ToString()));
                    if (characters.HasValue) properties.Append(new ExtProps.Characters(characters.Value.ToString()));
                    if (paragraphs.HasValue) properties.Append(new ExtProps.Paragraphs(paragraphs.Value.ToString()));
                    if (lines.HasValue) properties.Append(new ExtProps.Lines(lines.Value.ToString()));
                    if (company != null) properties.Append(new ExtProps.Company(company));
                    if (manager != null) properties.Append(new ExtProps.Manager(manager));
                    if (template != null) properties.Append(new ExtProps.Template(template));
                    extendedPart.Properties = properties;
                }

                if (macroEnabled)
                {
                    var vbaProjectPart = mainPart.AddNewPart<VbaProjectPart>();
                    using var vbaStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
                    vbaProjectPart.FeedData(vbaStream);
                }
            }

            return ms.ToArray();
        }

        [Fact]
        public void Open_DocxWithFullAppXmlProperties_PopulatesWordFormatMetadataAndFileMetadata()
        {
            var bytes = BuildDocxFixtureBytes(
                macroEnabled: false,
                pages: 5, words: 120, characters: 600, paragraphs: 10, lines: 30,
                company: "Acme Corp", manager: "Jane Manager", template: "Normal.dotm",
                title: "Quarterly Report", creator: "John Author");

            var result = new WordOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);

            Assert.Equal("Quarterly Report", result!.Metadata.Title);
            Assert.Equal("John Author", result.Metadata.Creator);

            var formatMetadata = Assert.IsType<WordFormatMetadata>(result.FormatMetadata);
            Assert.Equal(5, formatMetadata.PageCount);
            Assert.Equal(120, formatMetadata.WordCount);
            Assert.Equal(600, formatMetadata.CharacterCount);
            Assert.Equal(10, formatMetadata.ParagraphCount);
            Assert.Equal(30, formatMetadata.LineCount);
            Assert.Equal("Acme Corp", formatMetadata.Company);
            Assert.Equal("Jane Manager", formatMetadata.Manager);
            Assert.Equal("Normal.dotm", formatMetadata.Template);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_DocmWithVbaProject_HasMacrosIsTrue()
        {
            var bytes = BuildDocxFixtureBytes(macroEnabled: true);

            var result = new WordOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<WordFormatMetadata>(result!.FormatMetadata);
            Assert.True(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_NonMacroDocx_HasMacrosIsFalse()
        {
            var bytes = BuildDocxFixtureBytes(macroEnabled: false);

            var result = new WordOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<WordFormatMetadata>(result!.FormatMetadata);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_DocxWithoutExtendedFilePropertiesPart_AllWordFormatFieldsAreNullNotPlaceholder()
        {
            var bytes = BuildDocxFixtureBytes(macroEnabled: false, includeExtendedProperties: false);

            var result = new WordOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<WordFormatMetadata>(result!.FormatMetadata);
            Assert.Null(formatMetadata.PageCount);
            Assert.Null(formatMetadata.WordCount);
            Assert.Null(formatMetadata.CharacterCount);
            Assert.Null(formatMetadata.ParagraphCount);
            Assert.Null(formatMetadata.LineCount);
            Assert.Null(formatMetadata.Company);
            Assert.Null(formatMetadata.Manager);
            Assert.Null(formatMetadata.Template);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_DocxWithPartialAppXmlProperties_LeavesMissingFieldsNullNotPlaceholder()
        {
            var bytes = BuildDocxFixtureBytes(macroEnabled: false, pages: 3, company: "Only Company Set");

            var result = new WordOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<WordFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(3, formatMetadata.PageCount);
            Assert.Equal("Only Company Set", formatMetadata.Company);
            Assert.Null(formatMetadata.WordCount);
            Assert.Null(formatMetadata.CharacterCount);
            Assert.Null(formatMetadata.ParagraphCount);
            Assert.Null(formatMetadata.LineCount);
            Assert.Null(formatMetadata.Manager);
            Assert.Null(formatMetadata.Template);
        }

        [Fact]
        public void Open_CorruptBytes_ReturnsNull()
        {
            var result = new WordOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.Null(result);
        }
    }
}
