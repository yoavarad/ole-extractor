using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.MimeDetection;
using System;

namespace ExtractorOLE.Helpers
{
    /// <summary>
    /// Pre-parse guardrails for Extract ([ydk:nfr:extraction/max-file-size],
    /// [ydk:nfr:extraction/nesting-depth-guard]). Runs before any format dispatch or
    /// buffer allocation: rejects an over-limit byte length
    /// (<see cref="FileTooLargeException"/>) and ZIP/CFB headers whose DECLARED sizes
    /// exceed the caps (<see cref="OversizedNestedContentException"/>). Every rejection
    /// is logged with file size, filename and the configured limit.
    /// </summary>
    public class ExtractionGuardrails
    {
        private readonly MimeDetectionLimits _limits;
        private readonly Action<string> _log;

        public ExtractionGuardrails(MimeDetectionLimits limits, Action<string>? logRejection = null)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
            _log = logRejection ?? (message => Console.WriteLine(message));
        }

        public void Enforce(ExtractionRequest request)
        {
            var bytes = request?.FileBytes;
            if (bytes == null) return;

            if (bytes.Length > _limits.MaxFileSizeBytes)
            {
                _log($"[Extract] file-too-large: fileName='{request!.FileName}', actualBytes={bytes.Length}, limitBytes={_limits.MaxFileSizeBytes}");
                throw new FileTooLargeException(bytes.Length, _limits.MaxFileSizeBytes);
            }

            string? metric = null;
            long limit = 0;

            if (OoxmlMimeDetector.TryGetDeclaredZipTotalSize(bytes, out var zipTotal, out var entries, out var largestEntry))
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
            else if (CfbMimeDetector.TryGetDeclaredCfbTotalSize(bytes, out var cfbTotal, out var fatSectors, out var sectorSize)
                     && cfbTotal > (ulong)_limits.MaxDeclaredNestedContentBytes)
            {
                limit = _limits.MaxDeclaredNestedContentBytes;
                metric = $"CFB FAT sector count {fatSectors} at sector size {sectorSize} bytes implies {cfbTotal} bytes total (limit {limit})";
            }

            if (metric != null)
            {
                _log($"[Extract] oversized-nested-content: fileName='{request!.FileName}', fileBytes={bytes.Length}, limitBytes={limit}, {metric}");
                throw new OversizedNestedContentException(metric);
            }
        }
    }
}
