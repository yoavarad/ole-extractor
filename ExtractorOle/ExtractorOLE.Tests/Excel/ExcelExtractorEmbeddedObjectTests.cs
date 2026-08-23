using ExtractorOLE.Old.Excel;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    public class ExcelExtractorEmbeddedObjectTests
    {
        [Fact]
        public void ExtractFromXlsxBinary_FindsEmbeddingPlacedOnWorksheetPart()
        {
            var spec = new SampleSpec
            {
                BodyText = "hello xlsx",
                Embeddings = new List<EmbeddedContentSpec>
                {
                    new EmbeddedContentSpec
                    {
                        FileName = "note.png",
                        Content = new byte[] { 1, 2, 3, 4 },
                        ContentType = "image/png"
                    }
                }
            };

            var generated = new XlsxSampleGenerator().Generate(spec);

            var result = new ExcelExtractor().ExtractFromXlsxBinary(generated.Content);

            Assert.Single(result.EmbeddedFiles);
            Assert.Equal(4, result.EmbeddedFiles[0].SizeInBytes);
        }
    }
}
