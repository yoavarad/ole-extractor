using B2xPresentationDocument = b2xtranslator.OpenXmlLib.PresentationML.PresentationDocument;
using b2xtranslator.PptFileFormat;
using b2xtranslator.PresentationMLMapping;
using b2xtranslator.StructuredStorage.Reader;
using System;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Converts legacy .ppt bytes to .pptx bytes in-process via b2xtranslator's Ppt module
    // (ADR-004: docs/adrs/004-legacy-doc-ppt-parsing.md). Shared by PptOpenStrategy (slide-count
    // and embedded-file discovery) and PptTextExtractor (flat body text) so both read the same
    // converted package through the project's existing DocumentFormat.OpenXml path.
    //
    // b2xtranslator's Pptx writer only targets a file path, so the conversion goes through a temp
    // file that is always deleted before returning. Throws on any conversion failure -- callers
    // decide whether that is fatal (PptOpenStrategy: best-effort, PptTextExtractor: empty text).
    internal static class PptToPptxConverter
    {
        public static byte[] Convert(byte[] pptBytes)
        {
            string tempPptxPath = Path.Combine(Path.GetTempPath(), $"ole-extractor-ppt2x-{Guid.NewGuid():N}.pptx");
            try
            {
                using (var reader = new StructuredStorageReader(new MemoryStream(pptBytes)))
                {
                    var ppt = new PowerpointDocument(reader);
                    var outType = Converter.DetectOutputType(ppt);

                    // No `using` here: Converter.Convert disposes (closes) the package itself, and a
                    // second Close() rewrites every zip entry a second time, producing a package
                    // System.IO.Packaging rejects as malformed.
                    var pptx = B2xPresentationDocument.Create(tempPptxPath, outType);
                    Converter.Convert(ppt, pptx);
                }

                return File.ReadAllBytes(tempPptxPath);
            }
            finally
            {
                if (File.Exists(tempPptxPath))
                {
                    File.Delete(tempPptxPath);
                }
            }
        }
    }
}
