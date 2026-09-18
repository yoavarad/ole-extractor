using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Covers T-12dfd045: legacy .ppt's first-layer embedded-subfile walk. Per
    /// ydk:req:extraction/subfile-scope's ppt-specific acceptance criterion, an embedded OLE
    /// object in a .ppt must appear in Subfiles as raw bytes, named by its own CFB storage
    /// name -- this exercises ExtractionHelper's NPOIFSFileSystem overload of
    /// ExtractFirstLayerEmbedded (wired into PptOpenStrategy.Open independently of whether
    /// b2xtranslator can convert the body -- see PptOpenStrategyTests).
    ///
    /// As with PptOpenStrategyTests, no real .ppt sample fixture exists in this repo --
    /// fixtures are built directly via raw POIFS (NPOIFSFileSystem, with HSSFWorkbook used
    /// purely as a convenient writer for the base CFB/HPSF streams), the same technique
    /// XlsOpenStrategyTests uses for its own embedded-object fixtures.
    /// </summary>
    public class PptFirstLayerEmbeddedWalkTests
    {
        private static byte[] BuildBaseFixtureBytes()
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateInformationProperties();
            workbook.SummaryInformation.Title = "Embedding Fixture";
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        [Fact]
        public void Open_PptWithEmbeddedObjectStorage_AppearsInSubfilesNamedByCfbStorageName()
        {
            byte[] payload = Encoding.UTF8.GetBytes("VALID EMBEDDED OBJECT CONTENT");

            var raw = new NPOIFSFileSystem(new MemoryStream(BuildBaseFixtureBytes()));
            var storage = raw.Root.CreateDirectory("EmbeddedWordDoc");
            storage.CreateDocument("CONTENTS", new MemoryStream(payload));

            using var outStream = new MemoryStream();
            raw.WriteFileSystem(outStream);
            raw.Close();

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(outStream.ToArray());

            Assert.NotNull(result);
            var item = Assert.Single(result!.EmbeddedFiles);
            Assert.Equal("EmbeddedWordDoc", item.FileName);
            Assert.Equal("EmbeddedWordDoc", item.PackagePath);
            Assert.True(item.SizeInBytes > 0);

            // Opaque copy: the storage's own bytes round-trip as a standalone POIFS
            // filesystem carrying the original nested "CONTENTS" document -- confirms the
            // whole storage was captured as one blob, not recursively unpacked into
            // separate Subfiles entries.
            var copied = new NPOIFSFileSystem(new MemoryStream(item.BinaryData));
            try
            {
                using var contentsStream = new DocumentInputStream((DocumentEntry)copied.Root.GetEntry("CONTENTS"));
                using var buffer = new MemoryStream();
                contentsStream.CopyTo(buffer);
                Assert.Equal(payload, buffer.ToArray());
            }
            finally
            {
                copied.Close();
            }
        }

        [Fact]
        public void Open_OneValidOneCorruptEmbeddedObjectStorage_OmitsCorruptAndLogsWithoutThrowing()
        {
            byte[] validPayload = Encoding.UTF8.GetBytes("VALID EMBEDDED OBJECT CONTENT");

            var raw = new NPOIFSFileSystem(new MemoryStream(BuildBaseFixtureBytes()));

            var validStorage = raw.Root.CreateDirectory("ValidObject");
            validStorage.CreateDocument("CONTENTS", new MemoryStream(validPayload));

            var corruptStorage = raw.Root.CreateDirectory("CorruptObject");
            byte[] corruptPayload = new byte[5000];
            new Random(42).NextBytes(corruptPayload);
            var corruptDocEntry = corruptStorage.CreateDocument("CONTENTS", new MemoryStream(corruptPayload));

            // Corrupt the FAT chain: point the document's first block back to itself. NPOI's
            // own ChainLoopDetector (NPOI.POIFS.FileSystem.BlockStore.ChainLoopDetector.Claim)
            // throws deterministically the second time that block is claimed while reading --
            // a safe, designed-for-this failure mode, unlike a declared-size/actual-chain
            // mismatch which risks an unbounded read loop in NPOI's reader.
            int startBlock = ((DocumentNode)corruptDocEntry).Property.StartBlock;
            raw.SetNextBlock(startBlock, startBlock);

            using var outStream = new MemoryStream();
            raw.WriteFileSystem(outStream);
            raw.Close();

            var originalOut = Console.Out;
            var capturedOut = new StringWriter();
            Console.SetOut(capturedOut);

            DocumentExtractionResult? result;
            try
            {
                result = new PptOpenStrategy(new ExtractionHelper()).Open(outStream.ToArray());
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.NotNull(result);
            var item = Assert.Single(result!.EmbeddedFiles);
            Assert.Equal("ValidObject", item.FileName);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.FileName == "CorruptObject");
            Assert.Contains("corrupt", capturedOut.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Open_PptWithPicturesStream_AppearsInSubfilesAsOneOpaqueItem()
        {
            byte[] media = Encoding.UTF8.GetBytes("FAKE CONCATENATED SLIDE IMAGES BLOB");

            var raw = new NPOIFSFileSystem(new MemoryStream(BuildBaseFixtureBytes()));
            raw.Root.CreateDocument("Pictures", new MemoryStream(media));

            using var outStream = new MemoryStream();
            raw.WriteFileSystem(outStream);
            raw.Close();

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(outStream.ToArray());

            Assert.NotNull(result);
            var item = Assert.Single(result!.EmbeddedFiles);
            Assert.Equal("Pictures", item.FileName);
            Assert.Equal(media, item.BinaryData);
        }
    }
}
