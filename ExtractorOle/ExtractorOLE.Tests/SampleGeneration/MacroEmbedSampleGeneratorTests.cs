using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
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
    // separate format. Also documents the known, intentional doc/xls/ppt
    // gap (ADR-001: NPOI not yet wired into this build).
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
        [InlineData(typeof(DocSampleGenerator))]
        [InlineData(typeof(XlsSampleGenerator))]
        [InlineData(typeof(PptSampleGenerator))]
        public void LegacyFormat_MacroEmbedSpec_ThrowsNotSupported_KnownAdr001Gap(Type generatorType)
        {
            var generator = (ISampleGenerator)Activator.CreateInstance(generatorType)!;

            var ex = Assert.Throws<NotSupportedException>(() => generator.Generate(Spec));

            Assert.Contains("ADR-001", ex.Message);
        }
    }
}
