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
    // FileMetadata and PowerPointFormatMetadata.HasMacros are read directly off the *original* .ppt's OLE
    // streams (SummaryInformation/DocumentSummaryInformation and root storage, respectively) via NPOI's
    // HPSF/POIFS (format-agnostic, lives in NPOI's main tree, not blocked by the HSLF gap) rather than
    // from the converted .pptx -- b2xtranslator does not write OOXML docProps on conversion, and does not
    // carry a source VBA project over into the converted package either (same finding ADR-004 documents
    // for the .xls conversion path), so both must come from the source file, not the conversion output.
    public class PptOpenStrategy : IOpenStrategy
    {
        // Standard root-storage name legacy PowerPoint (and Word) binary files use for an embedded
        // VBA project (MS-OVBA), distinct from .xls's own "_VBA_PROJECT_CUR" convention.
        private const string MacrosStorageName = "Macros";

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
                var (metadata, hasMacros) = ReadFileMetadataAndMacroFlag(fileBytes);
                result = new DocumentExtractionResult
                {
                    Metadata = metadata,
                    MimeType = _helper.MimeFor(OfficeMimeTypeEnum.PowerPointLegacy),
                    // SlideCount/NotesSlideCount are populated below, best-effort, from the
                    // converted body; HasMacros is already final here since it's sourced from the
                    // original file directly, independent of whether body conversion succeeds.
                    FormatMetadata = new PowerPointFormatMetadata { HasMacros = hasMacros },
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
            // it doesn't handle should not take down the whole extraction -- FileMetadata,
            // FormatMetadata.HasMacros and EmbeddedFiles above are already valid and returned either
            // way; only SlideCount/NotesSlideCount stay at their defaults (0/null/empty).
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
        // from ReadFileMetadataAndMacroFlag's (HPSF/root-storage) and PopulateBodyContent's
        // (b2xtranslator-owned) readers, matching this method's existing "each step opens what it
        // needs" shape.
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

        private static (FileMetadata Metadata, bool HasMacros) ReadFileMetadataAndMacroFlag(byte[] fileBytes)
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
                    MetadataConversions.Apply(summary, metadata);
                }

                bool hasMacros = fs.Root.HasEntryCaseInsensitive(MacrosStorageName);

                return (metadata, hasMacros);
            }
            finally
            {
                fs.Close();
            }
        }

        private void PopulateBodyContent(byte[] fileBytes, DocumentExtractionResult result)
        {
            byte[] pptxBytes = PptToPptxConverter.Convert(fileBytes);
            using var pptxStream = new MemoryStream(pptxBytes);
            using var pres = PresentationDocument.Open(pptxStream, false);

            // EmbeddedFiles is populated separately, directly off the original .ppt's CFB
            // structure (see ExtractFirstLayerEmbedded above) -- not from this converted
            // pptx, which would misname entries and not exist at all when conversion fails.
            PopulateSlideCounts((PowerPointFormatMetadata)result.FormatMetadata!, pres.PresentationPart);
        }

        // Mirrors PowerPointOpenStrategy.BuildPowerPointFormatMetadata's slide/notes-slide counting
        // (kept as its own copy rather than shared, matching this codebase's existing convention of
        // each open strategy owning its own format-metadata logic) -- but, unlike the pptx path,
        // does not also derive HasMacros here: the converted .pptx never carries a VbaProjectPart
        // (b2xtranslator doesn't convert macro storage), so HasMacros is sourced from the original
        // .ppt's root storage instead, in ReadFileMetadataAndMacroFlag, and left untouched here.
        private static void PopulateSlideCounts(PowerPointFormatMetadata formatMetadata, PresentationPart? presentationPart)
        {
            var slideParts = presentationPart?.SlideParts.ToList() ?? new List<SlidePart>();

            formatMetadata.SlideCount = slideParts.Count;
            formatMetadata.NotesSlideCount = slideParts.Count(SlideHasNotesText);
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
