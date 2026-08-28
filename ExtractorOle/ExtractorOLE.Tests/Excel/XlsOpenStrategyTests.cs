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
        public void Open_CorruptBytes_ReturnsNull()
        {
            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.Null(result);
        }

        [Fact]
        public void ServiceRegistration_ResolvesXlsOpenStrategyViaDiRegistryForExcelLegacy()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();

            var dict = provider.GetRequiredService<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>();

            Assert.True(dict.ContainsKey(OfficeMimeTypeEnum.ExcelLegacy));
            Assert.IsType<XlsOpenStrategy>(dict[OfficeMimeTypeEnum.ExcelLegacy]);
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

            var objectItems = result.EmbeddedFiles.Where(f => f.FileName.StartsWith("embedded_object_")).ToList();
            var pictureItems = result.EmbeddedFiles.Where(f => f.FileName.StartsWith("picture_")).ToList();
            Assert.Single(objectItems);
            Assert.Single(pictureItems);

            var recoveredPayload = ReadOlePackagePayload(objectItems[0].BinaryData);
            Assert.Equal(validPayload, recoveredPayload);

            var logged = capturedOut.ToString();
            Assert.Contains("corrupt", logged, StringComparison.OrdinalIgnoreCase);
        }
    }
}
