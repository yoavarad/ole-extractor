using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.DTOs;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Xml.Linq;

namespace ExtractorOLE.Old.Doc
{
    public class DocExtractor
    {
        public DocumentExtractionResult ExtractFromDocxBinary(byte[] fileBytes)
        {
            var result = new DocumentExtractionResult();

            // Requirement #2: Direct, static IANA MIME-type allocation
            //result.MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            using (MemoryStream ms = new MemoryStream(fileBytes))
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(ms, false))
                {
                    // Requirement #1: Extract standard core metadata
                    result.Metadata = ExtractMetadataSafely(wordDoc);

                    // Requirement #3: Extract structural text
                    if (wordDoc.MainDocumentPart?.Document?.Body != null)
                    {
                        result.ExtractedText = wordDoc.MainDocumentPart.Document.Body.InnerText;
                    }

                    // Requirement #4: Extract ONLY 1st layer embedded files
                    if (wordDoc.MainDocumentPart != null)
                    {
                        int index = 1;

                        // MainDocumentPart.Parts only scans immediate, top-level relations
                        foreach (var partPair in wordDoc.MainDocumentPart.Parts)
                        {
                            var nestedPart = partPair.OpenXmlPart;

                            // Target explicit embedded binaries (OLE objects, packages, or image attachments)
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

                                    // The OpenXML spec masks original client file names. 
                                    // We construct a clean name utilizing the proper structural content-type extension.
                                    string extension = GetExtensionFromContentType(nestedPart.ContentType);
                                    item.FileName = $"embedded_object_{index}{extension}";

                                    result.EmbeddedFiles.Add(item);
                                    index++;
                                }
                            }
                        }
                    }
                }
            }

            return result;
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

        private FileMetadata ExtractMetadataSafely(WordprocessingDocument wordDoc)
        {
            var metadata = new FileMetadata();

            // Look up the standalone XML core properties section directly
            var corePropertiesPart = wordDoc.CoreFilePropertiesPart;

            var extendedProps = wordDoc.ExtendedFilePropertiesPart?.Properties;

            //need actual data
            Console.WriteLine(extendedProps!.Company!.Text);

            if (extendedProps?.Template != null)
            {
                string templateName = extendedProps.Template.Text;
                Console.WriteLine($"Template Name: {templateName}");
            }


            if (corePropertiesPart?.CoreFileProperties != null)
            {
                var props = corePropertiesPart.CoreFileProperties;

                // Grab standard strongly typed metadata nodes natively
                metadata.Title = props.Title;
                metadata.Creator = props.Creator;
                metadata.Created = props.Created;
                metadata.Modified = props.Modified;
                metadata.LastModifiedBy = props.LastModifiedBy;
            }

            return metadata;
        }
    }
}

/*Console.WriteLine(wordDoc.PackageProperties.GetType().Name);
var coreProps = wordDoc.PackageProperties;
result.Metadata.Title = coreProps.Title;
result.Metadata.Creator = coreProps.Creator;
result.Metadata.Created = coreProps.Created;
result.Metadata.Modified = coreProps.Modified;
result.Metadata.LastModifiedBy = coreProps.LastModifiedBy;    */                