using System.Buffers.Binary;
using OpenMcdf;

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
    }
}
