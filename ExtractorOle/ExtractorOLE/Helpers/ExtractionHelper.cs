using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.Registry;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ExtractorOLE.Helpers
{
    public class ExtractionHelper : IExtractionHelper
    {
        private readonly IMimeDetectionRegistry _detectionRegistry;

        public ExtractionHelper() : this(new MimeDetectionRegistry(new IMimeTypeDetector[]
        {
            new OoxmlMimeDetector(),
            new CfbMimeDetector(),
        }))
        {
        }

        // DetectMimeTypeFromBytes resolves its structural checks entirely through this
        // DI-registered registry ([ydk:req:extraction/dependency-injection]) - it never
        // constructs a detection component inline.
        public ExtractionHelper(IMimeDetectionRegistry detectionRegistry)
        {
            _detectionRegistry = detectionRegistry ?? throw new ArgumentNullException(nameof(detectionRegistry));
        }

        public string MimeFor(OfficeMimeTypeEnum type)
        {
            return type switch
            {
                OfficeMimeTypeEnum.Word => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                OfficeMimeTypeEnum.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                OfficeMimeTypeEnum.PowerPoint => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                OfficeMimeTypeEnum.ExcelLegacy => "application/vnd.ms-excel",
                OfficeMimeTypeEnum.WordLegacy => "application/msword",
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
            var containersToScan = new List<OpenXmlPart>();

            // Phase 1: direct children of rootPart.
            foreach (var partPair in rootPart.Parts)
            {
                var nestedPart = partPair.OpenXmlPart;
                if (nestedPart is EmbeddedObjectPart || nestedPart is EmbeddedPackagePart || nestedPart is ImagePart)
                {
                    try
                    {
                        ExtractPartData(nestedPart, result.EmbeddedFiles, ref index, "embedded_object");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Skipping unreadable embedded part '{nestedPart.Uri}': {ex.Message}");
                    }
                }
                else
                {
                    containersToScan.Add(nestedPart);
                }
            }

            // Phase 2: children of every non-matching level-1 part. Some formats (e.g. pptx SlidePart,
            // xlsx WorksheetPart) don't accept embeddings directly on the root part - the OpenXml SDK
            // rejects EmbeddedObjectPart/EmbeddedPackagePart/ImagePart there - so embeddings live one
            // hop down. This scan is generic and unconditional across all container parts.
            foreach (var container in containersToScan)
            {
                foreach (var partPair in container.Parts)
                {
                    var nestedPart = partPair.OpenXmlPart;

                    if (nestedPart is ImagePart)
                    {
                        try
                        {
                            ExtractPartData(nestedPart, result.EmbeddedFiles, ref index, "slide_image");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Skipping unreadable embedded part '{nestedPart.Uri}': {ex.Message}");
                        }
                    }
                    else if (nestedPart is EmbeddedObjectPart || nestedPart is EmbeddedPackagePart)
                    {
                        try
                        {
                            ExtractPartData(nestedPart, result.EmbeddedFiles, ref index, "embedded_object");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Skipping unreadable embedded part '{nestedPart.Uri}': {ex.Message}");
                        }
                    }
                }
            }
        }

        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, HSSFWorkbook workbook)
        {
            int index = 1;

            foreach (HSSFObjectData obj in workbook.GetAllEmbeddedObjects())
            {
                ExtractHssfObjectData(obj, result.EmbeddedFiles, ref index);
            }

            foreach (HSSFPictureData pic in workbook.GetAllPictures())
            {
                ExtractHssfPictureData(pic, result.EmbeddedFiles, ref index);
            }
        }

        // Reads one embedded OLE object. Wrapped in its own try/catch so that one corrupt item
        // (e.g. a POIFS directory entry that doesn't resolve to a real DirectoryEntry) is skipped
        // and logged without failing the whole extraction.
        private void ExtractHssfObjectData(HSSFObjectData obj, List<EmbeddedFileItem> fileList, ref int index)
        {
            try
            {
                byte[] extractedBytes;

                if (obj.HasDirectoryEntry())
                {
                    // Copy the container's directory tree into a standalone POIFS filesystem and
                    // serialize it to bytes. This returns the whole embedded object as one opaque
                    // blob - its internal streams (e.g. "Ole", "Ole10Native") are never unpacked
                    // or listed separately.
                    var target = new NPOIFSFileSystem();
                    try
                    {
                        EntryUtils.CopyNodes(obj.Directory, target.Root);
                        using (var outStream = new MemoryStream())
                        {
                            target.WriteFileSystem(outStream);
                            extractedBytes = outStream.ToArray();
                        }
                    }
                    finally
                    {
                        target.Close();
                    }
                }
                else
                {
                    extractedBytes = obj.ObjectData;
                }

                var item = new EmbeddedFileItem
                {
                    BinaryData = extractedBytes,
                    PackagePath = $"embedded_object_{index}",
                    SizeInBytes = extractedBytes.LongLength,
                    FileName = $"embedded_object_{index}.bin"
                };

                fileList.Add(item);
                index++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping corrupt embedded object at index {index}: {ex.Message}");
            }
        }

        // Reads one inline picture. Wrapped in its own try/catch to isolate one corrupt item from
        // the rest of the extraction, matching ExtractHssfObjectData's per-item behavior.
        private void ExtractHssfPictureData(HSSFPictureData pic, List<EmbeddedFileItem> fileList, ref int index)
        {
            try
            {
                byte[] extractedBytes = pic.Data;
                string extension = pic.SuggestFileExtension();
                string fileName = string.IsNullOrEmpty(extension)
                    ? $"picture_{index}.bin"
                    : $"picture_{index}.{extension}";

                var item = new EmbeddedFileItem
                {
                    BinaryData = extractedBytes,
                    PackagePath = $"picture_{index}",
                    SizeInBytes = extractedBytes.LongLength,
                    FileName = fileName
                };

                fileList.Add(item);
                index++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping corrupt picture at index {index}: {ex.Message}");
            }
        }

        private void ExtractPartData(OpenXmlPart part, List<EmbeddedFileItem> fileList, ref int index, string prefix)
        {
            try
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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Skipping unreadable embedded part '{part.Uri}': {ex.Message}");
            }
        }

        // Dispatches through the DI-registered detection set (IMimeDetectionRegistry):
        // tries every registered structural check (OOXML, then CFB) in order and
        // returns Unknown only when none of them match
        // ([ydk:req:extraction/format-extensibility]).
        public OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException(nameof(fileBytes));
            }

            if (fileBytes.Length == 0)
            {
                return OfficeMimeTypeEnum.OpenXmlUnknown;
            }

            var request = new MimeDetectionRequest { FileBytes = fileBytes };

            foreach (var detector in _detectionRegistry.Detectors)
            {
                var result = detector.Detect(request);
                var mapped = MapDetectedFormat(result.DetectedFormat);
                if (mapped.HasValue)
                {
                    return mapped.Value;
                }
            }

            return OfficeMimeTypeEnum.OpenXmlUnknown;
        }

        // Facade over the DI-registered detection registry: tries every registered
        // structural check (OOXML, then CFB) in registered order and returns the
        // first non-Unknown result, or an Unknown result if none match. Pure
        // orchestration - no format-specific parsing logic here
        // ([ydk:req:extraction/format-extensibility]).
        public MimeDetectionResult DetectMimeType(MimeDetectionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.FileBytes == null)
            {
                throw new ArgumentNullException(nameof(request.FileBytes));
            }

            foreach (var detector in _detectionRegistry.Detectors)
            {
                var result = detector.Detect(request);
                if (result.DetectedFormat != DetectedFormatEnum.Unknown)
                {
                    return result;
                }
            }

            return new MimeDetectionResult
            {
                DetectedFormat = DetectedFormatEnum.Unknown,
                MimeType = "application/octet-stream",
                IsSupported = false,
            };
        }

        // OfficeMimeTypeEnum has two legacy values so far (ExcelLegacy, WordLegacy) - Ppt CFB
        // detections and Unknown still fall through to OpenXmlUnknown here, matching
        // pre-existing dispatch behavior.
        private static OfficeMimeTypeEnum? MapDetectedFormat(DetectedFormatEnum format) => format switch
        {
            DetectedFormatEnum.Docx => OfficeMimeTypeEnum.Word,
            DetectedFormatEnum.Xlsx => OfficeMimeTypeEnum.Excel,
            DetectedFormatEnum.Pptx => OfficeMimeTypeEnum.PowerPoint,
            DetectedFormatEnum.Xls => OfficeMimeTypeEnum.ExcelLegacy,
            DetectedFormatEnum.Doc => OfficeMimeTypeEnum.WordLegacy,
            _ => null,
        };

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
