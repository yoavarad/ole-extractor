using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a minimal, valid .docx (or .docm, when <see cref="SampleSpec.VbaProject"/>
    /// is set) via DocumentFormat.OpenXml directly: controllable body text,
    /// general metadata, first-layer embeddings, and - for the macro-enabled
    /// variant - a structurally-valid VBA project storage (arbitrary
    /// placeholder bytes; no functioning macro is required). The
    /// macro-enabled variant must still be detected as base format docx, not
    /// as a separate format (Epic 5/6's macro-variant-misclassification
    /// scenario).
    /// </summary>
    public sealed class DocxSampleGenerator : ISampleGenerator
    {
        public SampleFormat Format => SampleFormat.Docx;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            var isMacroEnabled = spec.VbaProject is not null;
            var documentType = isMacroEnabled
                ? WordprocessingDocumentType.MacroEnabledDocument
                : WordprocessingDocumentType.Document;

            using var stream = new MemoryStream();
            using (var document = WordprocessingDocument.Create(stream, documentType))
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

                if (spec.VbaProject is { } vbaProject)
                {
                    var vbaProjectPart = mainPart.AddNewPart<VbaProjectPart>();
                    using var vbaStream = new MemoryStream(vbaProject);
                    vbaProjectPart.FeedData(vbaStream);
                }

                mainPart.Document.Save();
            }

            return new GeneratedSample
            {
                FileName = isMacroEnabled ? "sample.docm" : "sample.docx",
                Content = stream.ToArray()
            };
        }

        internal static IEnumerable<string> SplitLines(string text) =>
            string.IsNullOrEmpty(text)
                ? new[] { string.Empty }
                : text.Replace("\r\n", "\n").Split('\n');
    }
}
