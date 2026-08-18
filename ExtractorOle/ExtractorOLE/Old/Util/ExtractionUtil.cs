using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace ExtractorOLE.Old.Util
{
    internal class ExtractionUtil
    {
        public void GetFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart workbookPart)
        {
            int index = 1;
            foreach (var partPair in workbookPart.Parts)
            {
                var nestedPart = partPair.OpenXmlPart;

                // Identify embedded spreadsheet assets, OLE attachments, packages, or macro containers
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

                        // Look up corresponding extension from data type and format names
                        string extension = GetExtensionFromContentType(nestedPart.ContentType);
                        item.FileName = $"excel_embedded_object_{index}{extension}";

                        result.EmbeddedFiles.Add(item);
                        index++;
                    }
                }
            }
        }

        private string GetExtensionFromContentType(string contentType)
        {
            string cType = contentType.ToLower();
            if (cType.Contains("pdf")) return ".pdf";
            if (cType.Contains("spreadsheetml") || cType.Contains("excel")) return ".xlsx";
            if (cType.Contains("wordprocessingml")) return ".docx";
            if (cType.Contains("jpeg") || cType.Contains("jpg")) return ".jpg";
            if (cType.Contains("png")) return ".png";
            if (cType.Contains("presentationml")) return ".pptx";
            return ".bin"; // Generic binary data fallback
        }


        public DocumentExtractionResult ExtractionMain(OpenXmlPart workbookPart)
        {
            return new DocumentExtractionResult();
        }
    }
}
