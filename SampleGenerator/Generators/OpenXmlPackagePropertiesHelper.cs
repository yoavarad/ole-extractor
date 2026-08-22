using DocumentFormat.OpenXml.Packaging;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Applies <see cref="SampleSpec.Metadata"/> to an OOXML package's core
    /// document properties (Title, Creator, Subject, Keywords, Description,
    /// LastModifiedBy) - the same fields ExtractorOLE's ExtractionHelper reads
    /// back into FileMetadata. Shared by the docx/xlsx/pptx generators since
    /// all three OpenXml document types expose the same PackageProperties API.
    ///
    /// IPackageProperties is currently flagged experimental (OOXML0001) by the
    /// OpenXml SDK itself; suppressed locally since it is the SDK's own
    /// documented way to set these fields and there is no stable alternative
    /// for authoring (as opposed to reading) core properties from scratch.
    /// </summary>
    internal static class OpenXmlPackagePropertiesHelper
    {
#pragma warning disable OOXML0001
        public static void Apply(IPackageProperties properties, IDictionary<string, string> metadata)
        {
            foreach (var pair in metadata)
            {
                switch (pair.Key)
                {
                    case MetadataFields.Title:
                        properties.Title = pair.Value;
                        break;
                    case MetadataFields.Author:
                        properties.Creator = pair.Value;
                        break;
                    case MetadataFields.Subject:
                        properties.Subject = pair.Value;
                        break;
                    case MetadataFields.Keywords:
                        properties.Keywords = pair.Value;
                        break;
                    case MetadataFields.Comments:
                        properties.Description = pair.Value;
                        break;
                    case MetadataFields.LastModifiedBy:
                        properties.LastModifiedBy = pair.Value;
                        break;
                }
            }
        }
#pragma warning restore OOXML0001
    }
}
