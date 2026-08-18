using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using System;

namespace ExtractorOLE.Helpers
{
    public interface IExtractionHelper
    {
        string MimeFor(OfficeMimeTypeEnum type);
        OfficeMimeTypeEnum ParseOfficeMimeType(string? mime);

        void ExtractMetadataAndEmbedded(OpenXmlPackage package, OpenXmlPart? rootPart, DocumentExtractionResult result);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart rootPart);

        OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes);
        string GetExtensionFromContentType(string contentType);
        string DetectMimeFromMagicBytes(byte[] bytes);
    }
}
