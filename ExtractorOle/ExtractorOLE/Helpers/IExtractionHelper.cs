using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using System;

namespace ExtractorOLE.Helpers
{
    public interface IExtractionHelper
    {
        string MimeFor(OfficeMimeTypeEnum type);
        OfficeMimeTypeEnum ParseOfficeMimeType(string? mime);

        void ExtractMetadataAndEmbedded(OpenXmlPackage package, OpenXmlPart? rootPart, DocumentExtractionResult result);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart rootPart);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, HSSFWorkbook workbook);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, NPOIFSFileSystem fs);

        OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes);
        MimeDetectionResult DetectMimeType(MimeDetectionRequest request);
        string GetExtensionFromContentType(string contentType);
        string DetectMimeFromMagicBytes(byte[] bytes);
    }
}
