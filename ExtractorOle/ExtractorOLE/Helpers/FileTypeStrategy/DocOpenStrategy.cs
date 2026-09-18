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
    // First-layer embedded objects (EmbeddedFiles) are populated by walking the "ObjectPool"
    // storage directly off the raw CFB tree ([ydk:req:extraction/subfile-scope]) - this does
    // not depend on the b2xtranslator conversion path, since Word/OLE embedded-object storage
    // is a generic CFB convention (POIFS), not part of the Word body-text binary format. Body
    // text (ExtractedText) is intentionally left at its default empty value until the
    // b2xtranslator conversion path lands.
    //
    // WordFormatMetadata (page/word/character/paragraph/line counts, company/manager/template,
    // HasMacros) is likewise sourced from SummaryInformation/DocumentSummaryInformation (HPSF) -
    // these are the same cached-by-the-producing-app values used for FileMetadata, not
    // recomputed. HasMacros is true when the root storage carries a "Macros" entry, the standard
    // OLE-CFB location for an embedded VBA project in a binary (97-2003) Word document.
    public class DocOpenStrategy : IOpenStrategy
    {
        // Standard root-storage name Word uses for an embedded VBA project in a legacy
        // (binary, 97-2003) .doc compound file.
        private const string VbaMacrosStorageName = "Macros";

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

                        var docSummary = ReadDocumentSummaryInformation(fs.Root);
                        result.FormatMetadata = BuildWordFormatMetadata(fs.Root, summary, docSummary);

                        _helper.ExtractFirstLayerEmbedded(result, fs.Root);
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

        // Same absent-is-normal handling as ReadSummaryInformation, for the second HPSF
        // property-set stream (paragraph/line counts, company/manager).
        private static DocumentSummaryInformation? ReadDocumentSummaryInformation(DirectoryNode root)
        {
            try
            {
                return PropertySetFactory.Create(root, DocumentSummaryInformation.DEFAULT_STREAM_NAME) as DocumentSummaryInformation;
            }
            catch
            {
                return null;
            }
        }

        // Page/word/character counts and Template come from SummaryInformation; paragraph/line
        // counts and Company/Manager come from DocumentSummaryInformation - mirrors the OOXML
        // path's field-to-source mapping (WordOpenStrategy.BuildWordFormatMetadata), just off
        // the legacy CFB property streams instead of app.xml. A count property is left null when
        // its underlying HPSF property is absent (NPOI's Get*IntValue returns 0 for "absent",
        // indistinguishable from a real 0 without checking WasNull right after the read).
        private static WordFormatMetadata BuildWordFormatMetadata(
            DirectoryNode root, SummaryInformation? summary, DocumentSummaryInformation? docSummary)
        {
            return new WordFormatMetadata
            {
                PageCount = ReadNullableInt(summary, s => s.PageCount),
                WordCount = ReadNullableInt(summary, s => s.WordCount),
                CharacterCount = ReadNullableInt(summary, s => s.CharCount),
                ParagraphCount = ReadNullableInt(docSummary, d => d.ParCount),
                LineCount = ReadNullableInt(docSummary, d => d.LineCount),
                Company = docSummary?.Company,
                Manager = docSummary?.Manager,
                Template = summary?.Template,
                HasMacros = root.HasEntryCaseInsensitive(VbaMacrosStorageName),
            };
        }

        private static int? ReadNullableInt<T>(T? propertySet, Func<T, int> selector) where T : PropertySet
        {
            if (propertySet == null)
            {
                return null;
            }

            int value = selector(propertySet);
            return propertySet.WasNull ? (int?)null : value;
        }
    }
}
