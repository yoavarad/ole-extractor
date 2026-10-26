using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    // #249: ExtractionHelper split into mime mapping / mime detection / embedded-file
    // extraction components, with ExtractionHelper kept as a facade over them.
    public class ExtractionHelperSplitTests
    {
        private static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void MimeTypeMapper_MapsEnumToMimeAndBack()
        {
            var mapper = new MimeTypeMapper();

            Assert.Equal("application/msword", mapper.MimeFor(OfficeMimeType.WordLegacy));
            Assert.Equal(OfficeMimeType.Excel, mapper.ParseOfficeMimeType(mapper.MimeFor(OfficeMimeType.Excel)));
            Assert.Equal(OfficeMimeType.OpenXmlUnknown, mapper.ParseOfficeMimeType(null));
            Assert.Equal(".png", mapper.GetExtensionFromContentType("image/png"));
            Assert.Equal(".bin", mapper.GetExtensionFromContentType(""));
        }

        [Fact]
        public void OfficeMimeTypeDetector_DetectsXlsAndMagicBytes()
        {
            var detector = new OfficeMimeTypeDetector(new MimeDetectionRegistry(new IMimeTypeDetector[]
            {
                new OoxmlMimeDetector(),
                new CfbMimeDetector(),
            }));
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1");
            using var ms = new MemoryStream();
            workbook.Write(ms);

            Assert.Equal(OfficeMimeType.ExcelLegacy, detector.DetectMimeTypeFromBytes(ms.ToArray()));
            Assert.Equal(OfficeMimeType.OpenXmlUnknown, detector.DetectMimeTypeFromBytes(new byte[] { 1, 2, 3, 4 }));
            Assert.Equal("application/pdf", detector.DetectMimeFromMagicBytes(new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F' }));
        }

        [Fact]
        public void EmbeddedFileExtractor_DocWithoutObjectPool_AddsNothing()
        {
            var extractor = new EmbeddedFileExtractor(new MimeTypeMapper());
            var fs = new NPOI.POIFS.FileSystem.NPOIFSFileSystem();
            var result = new DocumentExtractionResult();

            extractor.ExtractFirstLayerEmbedded(result, fs.Root);

            Assert.Empty(result.EmbeddedFiles);
        }

        [Fact]
        public void Di_ResolvesEachComponent()
        {
            using var provider = BuildProvider();

            Assert.IsType<MimeTypeMapper>(provider.GetRequiredService<IMimeTypeMapper>());
            Assert.IsType<OfficeMimeTypeDetector>(provider.GetRequiredService<IOfficeMimeTypeDetector>());
            Assert.IsType<EmbeddedFileExtractor>(provider.GetRequiredService<IEmbeddedFileExtractor>());
            Assert.IsType<ExtractionHelper>(provider.GetRequiredService<IExtractionHelper>());
        }

        private sealed class AlwaysDocDetector : IMimeTypeDetector
        {
            public MimeDetectionResult Detect(MimeDetectionRequest request) =>
                new MimeDetectionResult { DetectedFormat = DetectedFormat.Doc, MimeType = "application/msword", IsSupported = true };
        }

        [Fact]
        public void Di_OverriddenDetectionRegistry_IsHonoredByDetectorAndFacade()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            services.AddSingleton<IMimeDetectionRegistry>(new MimeDetectionRegistry(new IMimeTypeDetector[] { new AlwaysDocDetector() }));
            using var provider = services.BuildServiceProvider();
            var garbage = new byte[] { 1, 2, 3, 4 };

            Assert.Equal(OfficeMimeType.WordLegacy, provider.GetRequiredService<IOfficeMimeTypeDetector>().DetectMimeTypeFromBytes(garbage));
            Assert.Equal(OfficeMimeType.WordLegacy, provider.GetRequiredService<IExtractionHelper>().DetectMimeTypeFromBytes(garbage));
        }

        [Fact]
        public void LegacyExtractBytes_UndetectedFormat_ThrowsLikeExtractRequest()
        {
            using var provider = BuildProvider();
            var extractor = provider.GetRequiredService<MainExtractor>();

#pragma warning disable CS0618 // exercises the [Obsolete] Extract(byte[]) shim
            Assert.Throws<UnsupportedFormatException>(() => extractor.Extract(new byte[] { 1, 2, 3, 4 }));
#pragma warning restore CS0618
        }
    }
}
