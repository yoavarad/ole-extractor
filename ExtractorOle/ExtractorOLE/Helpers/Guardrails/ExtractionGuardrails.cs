using ExtractorOLE.Helpers.Input;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.MimeDetection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;

namespace ExtractorOLE.Helpers.Guardrails
{
    /// <summary>
    /// Pre-parse guardrails for Extract ([ydk:nfr:extraction/max-file-size],
    /// [ydk:nfr:extraction/nesting-depth-guard]). Runs before any format dispatch or
    /// buffer allocation: rejects an over-limit byte length
    /// (<see cref="FileTooLargeException"/>) and ZIP/CFB headers whose DECLARED sizes
    /// exceed the caps (<see cref="OversizedNestedContentException"/>). Every rejection
    /// is logged through <see cref="ILogger"/> with file size and the configured limit; the
    /// caller-supplied filename is never logged (PII, CR/LF log forging). With no logger the
    /// guardrails are silent.
    /// </summary>
    public partial class ExtractionGuardrails
    {
        private readonly MimeDetectionLimits _limits;
        private readonly ILogger _logger;

        public ExtractionGuardrails(MimeDetectionLimits limits, ILogger<ExtractionGuardrails>? logger = null)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _logger = logger ?? NullLogger<ExtractionGuardrails>.Instance;
        }

        [LoggerMessage(EventId = 1, EventName = "FileTooLarge", Level = LogLevel.Warning,
            Message = "Extract rejected: file-too-large, actualBytes={ActualBytes}, limitBytes={LimitBytes}")]
        private static partial void LogFileTooLarge(ILogger logger, long actualBytes, long limitBytes);

        [LoggerMessage(EventId = 2, EventName = "OversizedNestedContent", Level = LogLevel.Warning,
            Message = "Extract rejected: oversized-nested-content, fileBytes={FileBytes}, limitBytes={LimitBytes}, {Metric}")]
        private static partial void LogOversizedNestedContent(ILogger logger, long fileBytes, long limitBytes, string metric);

        public void Enforce(ExtractionRequest request)
        {
            var bytes = request?.FileBytes;
            if (bytes == null) return;

            Enforce(bytes.LongLength, () => new MemoryStream(bytes, writable: false));
        }

        // Stream/path input (#192, #193): the size check uses the stream length, so an over-limit
        // file is rejected before anything is read; the declared-size header checks then read only
        // the headers they need from the stream, never buffering the whole file.
        internal void Enforce(ExtractionInput input) =>
            Enforce(input.Length, () => input.OriginalBytes != null
                ? new MemoryStream(input.OriginalBytes, writable: false)
                : input.Stream!);

        // openStream is only called once the length check passes; a byte[] is wrapped without copying.
        private void Enforce(long length, Func<Stream> openStream)
        {
            if (length > _limits.MaxFileSizeBytes)
            {
                LogFileTooLarge(_logger, length, _limits.MaxFileSizeBytes);
                throw new FileTooLargeException(length, _limits.MaxFileSizeBytes);
            }

            var stream = openStream();

            string? metric = null;
            long limit = 0;

            if (OoxmlMimeDetector.TryGetDeclaredZipTotalSize(stream, out var zipTotal, out var entries, out var largestEntry))
            {
                if (largestEntry > (ulong)_limits.MaxDeclaredEntryBytes)
                {
                    limit = _limits.MaxDeclaredEntryBytes;
                    metric = $"ZIP entry declares {largestEntry} bytes uncompressed (per-entry limit {limit})";
                }
                else if (zipTotal > (ulong)_limits.MaxDeclaredNestedContentBytes)
                {
                    limit = _limits.MaxDeclaredNestedContentBytes;
                    metric = $"ZIP local file header(s) ({entries} {(entries == 1 ? "entry" : "entries")}) declare {zipTotal} bytes total uncompressed (limit {limit})";
                }
            }
            else if (CfbMimeDetector.TryGetDeclaredCfbTotalSize(stream, out var cfbTotal, out var fatSectors, out var sectorSize)
                     && cfbTotal > (ulong)_limits.MaxDeclaredNestedContentBytes)
            {
                limit = _limits.MaxDeclaredNestedContentBytes;
                metric = $"CFB FAT sector count {fatSectors} at sector size {sectorSize} bytes implies {cfbTotal} bytes total (limit {limit})";
            }

            if (metric == null)
            {
                // Declared sizes are attacker-written; enforce the caps on bytes actually inflated.
                try
                {
                    ZipInflationGuard.Enforce(stream, _limits.MaxZipEntryCount, _limits.MaxDeclaredEntryBytes, _limits.MaxDeclaredNestedContentBytes);
                }
                catch (OversizedNestedContentException ex)
                {
                    metric = ex.DeclaredMetric;
                    limit = _limits.MaxDeclaredNestedContentBytes;
                }
            }

            if (metric != null)
            {
                LogOversizedNestedContent(_logger, length, limit, metric);
                throw new OversizedNestedContentException(metric);
            }
        }
    }
}
