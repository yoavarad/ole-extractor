using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    [Collection("ConsoleRedirect")]
    public class XlsOpenStrategyTests
    {
        private static byte[] BuildXlsFixtureBytes(string? title, string? author, DateTime? created, DateTime? lastSaved, string? lastAuthor)
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1");
            workbook.CreateInformationProperties();
            if (title != null) workbook.SummaryInformation.Title = title;
            if (author != null) workbook.SummaryInformation.Author = author;
            if (created != null) workbook.SummaryInformation.CreateDateTime = created;
            if (lastSaved != null) workbook.SummaryInformation.LastSaveDateTime = lastSaved;
            if (lastAuthor != null) workbook.SummaryInformation.LastAuthor = lastAuthor;
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        // Builds a fixture with one embedded OLE object (via AddOlePackage + CreateObjectData)
        // and one inline picture shape.
        //
        // NOTE (judgment call, see task plan §5 open questions): HSSFWorkbook.GetAllPictures()
        // returns every picture blip registered in the workbook's drawing group via AddPicture,
        // regardless of whether that blip backs an HSSFObjectData "icon" shape or a standalone
        // Picture shape. CreateObjectData requires a backing picture index for its icon, so
        // giving the icon and the standalone inline picture two DIFFERENT AddPicture byte arrays
        // would make GetAllPictures() report 2 pictures (icon + inline) in addition to the 1
        // embedded object, for 3 EmbeddedFiles total - not what "one embedded object + one inline
        // picture" is meant to exercise. Reusing the same AddPicture index for both the object's
        // icon and the standalone picture shape keeps the fixture at exactly 2 registered items
        // (1 object + 1 picture) without requiring production code to special-case icon-backing
        // pictures, which no acceptance criterion asks for.
        private static byte[] BuildXlsWithEmbeddedObjectAndPicture(byte[] payloadBytes, byte[] pictureBytes)
        {
            var wb = new HSSFWorkbook();
            var sheet = wb.CreateSheet("Sheet1");
            var patriarch = (HSSFPatriarch)sheet.CreateDrawingPatriarch();

            int picIdx = wb.AddPicture(pictureBytes, PictureType.PNG);
            int storageId = wb.AddOlePackage(payloadBytes, "label", "file.bin", "file.bin");

            var objectAnchor = (HSSFClientAnchor)wb.GetCreationHelper().CreateClientAnchor();
            objectAnchor.SetAnchor(1, 1, 0, 0, 2, 5, 0, 0);
            patriarch.CreateObjectData(objectAnchor, storageId, picIdx);

            var pictureAnchor = (HSSFClientAnchor)wb.GetCreationHelper().CreateClientAnchor();
            pictureAnchor.SetAnchor(3, 1, 0, 0, 4, 5, 0, 0);
            patriarch.CreatePicture(pictureAnchor, picIdx);

            using var ms = new MemoryStream();
            wb.Write(ms);
            return ms.ToArray();
        }

        // Builds a fixture with two embedded OLE objects, then corrupts the second one's POIFS
        // directory entry in place: renames the "MBD..." directory out of the way and creates a
        // plain document under the freed name. HasDirectoryEntry() still reports true (the
        // ObjRecord's streamId lives in the BIFF/escher records, untouched by this), but
        // HSSFObjectData.Directory throws IOException at read time because the resolved entry is
        // no longer a DirectoryEntry. See task plan §3.2.
        private static byte[] BuildXlsWithValidAndCorruptEmbeddedObjects(byte[] validPayload)
        {
            var wb = new HSSFWorkbook();
            var sheet = wb.CreateSheet("Sheet1");
            var patriarch = (HSSFPatriarch)sheet.CreateDrawingPatriarch();

            int picIdx = wb.AddPicture(new byte[] { 10, 20, 30, 40, 50 }, PictureType.PNG);

            int storageIdA = wb.AddOlePackage(validPayload, "labelA", "fileA.bin", "fileA.bin");
            var anchorA = (HSSFClientAnchor)wb.GetCreationHelper().CreateClientAnchor();
            anchorA.SetAnchor(1, 1, 0, 0, 2, 5, 0, 0);
            patriarch.CreateObjectData(anchorA, storageIdA, picIdx);

            int storageIdB = wb.AddOlePackage(new byte[] { 1, 2, 3 }, "labelB", "fileB.bin", "fileB.bin");
            var anchorB = (HSSFClientAnchor)wb.GetCreationHelper().CreateClientAnchor();
            anchorB.SetAnchor(3, 1, 0, 0, 4, 5, 0, 0);
            patriarch.CreateObjectData(anchorB, storageIdB, picIdx);

            byte[] wbBytes;
            using (var ms = new MemoryStream())
            {
                wb.Write(ms);
                wbBytes = ms.ToArray();
            }

            string corruptName = "MBD" + HexDump.ToHex(storageIdB);
            var raw = new NPOIFSFileSystem(new MemoryStream(wbBytes));
            var entry = raw.Root.GetEntry(corruptName);
            entry.RenameTo(corruptName + "_orig");
            raw.Root.CreateDocument(corruptName, new MemoryStream(new byte[] { 0x00 }));

            using var corruptOut = new MemoryStream();
            raw.WriteFileSystem(corruptOut);
            raw.Close();
            return corruptOut.ToArray();
        }

        // Reads back the payload bytes wrapped by AddOlePackage from a copied-out POIFS blob,
        // mirroring how NPOI's own TestOLE2Embeding does it (Ole10Native.CreateFromEmbeddedOleObject).
        private static byte[] ReadOlePackagePayload(byte[] copiedDirectoryBytes)
        {
            var fs = new NPOIFSFileSystem(new MemoryStream(copiedDirectoryBytes));
            var ole10 = Ole10Native.CreateFromEmbeddedOleObject(fs.Root);
            var data = ole10.DataBuffer;
            fs.Close();
            return data;
        }

        // Builds a multi-sheet workbook, explicitly marks one sheet active (writes a
        // WindowOneRecord), then post-processes the raw CFB to add a "_VBA_PROJECT_CUR"
        // storage entry at the root - the standard marker NPOI/Excel use for an embedded
        // VBA project in a legacy .xls compound file.
        private static byte[] BuildMacroEnabledMultiSheetXlsFixtureBytes(string[] sheetNames, int activeSheetIndex)
        {
            var wb = new HSSFWorkbook();
            foreach (var name in sheetNames)
            {
                wb.CreateSheet(name);
            }
            wb.SetActiveSheet(activeSheetIndex);

            byte[] wbBytes;
            using (var ms = new MemoryStream())
            {
                wb.Write(ms);
                wbBytes = ms.ToArray();
            }

            var raw = new NPOIFSFileSystem(new MemoryStream(wbBytes));
            raw.Root.CreateDirectory("_VBA_PROJECT_CUR");
            using var outStream = new MemoryStream();
            raw.WriteFileSystem(outStream);
            raw.Close();
            return outStream.ToArray();
        }

        // Builds a multi-sheet workbook with no macro storage, then removes the
        // WindowOneRecord (the BIFF record that carries the active-sheet index) from the
        // in-memory record list before serializing - simulating a workbook that never had
        // an active sheet recorded, per NPOI's own read-side fallback (InternalWorkbook
        // synthesizes a default WindowOneRecord, distinct from the file, when none is found).
        private static byte[] BuildNonMacroMultiSheetXlsFixtureBytesWithoutActiveSheetRecord(string[] sheetNames)
        {
            var wb = new HSSFWorkbook();
            foreach (var name in sheetNames)
            {
                wb.CreateSheet(name);
            }

            var internalWorkbook = wb.InternalWorkbook;
            var windowOneRecord = internalWorkbook.FindFirstRecordBySid(NPOI.HSSF.Record.WindowOneRecord.sid);
            internalWorkbook.Records.Remove(windowOneRecord);

            using var ms = new MemoryStream();
            wb.Write(ms);
            return ms.ToArray();
        }

        [Fact]
        public void Open_MacroEnabledMultiSheetXlsFixture_PopulatesExcelFormatMetadata()
        {
            var sheetNames = new[] { "Sheet1", "Data", "Summary" };
            var bytes = BuildMacroEnabledMultiSheetXlsFixtureBytes(sheetNames, activeSheetIndex: 1);

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<ExcelFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(3, formatMetadata.SheetCount);
            Assert.Equal(sheetNames, formatMetadata.SheetNames);
            Assert.Equal(1, formatMetadata.ActiveSheetIndex);
            Assert.True(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_NonMacroMultiSheetXlsFixtureWithoutActiveSheetRecord_PopulatesExcelFormatMetadataWithNullActiveSheetAndNoMacros()
        {
            var sheetNames = new[] { "Sheet1", "Data", "Summary" };
            var bytes = BuildNonMacroMultiSheetXlsFixtureBytesWithoutActiveSheetRecord(sheetNames);

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<ExcelFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(3, formatMetadata.SheetCount);
            Assert.Equal(sheetNames, formatMetadata.SheetNames);
            Assert.Null(formatMetadata.ActiveSheetIndex);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_KnownXlsFixture_PopulatesFileMetadataFromSummaryInformation()
        {
            var created = new DateTime(2019, 3, 14, 9, 0, 0, DateTimeKind.Utc);
            var lastSaved = new DateTime(2020, 6, 1, 17, 30, 0, DateTimeKind.Utc);
            var bytes = BuildXlsFixtureBytes("Quarterly Report", "Jane Doe", created, lastSaved, null);

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Equal("Quarterly Report", result!.Metadata.Title);
            Assert.Equal("Jane Doe", result.Metadata.Creator);
            // NPOI's Filetime round-trips DateTime via DateTime.ToFileTime()/FromFileTime(),
            // which returns DateTimeKind.Local; normalize to UTC to compare the same instant
            // regardless of the executing machine's local timezone offset.
            Assert.Equal(created, result.Metadata.Created!.Value.ToUniversalTime());
            Assert.Equal(lastSaved, result.Metadata.Modified!.Value.ToUniversalTime());
            Assert.Null(result.Metadata.LastModifiedBy);
            Assert.Equal("application/vnd.ms-excel", result.MimeType);
        }

        [Fact]
        public void Open_CorruptBytes_ThrowsTheLibraryParseException()
        {
            // #287: the parse exception propagates so Extract can carry it as InnerException.
            Assert.ThrowsAny<Exception>(() => new XlsOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 }));
        }

        private static readonly string[] SummaryLeafPaths =
        {
            "Metadata.Title", "Metadata.Creator", "Metadata.Created", "Metadata.Modified",
            "Metadata.LastModifiedBy", "Metadata.Subject", "Metadata.Comments", "Metadata.RevisionNumber",
            "Metadata.LastPrinted", "Metadata.EditingDurationMinutes", "Metadata.Keywords", "Metadata.ApplicationName",
        };

        // Replaces the SummaryInformation stream with replacement bytes, or removes it when null.
        private static byte[] WithSummaryStream(byte[] xlsBytes, byte[]? replacement)
        {
            const string streamName = "\u0005SummaryInformation";
            var fs = new NPOIFSFileSystem(new MemoryStream(xlsBytes));
            if (fs.Root.HasEntry(streamName)) fs.Root.GetEntry(streamName).Delete();
            if (replacement != null) fs.Root.CreateDocument(streamName, new MemoryStream(replacement));
            using var output = new MemoryStream();
            fs.WriteFileSystem(output);
            fs.Close();
            return output.ToArray();
        }

        [Fact]
        public void Open_GarbledSummaryInformation_ReportsEveryMetadataLeafAsCorruptAndKeepsWorkbook()
        {
            var garbled = new byte[64];
            Array.Fill(garbled, (byte)0xFF);
            var bytes = WithSummaryStream(BuildXlsFixtureBytes("t", "a", null, null, null), garbled);

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var metadataErrors = result!.FieldErrors.Where(e => e.FieldPath.StartsWith("Metadata.", StringComparison.Ordinal)).ToList();
            Assert.Equal(SummaryLeafPaths.OrderBy(p => p, StringComparer.Ordinal), metadataErrors.Select(e => e.FieldPath).OrderBy(p => p, StringComparer.Ordinal));
            Assert.All(metadataErrors, e => Assert.Equal(FieldErrorCode.CorruptFile, e.Code));
            Assert.Equal(1, ((ExcelFormatMetadata)result.FormatMetadata!).SheetCount);
            Assert.NotNull(result.ParsedDocument);
        }

        [Fact]
        public void Open_NoSummaryInformationStream_RecordsNoMetadataFieldErrors()
        {
            var bytes = WithSummaryStream(BuildXlsFixtureBytes("t", "a", null, null, null), null);

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.DoesNotContain(result!.FieldErrors, e => e.FieldPath.StartsWith("Metadata.", StringComparison.Ordinal));
            Assert.Equal(1, ((ExcelFormatMetadata)result.FormatMetadata!).SheetCount);
        }

        // A registered picture (no shape on the sheet) whose BLIP record header declares an absurd
        // length: the workbook still loads, but decoding the drawing group fails when the pictures
        // are enumerated.
        private static byte[] BuildXlsWithCorruptPictureBlip()
        {
            var pictureBytes = new byte[64];
            for (int i = 0; i < pictureBytes.Length; i++) pictureBytes[i] = (byte)(i + 1);
            var wb = new HSSFWorkbook();
            wb.CreateSheet("Sheet1");
            wb.AddPicture(pictureBytes, PictureType.PNG);
            using var written = new MemoryStream();
            wb.Write(written);

            var fs = new NPOIFSFileSystem(new MemoryStream(written.ToArray()));
            byte[] book;
            using (var stream = fs.Root.CreateDocumentInputStream("Workbook"))
            using (var copy = new MemoryStream())
            {
                stream.CopyTo(copy);
                book = copy.ToArray();
            }

            // The blip header (options, record id 0xF01E for PNG, 4-byte length) sits just before the payload.
            int payloadAt = book.AsSpan().IndexOf(pictureBytes.AsSpan(0, 16));
            Assert.True(payloadAt > 0, "picture payload not found in the Workbook stream");
            int headerAt = -1;
            for (int i = payloadAt - 40; i < payloadAt; i++)
            {
                if (book[i] == 0x1E && book[i + 1] == 0xF0) headerAt = i - 2;
            }
            Assert.True(headerAt > 0, "blip header not found in the Workbook stream");
            book[headerAt + 4] = 0xFF;
            book[headerAt + 5] = 0xFF;
            book[headerAt + 6] = 0xFF;
            book[headerAt + 7] = 0x7F;

            fs.Root.GetEntry("Workbook").Delete();
            fs.Root.CreateDocument("Workbook", new MemoryStream(book));
            using var output = new MemoryStream();
            fs.WriteFileSystem(output);
            fs.Close();
            return output.ToArray();
        }

        [Fact]
        public void Open_PicturesEnumerationFaults_RecordsBareEmbeddedFilesErrorsAndKeepsWorkbook()
        {
            var bytes = BuildXlsWithCorruptPictureBlip();

            var originalOut = Console.Out;
            Console.SetOut(new StringWriter());
            DocumentExtractionResult? result;
            try
            {
                result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.NotNull(result);
            Assert.Empty(result!.EmbeddedFiles);
            // Both enumeration loops (objects, then pictures) fault on the shared drawing group, one bare error each.
            var errors = result.FieldErrors.Where(e => e.FieldPath == "EmbeddedFiles").ToList();
            Assert.Equal(2, errors.Count);
            Assert.All(errors, e => Assert.Equal(FieldErrorCode.CorruptFile, e.Code));
            Assert.Equal(1, ((ExcelFormatMetadata)result.FormatMetadata!).SheetCount);
        }

        [Fact]
        public void ServiceRegistration_ResolvesXlsOpenStrategyViaDiRegistryForExcelLegacy()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();

            var dict = provider.GetRequiredService<IReadOnlyDictionary<OfficeMimeType, IOpenStrategy>>();

            Assert.True(dict.ContainsKey(OfficeMimeType.ExcelLegacy));
            Assert.IsType<XlsOpenStrategy>(dict[OfficeMimeType.ExcelLegacy]);
        }

        [Fact]
        public void Open_XlsWithEmbeddedObjectAndPicture_PopulatesEmbeddedFiles()
        {
            var payloadBytes = new byte[] { 0x50, 0x41, 0x59, 0x4C, 0x4F, 0x41, 0x44, 0x21 }; // "PAYLOAD!"
            var pictureBytes = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77 };
            var bytes = BuildXlsWithEmbeddedObjectAndPicture(payloadBytes, pictureBytes);

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            // AC #2: the container object is one opaque item - its internal POIFS children
            // ("Ole", "Ole10Native") must never be separately listed.
            Assert.Equal(2, result!.EmbeddedFiles.Count);

            var pictureItem = result.EmbeddedFiles.SingleOrDefault(f => f.BinaryData.SequenceEqual(pictureBytes));
            Assert.NotNull(pictureItem);
            Assert.Equal(pictureBytes.LongLength, pictureItem!.SizeInBytes);
            Assert.False(string.IsNullOrEmpty(pictureItem.FileName));

            var objectItem = result.EmbeddedFiles.SingleOrDefault(f => f != pictureItem);
            Assert.NotNull(objectItem);
            Assert.Equal(objectItem!.BinaryData.LongLength, objectItem.SizeInBytes);
            Assert.False(string.IsNullOrEmpty(objectItem.FileName));

            // The object's binary data must be the whole copied-out POIFS directory (opaque
            // blob), not just the raw payload - verify the original payload is recoverable
            // from within it, the same way NPOI's own tests unwrap an AddOlePackage payload.
            var recoveredPayload = ReadOlePackagePayload(objectItem.BinaryData);
            Assert.Equal(payloadBytes, recoveredPayload);

            // Not recursively unpacked: no item should be named after the internal POIFS
            // streams that live inside the container ("Ole", "Ole10Native").
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName.Contains("Ole10Native") || f.FileName == "Ole");
        }

        [Fact]
        public void Open_OneValidOneCorruptEmbeddedObject_OmitsCorruptAndLogsWithoutThrowing()
        {
            var validPayload = new byte[] { 0x56, 0x41, 0x4C, 0x49, 0x44 }; // "VALID"
            var bytes = BuildXlsWithValidAndCorruptEmbeddedObjects(validPayload);

            var originalOut = Console.Out;
            var capturedOut = new StringWriter();
            Console.SetOut(capturedOut);

            DocumentExtractionResult? result;
            try
            {
                result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.NotNull(result);
            // Deviation from plan §4's literal "Count == 1": HSSFPatriarch.CreateObjectData
            // requires a real registered picture index for its icon (it indexes into
            // GetAllPictures() internally and throws otherwise), so any fixture with a valid
            // embedded object necessarily also registers one picture, which the read side
            // (GetAllPictures(), per plan §2.1) legitimately reports as its own first-layer
            // item. The corrupt-omission behavior this test targets is about embedded OBJECTS,
            // not pictures, so we assert on the object/picture split directly instead of a raw
            // count of 1.
            Assert.Equal(2, result!.EmbeddedFiles.Count);

            var objectItems = result.EmbeddedFiles.Where(f => f.FileName.StartsWith("embedded_object_", StringComparison.Ordinal)).ToList();
            var pictureItems = result.EmbeddedFiles.Where(f => f.FileName.StartsWith("picture_", StringComparison.Ordinal)).ToList();
            Assert.Single(objectItems);
            Assert.Single(pictureItems);

            var recoveredPayload = ReadOlePackagePayload(objectItems[0].BinaryData);
            Assert.Equal(validPayload, recoveredPayload);

            Assert.StartsWith("EmbeddedFiles[embedded_object_", Assert.Single(result.FieldErrors).FieldPath);
            Assert.Equal(string.Empty, capturedOut.ToString());
        }
    }
}
