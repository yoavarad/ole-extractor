using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    public class ExcelExtractorEmbeddedObjectTests
    {
        [Fact]
        public void ExtractXlsx_FindsEmbeddingPlacedOnWorksheetPart()
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

            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var extractor = services.BuildServiceProvider().GetRequiredService<MainExtractor>();

            var result = extractor.Extract(new ExtractionRequest
            {
                FileBytes = generated.Content,
                FileName = "fixture.xlsx",
                DetectedMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });

            Assert.Single(result.EmbeddedFiles);
            Assert.Equal(4, result.EmbeddedFiles[0].SizeInBytes);
        }
    }
}
