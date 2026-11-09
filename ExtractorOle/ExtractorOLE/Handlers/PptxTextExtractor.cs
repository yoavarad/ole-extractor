using ExtractorOLE.Abstractions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExtractorOLE.Helpers;

namespace ExtractorOLE.Handlers
{
    internal class PptxTextExtractor : IParsedDocumentTextExtractor
    {
        public string ExtractText(byte[] fileBytes) => ExtractText(fileBytes, onSlideFailure: null);

        // Byte form with per-slide isolation, for PptTextExtractor's converted .pptx (#288).
        internal static string ExtractText(byte[] fileBytes, Action<Exception>? onSlideFailure)
        {
            using (var ppt = OpenXmlPackages.OpenPresentation(fileBytes))
            {
                return ExtractText(ppt, onSlideFailure);
            }
        }

        // parsedDocument is the package PowerPointOpenStrategy already opened (#167); caller owns it.
        public string? ExtractText(object parsedDocument) => ExtractText(parsedDocument, onPartFailure: null);

        // A failing slide costs only its own text (lines already read from it, if any, are kept);
        // the other slides are still read (#288). The deck's slide list, including resolving its slide
        // parts, is not per slide: its failure fails the whole text.
        public string? ExtractText(object parsedDocument, Action<Exception>? onPartFailure)
        {
            if (parsedDocument is not PresentationDocument ppt) return null;
            return ExtractText(ppt, onPartFailure);
        }

        // With onSlideFailure null, the first failing slide fails the whole text.
        private static string ExtractText(PresentationDocument ppt, Action<Exception>? onSlideFailure)
        {
            var presentationPart = ppt.PresentationPart;
            if (presentationPart == null) return string.Empty;

            StringBuilder sb = new StringBuilder();

            foreach (var slidePart in GetSlidePartsInDeckOrder(presentationPart))
            {
                ExtractionCancellation.ThrowIfCancellationRequested();
                try
                {
                    AppendTextLines(sb, slidePart.Slide);
                    AppendTextLines(sb, slidePart.NotesSlidePart?.NotesSlide);
                }
                catch (Exception ex) when (onSlideFailure != null && FieldIsolation.IsIsolatable(ex))
                {
                    onSlideFailure(ex);
                }
            }

            return sb.ToString().Trim();
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
