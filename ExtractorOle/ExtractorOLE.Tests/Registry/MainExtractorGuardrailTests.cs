using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers.Binary;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Covers T-3335d397: Extract(ExtractionRequest) rejects file-too-large and
    /// oversized-nested-content inputs BEFORE any parsing/dispatch happens
    /// ([ydk:nfr:extraction/max-file-size], [ydk:nfr:extraction/nesting-depth-guard]).
    /// "Before parsing" is verified with a spy registry that counts every lookup of a
    /// format-specific component, not just by exception type.
    /// </summary>
    public class MainExtractorGuardrailTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string DocMime = "application/msword";

        private sealed class SpyRegistry : IFormatDispatchRegistry
        {
            public int Lookups { get; private set; }

            public IOpenStrategy? GetOpenStrategy(OfficeMimeTypeEnum format) { Lookups++; return null; }

            public ITextExtractor? GetTextExtractor(OfficeMimeTypeEnum format) { Lookups++; return null; }
        }

        private static (MainExtractor extractor, SpyRegistry spy, List<string> log) Build(MimeDetectionLimits? limits = null)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var spy = new SpyRegistry();
            var log = new List<string>();
            var guardrails = new ExtractionGuardrails(limits ?? new MimeDetectionLimits(), log.Add);
            var extractor = new MainExtractor(provider.GetRequiredService<IExtractionHelper>(), spy, guardrails);
            return (extractor, spy, log);
        }

        private static ExtractionRequest Request(byte[] bytes, string mime = DocxMime, string fileName = "big.docx") =>
            new() { FileBytes = bytes, FileName = fileName, DetectedMimeType = mime };

        // Hand-built ZIP local file headers (PKZIP APPNOTE 4.3.7) with forged declared
        // uncompressed sizes and zero-length compressed data, so the file stays tiny.
        private static byte[] BuildZipWithDeclaredSizes(params uint[] declaredUncompressedSizes)
        {
            using var ms = new MemoryStream();
            foreach (var size in declaredUncompressedSizes)
            {
                var header = new byte[30 + 1];
                header[0] = 0x50; header[1] = 0x4B; header[2] = 0x03; header[3] = 0x04;
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(18, 4), 0);    // compressed size
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(22, 4), size); // uncompressed size
                BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(26, 2), 1);    // name length
                header[30] = (byte)'a';
                ms.Write(header);
            }
            return ms.ToArray();
        }

        // Minimal CFB header (MS-CFB 2.2): signature, sector shift, FAT sector count.
        private static byte[] BuildCfbWithFatSectorCount(uint fatSectorCount, ushort sectorShift = 9)
        {
            var bytes = new byte[512];
            new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }.CopyTo(bytes, 0);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x1E, 2), sectorShift);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x2C, 4), fatSectorCount);
            return bytes;
        }

        // ---- file-too-large ----

        [Fact]
        public void Extract_100MBPlusOneByte_ThrowsFileTooLarge_BeforeAnyDispatch()
        {
            var (extractor, spy, _) = Build();
            var bytes = new byte[104_857_600 + 1];

            var ex = Assert.Throws<FileTooLargeException>(() => extractor.Extract(Request(bytes)));

            Assert.Equal(104_857_601, ex.ActualBytes);
            Assert.Equal(104_857_600, ex.LimitBytes);
            Assert.Equal(0, spy.Lookups);
        }

        [Fact]
        public void Extract_ExactlyAtMaxFileSize_IsNotRejectedByFileSizeGuard()
        {
            var (extractor, spy, _) = Build(new MimeDetectionLimits { MaxFileSizeBytes = 10 });

            extractor.Extract(Request(new byte[10]));

            Assert.Equal(2, spy.Lookups); // proceeded to dispatch
        }

        [Fact]
        public void Extract_MaxFileSizeIsConfigurable_ChangingLimitChangesAcceptedBehavior()
        {
            var bytes = new byte[20];

            var (strict, strictSpy, _) = Build(new MimeDetectionLimits { MaxFileSizeBytes = 10 });
            Assert.Throws<FileTooLargeException>(() => strict.Extract(Request(bytes)));
            Assert.Equal(0, strictSpy.Lookups);

            var (lenient, lenientSpy, _) = Build();
            lenient.Extract(Request(bytes));
            Assert.Equal(2, lenientSpy.Lookups);
        }

        [Fact]
        public void Extract_FileTooLarge_LogsFileSizeFileNameAndLimit()
        {
            var (extractor, _, log) = Build(new MimeDetectionLimits { MaxFileSizeBytes = 10 });

            Assert.Throws<FileTooLargeException>(() => extractor.Extract(Request(new byte[11], fileName: "huge.docx")));

            var line = Assert.Single(log);
            Assert.Contains("file-too-large", line);
            Assert.Contains("huge.docx", line);
            Assert.Contains("11", line);
            Assert.Contains("10", line);
        }

        [Fact]
        public void Extract_ThroughProductionDi_ReadsLimitFromSharedConfiguration()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var extractor = provider.GetRequiredService<MainExtractor>();
            var limits = provider.GetRequiredService<MimeDetectionLimits>();

            limits.MaxFileSizeBytes = 10;

            Assert.Throws<FileTooLargeException>(() => extractor.Extract(Request(new byte[11])));
        }

        // ---- ordering: null checks -> guardrails -> format resolution ----

        [Fact]
        public void Extract_NullFileBytes_ThrowsArgumentNull_NotSilentlyPassingGuard()
        {
            var (extractor, spy, _) = Build();
            var request = new ExtractionRequest { FileBytes = null!, FileName = "a.docx", DetectedMimeType = DocxMime };

            Assert.Throws<ArgumentNullException>(() => extractor.Extract(request));
            Assert.Equal(0, spy.Lookups);
        }

        [Fact]
        public void Extract_OversizedInputWithUnsupportedMime_ThrowsGuardrailNotUnsupportedFormat()
        {
            var (extractor, spy, _) = Build(new MimeDetectionLimits { MaxFileSizeBytes = 10 });

            Assert.Throws<FileTooLargeException>(() => extractor.Extract(Request(new byte[11], "application/x-unsupported")));
            Assert.Equal(0, spy.Lookups);
        }

        // ---- oversized-nested-content ----

        [Fact]
        public void Extract_ZipEntryDeclaringMoreThanPerEntryCap_ThrowsOversizedNestedContent_BeforeAnyDispatch()
        {
            var (extractor, spy, _) = Build();
            var bytes = BuildZipWithDeclaredSizes(1_500_000_000); // > 1 GB per entry, < 2 GB total

            var ex = Assert.Throws<OversizedNestedContentException>(() => extractor.Extract(Request(bytes)));

            Assert.False(string.IsNullOrWhiteSpace(ex.DeclaredMetric));
            Assert.Equal(0, spy.Lookups);
        }

        [Fact]
        public void Extract_ZipEntriesEachUnderPerEntryCapButOverTotalCap_ThrowsOversizedNestedContent()
        {
            var (extractor, spy, _) = Build();
            var bytes = BuildZipWithDeclaredSizes(900_000_000, 900_000_000, 900_000_000); // 2.7 GB total

            Assert.Throws<OversizedNestedContentException>(() => extractor.Extract(Request(bytes)));
            Assert.Equal(0, spy.Lookups);
        }

        [Fact]
        public void Extract_ZipWithZip64SentinelSize_ThrowsOversizedNestedContent()
        {
            var (extractor, spy, _) = Build();
            var bytes = BuildZipWithDeclaredSizes(0xFFFFFFFF);

            Assert.Throws<OversizedNestedContentException>(() => extractor.Extract(Request(bytes)));
            Assert.Equal(0, spy.Lookups);
        }

        [Fact]
        public void Extract_CfbFatChainImplyingMoreThan2GB_ThrowsOversizedNestedContent_BeforeAnyDispatch()
        {
            var (extractor, spy, _) = Build();
            var bytes = BuildCfbWithFatSectorCount(20_000_000); // 20M * 128 entries * 512 B ~ 1.3 TB

            var ex = Assert.Throws<OversizedNestedContentException>(() => extractor.Extract(Request(bytes, DocMime)));

            Assert.False(string.IsNullOrWhiteSpace(ex.DeclaredMetric));
            Assert.Equal(0, spy.Lookups);
        }

        [Fact]
        public void Extract_CfbWithInvalidSectorShift_ThrowsOversizedNestedContent()
        {
            var (extractor, spy, _) = Build();
            var bytes = BuildCfbWithFatSectorCount(1, sectorShift: 31);

            Assert.Throws<OversizedNestedContentException>(() => extractor.Extract(Request(bytes, DocMime)));
            Assert.Equal(0, spy.Lookups);
        }

        [Fact]
        public void Extract_PlausibleDeclaredSizes_AreNotRejected()
        {
            var (extractor, spy, _) = Build();

            extractor.Extract(Request(BuildZipWithDeclaredSizes(1_000, 2_000)));
            // The single declared FAT sector must physically exist (all-free entries), otherwise the
            // parse-time truncated-container check (T-61477563) rightly rejects it after the guardrail passes.
            var cfb = new byte[1024];
            BuildCfbWithFatSectorCount(1).CopyTo(cfb, 0);
            Array.Fill(cfb, (byte)0xFF, 512, 512);
            extractor.Extract(Request(cfb, DocMime));

            Assert.Equal(4, spy.Lookups);
        }

        [Fact]
        public void Extract_OversizedNestedContent_LogsFileSizeFileNameAndConfiguredLimit()
        {
            var (extractor, _, log) = Build();
            var bytes = BuildZipWithDeclaredSizes(1_500_000_000);

            Assert.Throws<OversizedNestedContentException>(() => extractor.Extract(Request(bytes, fileName: "bomb.docx")));

            var line = Assert.Single(log);
            Assert.Contains("oversized-nested-content", line);
            Assert.Contains("bomb.docx", line);
            Assert.Contains(bytes.Length.ToString(), line);
            Assert.Contains("1073741824", line); // configured per-entry limit
        }

        [Fact]
        public void Extract_NestedContentCapsAreConfigurable()
        {
            var bytes = BuildZipWithDeclaredSizes(5_000);

            var (strict, _, _) = Build(new MimeDetectionLimits { MaxDeclaredEntryBytes = 1_000 });
            Assert.Throws<OversizedNestedContentException>(() => strict.Extract(Request(bytes)));

            var (lenient, spy, _) = Build();
            lenient.Extract(Request(bytes));
            Assert.Equal(2, spy.Lookups);
        }
    }
}
