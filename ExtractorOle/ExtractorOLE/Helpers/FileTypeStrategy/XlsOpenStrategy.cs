using ExtractorOLE.Helpers.Input;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.Encryption.Legacy;
using NPOI.HSSF.Record;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;

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

        public DocumentExtractionResult? Open(byte[] fileBytes) => Open(fileBytes, null);

        // Reads the caller's seekable stream directly (#194, ADR-008): NPOI reads it block by block without buffering it (#236). The caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input) => Open(input, null);

        public DocumentExtractionResult? Open(byte[] fileBytes, string? password) => OpenFrom(() => new MemoryStream(fileBytes), password);

        public DocumentExtractionResult? Open(Stream input, string? password) => OpenFrom(() => new NonClosingStream(input), password);

        private DocumentExtractionResult? OpenFrom(Func<Stream> openSource, string? password)
        {
            HSSFWorkbook workbook;
            try
            {
                workbook = LoadWorkbook(openSource, password);
            }
            catch (Exception ex) when (ex is not NPOI.HSSF.OldExcelFormatException && StrategyIsolation.IsOpenFailure(ex))
            {
                // BIFF5 and older (OldExcelFormatException) and encrypted documents are left to
                // ParseTimeErrorGuard. Anything else the library rejected may leave an intact compound
                // file with only the sheet records unreadable; if it is not, the original failure is rethrown.
                return OpenWithoutWorkbook(openSource, ex);
            }

            // Everything past the container open is isolated per leaf.
            var result = new DocumentExtractionResult();
            // Read from the directory, not workbook.SummaryInformation: NPOI's getter logs and
            // returns null for an unparsable property set, which would hide the failure (#288).
            MetadataConversions.ApplySummaryInformation(workbook.Directory, result);
            ApplyEncryptedProperties(workbook, password, result);

            result.FormatMetadata = BuildExcelFormatMetadata(workbook, result);

            StrategyIsolation.PopulateEmbedded(result, () => _helper.ExtractFirstLayerEmbedded(result, workbook));
            // Handed to XlsTextExtractor so the workbook is parsed once; MainExtractor releases it.
            result.ParsedDocument = workbook;
            result.MimeType = _helper.MimeFor(OfficeMimeType.ExcelLegacy);
            return result;
        }

        // CryptoAPI files that also encrypt their document properties carry no plain SummaryInformation stream;
        // it is read from the "encryption" stream. A failure is reported at Metadata, never fatal.
        private void ApplyEncryptedProperties(HSSFWorkbook workbook, string? password, DocumentExtractionResult result)
        {
            if (!RequestPasswords.HasPassword(password) || !XlsEncryptedProperties.AppliesTo(workbook.Directory)) return;

            var mime = _helper.MimeFor(OfficeMimeType.ExcelLegacy);
            FieldIsolation.TryPopulate(result, nameof(DocumentExtractionResult.Metadata), () =>
            {
                var header = XlsEncryptedProperties.ReadFilePassHeader(workbook.Directory, mime);
                if (header is not null) XlsEncryptedProperties.Apply(workbook.Directory, header, password!, mime, result);
            });
        }

        // NPOI uses a supplied password instead of Excel's default key, with no fallback, so a
        // default-key (VelvetSweatshop) file would reject any password. To ignore a supplied password
        // for it, a rejected password is retried once with the default key; if that also fails, the
        // EncryptedDocumentException propagates and ParseTimeErrorGuard reports INVALID_PASSWORD.
        private static HSSFWorkbook LoadWorkbook(Func<Stream> openSource, string? password)
        {
            if (!RequestPasswords.HasPassword(password)) return XlsWorkbooks.Load(openSource(), null);

            try
            {
                return XlsWorkbooks.Load(openSource(), password);
            }
            catch (NPOI.EncryptedDocumentException)
            {
                return XlsWorkbooks.Load(openSource(), null);
            }
        }

        // The container is a readable compound file but the workbook did not assemble (corrupt sheet
        // records): SheetCount fails with the load failure while the sheet names, active sheet and
        // macro flag are still read independently from the workbook globals / directory.
        private DocumentExtractionResult OpenWithoutWorkbook(Func<Stream> openSource, Exception loadFailure)
        {
            // A failed load may have closed or advanced the stream, so the source is opened afresh.
            // A failure here (including a read fault of the caller's stream) propagates to ParseTimeErrorGuard.
            var fs = CfbFileSystems.Open(openSource());

            try
            {
                var root = fs.Root;
                var streamName = root.HasEntryCaseInsensitive("Workbook") ? "Workbook"
                    : root.HasEntryCaseInsensitive("Book") ? "Book" : null;
                if (streamName == null)
                {
                    ExceptionDispatchInfo.Capture(loadFailure).Throw();
                    return null!; // unreachable
                }

                var result = new DocumentExtractionResult();
                // Absent property set: FileMetadata stays null; unparsable: each leaf is reported.
                MetadataConversions.ApplySummaryInformation(root, result);

                // The globals substream (BoundSheet / WindowOne records) is read once and shared by the leaves that need it.
                var globals = new Lazy<NPOI.HSSF.Model.InternalWorkbook>(() =>
                    NPOI.HSSF.Model.InternalWorkbook.CreateWorkbook(RecordFactory.CreateRecords(root.CreateDocumentInputStream(streamName))));
                var fm = new ExcelFormatMetadata();
                void Leaf(string name, Action populate) => StrategyIsolation.PopulateLeaf(result, name, populate);

                Leaf(nameof(ExcelFormatMetadata.SheetCount), () => ExceptionDispatchInfo.Capture(loadFailure).Throw());
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
                StrategyIsolation.PopulateEmbedded(result, () => ExceptionDispatchInfo.Capture(loadFailure).Throw());
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
