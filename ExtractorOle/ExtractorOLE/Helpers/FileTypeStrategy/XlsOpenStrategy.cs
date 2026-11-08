using ExtractorOLE.Helpers.Input;
using ExtractorOLE.DTOs;
using NPOI.HSSF.Record;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class XlsOpenStrategy : IOpenStrategy
    {
        // Standard root-storage name Excel/NPOI use for an embedded VBA project in a
        // legacy (BIFF8) .xls compound file.
        private const string VbaProjectStorageName = "_VBA_PROJECT_CUR";

        private readonly IExtractionHelper _helper;

        public XlsOpenStrategy(IExtractionHelper helper)
        {
            _helper = helper;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes) => OpenFrom(() => new MemoryStream(fileBytes));

        // Reads the caller's seekable stream directly (#194, ADR-008): NPOI reads it block by block without buffering it (#236). The caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input) => OpenFrom(() => new NonClosingStream(input));

        private DocumentExtractionResult? OpenFrom(Func<Stream> openSource)
        {
            HSSFWorkbook workbook;
            try
            {
                workbook = XlsWorkbooks.Load(openSource());
            }
            catch (NPOI.HSSF.OldExcelFormatException)
            {
                // BIFF5 and older: not a workbook this library can open.
                return null;
            }
            catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
            {
                // Encrypted-document exceptions are not open failures: Extract maps them to password-protected.
                // The compound file may still be intact with only the sheet records unreadable.
                return OpenWithoutWorkbook(openSource, ex);
            }

            // Everything past the container open is isolated per leaf; an undeclared failure
            // here is not a parse failure and propagates.
            var result = new DocumentExtractionResult();
            NPOI.HPSF.SummaryInformation? summary = null;
            try
            {
                summary = workbook.SummaryInformation;
            }
            catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
            {
                // Unparsable property set: FileMetadata stays null.
            }
            if (summary != null)
            {
                MetadataConversions.Apply(summary, result);
            }

            result.FormatMetadata = BuildExcelFormatMetadata(workbook, result);

            StrategyIsolation.PopulateEmbedded(result, () => _helper.ExtractFirstLayerEmbedded(result, workbook));
            // Handed to XlsTextExtractor so the workbook is parsed once; MainExtractor releases it.
            result.ParsedDocument = workbook;
            result.MimeType = _helper.MimeFor(OfficeMimeType.ExcelLegacy);
            return result;
        }

        // The container is a readable compound file but the workbook did not assemble (corrupt sheet
        // records): SheetCount fails with the load failure while the sheet names, active sheet and
        // macro flag are still read independently from the workbook globals / directory.
        private DocumentExtractionResult? OpenWithoutWorkbook(Func<Stream> openSource, Exception loadFailure)
        {
            NPOI.POIFS.FileSystem.NPOIFSFileSystem fs;
            try
            {
                // A failed load may have closed or advanced the stream, so the source is opened afresh.
                fs = CfbFileSystems.Open(openSource());
            }
            catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
            {
                return null;
            }

            try
            {
                var root = fs.Root;
                var streamName = root.HasEntryCaseInsensitive("Workbook") ? "Workbook"
                    : root.HasEntryCaseInsensitive("Book") ? "Book" : null;
                if (streamName == null) return null;

                var result = new DocumentExtractionResult();
                NPOI.HPSF.SummaryInformation? summary = null;
                try
                {
                    summary = NPOI.HPSF.PropertySetFactory.Create(root, NPOI.HPSF.SummaryInformation.DEFAULT_STREAM_NAME) as NPOI.HPSF.SummaryInformation;
                }
                catch (Exception ex) when (StrategyIsolation.IsOpenFailure(ex))
                {
                    // Absent or unparsable property set: FileMetadata stays null.
                }
                if (summary != null) MetadataConversions.Apply(summary, result);

                // The globals substream (BoundSheet / WindowOne records) is read once and shared by the leaves that need it.
                var globals = new Lazy<NPOI.HSSF.Model.InternalWorkbook>(() =>
                    NPOI.HSSF.Model.InternalWorkbook.CreateWorkbook(RecordFactory.CreateRecords(root.CreateDocumentInputStream(streamName))));
                var fm = new ExcelFormatMetadata();
                void Leaf(string name, Action populate) => StrategyIsolation.PopulateLeaf(result, name, populate);

                Leaf(nameof(ExcelFormatMetadata.SheetCount), () => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(loadFailure).Throw());
                Leaf(nameof(ExcelFormatMetadata.SheetNames), () =>
                {
                    var names = new List<string>(globals.Value.NumSheets);
                    for (int i = 0; i < globals.Value.NumSheets; i++) names.Add(globals.Value.GetSheetName(i));
                    fm.SheetNames = names;
                });
                Leaf(nameof(ExcelFormatMetadata.ActiveSheetIndex), () =>
                {
                    var window = globals.Value.FindFirstRecordBySid(WindowOneRecord.sid) as WindowOneRecord;
                    fm.ActiveSheetIndex = window?.ActiveSheetIndex;
                });
                Leaf(nameof(ExcelFormatMetadata.HasMacros), () => fm.HasMacros = root.HasEntryCaseInsensitive(VbaProjectStorageName));
                result.FormatMetadata = fm;

                // The embedded walk needs the assembled workbook.
                StrategyIsolation.PopulateEmbedded(result, () => System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(loadFailure).Throw());
                result.MimeType = _helper.MimeFor(OfficeMimeType.ExcelLegacy);
                return result;
            }
            finally
            {
                fs.Close();
            }
        }

        // Each field is its own leaf at FormatMetadata.<Name>; SheetCount and SheetNames are read
        // independently and never reconciled. A failed leaf keeps its default (0 / empty / null / false).
        // ActiveSheetIndex is only populated when the file carries a WindowOneRecord: NPOI synthesizes a
        // default on read, so presence is checked on the record list. HasMacros is true when the root
        // storage contains a VBA project.
        private static ExcelFormatMetadata BuildExcelFormatMetadata(HSSFWorkbook workbook, DocumentExtractionResult result)
        {
            var fm = new ExcelFormatMetadata();

            void Leaf(string name, Action populate) => StrategyIsolation.PopulateLeaf(result, name, populate);

            Leaf(nameof(ExcelFormatMetadata.SheetCount), () => fm.SheetCount = workbook.NumberOfSheets);
            Leaf(nameof(ExcelFormatMetadata.SheetNames), () =>
            {
                // Built locally and assigned only when complete, so a failure leaves the names empty.
                var names = new List<string>(workbook.NumberOfSheets);
                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    names.Add(workbook.GetSheetName(i));
                }
                fm.SheetNames = names;
            });
            Leaf(nameof(ExcelFormatMetadata.ActiveSheetIndex), () =>
            {
                bool hasRecordedActiveSheet = workbook.InternalWorkbook.FindFirstRecordBySid(WindowOneRecord.sid) != null;
                fm.ActiveSheetIndex = hasRecordedActiveSheet ? workbook.ActiveSheetIndex : (int?)null;
            });
            Leaf(nameof(ExcelFormatMetadata.HasMacros), () =>
                fm.HasMacros = workbook.Directory != null && workbook.Directory.HasEntryCaseInsensitive(VbaProjectStorageName));
            return fm;
        }
    }
}
