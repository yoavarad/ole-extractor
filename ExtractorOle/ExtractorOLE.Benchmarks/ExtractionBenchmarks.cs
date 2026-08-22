using BenchmarkDotNet.Attributes;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExtractorOLE;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE.Benchmarks
{
    [MemoryDiagnoser]
    public class ExtractionBenchmarks
    {
        private MainExtractor _extractor = null!;
        private byte[] _sampleDocxBytes = null!;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();
            _extractor = provider.GetRequiredService<MainExtractor>();

            _sampleDocxBytes = BuildSampleDocx();
        }

        [Benchmark]
        public DocumentExtractionResult ExtractWordDocument()
        {
            return _extractor.Extract(_sampleDocxBytes);
        }

        private static byte[] BuildSampleDocx()
        {
            using var stream = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document(
                    new Body(
                        new Paragraph(
                            new Run(
                                new Text("Benchmark sample text for ExtractorOLE.")))));
                mainPart.Document.Save();
            }
            return stream.ToArray();
        }
    }
}
