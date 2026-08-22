using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a minimal, valid .docx via DocumentFormat.OpenXml directly:
    /// controllable body text, general metadata, and first-layer embeddings.
    /// </summary>
    public sealed class DocxSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Docx;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            using var stream = new MemoryStream();
            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                var body = new Body();
                mainPart.Document = new Document(body);

                foreach (var line in SplitLines(spec.BodyText))
                {
                    var run = new Run(new Text(line) { Space = SpaceProcessingModeValues.Preserve });
                    body.AppendChild(new Paragraph(run));
                }

                OpenXmlPackagePropertiesHelper.Apply(document.PackageProperties, spec.Metadata);
                OpenXmlEmbeddingHelper.AddEmbeddings(mainPart, spec.Embeddings);

                mainPart.Document.Save();
            }

            return new GeneratedSample
            {
                FileName = "sample.docx",
                Content = stream.ToArray()
            };
        }

        internal static IEnumerable<string> SplitLines(string text) =>
            string.IsNullOrEmpty(text)
                ? new[] { string.Empty }
                : text.Replace("\r\n", "\n").Split('\n');
    }
}
