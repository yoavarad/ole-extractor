using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.DTOs;
using NPOI.POIFS.FileSystem;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ExtractorOLE.Tests.Doc
{
    // [ydk:req:extraction/subfile-scope] - first-layer OLE-embedded-object walk for legacy
    // .doc. Word/OLE stores embedded objects as child storages of a root-level "ObjectPool"
    // directory (docs/research/npoi.md); these fixtures build that shape directly via
    // NPOIFSFileSystem, mirroring XlsOpenStrategyTests' raw-POIFS fixture technique since no
    // HWPFDocument-level convenience API exists for this format (ADR-004).
    public class DocFirstLayerEmbeddedWalkTests
    {
        private static byte[] BuildDocFixtureWithObjectPoolChild(string childName, Action<DirectoryEntry> populate)
        {
            var fs = new NPOIFSFileSystem();
            try
            {
                fs.Root.CreateDocument("WordDocument", new MemoryStream(new byte[] { 0x00 }));
                var pool = fs.Root.CreateDirectory("ObjectPool");
                var child = pool.CreateDirectory(childName);
                populate(child);

                using var ms = new MemoryStream();
                fs.WriteFileSystem(ms);
                return ms.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }

        // Builds a fixture with one valid ObjectPool child (a real directory storage) and one
        // corrupt entry: a plain document sitting directly under ObjectPool where a directory
        // storage is expected. This is the shape the production walk's "entry is not a
        // DirectoryEntry" guard exists for - a malformed ObjectPool entry that doesn't resolve
        // to a real embedded-object storage.
        private static byte[] BuildDocFixtureWithValidAndCorruptObjectPoolEntries(byte[] validPayload)
        {
            var fs = new NPOIFSFileSystem();
            try
            {
                fs.Root.CreateDocument("WordDocument", new MemoryStream(new byte[] { 0x00 }));
                var pool = fs.Root.CreateDirectory("ObjectPool");

                var validChild = pool.CreateDirectory("_1000000001");
                validChild.CreateDocument("Package", new MemoryStream(validPayload));

                pool.CreateDocument("_1000000002", new MemoryStream(new byte[] { 1, 2, 3 }));

                using var ms = new MemoryStream();
                fs.WriteFileSystem(ms);
                return ms.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }

        private static byte[] ReadDocumentStream(byte[] copiedDirectoryBytes, string documentName)
        {
            var fs = new NPOIFSFileSystem(new MemoryStream(copiedDirectoryBytes));
            try
            {
                using var input = fs.Root.CreateDocumentInputStream(documentName);
                using var ms = new MemoryStream();
                input.CopyTo(ms);
                return ms.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }

        [Fact]
        public void Open_DocWithOneEmbeddedObject_PopulatesEmbeddedFilesFromCfbStorageName()
        {
            var payload = new byte[] { 0x50, 0x41, 0x59, 0x4C, 0x4F, 0x41, 0x44 }; // "PAYLOAD"
            var bytes = BuildDocFixtureWithObjectPoolChild(
                "_9000000001",
                child => child.CreateDocument("Package", new MemoryStream(payload)));

            var result = new DocOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var item = Assert.Single(result!.EmbeddedFiles);
            // AC: FileName from CFB storage name.
            Assert.Equal("_9000000001", item.FileName);
            Assert.Equal(item.BinaryData.LongLength, item.SizeInBytes);

            var recovered = ReadDocumentStream(item.BinaryData, "Package");
            Assert.Equal(payload, recovered);
        }

        [Fact]
        public void Open_DocWithContainerEmbeddedObject_ReturnsOneOpaqueSubfileItem()
        {
            var payload = new byte[] { 0x11, 0x22, 0x33 };
            var bytes = BuildDocFixtureWithObjectPoolChild(
                "_9000000002",
                child =>
                {
                    // The embedded object is itself a container: one direct stream plus a
                    // nested storage of its own.
                    child.CreateDocument("Package", new MemoryStream(payload));
                    var nested = child.CreateDirectory("NestedInner");
                    nested.CreateDocument("InnerStream", new MemoryStream(new byte[] { 9, 9, 9 }));
                });

            var result = new DocOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            // Not recursively unpacked: exactly one SubfileItem for the whole container, not
            // one per internal stream/nested storage.
            var item = Assert.Single(result!.EmbeddedFiles);
            Assert.Equal("_9000000002", item.FileName);

            var recovered = ReadDocumentStream(item.BinaryData, "Package");
            Assert.Equal(payload, recovered);
        }

        [Fact]
        public void Open_OneValidOneCorruptObjectPoolEntry_OmitsCorruptAndLogsWithoutThrowing()
        {
            var validPayload = new byte[] { 0x56, 0x41, 0x4C, 0x49, 0x44 }; // "VALID"
            var bytes = BuildDocFixtureWithValidAndCorruptObjectPoolEntries(validPayload);

            var originalOut = Console.Out;
            var capturedOut = new StringWriter();
            Console.SetOut(capturedOut);

            DocumentExtractionResult? result;
            try
            {
                result = new DocOpenStrategy(new ExtractionHelper()).Open(bytes);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.NotNull(result);
            var item = Assert.Single(result!.EmbeddedFiles);
            Assert.Equal("_1000000001", item.FileName);

            var recovered = ReadDocumentStream(item.BinaryData, "Package");
            Assert.Equal(validPayload, recovered);

            var logged = capturedOut.ToString();
            Assert.Contains("corrupt", logged, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Open_DocWithNoObjectPool_ReturnsEmptyEmbeddedFiles()
        {
            var fs = new NPOIFSFileSystem();
            using var ms = new MemoryStream();
            try
            {
                fs.Root.CreateDocument("WordDocument", new MemoryStream(new byte[] { 0x00 }));
                fs.WriteFileSystem(ms);
            }
            finally
            {
                fs.Close();
            }

            var result = new DocOpenStrategy(new ExtractionHelper()).Open(ms.ToArray());

            Assert.NotNull(result);
            Assert.Empty(result!.EmbeddedFiles);
        }
    }
}
