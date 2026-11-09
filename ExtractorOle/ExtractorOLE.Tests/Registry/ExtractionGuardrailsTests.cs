using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            new ExtractionGuardrails(limits).Enforce(new ExtractionRequest { FileName = "x.docx", FileBytes = bytes });

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

        // #280: the caller-supplied FileName is PII and attacker-controlled (CR/LF log forging),
        // so rejections log sizes and limits through ILogger but never the filename.
        private const string HostileFileName = "Jane Doe salary.docx\r\n[Extract] forged-entry: admin=true";

        [Fact]
        public void FileTooLarge_LogsWarningWithoutFileNameOrNewline()
        {
            var logger = new CapturingLogger<ExtractionGuardrails>();
            var guardrails = new ExtractionGuardrails(new MimeDetectionLimits { MaxFileSizeBytes = 10 }, logger);

            Assert.Throws<FileTooLargeException>(() =>
                guardrails.Enforce(new ExtractionRequest { FileName = HostileFileName, FileBytes = new byte[11] }));

            AssertSingleRedactedWarning(logger, "file-too-large");
            Assert.Contains(logger.States[0], kv => kv.Key == "ActualBytes" && Equals(kv.Value, 11L));
            Assert.Contains(logger.States[0], kv => kv.Key == "LimitBytes" && Equals(kv.Value, 10L));
        }

        [Fact]
        public void OversizedNestedContent_LogsWarningWithoutFileNameOrNewline()
        {
            var logger = new CapturingLogger<ExtractionGuardrails>();
            var guardrails = new ExtractionGuardrails(new MimeDetectionLimits { MaxZipEntryCount = 5 }, logger);

            Assert.Throws<OversizedNestedContentException>(() =>
                guardrails.Enforce(new ExtractionRequest { FileName = HostileFileName, FileBytes = BuildZip(20, 10) }));

            AssertSingleRedactedWarning(logger, "oversized-nested-content");
        }

        [Fact]
        public void NullLogger_DefaultConstruction_StillRejects()
        {
            var guardrails = new ExtractionGuardrails(new MimeDetectionLimits { MaxFileSizeBytes = 10 }, logger: null);

            Assert.Throws<FileTooLargeException>(() =>
                guardrails.Enforce(new ExtractionRequest { FileName = "x.docx", FileBytes = new byte[11] }));
        }

        private static void AssertSingleRedactedWarning(CapturingLogger<ExtractionGuardrails> logger, string reason)
        {
            var message = Assert.Single(logger.Messages);
            Assert.Contains(reason, message, StringComparison.Ordinal);
            Assert.DoesNotContain("Jane Doe", message, StringComparison.Ordinal);
            Assert.DoesNotContain("forged-entry", message, StringComparison.Ordinal);
            Assert.DoesNotContain('\r', message);
            Assert.DoesNotContain('\n', message);
            Assert.DoesNotContain(logger.States[0], kv => kv.Value is string s && (s.Contains("Jane Doe", StringComparison.Ordinal) || s.IndexOfAny(['\r', '\n']) >= 0));
            Assert.Equal(LogLevel.Warning, Assert.Single(logger.Levels));
        }
    }

    /// <summary>#280: a library must be silent on stdout/stderr when no logger is configured.</summary>
    [Collection("ConsoleRedirect")]
    public class ExtractionGuardrailsConsoleTests
    {
        [Fact]
        public void RejectedExtraction_WithDefaultDiAndNoLogger_WritesNothingToConsole()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services, new MimeDetectionLimits { MaxFileSizeBytes = 10 });
            using var provider = services.BuildServiceProvider();
            var extractor = provider.GetRequiredService<MainExtractor>();

            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var captured = new StringWriter();
            Console.SetOut(captured);
            Console.SetError(captured);
            try
            {
                Assert.Throws<FileTooLargeException>(() => extractor.Extract(new ExtractionRequest
                {
                    FileName = "secret.docx",
                    FileBytes = new byte[11],
                    DetectedMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                }));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }

            Assert.Equal(string.Empty, captured.ToString());
        }
    }
}
