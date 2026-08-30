using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class PowerPointOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public PowerPointOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            try
            {
                var result = new DocumentExtractionResult();
                using (var ms = new MemoryStream(fileBytes))
                using (var pres = PresentationDocument.Open(ms, false))
                {
                    _helper.ExtractMetadataAndEmbedded(pres, pres.PresentationPart, result);
                    result.FormatMetadata = BuildPowerPointFormatMetadata(pres.PresentationPart);
                    if (string.IsNullOrEmpty(result.MimeType) && pres.CoreFilePropertiesPart != null)
                    {
                        result.MimeType = pres.CoreFilePropertiesPart.ContentType ?? string.Empty;
                    }
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        // Slide/notes-slide counts come straight off the presentation part's slide parts.
        // A slide counts toward NotesSlideCount only when its NotesSlidePart carries actual
        // text content, not merely a (typically-empty) notes placeholder shape - matching
        // the "slides that have speaker notes content" contract. HasMacros is true when the
        // package carries a VbaProjectPart (present for pptm, absent for pptx).
        private static PowerPointFormatMetadata BuildPowerPointFormatMetadata(PresentationPart? presentationPart)
        {
            var slideParts = presentationPart?.SlideParts.ToList() ?? new List<SlidePart>();

            return new PowerPointFormatMetadata
            {
                SlideCount = slideParts.Count,
                NotesSlideCount = slideParts.Count(SlideHasNotesText),
                HasMacros = presentationPart != null && presentationPart.GetPartsOfType<VbaProjectPart>().Any(),
            };
        }

        private static bool SlideHasNotesText(SlidePart slidePart)
        {
            var notesSlide = slidePart.NotesSlidePart?.NotesSlide;
            if (notesSlide == null) return false;

            return notesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                .Any(t => !string.IsNullOrWhiteSpace(t.Text));
        }
    }
}
