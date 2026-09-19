using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.MimeDetection;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    /// <summary>
    /// Covers T-09102f84 (DetectMimeType half): the public DetectMimeType entry points
    /// (<see cref="IExtractionHelper.DetectMimeType"/> / <see cref="IExtractionHelper.DetectMimeTypeFromBytes"/>)
    /// raise file-too-large for a 100MB+1 byte input BEFORE any format-specific parsing
    /// ([ydk:nfr:extraction/max-file-size]). The Extract half is covered by
    /// MainExtractorGuardrailTests; per-detector tests by the MimeDetection guardrail tests.
    /// "Before parsing" is verified via the detectors' parse-attempt counters (assembly-level
    /// test parallelization is disabled in CfbMimeDetectorGuardrailTests.cs, so the shared
    /// static counters are race-free) and a trailing spy detector that must never be reached.
    /// </summary>
    public class ExtractionHelperFileTooLargeTests
    {
        private const int DefaultLimitBytes = 104_857_600;

        private sealed class SpyDetector : IMimeTypeDetector
        {
            public int Calls { get; private set; }

            public MimeDetectionResult Detect(MimeDetectionRequest request)
            {
                Calls++;
                return new MimeDetectionResult
                {
                    DetectedFormat = DetectedFormatEnum.Unknown,
                    MimeType = "application/octet-stream",
                    IsSupported = false
                };
            }
        }

        private static (IExtractionHelper helper, MimeDetectionLimits limits, SpyDetector trailingSpy) BuildThroughProductionDi()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var real = provider.GetRequiredService<IMimeDetectionRegistry>();
            var spy = new SpyDetector();
            var withSpy = new MimeDetectionRegistry(new System.Collections.Generic.List<IMimeTypeDetector>(real.Detectors) { spy });
            return (new ExtractionHelper(withSpy), provider.GetRequiredService<MimeDetectionLimits>(), spy);
        }

        private static void ResetParseCounters()
        {
            OoxmlMimeDetector.PackageOpenAttemptCount = 0;
            CfbMimeDetector.OpenMcdfParseAttemptCount = 0;
        }

        private static void AssertNoFormatParsingHappened(SpyDetector trailingSpy)
        {
            Assert.Equal(0, OoxmlMimeDetector.PackageOpenAttemptCount);
            Assert.Equal(0, CfbMimeDetector.OpenMcdfParseAttemptCount);
            Assert.Equal(0, trailingSpy.Calls);
        }

        [Fact]
        public void DetectMimeType_100MBPlusOneByte_ThrowsFileTooLarge_BeforeAnyFormatParsing()
        {
            var (helper, _, spy) = BuildThroughProductionDi();
            ResetParseCounters();
            var request = new MimeDetectionRequest { FileBytes = new byte[DefaultLimitBytes + 1], FileName = "big.docx" };

            var ex = Assert.Throws<FileTooLargeException>(() => helper.DetectMimeType(request));

            Assert.Equal(DefaultLimitBytes + 1, ex.ActualBytes);
            Assert.Equal(DefaultLimitBytes, ex.LimitBytes);
            AssertNoFormatParsingHappened(spy);
        }

        [Fact]
        public void DetectMimeTypeFromBytes_100MBPlusOneByte_ThrowsFileTooLarge_BeforeAnyFormatParsing()
        {
            var (helper, _, spy) = BuildThroughProductionDi();
            ResetParseCounters();

            var ex = Assert.Throws<FileTooLargeException>(() => helper.DetectMimeTypeFromBytes(new byte[DefaultLimitBytes + 1]));

            Assert.Equal(DefaultLimitBytes + 1, ex.ActualBytes);
            Assert.Equal(DefaultLimitBytes, ex.LimitBytes);
            AssertNoFormatParsingHappened(spy);
        }

        [Fact]
        public void DetectMimeType_MaxFileSizeIsReadFromConfiguration_ChangingLimitChangesAcceptedBehavior()
        {
            var (helper, limits, spy) = BuildThroughProductionDi();
            ResetParseCounters();
            var request = new MimeDetectionRequest { FileBytes = new byte[20], FileName = "small.docx" };

            limits.MaxFileSizeBytes = 10;
            var ex = Assert.Throws<FileTooLargeException>(() => helper.DetectMimeType(request));
            Assert.Equal(10, ex.LimitBytes);
            AssertNoFormatParsingHappened(spy);

            limits.MaxFileSizeBytes = 20; // exactly at the limit: accepted
            var result = helper.DetectMimeType(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
            Assert.Equal(1, spy.Calls); // ran through every detector, none rejected it
        }
    }
}
