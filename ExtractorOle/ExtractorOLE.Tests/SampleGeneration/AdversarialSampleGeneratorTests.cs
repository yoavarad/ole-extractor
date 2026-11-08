using System;
using System.IO;
using System.Linq;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using NPOI.HPSF;
using NPOI.HSSF.Model;
using NPOI.HSSF.Record;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.SampleGeneration
{
    // Legacy per-field-isolation fixtures (docs/specs/extraction.md, ydk:req:extraction/per-field-error-isolation).
    // Each file must open as its format and show exactly one failing raw read beside otherwise-readable
    // content. The reads here go straight to NPOI/HPSF/POIFS, not through Extract(). Every test also
    // asserts the committed samples/adversarial artifact is byte-identical to the generator output.
    public class AdversarialSampleGeneratorTests
    {
        private static byte[] Committed(string fileName)
        {
            var relative = Path.Combine("samples", "adversarial", fileName);
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
            }
            throw new FileNotFoundException(relative);
        }

        [Fact]
        public void CorruptMetadataProperty_OpensAsDoc_AndOnlyCreatedIsUnreadable()
        {
            var bytes = AdversarialSampleGenerator.BuildCorruptMetadataPropertyDoc();
            Assert.Equal(bytes, Committed("corrupt-metadata-property.doc"));
            Assert.Equal(OfficeMimeType.WordLegacy, new ExtractionHelper().DetectMimeTypeFromBytes(bytes));

            using var fs = new NPOIFSFileSystem(new MemoryStream(bytes));
            var summary = Assert.IsType<SummaryInformation>(
                PropertySetFactory.Create(fs.Root, SummaryInformation.DEFAULT_STREAM_NAME));

            Assert.Equal("Adversarial title", summary.Title);
            Assert.Equal("Adversarial author", summary.Author);
            Assert.Equal("Adversarial subject", summary.Subject);
            Assert.Equal("adversarial, corrupt", summary.Keywords);
            Assert.Equal("Adversarial comments", summary.Comments);
            Assert.Equal("Adversarial editor", summary.LastAuthor);
            Assert.Equal("3", summary.RevNumber);
            Assert.NotNull(summary.LastSaveDateTime);
            Assert.ThrowsAny<Exception>(() => summary.CreateDateTime);
        }

        [Fact]
        public void CorruptSheetRecords_OpensAsXls_SheetNamesReadableButSheetsCannotBeAssembled()
        {
            var bytes = AdversarialSampleGenerator.BuildCorruptSheetRecordsXls();
            Assert.Equal(bytes, Committed("corrupt-sheet-records.xls"));
            Assert.Equal(OfficeMimeType.ExcelLegacy, new ExtractionHelper().DetectMimeTypeFromBytes(bytes));

            // Sheet count (the parsed sheets) cannot be read: the workbook does not assemble.
            Assert.ThrowsAny<Exception>(() => new HSSFWorkbook(new MemoryStream(bytes)));

            // Sheet names live in the workbook-globals BoundSheet records, which stay readable.
            using var fs = new NPOIFSFileSystem(new MemoryStream(bytes));
            var records = RecordFactory.CreateRecords(fs.Root.CreateDocumentInputStream("Workbook"));
            var globals = InternalWorkbook.CreateWorkbook(records);
            Assert.Equal("Sheet1", globals.GetSheetName(0));
        }

        [Fact]
        public void UnnamedEmbeddedStorage_OpensAsDoc_OnlyTheUnnamedObjectPoolItemFails()
        {
            var bytes = AdversarialSampleGenerator.BuildUnnamedEmbeddedStorageDoc();
            Assert.Equal(bytes, Committed("unnamed-embedded-storage.doc"));
            Assert.Equal(OfficeMimeType.WordLegacy, new ExtractionHelper().DetectMimeTypeFromBytes(bytes));

            using var fs = new NPOIFSFileSystem(new MemoryStream(bytes));
            var pool = Assert.IsAssignableFrom<DirectoryEntry>(fs.Root.GetEntry("ObjectPool"));
            var items = pool.ToList();
            Assert.Equal(2, items.Count);

            var good = items.Single(e => e.Name == "_1");
            Assert.IsAssignableFrom<DirectoryEntry>(good);

            var unnamed = items.Single(e => e.Name != "_1");
            Assert.Equal(string.Empty, unnamed.Name);
            Assert.IsNotAssignableFrom<DirectoryEntry>(unnamed);

            var result = DocSampleGeneratorTests.Extract(bytes, OfficeMimeType.WordLegacy);
            var item = Assert.Single(result.EmbeddedFiles);
            Assert.Equal("ObjectPool/_1", item.PackagePath);
        }
    }
}
