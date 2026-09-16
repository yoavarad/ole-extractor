using ExtractorOLE.DTOs;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using System;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Legacy .doc (OLE-CFB, Word 97-2003) open strategy.
    //
    // Per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md), NPOI's HWPFDocument (the
    // body-text parser for this format) lives only in NPOI's scratchpad tree, which this
    // project does not self-compile (see third_party/npoi's main-tree-only ProjectReference
    // in ExtractorOLE.csproj). The ADR's accepted replacement for body-text is an in-process
    // .doc->.docx conversion via b2xtranslator, which is not wired into this repo yet.
    //
    // FileMetadata, however, is explicitly NOT blocked by that gap (ADR-004): SummaryInformation
    // / DocumentSummaryInformation (HPSF) and generic CFB directory access (POIFS) both live in
    // NPOI's main tree and are format-agnostic, so this strategy reads them directly off the raw
    // compound file - the same mechanism HSSFWorkbook.SummaryInformation uses internally for
    // .xls, just without a format-specific workbook class to hang it off since none exists here.
    //
    // Body text (ExtractedText) and embedded-object discovery (EmbeddedFiles) are intentionally
    // left at their default empty values until the b2xtranslator conversion path lands.
    public class DocOpenStrategy : IOpenStrategy
    {
        private readonly IExtractionHelper _helper;

        public DocOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            try
            {
                var result = new DocumentExtractionResult();
                using (var ms = new MemoryStream(fileBytes))
                {
                    // NPOIFSFileSystem implements ICloseable, not IDisposable, so it is closed
                    // explicitly in a finally block rather than via a using statement.
                    var fs = new NPOIFSFileSystem(ms);
                    try
                    {
                        var summary = ReadSummaryInformation(fs.Root);
                        if (summary != null)
                        {
                            result.Metadata.Title = summary.Title;
                            result.Metadata.Creator = summary.Author;
                            result.Metadata.Created = summary.CreateDateTime;
                            result.Metadata.Modified = summary.LastSaveDateTime;
                            result.Metadata.LastModifiedBy = summary.LastAuthor;
                        }
                    }
                    finally
                    {
                        fs.Close();
                    }
                }
                result.MimeType = _helper.MimeFor(OfficeMimeTypeEnum.WordLegacy);
                return result;
            }
            catch
            {
                return null;
            }
        }

        // A property-set stream that is absent or unparsable is a normal, expected state for
        // some real-world .doc files (never every field is guaranteed present) - it must leave
        // FileMetadata's fields null rather than fail the whole Open() call, matching this
        // project's "never a placeholder string" convention ([ydk:entity:extraction/FileMetadata]).
        private static SummaryInformation? ReadSummaryInformation(DirectoryEntry root)
        {
            try
            {
                return PropertySetFactory.Create(root, SummaryInformation.DEFAULT_STREAM_NAME) as SummaryInformation;
            }
            catch
            {
                return null;
            }
        }
    }
}
