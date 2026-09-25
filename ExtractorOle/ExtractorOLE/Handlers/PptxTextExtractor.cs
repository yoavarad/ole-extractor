using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExtractorOLE.Handlers
{
    internal class PptxTextExtractor : ITextExtractor
    {
        public string ExtractText(byte[] fileBytes)
        {
            try
            {
                using (var ms = new MemoryStream(fileBytes))
                using (var ppt = PresentationDocument.Open(ms, false))
                {
                    var presentationPart = ppt.PresentationPart;
                    if (presentationPart == null) return string.Empty;

                    StringBuilder sb = new StringBuilder();

                    foreach (var slidePart in GetSlidePartsInDeckOrder(presentationPart))
                    {
                        AppendTextLines(sb, slidePart.Slide);
                        AppendTextLines(sb, slidePart.NotesSlidePart?.NotesSlide);
                    }

                    return sb.ToString().Trim();
                }
            }
            catch (Exception)
            {
                // swallow and return empty on failure
            }

            return string.Empty;
        }

        // Deck order is p:sldIdLst in presentation.xml, not the package relationship order
        // PresentationPart.SlideParts yields (which differs for decks whose slides were
        // reordered after creation). Falls back to SlideParts only when the deck has no
        // slide id list at all.
        private static IEnumerable<SlidePart> GetSlidePartsInDeckOrder(PresentationPart presentationPart)
        {
            var slideIds = presentationPart.Presentation?.SlideIdList;
            if (slideIds == null) return presentationPart.SlideParts;

            // A dangling or non-slide relationship id is skipped rather than failing the whole
            // extraction, matching how the SlideParts walk tolerated it.
            var slidePartsById = presentationPart.Parts
                .Where(pair => pair.OpenXmlPart is SlidePart)
                .ToDictionary(pair => pair.RelationshipId, pair => (SlidePart)pair.OpenXmlPart);

            return slideIds.Elements<DocumentFormat.OpenXml.Presentation.SlideId>()
                .Select(slideId => slideId.RelationshipId?.Value)
                .Where(relationshipId => relationshipId != null && slidePartsById.ContainsKey(relationshipId))
                .Select(relationshipId => slidePartsById[relationshipId!]);
        }

        // Appends one line per Drawing.Text run found in document order (slide body first,
        // then that slide's speaker notes, if any) - the flat-text shape docs/specs/
        // extraction.md calls for ("slide + notes text"). The OpenXml SDK's already-decoded
        // string is appended verbatim with no Unicode normalization, so multilingual text
        // and emoji (including ZWJ/skin-tone sequences) round-trip exactly per
        // [ydk:req:extraction/unicode-fidelity].
        private static void AppendTextLines(StringBuilder sb, OpenXmlElement? root)
        {
            if (root == null) return;

            foreach (var text in root.Descendants<Text>())
            {
                if (!string.IsNullOrWhiteSpace(text.Text))
                {
                    sb.AppendLine(text.Text);
                }
            }
        }
    }
}
