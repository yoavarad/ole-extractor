using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Drawing;
using System;
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

                    foreach (var slidePart in presentationPart.SlideParts)
                    {
                        if (slidePart.Slide == null) continue;

                        var drawingTexts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
                        foreach (var drawText in drawingTexts)
                        {
                            if (!string.IsNullOrWhiteSpace(drawText.Text))
                            {
                                sb.AppendLine(drawText.Text);
                            }
                        }
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
    }
}
