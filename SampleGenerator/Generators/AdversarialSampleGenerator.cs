using System.Buffers.Binary;
using NPOI.POIFS.FileSystem;
using OpenMcdf;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Builds adversarial CFB (OLE Compound File Binary) fixtures whose
    /// on-disk bytes stay tiny but whose declared internal size is forged to
    /// be enormous - for
    /// docs/specs/dataset-curation.md's shared adversarial set
    /// ("oversized-declared-size sample (zip/CFB bomb)"), which exercises
    /// this repo's nesting-depth-guard
    /// (.ydk/components/nfr/extraction/nesting-depth-guard.yaml: 1GB
    /// per-entry / 2GB total, checked against the *declared* size before any
    /// decompression). Mirrors
    /// ExtractorOle/ExtractorOLE.Tests/MimeDetection/CfbMimeDetectorGuardrailTests.cs's
    /// Detect_ForgedFatSectorCountImplyingOver2GB_ThrowsOversizedNestedContentException
    /// test, which proves this exact forgery makes
    /// CfbMimeDetector.Detect() throw OversizedNestedContentException.
    /// </summary>
    public static class AdversarialSampleGenerator
    {
        /// <summary>
        /// Returns a small, real CFB file (one root-level stream, a few
        /// bytes of payload) with the FAT sector count field at header
        /// offset 0x2C forged to 100000. That implies a declared size of
        /// ~6.1GB (100000 sectors x 128 entries/sector x 512 bytes/sector),
        /// well over the 2GB total cap, while the actual bytes on disk stay
        /// a few KB.
        /// </summary>
        public static byte[] BuildCfbSizeBomb()
        {
            using var stream = new MemoryStream();
            using (var root = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
            {
                using var entryStream = root.CreateStream("SomeStream");
                var data = new byte[] { 1, 2, 3 };
                entryStream.Write(data, 0, data.Length);
            }

            var bytes = stream.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x2C, 4), 100000);
            return bytes;
        }

        private static byte[] ReadAll(NPOIFSFileSystem fs, string name)
        {
            using var reader = fs.Root.CreateDocumentInputStream(name);
            var bytes = new byte[reader.Available()];
            reader.ReadFully(bytes);
            return bytes;
        }

        private const ushort Vt_I4 = 3;
        private const int PidCreateDtm = 12;
        private const int DirectoryEntrySize = 128;

        /// <summary>
        /// A valid .doc whose SummaryInformation stream carries nine properties, one of which
        /// (PID_CREATE_DTM, "Created") has its variant type rewritten from VT_FILETIME to VT_I4.
        /// The property set still parses and every other getter works; only the Created getter
        /// fails (its value is no longer a date).
        /// </summary>
        public static byte[] BuildCorruptMetadataPropertyDoc()
        {
            var good = new DocSampleGenerator().Generate(new SampleSpec
            {
                BodyText = "Valid Word body; exactly one SummaryInformation property is unreadable.",
                Metadata = new Dictionary<string, string>
                {
                    [MetadataFields.Title] = "Adversarial title",
                    [MetadataFields.Author] = "Adversarial author",
                    [MetadataFields.Subject] = "Adversarial subject",
                    [MetadataFields.Keywords] = "adversarial, corrupt",
                    [MetadataFields.Comments] = "Adversarial comments",
                    [MetadataFields.LastModifiedBy] = "Adversarial editor",
                    [MetadataFields.RevisionNumber] = "3",
                    [MetadataFields.Created] = "2024-01-02T03:04:05Z",
                    [MetadataFields.Modified] = "2024-02-03T04:05:06Z",
                },
            }).Content;

            var fs = new NPOIFSFileSystem(new MemoryStream(good));
            try
            {
                var stream = ReadAll(fs, "SummaryInformation");

                // Property set header is 28 bytes, then FMTID (16) and the section offset (4).
                var sectionOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(stream.AsSpan(44, 4));
                var count = (int)BinaryPrimitives.ReadUInt32LittleEndian(stream.AsSpan(sectionOffset + 4, 4));
                var patched = false;
                for (var i = 0; i < count; i++)
                {
                    var listAt = sectionOffset + 8 + 8 * i;
                    if (BinaryPrimitives.ReadUInt32LittleEndian(stream.AsSpan(listAt, 4)) != PidCreateDtm) continue;
                    var valueAt = sectionOffset + (int)BinaryPrimitives.ReadUInt32LittleEndian(stream.AsSpan(listAt + 4, 4));
                    BinaryPrimitives.WriteUInt16LittleEndian(stream.AsSpan(valueAt, 2), Vt_I4);
                    patched = true;
                }
                if (!patched) throw new InvalidOperationException("Created property not found in SummaryInformation.");

                fs.Root.GetEntry("SummaryInformation").Delete();
                fs.Root.CreateDocument("SummaryInformation", new MemoryStream(stream));
                using var output = new MemoryStream();
                fs.WriteFileSystem(output);
                return output.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }

        /// <summary>
        /// A valid .xls whose worksheet substream is blanked after its BOF record. The workbook-globals
        /// substream is untouched, so the BoundSheet records (sheet names) still read, but the zeroed
        /// sheet records cannot be assembled into a sheet (HSSFWorkbook throws "WINDOW2 was not found"),
        /// so the sheet count cannot be read.
        /// </summary>
        public static byte[] BuildCorruptSheetRecordsXls()
        {
            var good = new XlsSampleGenerator().Generate(new SampleSpec
            {
                BodyText = "Valid worksheet text; the sheet records are zeroed.",
            }).Content;

            var fs = new NPOIFSFileSystem(new MemoryStream(good));
            try
            {
                var workbook = ReadAll(fs, "Workbook");

                // Walk records (sid, length, payload) to the second BOF (sid 0x0809): the worksheet's.
                const int Bof = 0x0809;
                var bofsSeen = 0;
                var sheetBodyAt = -1;
                for (var at = 0; at + 4 <= workbook.Length;)
                {
                    var sid = BinaryPrimitives.ReadUInt16LittleEndian(workbook.AsSpan(at, 2));
                    var length = BinaryPrimitives.ReadUInt16LittleEndian(workbook.AsSpan(at + 2, 2));
                    at += 4 + length;
                    if (sid == Bof && ++bofsSeen == 2) { sheetBodyAt = at; break; }
                }
                if (sheetBodyAt < 0) throw new InvalidOperationException("Worksheet BOF not found in Workbook stream.");
                workbook.AsSpan(sheetBodyAt).Clear();

                fs.Root.GetEntry("Workbook").Delete();
                fs.Root.CreateDocument("Workbook", new MemoryStream(workbook));
                using var output = new MemoryStream();
                fs.WriteFileSystem(output);
                return output.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }

        /// <summary>
        /// A valid .doc whose ObjectPool holds a good embedded storage ("_1") and a second item
        /// ("_2") that is a plain stream rather than a storage, so it cannot be walked as an
        /// object. That second item's directory entry then gets an empty name (name length 0),
        /// so the failing item has no usable path.
        /// </summary>
        public static byte[] BuildUnnamedEmbeddedStorageDoc()
        {
            var good = new DocSampleGenerator().Generate(new SampleSpec
            {
                BodyText = "Valid Word body; one embedded ObjectPool item has no name.",
                Embeddings =
                {
                    new EmbeddedContentSpec { FileName = "good.bin", Content = [1, 2, 3, 4], ContentType = "application/octet-stream" },
                },
            }).Content;

            var fs = new NPOIFSFileSystem(new MemoryStream(good));
            try
            {
                var pool = (DirectoryEntry)fs.Root.GetEntry("ObjectPool");
                pool.CreateDocument("_2", new MemoryStream([9, 9, 9, 9]));
                using var output = new MemoryStream();
                fs.WriteFileSystem(output);
                var bytes = output.ToArray();

                // Directory entries are 128 bytes: UTF-16 name (64 bytes), then the name length in
                // bytes (2 bytes, including the terminator). "_2" is 6.
                var patched = 0;
                for (var at = 512; at + DirectoryEntrySize <= bytes.Length; at += DirectoryEntrySize)
                {
                    if (bytes[at] != '_' || bytes[at + 1] != 0 || bytes[at + 2] != '2' || bytes[at + 3] != 0) continue;
                    if (BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(at + 64, 2)) != 6) continue;
                    bytes.AsSpan(at, 64).Clear();
                    BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(at + 64, 2), 0);
                    patched++;
                }
                if (patched != 1) throw new InvalidOperationException($"Expected one '_2' directory entry, found {patched}.");
                return bytes;
            }
            finally
            {
                fs.Close();
            }
        }
    }
}
