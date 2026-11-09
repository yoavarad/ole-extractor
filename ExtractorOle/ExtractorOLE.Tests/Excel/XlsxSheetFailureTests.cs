using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExtractorOLE.DTOs;
using ExtractorOLE.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    /// <summary>
    /// #288: one malformed worksheet keeps the other sheets' text and records one FieldError.
    /// </summary>
    public class XlsxSheetFailureTests
    {
        private const string XlsxMime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private static byte[] BuildThreeSheetXlsx()
        {
            using var stream = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();
                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                uint id = 1;
                foreach (var text in new[] { "alpha", "beta", "gamma" })
                {
                    var part = workbookPart.AddNewPart<WorksheetPart>();
                    var cell = new Cell { DataType = CellValues.InlineString, InlineString = new InlineString(new Text(text)) };
                    part.Worksheet = new Worksheet(new SheetData(new Row(cell)));
                    sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(part), SheetId = id, Name = "S" + id });
                    id++;
                }
                workbookPart.Workbook.Save();
            }
            return stream.ToArray();
        }

        // Rewrites the worksheet entry holding "beta" as malformed XML.
        private static byte[] CorruptBetaSheet(byte[] good)
        {
            using var ms = new MemoryStream();
            ms.Write(good);
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
            {
                var entry = zip.Entries.Single(e => e.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal)
                    && ReadAll(e).Contains("beta", StringComparison.Ordinal));
                var name = entry.FullName;
                entry.Delete();
                using var w = zip.CreateEntry(name).Open();
                w.Write(Encoding.UTF8.GetBytes("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row><c t=\"inlineStr\"><is><t>beta"));
            }
            return ms.ToArray();
        }

        private static string ReadAll(ZipArchiveEntry entry)
        {
            using var reader = new StreamReader(entry.Open());
            return reader.ReadToEnd();
        }

        private static DocumentExtractionResult Extract(byte[] bytes, string fileName)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<MainExtractor>().Extract(
                new ExtractionRequest { FileBytes = bytes, FileName = fileName, DetectedMimeType = XlsxMime });
        }

        [Fact]
        public void BadMiddleSheet_KeepsHealthySheetsAndRecordsOneFieldError()
        {
            var result = Extract(CorruptBetaSheet(BuildThreeSheetXlsx()), "bad-sheet.xlsx");

            Assert.Contains("alpha", result.ExtractedText, StringComparison.Ordinal);
            Assert.Contains("gamma", result.ExtractedText, StringComparison.Ordinal);
            Assert.DoesNotContain("beta", result.ExtractedText, StringComparison.Ordinal);
            var error = Assert.Single(result.FieldErrors);
            Assert.Equal("ExtractedText", error.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
        }

        [Fact]
        public void BadMiddleSheet_ByteSinglePath_FailsAsAWhole()
        {
            var bytes = CorruptBetaSheet(BuildThreeSheetXlsx());

            Assert.ThrowsAny<Exception>(() => new XlsxTextExtractor().ExtractText(bytes));
        }

        [Fact]
        public void HealthyThreeSheets_AllTextNoFieldErrors()
        {
            var result = Extract(BuildThreeSheetXlsx(), "ok.xlsx");

            Assert.Contains("beta", result.ExtractedText, StringComparison.Ordinal);
            Assert.Empty(result.FieldErrors);
        }
    }
}
