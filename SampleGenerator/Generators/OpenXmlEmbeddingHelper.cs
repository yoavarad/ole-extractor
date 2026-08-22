using DocumentFormat.OpenXml.Packaging;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Adds <see cref="EmbeddedContentSpec"/> entries as first-layer embedded
    /// objects (EmbeddedObjectPart) or inline media (ImagePart, for image/*
    /// content types) - the same part types
    /// ExtractorOLE's ExtractionHelper.ExtractFirstLayerEmbedded scans for.
    ///
    /// Object and image content can require different target parts, since the
    /// OOXML schema does not allow every part type to hold both directly:
    /// - docx: MainDocumentPart accepts both.
    /// - xlsx: WorkbookPart accepts neither directly - use a WorksheetPart.
    /// - pptx: PresentationPart accepts EmbeddedObjectPart but not ImagePart;
    ///   images must go on a SlidePart (matching ExtractionHelper's dedicated
    ///   slide-image scan).
    /// </summary>
    internal static class OpenXmlEmbeddingHelper
    {
        public static void AddEmbeddings(OpenXmlPart rootPart, IEnumerable<EmbeddedContentSpec> embeddings) =>
            AddEmbeddings(rootPart, rootPart, embeddings);

        public static void AddEmbeddings(OpenXmlPart objectTargetPart, OpenXmlPart imageTargetPart, IEnumerable<EmbeddedContentSpec> embeddings)
        {
            foreach (var embedding in embeddings)
            {
                AddEmbedding(objectTargetPart, imageTargetPart, embedding);
            }
        }

        private static void AddEmbedding(OpenXmlPart objectTargetPart, OpenXmlPart imageTargetPart, EmbeddedContentSpec embedding)
        {
            var contentType = string.IsNullOrWhiteSpace(embedding.ContentType)
                ? "application/octet-stream"
                : embedding.ContentType;

            OpenXmlPart part = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? imageTargetPart.AddNewPart<ImagePart>(contentType)
                : objectTargetPart.AddNewPart<EmbeddedObjectPart>(contentType);

            using var stream = new MemoryStream(embedding.Content);
            part.FeedData(stream);
        }
    }
}
