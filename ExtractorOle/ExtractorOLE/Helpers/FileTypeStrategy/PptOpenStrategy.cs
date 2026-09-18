using B2xPresentationDocument = b2xtranslator.OpenXmlLib.PresentationML.PresentationDocument;
using b2xtranslator.OpenXmlLib;
using b2xtranslator.PptFileFormat;
using b2xtranslator.PresentationMLMapping;
using b2xtranslator.StructuredStorage.Reader;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Legacy .ppt has no working NPOI HSLF entry point (ADR-004: docs/adrs/004-legacy-doc-ppt-parsing.md
    // -- HSLF exists only as incomplete scratchpad Model/Record classes, no "open file" glue). Body/slide
    // content is instead converted in-process to a real .pptx via b2xtranslator's Ppt module, then read
    // through the project's existing DocumentFormat.OpenXml pptx path (same as PowerPointOpenStrategy).
    //
    // FileMetadata is read directly off the *original* .ppt's OLE SummaryInformation/DocumentSummaryInformation
    // via NPOI's HPSF (format-agnostic, lives in NPOI's main tree, not blocked by the HSLF gap) rather than
    // from the converted .pptx -- b2xtranslator does not write OOXML docProps on conversion (same finding
    // ADR-004 documents for the .xls conversion path), so title/author/etc must come from the source file.
    public class PptOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public PptOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            DocumentExtractionResult result;
            try
            {
                result = new DocumentExtractionResult
                {
                    Metadata = ReadFileMetadata(fileBytes),
                    MimeType = _helper.MimeFor(OfficeMimeTypeEnum.PowerPointLegacy),
                };
            }
            catch
            {
                // Not a readable compound file at all -- nothing to open.
                return null;
            }

            // First-layer embedded objects/media are read directly off the *original* .ppt's raw
            // CFB structure (T-12dfd045), independent of whether body conversion below succeeds --
            // same reasoning as FileMetadata above: a walk over POIFS directory entries doesn't
            // depend on b2xtranslator's Ppt module understanding the file's body records.
            try
            {
                ExtractFirstLayerEmbedded(fileBytes, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ppt first-layer embedded walk failed: {ex.Message}");
            }

            // Body/slide content is best-effort: b2xtranslator's Ppt module was built against the
            // published MS-PPT binary spec but is unmaintained since 2018 (ADR-004), so a record shape
            // it doesn't handle should not take down the whole extraction -- FileMetadata above is
            // already valid and returned either way; only FormatMetadata stays at its default.
            try
            {
                PopulateBodyContent(fileBytes, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ppt body conversion failed, returning metadata-only result: {ex.Message}");
            }

            return result;
        }

        // Opens its own short-lived NPOIFSFileSystem over the original .ppt bytes -- separate
        // from ReadFileMetadata's (HPSF-only) and PopulateBodyContent's (b2xtranslator-owned)
        // readers, matching this method's existing "each step opens what it needs" shape.
        private void ExtractFirstLayerEmbedded(byte[] fileBytes, DocumentExtractionResult result)
        {
            var fs = new NPOIFSFileSystem(new MemoryStream(fileBytes));
            try
            {
                _helper.ExtractFirstLayerEmbedded(result, fs);
            }
            finally
            {
                fs.Close();
            }
        }

        private static FileMetadata ReadFileMetadata(byte[] fileBytes)
        {
            // NPOIFSFileSystem takes ownership of the stream it's given and closes it as part
            // of its own Close() (mirrors XlsOpenStrategyTests' raw-POIFS fixture helpers) -- no
            // separate `using` on the MemoryStream, which would double-dispose it.
            var fs = new NPOIFSFileSystem(new MemoryStream(fileBytes));
            try
            {
                var propsDoc = new HPSFPropertiesOnlyDocument(fs);

                var metadata = new FileMetadata();
                var summary = propsDoc.SummaryInformation;
                if (summary != null)
                {
                    metadata.Title = summary.Title;
                    metadata.Creator = summary.Author;
                    metadata.Created = summary.CreateDateTime;
                    metadata.Modified = summary.LastSaveDateTime;
                    metadata.LastModifiedBy = summary.LastAuthor;
                }

                return metadata;
            }
            finally
            {
                fs.Close();
            }
        }

        private void PopulateBodyContent(byte[] fileBytes, DocumentExtractionResult result)
        {
            string tempPptxPath = Path.Combine(Path.GetTempPath(), $"ole-extractor-ppt2x-{Guid.NewGuid():N}.pptx");
            try
            {
                using (var reader = new StructuredStorageReader(new MemoryStream(fileBytes)))
                {
                    var ppt = new PowerpointDocument(reader);
                    var outType = Converter.DetectOutputType(ppt);

                    using var pptx = B2xPresentationDocument.Create(tempPptxPath, outType);
                    Converter.Convert(ppt, pptx);
                }

                byte[] pptxBytes = File.ReadAllBytes(tempPptxPath);
                using var pptxStream = new MemoryStream(pptxBytes);
                using var pres = PresentationDocument.Open(pptxStream, false);

                // EmbeddedFiles is populated separately, directly off the original .ppt's CFB
                // structure (see ExtractFirstLayerEmbedded above) -- not from this converted
                // pptx, which would misname entries and not exist at all when conversion fails.
                result.FormatMetadata = BuildPowerPointFormatMetadata(pres.PresentationPart);
            }
            finally
            {
                if (File.Exists(tempPptxPath))
                {
                    File.Delete(tempPptxPath);
                }
            }
        }

        // Mirrors PowerPointOpenStrategy.BuildPowerPointFormatMetadata: kept as its own copy
        // rather than shared, matching this codebase's existing convention of each open
        // strategy (Excel vs Xls, and now PowerPoint vs Ppt) owning its own format-metadata
        // builder instead of a shared OOXML/legacy helper.
        private static PowerPointFormatMetadata BuildPowerPointFormatMetadata(PresentationPart? presentationPart)
        {
            var slideParts = presentationPart?.SlideParts.ToList() ?? new List<SlidePart>();

            return new PowerPointFormatMetadata
            {
                SlideCount = slideParts.Count,
                NotesSlideCount = slideParts.Count(SlideHasNotesText),
                HasMacros = presentationPart != null && presentationPart.GetPartsOfType<DocumentFormat.OpenXml.Packaging.VbaProjectPart>().Any(),
            };
        }

        private static bool SlideHasNotesText(SlidePart slidePart)
        {
            var notesSlide = slidePart.NotesSlidePart?.NotesSlide;
            if (notesSlide == null) return false;

            return notesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                .Any(t => !string.IsNullOrWhiteSpace(t.Text));
        }
    }
}
