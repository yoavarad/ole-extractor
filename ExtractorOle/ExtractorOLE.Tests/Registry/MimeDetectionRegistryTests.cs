using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Covers T-0a81d133 / [ydk:req:extraction/dependency-injection] and
    /// [ydk:req:extraction/format-extensibility]: the CFB detection component
    /// must be present in the DI-registered detection set (alongside the OOXML
    /// structural check) and resolved by DetectMimeType dispatch purely through
    /// that registry - never constructed inline in orchestration code.
    /// </summary>
    public class MimeDetectionRegistryTests
    {
        [Fact]
        public void ServiceRegistration_RegistersCfbAndOoxmlDetectors_InDetectionRegistry()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);

            using var provider = services.BuildServiceProvider();
            var registry = provider.GetRequiredService<IMimeDetectionRegistry>();

            Assert.Contains(registry.Detectors, d => d is CfbMimeDetector);
            Assert.Contains(registry.Detectors, d => d is OoxmlMimeDetector);
        }

        // Stands in for CfbMimeDetector so this test can prove DetectMimeTypeFromBytes
        // resolves the CFB check purely through IMimeDetectionRegistry, with no
        // hardcoded reference to CfbMimeDetector anywhere in the dispatch path.
        private sealed class FakeCfbDetector : IMimeTypeDetector
        {
            public bool WasCalled { get; private set; }

            public MimeDetectionResult Detect(MimeDetectionRequest request)
            {
                WasCalled = true;
                return new MimeDetectionResult
                {
                    DetectedFormat = DetectedFormatEnum.Xls,
                    MimeType = "application/vnd.ms-excel",
                    IsSupported = true
                };
            }
        }

        private sealed class UnknownDetector : IMimeTypeDetector
        {
            public MimeDetectionResult Detect(MimeDetectionRequest request) => new()
            {
                DetectedFormat = DetectedFormatEnum.Unknown,
                MimeType = "application/octet-stream",
                IsSupported = false
            };
        }

        [Fact]
        public void DetectMimeTypeFromBytes_TriesEachRegisteredDetector_ViaRegistryNotHardcodedReference()
        {
            var fakeCfb = new FakeCfbDetector();
            var registry = new MimeDetectionRegistry(new IMimeTypeDetector[] { new UnknownDetector(), fakeCfb });

            var helper = new ExtractionHelper(registry);

            var result = helper.DetectMimeTypeFromBytes(new byte[] { 0x01, 0x02, 0x03 });

            Assert.True(fakeCfb.WasCalled);
            Assert.Equal(OfficeMimeTypeEnum.ExcelLegacy, result);
        }

        [Fact]
        public void DetectMimeTypeFromBytes_ReturnsUnknown_OnlyWhenNoRegisteredDetectorMatches()
        {
            var registry = new MimeDetectionRegistry(new IMimeTypeDetector[] { new UnknownDetector(), new UnknownDetector() });

            var helper = new ExtractionHelper(registry);

            var result = helper.DetectMimeTypeFromBytes(new byte[] { 0x01, 0x02, 0x03 });

            Assert.Equal(OfficeMimeTypeEnum.OpenXmlUnknown, result);
        }
    }
}
