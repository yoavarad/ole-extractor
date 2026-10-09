using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.MimeDetection;
using OpenMcdf;
using System;
using System.Buffers.Binary;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.MimeDetection
{
    public class CfbMimeDetectorGuardrailTests
    {
        // Builds a minimal, real CFB byte array (via OpenMcdf itself) containing a
        // single root-level stream with the given name. Duplicated locally from
        // CfbMimeDetectorTests to keep this file independent per the task's TDD
        // guidance -- these guardrail tests exercise a different concern (size /
        // declared-size limits) than the existing content-detection tests.
        private static byte[] BuildCfbWithRootEntry(string entryName)
        {
            using var stream = new MemoryStream();
            using (var root = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
            {
                using var entryStream = root.CreateStream(entryName);
                var data = new byte[] { 1, 2, 3 };
                entryStream.Write(data, 0, data.Length);
            }

            return stream.ToArray();
        }

        [Fact]
        public void Detect_FileBytesOverMaxFileSize_ThrowsFileTooLargeException()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = new byte[11],
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 10 });

            var ex = Assert.Throws<FileTooLargeException>(() => detector.Detect(request));

            Assert.Equal(11, ex.ActualBytes);
            Assert.Equal(10, ex.LimitBytes);
        }

        [Fact]
        public void Detect_FileBytesOverMaxFileSize_DoesNotAttemptOpenMcdfParse()
        {
            CfbMimeDetector.OpenMcdfParseAttemptCount = 0;
            var request = new MimeDetectionRequest
            {
                FileBytes = new byte[11],
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 10 });

            Assert.Throws<FileTooLargeException>(() => detector.Detect(request));

            Assert.Equal(0, CfbMimeDetector.OpenMcdfParseAttemptCount);
        }

        [Fact]
        public void Detect_ForgedFatSectorCountImplyingOver2GB_ThrowsOversizedNestedContentException()
        {
            var bytes = BuildCfbWithRootEntry("SomeStream");
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x2C, 4), 100000);
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var ex = Assert.Throws<OversizedNestedContentException>(() => detector.Detect(request));

            Assert.False(string.IsNullOrWhiteSpace(ex.DeclaredMetric));
        }

        [Fact]
        public void Detect_ForgedFatSectorCountImplyingOver2GB_DoesNotCallOpenMcdfDirectoryWalk()
        {
            CfbMimeDetector.OpenMcdfParseAttemptCount = 0;
            var bytes = BuildCfbWithRootEntry("SomeStream");
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x2C, 4), 100000);
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            Assert.Throws<OversizedNestedContentException>(() => detector.Detect(request));

            Assert.Equal(0, CfbMimeDetector.OpenMcdfParseAttemptCount);
        }

        [Fact]
        public void Detect_ChangingMaxFileSizeLimit_ChangesAcceptedBehavior()
        {
            var bytes = new byte[20];
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.bin"
            };

            var strictDetector = new CfbMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 10 });
            Assert.Throws<FileTooLargeException>(() => strictDetector.Detect(request));

            var defaultDetector = new CfbMimeDetector();
            var result = defaultDetector.Detect(request);
            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }

        [Fact]
        public void Detect_EmptyBytes_StillReturnsUnknown_EvenWithZeroLimit()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = Array.Empty<byte>(),
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 0 });

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }

        [Fact]
        public void Detect_ForgedSectorShiftOutsideValidRange_TreatedAsMaximallyOversized()
        {
            CfbMimeDetector.OpenMcdfParseAttemptCount = 0;
            var bytes = BuildCfbWithRootEntry("SomeStream");
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x1E, 2), 20);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x2C, 4), 0x04000000);
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.bin"
            };
            var detector = new CfbMimeDetector();

            var ex = Assert.Throws<OversizedNestedContentException>(() => detector.Detect(request));

            Assert.False(string.IsNullOrWhiteSpace(ex.DeclaredMetric));
            Assert.Equal(0, CfbMimeDetector.OpenMcdfParseAttemptCount);
        }

        [Fact]
        public void Detect_ChangingMaxDeclaredNestedContentBytesLimit_ChangesAcceptedBehavior()
        {
            var bytes = BuildCfbWithRootEntry("SomeStream");
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.bin"
            };

            var defaultDetector = new CfbMimeDetector();
            var result = defaultDetector.Detect(request);
            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);

            var strictDetector = new CfbMimeDetector(new MimeDetectionLimits { MaxDeclaredNestedContentBytes = 1000 });
            Assert.Throws<OversizedNestedContentException>(() => strictDetector.Detect(request));
        }
    }
}
