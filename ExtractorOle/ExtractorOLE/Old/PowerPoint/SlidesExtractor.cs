using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtractorOLE.Old.PowerPoint
{
    internal class SlidesExtractor
    {
        public DocumentExtractionResult ExtractFromPptxBinary(byte[] fileBytes)
        {
            var result = new DocumentExtractionResult();

            // Fix: Use a writable MemoryStream to ensure relation property graphs populate natively
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(fileBytes, 0, fileBytes.Length);
                ms.Position = 0;

                using (PresentationDocument pptDoc = PresentationDocument.Open(ms, true))
                {
                    PresentationPart? presentationPart = pptDoc.PresentationPart;
                    if (presentationPart == null) return result;

                    // Requirement #2: Dynamic Content Type Checked Directly From Internal Registry
                    result.MimeType = presentationPart.ContentType ?? "application/vnd.openxmlformats-officedocument.presentationml.presentation";

                    // Requirement #1: Warning-Free Metadata Parsing (Direct CoreFilePropertiesPart)
                    CoreFilePropertiesPart? corePart = pptDoc.CoreFilePropertiesPart;
                    if (corePart?.CoreFileProperties != null)
                    {
                        var props = corePart.CoreFileProperties;
                        result.Metadata.Title = props.Title;
                        result.Metadata.Creator = props.Creator;
                        result.Metadata.LastModifiedBy = props.LastModifiedBy;
                        result.Metadata.Created = props.Created;
                        result.Metadata.Modified = props.Modified;
                    }

                    // Requirement #3: Flat Text Extraction (Iterating Slide Parts)
                    StringBuilder textBuilder = new StringBuilder();
                    //int slideIndex = 1;

                    foreach (SlidePart slidePart in presentationPart.SlideParts)
                    {
                        //textBuilder.AppendLine($"--- Slide {slideIndex} ---");

                        if (slidePart.Slide != null)
                        {
                            // Fix: Query for the absolute, fully qualified Drawing Text class node type
                            var drawingTexts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();

                            foreach (var drawText in drawingTexts)
                            {
                                if (!string.IsNullOrWhiteSpace(drawText.Text))
                                {
                                    textBuilder.AppendLine(drawText.Text);
                                }
                            }
                        }
                        //slideIndex++;
                    }
                    result.ExtractedText = textBuilder.ToString().Trim();

                    // Requirement #4: Extract ONLY 1st Layer Embedded Files
                    // presentationPart.Parts only scans root components attached directly to the presentation workspace
                    int index = 1;
                    foreach (var partPair in presentationPart.Parts)
                    {
                        var nestedPart = partPair.OpenXmlPart;

                        // Target explicit embedded binaries (OLE objects, packages, or image layout assets)
                        if (nestedPart is EmbeddedObjectPart || nestedPart is EmbeddedPackagePart || nestedPart is ImagePart)
                        {
                            using (Stream partStream = nestedPart.GetStream())
                            using (MemoryStream binaryStream = new MemoryStream())
                            {
                                partStream.CopyTo(binaryStream);
                                byte[] extractedBytes = binaryStream.ToArray();

                                var item = new EmbeddedFileItem
                                {
                                    BinaryData = extractedBytes,
                                    PackagePath = nestedPart.Uri.ToString(), // Path inside zip container
                                    SizeInBytes = extractedBytes.LongLength
                                };

                                // Look up corresponding extension from the element format registry
                                string extension = GetExtensionFromContentType(nestedPart.ContentType);
                                item.FileName = $"presentation_embedded_object_{index}{extension}";

                                result.EmbeddedFiles.Add(item);
                                index++;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private string GetExtensionFromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return ".bin";
            string cType = contentType.ToLower();

            if (cType.Contains("pdf")) return ".pdf";
            if (cType.Contains("spreadsheetml") || cType.Contains("excel")) return ".xlsx";
            if (cType.Contains("wordprocessingml")) return ".docx";
            if (cType.Contains("jpeg") || cType.Contains("jpg")) return ".jpg";
            if (cType.Contains("png")) return ".png";
            if (cType.Contains("presentationml")) return ".pptx";
            return ".bin"; // Generic fallback for OLE presentation fragments
        }
    }
}
