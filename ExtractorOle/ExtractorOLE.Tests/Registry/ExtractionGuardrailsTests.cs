using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using System.Buffers.Binary;
using System.IO.Compression;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>Task #276 (ExtractionGuardrails): caps apply to bytes actually inflated, not to forged local-header sizes.</summary>
    public class ExtractionGuardrailsTests
    {
        private static byte[] BuildZip(int entries, int bytesPerEntry)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                for (var i = 0; i < entries; i++)
                {
                    using var s = zip.CreateEntry($"e{i}.bin", CompressionLevel.Optimal).Open();
                    s.Write(new byte[bytesPerEntry]);
                }
            }
            return ms.ToArray();
        }

        // Zero the sizes in every local file header; the central directory keeps the real data.
        private static byte[] ForgeLocalHeaders(byte[] zip)
        {
            var forged = (byte[])zip.Clone();
            for (var i = 0; i + 30 < forged.Length; i++)
            {
                if (forged[i] == 0x50 && forged[i + 1] == 0x4B && forged[i + 2] == 3 && forged[i + 3] == 4)
                {
                    forged.AsSpan(i + 18, 8).Clear();
                }
            }
            return forged;
        }

        private static void Run(byte[] bytes, MimeDetectionLimits limits) =>
            new ExtractionGuardrails(limits, _ => { }).Enforce(new ExtractionRequest { FileName = "x.docx", FileBytes = bytes });

        [Fact]
        public void ForgedZeroSizeHeaders_InflatingPastEntryCap_Throws()
        {
            var forged = ForgeLocalHeaders(BuildZip(1, 4_000_000));
            Assert.Throws<OversizedNestedContentException>(() =>
                Run(forged, new MimeDetectionLimits { MaxDeclaredEntryBytes = 1_000_000 }));
        }

        [Fact]
        public void ForgedHeaders_InflatingPastTotalCap_Throws()
        {
            var forged = ForgeLocalHeaders(BuildZip(5, 1_000_000));
            Assert.Throws<OversizedNestedContentException>(() =>
                Run(forged, new MimeDetectionLimits { MaxDeclaredEntryBytes = 2_000_000, MaxDeclaredNestedContentBytes = 3_000_000 }));
        }

        [Fact]
        public void EntryCountOverCap_RejectedBeforeOpen()
        {
            var zip = BuildZip(20, 10);
            Assert.Throws<OversizedNestedContentException>(() =>
                Run(zip, new MimeDetectionLimits { MaxZipEntryCount = 5 }));
        }

        [Fact]
        public void LegitSmallZip_Passes()
        {
            Run(BuildZip(3, 1000), new MimeDetectionLimits());
        }
    }
}
