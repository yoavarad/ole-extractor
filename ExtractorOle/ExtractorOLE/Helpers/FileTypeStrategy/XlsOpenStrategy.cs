using ExtractorOLE.Helpers.Input;
using ExtractorOLE.DTOs;
using NPOI.HSSF.Record;
using NPOI.HSSF.UserModel;
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

        public DocumentExtractionResult? Open(byte[] fileBytes) => OpenFrom(new MemoryStream(fileBytes));

        // Reads the caller's seekable stream directly (#194, ADR-008): NPOI reads it block by block without buffering it (#236). The caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input) => OpenFrom(new NonClosingStream(input));

        private DocumentExtractionResult? OpenFrom(Stream source)
        {
            try
            {
                var result = new DocumentExtractionResult();
                var workbook = XlsWorkbooks.Load(source);
                var summary = workbook.SummaryInformation;
                if (summary != null)
                {
                    MetadataConversions.Apply(summary, result.Metadata);
                }

                result.FormatMetadata = BuildExcelFormatMetadata(workbook);

                _helper.ExtractFirstLayerEmbedded(result, workbook);
                // Handed to XlsTextExtractor so the workbook is parsed once; MainExtractor releases it.
                result.ParsedDocument = workbook;
                result.MimeType = _helper.MimeFor(OfficeMimeTypeEnum.ExcelLegacy);
                return result;
            }
            catch (NPOI.EncryptedDocumentException)
            {
                // Not a parse failure: Extract maps this to password-protected.
                throw;
            }
            catch
            {
                return null;
            }
        }

        // Sheet count/names come straight off the workbook. ActiveSheetIndex is only
        // populated when the file actually carries a WindowOneRecord (the BIFF record
        // that holds the active-sheet index) - HSSFWorkbook.ActiveSheetIndex always
        // returns a value because NPOI synthesizes a default WindowOneRecord on read
        // when one is missing, so presence must be checked on the underlying record
        // list directly rather than trusting that property alone. HasMacros is true
        // when the compound file's root storage contains a VBA project.
        private static ExcelFormatMetadata BuildExcelFormatMetadata(HSSFWorkbook workbook)
        {
            var sheetNames = new List<string>(workbook.NumberOfSheets);
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                sheetNames.Add(workbook.GetSheetName(i));
            }

            bool hasRecordedActiveSheet = workbook.InternalWorkbook.FindFirstRecordBySid(WindowOneRecord.sid) != null;

            return new ExcelFormatMetadata
            {
                SheetCount = workbook.NumberOfSheets,
                SheetNames = sheetNames,
                ActiveSheetIndex = hasRecordedActiveSheet ? workbook.ActiveSheetIndex : (int?)null,
                HasMacros = workbook.Directory != null && workbook.Directory.HasEntryCaseInsensitive(VbaProjectStorageName),
            };
        }
    }
}
