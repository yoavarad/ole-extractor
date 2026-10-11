using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using System;

namespace ExtractorOLE
{
    public class MainExtractor
    {
        private readonly IExtractionHelper _helper;

        // Format dispatch is resolved entirely through this registry
        // (ydk:req:extraction/format-extensibility) - MainExtractor never
        // instantiates or references a specific IOpenStrategy/ITextExtractor
        // implementation, or its backing library, directly.
        private readonly IFormatDispatchRegistry _registry;

        // Pre-parse file-too-large / oversized-nested-content checks (T-3335d397).
        private readonly ExtractionGuardrails _guardrails;

        public MainExtractor(IExtractionHelper helper, IFormatDispatchRegistry registry)
            : this(helper, registry, new ExtractionGuardrails(new MimeDetectionLimits()))
        {
        }

        public MainExtractor(IExtractionHelper helper, IFormatDispatchRegistry registry, ExtractionGuardrails guardrails)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _guardrails = guardrails ?? throw new ArgumentNullException(nameof(guardrails));
        }

        private IOpenStrategy? FindOpenStrategyForMime(OfficeMimeTypeEnum mimeType)
        {
            return _registry.GetOpenStrategy(mimeType);
        }

        private static readonly OfficeMimeTypeEnum[] SupportedFormats =
        {
            OfficeMimeTypeEnum.Word, OfficeMimeTypeEnum.Excel, OfficeMimeTypeEnum.PowerPoint,
            OfficeMimeTypeEnum.WordLegacy, OfficeMimeTypeEnum.ExcelLegacy, OfficeMimeTypeEnum.PowerPointLegacy,
        };

        /// <summary>
        /// Extraction entry point ([ydk:contract:extraction/extract]). Trusts
        /// <see cref="ExtractionRequest.DetectedMimeType"/> as-is (no re-detection) and
        /// dispatches to the open + text-extraction components registered for that format
        /// in <see cref="IFormatDispatchRegistry"/>; contains no format-specific parsing.
        /// A null request or null required field throws <see cref="ArgumentNullException"/>
        /// first; then an invalid input source (more than one of FileBytes/InputStream/FilePath, or a
        /// non-seekable stream) throws <see cref="ArgumentException"/> and a FilePath that cannot be
        /// opened throws the FileStream I/O exception; then the pre-parse guardrails reject file-too-large and
        /// oversized-nested-content input; then a mime type that is not one of the six
        /// supported formats throws <see cref="Exceptions.UnsupportedFormatException"/>;
        /// then a parse-time preflight and open raise the specific
        /// password-protected / truncated-container / corrupt-file errors instead of
        /// returning an empty result.
        /// </summary>
        public DocumentExtractionResult Extract(ExtractionRequest request)
        {
            ValidateRequest(request);
            // Owns the FileStream opened for FilePath; disposed after ReleaseParsedDocument below,
            // because an OOXML package opened from it reads the stream lazily (ADR-008).
            using var input = ExtractionInput.From(request);
            var mime = request.DetectedMimeType;
            var size = input.Length;
            using (ExtractionTelemetry.Stage("guardrails", mime, size))
                _guardrails.Enforce(request, input); // after null checks, before any parsing/dispatch (T-3335d397)

            var format = ResolveFormat(request.DetectedMimeType)
                         ?? throw new Exceptions.UnsupportedFormatException(request.DetectedMimeType);

            using (ExtractionTelemetry.Stage("preflight", mime, size))
                ParseTimeErrorGuard.Preflight(input.Bytes, IsOoxml(format));
            // Stream/path input: drop the buffer guardrails/preflight needed before the format
            // library opens the stream (#193 removes that buffer).
            input.ReleaseBytes();
            DocumentExtractionResult result;
            using (ExtractionTelemetry.Stage("open", mime, size))
                result = ParseTimeErrorGuard.OpenOrThrow(_registry.GetOpenStrategy(format), input, request.DetectedMimeType);
            try
            {
                result.MimeType = request.DetectedMimeType;

                using var textScope = ExtractionTelemetry.Stage("extract_text", mime, size);
                var extractor = _registry.GetTextExtractor(format);
                if (extractor != null)
                {
                    // Byte fallback for stream input re-buffers the file (only when the extractor
                    // cannot read ParsedDocument). An open OOXML package seeks before every read, so
                    // rewinding the shared stream here is safe.
                    result.ExtractedText = ExtractText(extractor, result, () => input.Bytes);
                }
            }
            finally
            {
                ReleaseParsedDocument(result);
            }

            return result;
        }

        /// <summary>
        /// Batch form of <see cref="Extract(ExtractionRequest)"/> (ADR-007). Runs up to
        /// <paramref name="maxDegreeOfParallelism"/> (default <see cref="Environment.ProcessorCount"/>)
        /// extractions at once and returns one <see cref="BatchExtractionResult"/> per request, in
        /// input order. An item's exception is captured in its <see cref="BatchExtractionResult.Error"/>
        /// and does not fail the batch (except <see cref="OutOfMemoryException"/>, which fails it). Cancellation stops new items from starting and throws
        /// <see cref="OperationCanceledException"/>.
        /// </summary>
        public async Task<IReadOnlyList<BatchExtractionResult>> ExtractManyAsync(
            IEnumerable<ExtractionRequest> requests,
            int? maxDegreeOfParallelism = null,
            CancellationToken cancellationToken = default)
        {
            if (requests == null) throw new ArgumentNullException(nameof(requests));
            var dop = maxDegreeOfParallelism ?? Environment.ProcessorCount;
            if (dop <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), dop, "Must be positive.");

            var results = new List<BatchExtractionResult>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken };
            await Parallel.ForEachAsync(requests.Select((request, index) => (request, index)), options, (item, _) =>
            {
                BatchExtractionResult itemResult;
                try
                {
                    itemResult = new BatchExtractionResult { Index = item.index, FileName = item.request?.FileName, Result = Extract(item.request!) };
                }
                catch (Exception ex) when (ex is not OutOfMemoryException)
                {
                    itemResult = new BatchExtractionResult { Index = item.index, FileName = item.request?.FileName, Error = ex };
                }
                lock (results) results.Add(itemResult);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

            results.Sort((a, b) => a.Index.CompareTo(b.Index));
            return results;
        }

        // Boundary validation: runs before any detection/extraction logic.
        private static void ValidateRequest(ExtractionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.FileBytes == null) throw new ArgumentNullException(nameof(request.FileBytes));
            if (request.FileName == null) throw new ArgumentNullException(nameof(request.FileName));
            if (request.DetectedMimeType == null) throw new ArgumentNullException(nameof(request.DetectedMimeType));
        }

        private static bool IsOoxml(OfficeMimeTypeEnum format) =>
            format is OfficeMimeTypeEnum.Word or OfficeMimeTypeEnum.Excel or OfficeMimeTypeEnum.PowerPoint;

        // Reverse of IExtractionHelper.MimeFor over the six supported formats.
        private OfficeMimeTypeEnum? ResolveFormat(string? mime)
        {
            foreach (var format in SupportedFormats)
            {
                if (string.Equals(_helper.MimeFor(format), mime, StringComparison.OrdinalIgnoreCase)) return format;
            }
            return null;
        }

        public DocumentExtractionResult Extract(byte[] fileBytes)
        {
            var result = new DocumentExtractionResult();

            string? coreMime = null;
            // detect package type as enum
            var packageType = _helper.DetectMimeTypeFromBytes(fileBytes);

            // Try strategy based on detected package MIME first            
            var strategy = FindOpenStrategyForMime(packageType);
            if (strategy != null)
            {
                try
                {
                    var stratResult = strategy.Open(fileBytes);
                    if (stratResult != null)
                    {
                        if (!string.IsNullOrEmpty(stratResult.MimeType)) coreMime = stratResult.MimeType;
                        result = stratResult;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"failed running on found Mime type, switching to fallback. error: {e.Message}");
                    // strategy failed - fall back to trial open order below
                }
            }

            try
            {
                // Determine final mime: priority to coreMime, then package type, then magic
                OfficeMimeTypeEnum finalMimeType = OfficeMimeTypeEnum.OpenXmlUnknown;
                if (packageType != OfficeMimeTypeEnum.OpenXmlUnknown)
                {
                    finalMimeType = packageType;

                }
                else if (!string.IsNullOrEmpty(coreMime))
                {
                    finalMimeType = _helper.ParseOfficeMimeType(coreMime);
                }
                else
                {
                    // fallback via magic bytes
                    var magic = _helper.DetectMimeFromMagicBytes(fileBytes);
                    finalMimeType = _helper.ParseOfficeMimeType(magic);
                }

                string finalMime = _helper.MimeFor(finalMimeType) ?? coreMime ?? "application/octet-stream";

                result.MimeType = finalMime;

                // Text extraction via registry (enum keyed)
                var extractor = FindHandlerForMime(finalMimeType);
                if (extractor != null)
                {
                    try
                    {
                        result.ExtractedText = ExtractText(extractor, result, () => fileBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Text extraction failed: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"No text extractor registered for MIME '{finalMime}'");
                }
            }
            finally
            {
                ReleaseParsedDocument(result);
            }

            return result;
        }

        // Reuses what the open strategy already parsed (#168, #167) when the extractor can take
        // it; otherwise extracts from the raw bytes, read only on that fallback.
        private static string ExtractText(ITextExtractor extractor, DocumentExtractionResult result, Func<byte[]> fileBytes)
        {
            object? parsed = result.ParsedDocument;

            string? text = parsed != null && extractor is IParsedDocumentTextExtractor parsedExtractor
                ? parsedExtractor.ExtractText(parsed)
                : null;
            return text ?? extractor.ExtractText(fileBytes()) ?? string.Empty;
        }

        // MainExtractor owns the handed-off parsed document: an open OOXML package (#167) is
        // disposed here, after text extraction, whether or not an extractor consumed it.
        private static void ReleaseParsedDocument(DocumentExtractionResult result)
        {
            var parsed = result.ParsedDocument;
            result.ParsedDocument = null;
            (parsed as IDisposable)?.Dispose();
        }

        private ITextExtractor? FindHandlerForMime(OfficeMimeTypeEnum mimeType)
        {
            var exact = _registry.GetTextExtractor(mimeType);
            if (exact != null) return exact;

            // fallback: if unknown openxml package, prefer Word then Excel
            if (mimeType == OfficeMimeTypeEnum.OpenXmlUnknown)
            {
                var w = _registry.GetTextExtractor(OfficeMimeTypeEnum.Word);
                if (w != null) return w;
                var x = _registry.GetTextExtractor(OfficeMimeTypeEnum.Excel);
                if (x != null) return x;
                var y = _registry.GetTextExtractor(OfficeMimeTypeEnum.PowerPoint);
                if (y != null) return y;
            }

            return null;
        }

    }
}
