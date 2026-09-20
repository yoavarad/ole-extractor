using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.SampleGeneration
{
    // Verifies the macro-enabled-with-embedding synthetic sample scenario
    // (docs/specs/dataset-curation.md Corpus Composition rule #3): every
    // docx/xlsx/pptx generator, when given MacroEmbedSampleSpecs.Build(),
    // produces a real OOXML package carrying a VbaProjectPart (the macro
    // storage) and at least one first-layer embedding. This is also the
    // fixture the macro-variant-misclassification test scenario exercises -
    // docm/xlsm/pptm must be detected as their base format, not as a
    // separate format. The legacy doc/xls/ppt generators (T-b856b14e, ADR-005)
    // author the same scenario as one binary file each.
    public class MacroEmbedSampleGeneratorTests
    {
        private static readonly SampleSpec Spec = MacroEmbedSampleSpecs.Build();

        [Fact]
        public void Docx_MacroEnabled_HasVbaProjectAndEmbedding()
        {
            var sample = new DocxSampleGenerator().Generate(Spec);

            Assert.Equal("sample.docm", sample.FileName);

            using var stream = new MemoryStream(sample.Content);
            using var document = WordprocessingDocument.Open(stream, false);

            Assert.NotNull(document.MainDocumentPart!.VbaProjectPart);
            Assert.NotEmpty(document.MainDocumentPart.ImageParts);
        }

        [Fact]
        public void Xlsx_MacroEnabled_HasVbaProjectAndEmbedding()
        {
            var sample = new XlsxSampleGenerator().Generate(Spec);

            Assert.Equal("sample.xlsm", sample.FileName);

            using var stream = new MemoryStream(sample.Content);
            using var document = SpreadsheetDocument.Open(stream, false);

            Assert.NotNull(document.WorkbookPart!.VbaProjectPart);
            var worksheetPart = document.WorkbookPart.WorksheetParts.Single();
            Assert.NotEmpty(worksheetPart.ImageParts);
        }

        [Fact]
        public void Pptx_MacroEnabled_HasVbaProjectAndEmbedding()
        {
            var sample = new PptxSampleGenerator().Generate(Spec);

            Assert.Equal("sample.pptm", sample.FileName);

            using var stream = new MemoryStream(sample.Content);
            using var document = PresentationDocument.Open(stream, false);

            Assert.NotNull(document.PresentationPart!.VbaProjectPart);
            var slidePart = document.PresentationPart.SlideParts.Single();
            Assert.NotEmpty(slidePart.ImageParts);
        }

        [Fact]
        public void Docx_WithoutVbaProject_IsPlainNonMacroDocument()
        {
            var plainSpec = new SampleSpec { BodyText = "plain text, no macro" };

            var sample = new DocxSampleGenerator().Generate(plainSpec);

            Assert.Equal("sample.docx", sample.FileName);

            using var stream = new MemoryStream(sample.Content);
            using var document = WordprocessingDocument.Open(stream, false);

            Assert.Null(document.MainDocumentPart!.VbaProjectPart);
        }

        [Theory]
        [InlineData(typeof(DocSampleGenerator), OfficeMimeTypeEnum.WordLegacy)]
        [InlineData(typeof(XlsSampleGenerator), OfficeMimeTypeEnum.ExcelLegacy)]
        [InlineData(typeof(PptSampleGenerator), OfficeMimeTypeEnum.PowerPointLegacy)]
        public void LegacyFormat_MacroEmbedSpec_ProducesMacroFileWithEmbeddingThatExtractsBack(
            Type generatorType, OfficeMimeTypeEnum format)
        {
            var generator = (ISampleGenerator)Activator.CreateInstance(generatorType)!;

            var sample = generator.Generate(Spec);
            var result = DocSampleGeneratorTests.Extract(sample.Content, format);

            Assert.NotEmpty(result.EmbeddedFiles);
            var hasMacros = result.FormatMetadata switch
            {
                WordFormatMetadata w => w.HasMacros,
                ExcelFormatMetadata e => e.HasMacros,
                PowerPointFormatMetadata p => p.HasMacros,
                _ => false,
            };
            Assert.True(hasMacros);
        }
    }
}
