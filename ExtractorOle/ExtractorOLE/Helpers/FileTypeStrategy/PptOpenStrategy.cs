using ExtractorOLE.Helpers.Input;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.Guardrails;
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

        // Stream input is read lazily and left open; byte[] input is buffered by NPOI (#236).
        private static readonly Func<Stream, NPOIFSFileSystem> DefaultOpenFileSystem = CfbFileSystems.Open;

        private readonly Func<Stream, NPOIFSFileSystem> _openFileSystem;

        private readonly Func<Stream, byte[]> _convert;

        public PptOpenStrategy(IExtractionHelper helper) : this(helper, DefaultOpenFileSystem)
        {
        }

        // Test seam: lets tests count how many times the compound file is opened per Open().
        internal PptOpenStrategy(IExtractionHelper helper, Func<Stream, NPOIFSFileSystem> openFileSystem)
            : this(helper, openFileSystem, PptToPptxConverter.Convert)
        {
        }

        // Test seam: lets tests make the ppt-to-pptx body conversion fail.
        internal PptOpenStrategy(IExtractionHelper helper, Func<Stream, NPOIFSFileSystem> openFileSystem, Func<Stream, byte[]> convert)
        {
            _helper = helper;
            _openFileSystem = openFileSystem;
            _convert = convert;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes) =>
            OpenFrom(new MemoryStream(fileBytes), () => _convert(new MemoryStream(fileBytes)));

        // Reads the caller's seekable stream directly (#194, ADR-008): no byte[] bridge; the caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input) =>
            OpenFrom(new NonClosingStream(input), () => _convert(input));

        private DocumentExtractionResult? OpenFrom(Stream cfbSource, Func<byte[]> convertToPptx)
        {
            DocumentExtractionResult result;
            // One NPOIFSFileSystem over the original .ppt bytes serves both the metadata/macro read
            // and the first-layer embedded walk (each open re-reads the whole compound file).
            // A compound file or property set that cannot be read propagates the library exception
            // so ParseTimeErrorGuard can report the cause (#287).
            NPOIFSFileSystem fs = _openFileSystem(cfbSource);

            try
            {
                result = new DocumentExtractionResult
                {
                    MimeType = _helper.MimeFor(OfficeMimeType.PowerPointLegacy),
                };
                ReadFileMetadata(fs, result);
                // SlideCount/NotesSlideCount are populated below, best-effort, from the
                // converted body; HasMacros is final here since it's sourced from the
                // original file directly, independent of whether body conversion succeeds.
                var formatMetadata = new PowerPointFormatMetadata();
                result.FormatMetadata = formatMetadata;
                FieldIsolation.TryPopulate(result, PowerPointOpenStrategy.LeafPath(nameof(PowerPointFormatMetadata.HasMacros)),
                    () => formatMetadata.HasMacros = fs.Root.HasEntryCaseInsensitive(MacrosStorageName));

                // First-layer embedded objects/media are read directly off the *original* .ppt's raw
                // CFB structure (T-12dfd045), independent of whether body conversion below succeeds.
                FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath(null), () => _helper.ExtractFirstLayerEmbedded(result, fs));
            }
            finally
            {
                fs.Close();
            }

            // Body/slide content is best-effort: b2xtranslator's Ppt module was built against the
            // published MS-PPT binary spec but is unmaintained since 2018 (ADR-004), so a record shape
            // it doesn't handle should not take down the whole extraction -- FileMetadata,
            // FormatMetadata.HasMacros and EmbeddedFiles above are already valid and returned either
            // way; a conversion failure reports FormatMetadata.SlideCount and
            // FormatMetadata.NotesSlideCount as field errors and leaves them at their defaults.
            PopulateBodyContent(convertToPptx, result);

            return result;
        }

        // Populates result.Metadata (each property isolated, see MetadataConversions.Apply).
        private static void ReadFileMetadata(NPOIFSFileSystem fs, DocumentExtractionResult result)
        {
            var propsDoc = new HPSFPropertiesOnlyDocument(fs);

            var summary = propsDoc.SummaryInformation;
            if (summary != null)
            {
                MetadataConversions.Apply(summary, result);
            }
        }

        // Both slide leaves share one conversion: Lazy caches its exception, so a failed
        // conversion is reported once per leaf without converting twice. EmbeddedFiles is
        // populated separately off the original .ppt's CFB, not from the converted pptx.
        private static void PopulateBodyContent(Func<byte[]> convertToPptx, DocumentExtractionResult result)
        {
            var formatMetadata = (PowerPointFormatMetadata)result.FormatMetadata!;
            MemoryStream? pptxStream = null;
            PresentationDocument? pres = null;
            try
            {
                var body = new Lazy<PresentationPart?>(() =>
                {
                    byte[] pptxBytes = convertToPptx();
                    result.ParsedDocument = pptxBytes; // reused by PptTextExtractor (#168)
                    pptxStream = new MemoryStream(pptxBytes);
                    pres = PresentationDocument.Open(pptxStream, false);
                    return pres.PresentationPart;
                });

                FieldIsolation.TryPopulate(result, PowerPointOpenStrategy.LeafPath(nameof(PowerPointFormatMetadata.SlideCount)),
                    () => formatMetadata.SlideCount = body.Value?.SlideParts.Count() ?? 0);
                FieldIsolation.TryPopulate(result, PowerPointOpenStrategy.LeafPath(nameof(PowerPointFormatMetadata.NotesSlideCount)),
                    () => formatMetadata.NotesSlideCount = body.Value?.SlideParts.Count(SlideHasNotesText) ?? 0);
            }
            finally
            {
                pres?.Dispose();
                pptxStream?.Dispose();
            }
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
