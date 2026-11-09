using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System;
using System.IO;
using System.Linq;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class PowerPointOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        private readonly Func<PresentationPart?, int> _slideCount;
        private readonly Func<PresentationPart?, int?> _notesSlideCount;
        private readonly Func<PresentationPart?, bool> _hasMacros;

        public PowerPointOpenStrategy(IExtractionHelper helper)
            : this(helper, SlideCountOf, NotesSlideCountOf, HasMacrosOf)
        {
        }

        // Test seam: lets tests make one leaf source fail on an otherwise valid package.
        internal PowerPointOpenStrategy(
            IExtractionHelper helper,
            Func<PresentationPart?, int> slideCount,
            Func<PresentationPart?, int?> notesSlideCount,
            Func<PresentationPart?, bool> hasMacros)
        {
            _helper = helper;
            _slideCount = slideCount;
            _notesSlideCount = notesSlideCount;
            _hasMacros = hasMacros;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes) => Open(new MemoryStream(fileBytes, false));

        // Reads the seekable stream lazily (#192, ADR-008); the caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input)
        {
            // Open step: a package that cannot be opened propagates the library exception so
            // ParseTimeErrorGuard can report the cause (#287).
            PresentationDocument pres = OpenXmlPackages.OpenPresentation(input);

            // Populate step: leaves are isolated; anything else propagates unwrapped.
            try
            {
                var result = new DocumentExtractionResult();
                _helper.ExtractMetadataAndEmbedded(pres, pres.PresentationPart, result);
                result.FormatMetadata = BuildPowerPointFormatMetadata(pres.PresentationPart, result);
                if (string.IsNullOrEmpty(result.MimeType) && pres.CoreFilePropertiesPart != null)
                {
                    result.MimeType = pres.CoreFilePropertiesPart.ContentType ?? string.Empty;
                }

                // Kept open for PptxTextExtractor (#167); MainExtractor disposes it.
                result.ParsedDocument = pres;
                return result;
            }
            catch
            {
                // Parse failures propagate so ParseTimeErrorGuard can report the cause (#287).
                pres?.Dispose();
                throw;
            }
        }

        // [ydk:req:extraction/per-field-error-isolation]: SlideCount, NotesSlideCount and HasMacros
        // are each their own leaf at "FormatMetadata.<Name>". A leaf that fails keeps its default.
        // A slide counts toward NotesSlideCount only when its NotesSlidePart carries actual
        // text content, not merely a (typically-empty) notes placeholder shape. HasMacros is true
        // when the package carries a VbaProjectPart (present for pptm, absent for pptx).
        private PowerPointFormatMetadata BuildPowerPointFormatMetadata(PresentationPart? presentationPart, DocumentExtractionResult result)
        {
            var metadata = new PowerPointFormatMetadata();
            FieldIsolation.TryPopulate(result, LeafPath(nameof(PowerPointFormatMetadata.SlideCount)), () => metadata.SlideCount = _slideCount(presentationPart));
            FieldIsolation.TryPopulate(result, LeafPath(nameof(PowerPointFormatMetadata.NotesSlideCount)), () => metadata.NotesSlideCount = _notesSlideCount(presentationPart));
            FieldIsolation.TryPopulate(result, LeafPath(nameof(PowerPointFormatMetadata.HasMacros)), () => metadata.HasMacros = _hasMacros(presentationPart));
            return metadata;
        }

        internal static string LeafPath(string leaf) => nameof(DocumentExtractionResult.FormatMetadata) + "." + leaf;

        private static int SlideCountOf(PresentationPart? presentationPart) =>
            presentationPart?.SlideParts.Count() ?? 0;

        private static int? NotesSlideCountOf(PresentationPart? presentationPart) =>
            presentationPart?.SlideParts.Count(SlideHasNotesText) ?? 0;

        private static bool HasMacrosOf(PresentationPart? presentationPart) =>
            presentationPart != null && presentationPart.GetPartsOfType<VbaProjectPart>().Any();

        private static bool SlideHasNotesText(SlidePart slidePart)
        {
            var notesSlide = slidePart.NotesSlidePart?.NotesSlide;
            if (notesSlide == null) return false;

            return notesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                .Any(t => !string.IsNullOrWhiteSpace(t.Text));
        }
    }
}
