using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.MimeDetection;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using Xunit;

// NOTE: The assembly-level [CollectionBehavior(DisableTestParallelization = true)]
// needed to keep these guard tests' assertions against the shared static
// OoxmlMimeDetector.PackageOpenAttemptCount counter race-free is already declared
// once, in CfbMimeDetectorGuardrailTests.cs, for the whole test assembly.

namespace ExtractorOLE.Tests.MimeDetection
{
    public class OoxmlMimeDetectorGuardrailTests
    {
        // Builds a minimal, real ZIP byte array (via System.IO.Compression itself)
        // containing a single stored entry. Duplicated locally from
        // OoxmlMimeDetectorTests to keep this file independent per the task's TDD
        // guidance -- these guardrail tests exercise a different concern (size /
        // declared-size limits) than the existing content-detection tests.
        private static byte[] BuildZipWithSingleEntry(string entryName, byte[] content)
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                using var entryStream = entry.Open();
                entryStream.Write(content, 0, content.Length);
            }

            return stream.ToArray();
        }

        [Fact]
        public void Detect_FileBytesOverMaxFileSize_ThrowsFileTooLargeException()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = new byte[11],
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 10 });

            var ex = Assert.Throws<FileTooLargeException>(() => detector.Detect(request));

            Assert.Equal(11, ex.ActualBytes);
            Assert.Equal(10, ex.LimitBytes);
        }

        [Fact]
        public void Detect_FileBytesOverMaxFileSize_DoesNotAttemptPackageOpen()
        {
            OoxmlMimeDetector.PackageOpenAttemptCount = 0;
            var request = new MimeDetectionRequest
            {
                FileBytes = new byte[11],
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 10 });

            Assert.Throws<FileTooLargeException>(() => detector.Detect(request));

            Assert.Equal(0, OoxmlMimeDetector.PackageOpenAttemptCount);
        }

        [Fact]
        public void Detect_ForgedLocalHeaderUncompressedSizeOver2GB_ThrowsOversizedNestedContentException()
        {
            var bytes = BuildZipWithSingleEntry("word/document.xml", new byte[] { 1, 2, 3 });
            // Local file header uncompressed-size field sits at fixed offset 22 from the
            // start of the first PK\x03\x04 record (PKZIP APPNOTE section 4.3.7).
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(22, 4), 3_000_000_000);
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector();

            var ex = Assert.Throws<OversizedNestedContentException>(() => detector.Detect(request));

            Assert.False(string.IsNullOrWhiteSpace(ex.DeclaredMetric));
        }

        [Fact]
        public void Detect_ForgedLocalHeaderUncompressedSizeOver2GB_DoesNotCallPackageOpen()
        {
            OoxmlMimeDetector.PackageOpenAttemptCount = 0;
            var bytes = BuildZipWithSingleEntry("word/document.xml", new byte[] { 1, 2, 3 });
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(22, 4), 3_000_000_000);
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector();

            Assert.Throws<OversizedNestedContentException>(() => detector.Detect(request));

            Assert.Equal(0, OoxmlMimeDetector.PackageOpenAttemptCount);
        }

        [Fact]
        public void Detect_ForgedZip64SentinelUncompressedSize_TreatedAsMaximallyOversized()
        {
            OoxmlMimeDetector.PackageOpenAttemptCount = 0;
            var bytes = BuildZipWithSingleEntry("word/document.xml", new byte[] { 1, 2, 3 });
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(22, 4), 0xFFFFFFFF);
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector();

            var ex = Assert.Throws<OversizedNestedContentException>(() => detector.Detect(request));

            Assert.False(string.IsNullOrWhiteSpace(ex.DeclaredMetric));
            Assert.Equal(0, OoxmlMimeDetector.PackageOpenAttemptCount);
        }

        [Fact]
        public void Detect_ChangingMaxFileSizeLimit_ChangesAcceptedBehavior()
        {
            var bytes = new byte[20];
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.docx"
            };

            var strictDetector = new OoxmlMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 10 });
            Assert.Throws<FileTooLargeException>(() => strictDetector.Detect(request));

            var defaultDetector = new OoxmlMimeDetector();
            var result = defaultDetector.Detect(request);
            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }

        [Fact]
        public void Detect_EmptyBytes_StillReturnsUnknown_EvenWithZeroLimit()
        {
            var request = new MimeDetectionRequest
            {
                FileBytes = Array.Empty<byte>(),
                FileName = "irrelevant.docx"
            };
            var detector = new OoxmlMimeDetector(new MimeDetectionLimits { MaxFileSizeBytes = 0 });

            var result = detector.Detect(request);

            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);
        }

        [Fact]
        public void Detect_ChangingMaxDeclaredNestedContentBytesLimit_ChangesAcceptedBehavior()
        {
            var bytes = BuildZipWithSingleEntry("word/document.xml", new byte[] { 1, 2, 3 });
            var request = new MimeDetectionRequest
            {
                FileBytes = bytes,
                FileName = "irrelevant.docx"
            };

            var defaultDetector = new OoxmlMimeDetector();
            var result = defaultDetector.Detect(request);
            Assert.Equal(DetectedFormatEnum.Unknown, result.DetectedFormat);

            var strictDetector = new OoxmlMimeDetector(new MimeDetectionLimits { MaxDeclaredNestedContentBytes = 1 });
            Assert.Throws<OversizedNestedContentException>(() => strictDetector.Detect(request));
        }
    }
}
