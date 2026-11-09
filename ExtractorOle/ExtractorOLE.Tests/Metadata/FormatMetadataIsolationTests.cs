using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using NPOI.HPSF;
using NPOI.HPSF.Wellknown;
using NPOI.HSSF.UserModel;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;

namespace ExtractorOLE.Tests.Metadata
{
    // #307: each Word/Excel FormatMetadata field is its own isolated leaf at FormatMetadata.<Name>.
    public class FormatMetadataIsolationTests
    {
        [Fact]
        public void XlsOpen_CorruptSheetRecordsFixture_LosesSheetCountOnly()
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile("samples/adversarial/corrupt-sheet-records.xls"));

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var error = Assert.Single(result!.FieldErrors, e => e.FieldPath.StartsWith("FormatMetadata.", StringComparison.Ordinal));
            Assert.Equal("FormatMetadata.SheetCount", error.FieldPath);
            var fm = Assert.IsType<ExcelFormatMetadata>(result.FormatMetadata);
            Assert.Equal(0, fm.SheetCount);
            Assert.NotEmpty(fm.SheetNames);
        }

        // Word leaf, whether it lives in DocumentSummaryInformation, and its HPSF property id.
        public static TheoryData<string, bool, int> DocFailingLeaves => new()
        {
            { nameof(WordFormatMetadata.PageCount), false, PropertyIDMap.PID_PAGECOUNT },
            { nameof(WordFormatMetadata.WordCount), false, PropertyIDMap.PID_WORDCOUNT },
            { nameof(WordFormatMetadata.CharacterCount), false, PropertyIDMap.PID_CHARCOUNT },
            { nameof(WordFormatMetadata.ParagraphCount), true, PropertyIDMap.PID_PARCOUNT },
            { nameof(WordFormatMetadata.LineCount), true, PropertyIDMap.PID_LINECOUNT },
        };

        [Theory]
        [MemberData(nameof(DocFailingLeaves))]
        public void DocOpen_OneUnreadableCount_LosesOnlyThatLeaf(string leaf, bool inDocSummary, int pid)
        {
            var wb = new HSSFWorkbook();
            wb.CreateSheet("s");
            wb.CreateInformationProperties();
            wb.SummaryInformation.PageCount = 1;
            wb.SummaryInformation.WordCount = 2;
            wb.SummaryInformation.CharCount = 3;
            wb.DocumentSummaryInformation.ParCount = 4;
            wb.DocumentSummaryInformation.LineCount = 5;
            wb.DocumentSummaryInformation.Company = "Co";
            wb.SummaryInformation.Template = "Tpl";
            var section = inDocSummary ? wb.DocumentSummaryInformation.FirstSection : wb.SummaryInformation.FirstSection;
            section.SetProperty(pid, Variant.VT_LPSTR, "not a number");
            using var ms = new MemoryStream();
            wb.Write(ms);

            var result = new DocOpenStrategy(new ExtractionHelper()).Open(ms.ToArray());

            Assert.NotNull(result);
            var error = Assert.Single(result!.FieldErrors, e => e.FieldPath.StartsWith("FormatMetadata.", StringComparison.Ordinal));
            Assert.Equal($"FormatMetadata.{leaf}", error.FieldPath);
            var fm = Assert.IsType<WordFormatMetadata>(result.FormatMetadata);
            Assert.Null(typeof(WordFormatMetadata).GetProperty(leaf)!.GetValue(fm));
            Assert.Equal("Co", fm.Company);
            Assert.Equal("Tpl", fm.Template);
            var counts = new[] { fm.PageCount, fm.WordCount, fm.CharacterCount, fm.ParagraphCount, fm.LineCount };
            Assert.Equal(4, counts.Count(v => v != null));
        }

        [Fact]
        public void WordOpen_MalformedAppXml_FailsExtendedPropertyLeavesButKeepsHasMacros()
        {
            var bytes = BuildPackageWithMalformedPart(spreadsheet: false);

            var result = new WordOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var paths = result!.FieldErrors.Select(e => e.FieldPath).ToList();
            Assert.Contains("FormatMetadata.PageCount", paths);
            Assert.Contains("FormatMetadata.Template", paths);
            Assert.DoesNotContain("FormatMetadata.HasMacros", paths);
            var fm = Assert.IsType<WordFormatMetadata>(result.FormatMetadata);
            Assert.Null(fm.PageCount);
            Assert.False(fm.HasMacros);
        }

        [Fact]
        public void ExcelOpen_MalformedWorkbook_FailsSheetLeavesIndependentlyAndOpens()
        {
            var bytes = BuildPackageWithMalformedPart(spreadsheet: true);

            var result = new ExcelOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var fm = Assert.IsType<ExcelFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(0, fm.SheetCount);
            Assert.Empty(fm.SheetNames);
            var paths = result.FieldErrors.Select(e => e.FieldPath).ToList();
            Assert.Contains("FormatMetadata.SheetCount", paths);
            Assert.Contains("FormatMetadata.SheetNames", paths);
            Assert.DoesNotContain("FormatMetadata.HasMacros", paths);
        }

        [Fact]
        public void WordOpen_NotAPackage_Throws() =>
            // Container-open failures propagate; ParseTimeErrorGuard maps them to corrupt-file (#287).
            Assert.ThrowsAny<Exception>(() => new WordOpenStrategy(new ExtractionHelper()).Open(new byte[] { 1, 2, 3, 4 }));

        [Fact]
        public void ExcelOpen_NotAPackage_Throws() =>
            // Container-open failures propagate; ParseTimeErrorGuard maps them to corrupt-file (#287).
            Assert.ThrowsAny<Exception>(() => new ExcelOpenStrategy(new ExtractionHelper()).Open(new byte[] { 1, 2, 3, 4 }));

        // A valid package whose workbook.xml (xlsx) / app.xml (docx) is replaced with malformed XML.
        private static byte[] BuildPackageWithMalformedPart(bool spreadsheet)
        {
            using var ms = new MemoryStream();
            string brokenPartPath;
            if (spreadsheet)
            {
                using var doc = SpreadsheetDocument.Create(ms, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
                var wb = doc.AddWorkbookPart();
                wb.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook(new DocumentFormat.OpenXml.Spreadsheet.Sheets());
                brokenPartPath = wb.Uri.OriginalString;
            }
            else
            {
                using var doc = WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
                var main = doc.AddMainDocumentPart();
                main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(new DocumentFormat.OpenXml.Wordprocessing.Body());
                doc.AddExtendedFilePropertiesPart();
                doc.ExtendedFilePropertiesPart!.Properties = new DocumentFormat.OpenXml.ExtendedProperties.Properties();
                brokenPartPath = doc.ExtendedFilePropertiesPart.Uri.OriginalString;
            }

            using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
            {
                var name = brokenPartPath.TrimStart('/');
                zip.GetEntry(name)!.Delete();
                using var w = new StreamWriter(zip.CreateEntry(name).Open());
                w.Write("<not-closed");
            }
            return ms.ToArray();
        }

        private static string FindRepoRootFile(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
        }
    }
}
