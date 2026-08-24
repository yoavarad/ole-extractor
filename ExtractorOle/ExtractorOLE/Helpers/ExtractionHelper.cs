using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace ExtractorOLE.Helpers
{
    public class ExtractionHelper : IExtractionHelper
    {

        public string MimeFor(OfficeMimeTypeEnum type)
        {
            return type switch
            {
                OfficeMimeTypeEnum.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                OfficeMimeTypeEnum.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                OfficeMimeTypeEnum.PowerPoint => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/vnd.openxmlformats-package",
            };
        }        

        public OfficeMimeTypeEnum ParseOfficeMimeType(string? mime)
        {
            if (string.IsNullOrEmpty(mime)) return OfficeMimeTypeEnum.OpenXmlUnknown;
            var m = mime.ToLowerInvariant();

            if (m.Contains("word") || m.Contains("wordprocessingml") || m.Contains("/doc")) return OfficeMimeTypeEnum.Word;
            if (m.Contains("sheet") || m.Contains("spreadsheetml") || m.Contains("excel") || m.Contains("/xl")) return OfficeMimeTypeEnum.Excel;
            if (m.Contains("presentation") || m.Contains("presentationml") || m.Contains("ppt")) return OfficeMimeTypeEnum.PowerPoint;
            if (m.Equals("application/zip") || m.Equals("application/vnd.openxmlformats-package")) return OfficeMimeTypeEnum.OpenXmlUnknown;

            return OfficeMimeTypeEnum.OpenXmlUnknown;
        }

        public void ExtractMetadataAndEmbedded(OpenXmlPackage package, OpenXmlPart? rootPart, DocumentExtractionResult result)
        {
            if (package == null) return;

            var extendedPropsPart = package.GetPartsOfType<ExtendedFilePropertiesPart>().FirstOrDefault();
            if (extendedPropsPart?.Properties != null)
            {
                //todo: add extra metadata fields
                Console.WriteLine("got extended props");
            }

            var corePart = package.GetPartsOfType<CoreFilePropertiesPart>().FirstOrDefault();

            if (corePart?.CoreFileProperties != null)
            {
                var props = corePart.CoreFileProperties;
                result.Metadata.Title = props.Title;
                result.Metadata.Creator = props.Creator;
                result.Metadata.Created = props.Created;
                result.Metadata.Modified = props.Modified;
                result.Metadata.LastModifiedBy = props.LastModifiedBy;

                if (string.IsNullOrEmpty(result.MimeType) && !string.IsNullOrEmpty(corePart.ContentType))
                {
                    result.MimeType = corePart.ContentType;
                }
            }

            if (rootPart != null)
            {
                ExtractFirstLayerEmbedded(result, rootPart);
            }
        }

        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart rootPart)
        {
            int index = 1;
            
            foreach (var partPair in rootPart.Parts)
            {
                //change to check for each file type in a more thorough fashion
                var nestedPart = partPair.OpenXmlPart;
                if (nestedPart is EmbeddedObjectPart || nestedPart is EmbeddedPackagePart || nestedPart is ImagePart)
                {
                    using (var partStream = nestedPart.GetStream())
                    using (var binaryStream = new MemoryStream())
                    {
                        partStream.CopyTo(binaryStream);
                        byte[] extractedBytes = binaryStream.ToArray();

                        var item = new EmbeddedFileItem
                        {
                            //todo: extract extra metadata fields
                            BinaryData = extractedBytes,
                            PackagePath = nestedPart.Uri.ToString(),
                            SizeInBytes = extractedBytes.LongLength
                        };

                        string extension = GetExtensionFromContentType(nestedPart.ContentType);
                        // todo: change to be based on extracted base metadata
                        item.FileName = $"embedded_object_{index}{extension}";
                        result.EmbeddedFiles.Add(item);
                        index++;
                    }
                }                
            }

            // todo: needs to be moved to a PowerPoint strategy 
            // todo: check maybe about extracting background images
            var slideTest = rootPart as PresentationPart;
            if (slideTest != null)
            {
                foreach (SlidePart slidePart in slideTest.SlideParts)
                {
                    // slidePart.Parts tracks the top-level images/objects/layouts belonging directly to THIS slide.
                    // NOTE: PresentationPart cannot hold EmbeddedObjectPart/EmbeddedPackagePart/ImagePart directly
                    // (the OpenXml SDK rejects them there), so SampleGenerator places every first-layer pptx
                    // embedding, image or object, on the slide itself; both kinds must be scanned here.
                    foreach (var slidePartPair in slidePart.Parts)
                    {
                        var nestedSlidePart = slidePartPair.OpenXmlPart;

                        if (nestedSlidePart is ImagePart)
                        {
                            ExtractPartData(nestedSlidePart, result.EmbeddedFiles, ref index, "slide_image");
                        }
                        else if (nestedSlidePart is EmbeddedObjectPart || nestedSlidePart is EmbeddedPackagePart)
                        {
                            ExtractPartData(nestedSlidePart, result.EmbeddedFiles, ref index, "embedded_object");
                        }
                    }
                }
            }
        }

        private void ExtractPartData(OpenXmlPart part, List<EmbeddedFileItem> fileList, ref int index, string prefix)
        {
            using (Stream partStream = part.GetStream())
            using (MemoryStream binaryStream = new MemoryStream())
            {
                partStream.CopyTo(binaryStream);
                byte[] extractedBytes = binaryStream.ToArray();

                var item = new EmbeddedFileItem
                {
                    BinaryData = extractedBytes,
                    PackagePath = part.Uri.ToString(),
                    SizeInBytes = extractedBytes.LongLength,
                    FileName = $"{prefix}_{index}{GetExtensionFromContentType(part.ContentType)}"
                };

                fileList.Add(item);
                index++;
            }
        }

        public OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return OfficeMimeTypeEnum.OpenXmlUnknown;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(fileBytes))
                {
                    using (Package package = Package.Open(stream, FileMode.Open, FileAccess.Read))
                    {
                        foreach (PackageRelationship rel in package.GetRelationships())
                        {
                            string targetUri = rel.TargetUri.ToString();

                            if (targetUri.Contains("word/"))
                                return OfficeMimeTypeEnum.Word;
                            if (targetUri.Contains("xl/"))
                                return OfficeMimeTypeEnum.Excel;
                            if (targetUri.Contains("ppt/"))
                                return OfficeMimeTypeEnum.PowerPoint;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return OfficeMimeTypeEnum.OpenXmlUnknown;
            }

            return OfficeMimeTypeEnum.OpenXmlUnknown;
        }

        public string GetExtensionFromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return ".bin";
            string cType = contentType.ToLowerInvariant();
            if (cType.Contains("pdf")) return ".pdf";
            if (cType.Contains("spreadsheetml") || cType.Contains("excel")) return ".xlsx";
            if (cType.Contains("wordprocessingml")) return ".docx";
            if (cType.Contains("jpeg") || cType.Contains("jpg")) return ".jpg";
            if (cType.Contains("png")) return ".png";
            if (cType.Contains("presentationml")) return ".pptx";
            return ".bin";
        }

        public string DetectMimeFromMagicBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return "application/octet-stream";
            if (bytes.Length >= 4 && bytes[0] == '%' && bytes[1] == 'P' && bytes[2] == 'D' && bytes[3] == 'F') return "application/pdf";
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return "image/png";
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
            if (bytes.Length >= 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K' && (bytes[2] == 3 || bytes[2] == 5 || bytes[2] == 7)) return "application/zip";
            return "application/octet-stream";
        }
    }
}
