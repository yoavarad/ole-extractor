using ExtractorOLE.Abstractions;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

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

        // Non-DI entry point: custom limits feed the guardrails. Pass the same instance to
        // new ExtractionHelper(limits) so the mime detectors share it.
        public MainExtractor(IExtractionHelper helper, IFormatDispatchRegistry registry, MimeDetectionLimits limits)
            : this(helper, registry, new ExtractionGuardrails(limits ?? throw new ArgumentNullException(nameof(limits))))
        {
        }

        public MainExtractor(IExtractionHelper helper, IFormatDispatchRegistry registry, ExtractionGuardrails guardrails)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _guardrails = guardrails ?? throw new ArgumentNullException(nameof(guardrails));
        }

        private static readonly OfficeMimeType[] SupportedFormats =
        {
            OfficeMimeType.Word, OfficeMimeType.Excel, OfficeMimeType.PowerPoint,
            OfficeMimeType.WordLegacy, OfficeMimeType.ExcelLegacy, OfficeMimeType.PowerPointLegacy,
        };

        /// <summary>
        /// Extraction entry point ([ydk:contract:extraction/extract]). Trusts
        /// <see cref="ExtractionRequest.DetectedMimeType"/> as-is (no re-detection) and
        /// dispatches to the open + text-extraction components registered for that format
        /// in <see cref="IFormatDispatchRegistry"/>; contains no format-specific parsing.
        /// A null request or null required field throws <see cref="ArgumentNullException"/> (ParamName "request"; the message names the null field)
        /// first; then an invalid input source (more than one of FileBytes/InputStream/FilePath, or a
        /// non-seekable stream) throws <see cref="ArgumentException"/> and a FilePath that cannot be
        /// opened throws the FileStream I/O exception; then the pre-parse guardrails reject file-too-large and
        /// oversized-nested-content input; then a mime type that is not one of the six
        /// supported formats throws <see cref="Exceptions.UnsupportedFormatException"/>;
        /// then a parse-time preflight and open raise the specific
        /// password-protected / truncated-container / corrupt-file errors instead of
        /// returning an empty result.
        /// </summary>
        public DocumentExtractionResult Extract(ExtractionRequest request) => Extract(request, CancellationToken.None);

        /// <summary>
        /// <see cref="Extract(ExtractionRequest)"/> with cancellation (#290). After request validation,
        /// an already-cancelled token throws <see cref="OperationCanceledException"/> before the input is
        /// opened or parsed. A token cancelled during the call is observed at the next stage boundary
        /// (guardrails, preflight, open, text extraction) and inside the built-in sheet, slide and
        /// embedded-file loops; the call then throws <see cref="OperationCanceledException"/> and returns
        /// no partial result. Cancellation is never recorded as a field error. The legacy .doc/.ppt
        /// conversion (b2xtranslator) has no cancellation hook, so one conversion runs to completion and
        /// the token is checked only before and after it.
        /// </summary>
        public DocumentExtractionResult Extract(ExtractionRequest request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);
            cancellationToken.ThrowIfCancellationRequested();
            using var cancellationScope = ExtractionCancellation.Enter(cancellationToken);
            // Owns the FileStream opened for FilePath; disposed after ReleaseParsedDocument below,
            // because an OOXML package opened from it reads the stream lazily (ADR-008).
            using var input = ExtractionInput.From(request);
            var resolved = ResolveFormat(request.DetectedMimeType);
            // Bounded telemetry tag: caller-supplied mime never reaches a tag/label (#284).
            var mime = resolved?.ToString() ?? ExtractionTelemetry.UnsupportedFormatTag;
            var size = input.Length;
            ExtractionTelemetry.RecordBytes(size, mime);
            cancellationToken.ThrowIfCancellationRequested();
            using (ExtractionTelemetry.Stage("guardrails", mime, size))
                _guardrails.Enforce(input); // after null checks, before any parsing/dispatch (T-3335d397)

            var format = resolved
                         ?? throw new Exceptions.UnsupportedFormatException(request.DetectedMimeType);

            cancellationToken.ThrowIfCancellationRequested();
            using (ExtractionTelemetry.Stage("preflight", mime, size))
            {
                // Stream/path input is checked in place, reading only headers/tail (#193).
                if (input.OriginalBytes != null) ParseTimeErrorGuard.Preflight(input.OriginalBytes, IsOoxml(format));
                else ParseTimeErrorGuard.Preflight(input.Stream!, IsOoxml(format));
            }
            cancellationToken.ThrowIfCancellationRequested();
            DocumentExtractionResult result;
            using (ExtractionTelemetry.Stage("open", mime, size))
                result = ParseTimeErrorGuard.OpenOrThrow(_registry.GetOpenStrategy(format), input, request.DetectedMimeType);
            try
            {
                result.MimeType = request.DetectedMimeType;
                cancellationToken.ThrowIfCancellationRequested();

                using var textScope = ExtractionTelemetry.Stage("extract_text", mime, size);
                var extractor = _registry.GetTextExtractor(format);
                if (extractor != null)
                {
                    // Byte fallback for stream input re-buffers the file (only when the extractor
                    // cannot read ParsedDocument). An open OOXML package seeks before every read, so
                    // rewinding the shared stream here is safe.
                    // A throwing extractor leaves the empty default and records one FieldError. A failure
                    // confined to one part (one worksheet) keeps the text of the other parts and still
                    // records one FieldError at ExtractedText (#288).
                    FieldIsolation.TryPopulate(result, nameof(DocumentExtractionResult.ExtractedText), () =>
                    {
                        ExceptionDispatchInfo? partFailure = null;
                        result.ExtractedText = ExtractText(extractor, result, () => input.Bytes,
                            ex => partFailure ??= ExceptionDispatchInfo.Capture(ex));
                        partFailure?.Throw();
                    });
                }
                cancellationToken.ThrowIfCancellationRequested();
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
        /// and does not fail the batch (except <see cref="OutOfMemoryException"/>, which fails it). Cancellation stops new items from starting,
        /// is observed by in-flight items through <see cref="Extract(ExtractionRequest, CancellationToken)"/>, and throws
        /// <see cref="OperationCanceledException"/>. Every result (including embedded file bytes) is kept until
        /// the batch returns, so memory grows with batch size; use <see cref="ExtractManyStreamingAsync"/> for
        /// large batches (ADR-011).
        /// </summary>
        public async Task<IReadOnlyList<BatchExtractionResult>> ExtractManyAsync(
            IEnumerable<ExtractionRequest> requests,
            int? maxDegreeOfParallelism = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requests);
            var dop = maxDegreeOfParallelism ?? Environment.ProcessorCount;
            if (dop <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), dop, "Must be positive.");

            var results = new List<BatchExtractionResult>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken };
            await Parallel.ForEachAsync(requests.Select((request, index) => (request, index)), options, (item, itemToken) =>
            {
                var itemResult = ExtractItem(item.request, item.index, itemToken);
                lock (results) results.Add(itemResult);
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);

            results.Sort((a, b) => a.Index.CompareTo(b.Index));
            return results;
        }

        /// <summary>
        /// Streaming form of <see cref="ExtractManyAsync"/> for batches too large to hold every result at
        /// once (ADR-011). Yields one <see cref="BatchExtractionResult"/> per request in completion order,
        /// not input order; use <see cref="BatchExtractionResult.Index"/> to re-key. Item errors, the
        /// <see cref="OutOfMemoryException"/> handling, and cancellation behave as in
        /// <see cref="ExtractManyAsync"/>; a batch failure or cancellation is thrown from the enumeration.
        /// Memory is bounded by back-pressure: each of the <paramref name="maxDegreeOfParallelism"/> workers holds at
        /// most one result (being extracted, or finished and waiting to be queued) and at most
        /// <paramref name="maxDegreeOfParallelism"/> results are queued, so no more than 2 x MaxDOP + 1 results
        /// (counting the one the consumer is handling, plus any the caller keeps) are alive at once, whatever the
        /// batch size. A slow consumer pauses extraction rather than letting results pile up. Disposing the
        /// enumerator early stops new items from starting and waits for in-flight items to end; a caller that
        /// abandons it without disposing leaves the workers blocked. A null <paramref name="requests"/> or a
        /// non-positive <paramref name="maxDegreeOfParallelism"/> throws when this method is called.
        /// </summary>
        public IAsyncEnumerable<BatchExtractionResult> ExtractManyStreamingAsync(
            IEnumerable<ExtractionRequest> requests,
            int? maxDegreeOfParallelism = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(requests);
            var dop = maxDegreeOfParallelism ?? Environment.ProcessorCount;
            if (dop <= 0) throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), dop, "Must be positive.");
            return StreamItemsAsync(requests, dop, cancellationToken);
        }

        private async IAsyncEnumerable<BatchExtractionResult> StreamItemsAsync(
            IEnumerable<ExtractionRequest> requests, int dop, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Linked so that disposing the enumerator early can stop the producer.
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var channel = Channel.CreateBounded<BatchExtractionResult>(
                new BoundedChannelOptions(dop) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
            var producer = ProduceAsync(requests, dop, channel.Writer, stop.Token);
            try
            {
                // Cancellation stops the read at once instead of draining results already buffered.
                await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    // ReadAllAsync drains already-available items without re-checking the token.
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }
            }
            finally
            {
                // Always observe the producer. On early dispose stop it first. A batch failure
                // (OutOfMemoryException, enumerator exception) is rethrown; its own cancellation is not.
                if (!producer.IsCompleted) await stop.CancelAsync().ConfigureAwait(false);
                try
                {
                    await producer.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stop.IsCancellationRequested)
                {
                    // Expected: the caller cancelled or the consumer stopped early.
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        private async Task ProduceAsync(IEnumerable<ExtractionRequest> requests, int dop,
            ChannelWriter<BatchExtractionResult> writer, CancellationToken cancellationToken)
        {
            try
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = dop, CancellationToken = cancellationToken };
                await Parallel.ForEachAsync(requests.Select((request, index) => (request, index)), options,
                    (item, itemToken) => writer.WriteAsync(ExtractItem(item.request, item.index, itemToken), itemToken))
                    .ConfigureAwait(false);
            }
            finally
            {
                writer.Complete();
            }
        }

        // One batch item: an item's exception becomes its Error. The batch's own cancellation and
        // OutOfMemoryException propagate and fail the batch; they are not item errors.
        private BatchExtractionResult ExtractItem(ExtractionRequest? request, int index, CancellationToken itemToken)
        {
            try
            {
                return new BatchExtractionResult { Index = index, FileName = request?.FileName, Result = Extract(request!, itemToken) };
            }
            catch (Exception ex) when (ex is not OutOfMemoryException
                                       && !(ex is OperationCanceledException && itemToken.IsCancellationRequested))
            {
                return new BatchExtractionResult { Index = index, FileName = request?.FileName, Error = ex };
            }
        }

        // Boundary validation: runs before any detection/extraction logic.
        private static void ValidateRequest(ExtractionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            // ParamName is the method parameter ("request"); the message names the null property.
            if (request.FileBytes == null) throw new ArgumentNullException(nameof(request), "ExtractionRequest.FileBytes must not be null.");
            if (request.FileName == null) throw new ArgumentNullException(nameof(request), "ExtractionRequest.FileName must not be null.");
            if (request.DetectedMimeType == null) throw new ArgumentNullException(nameof(request), "ExtractionRequest.DetectedMimeType must not be null.");
        }

        private static bool IsOoxml(OfficeMimeType format) =>
            format is OfficeMimeType.Word or OfficeMimeType.Excel or OfficeMimeType.PowerPoint;

        // Reverse of IExtractionHelper.MimeFor over the six supported formats.
        private OfficeMimeType? ResolveFormat(string? mime)
        {
            foreach (var format in SupportedFormats)
            {
                if (string.Equals(_helper.MimeFor(format), mime, StringComparison.OrdinalIgnoreCase)) return format;
            }
            return null;
        }

        /// <summary>
        /// Legacy convenience overload: detects the mime type itself, then delegates to
        /// <see cref="Extract(ExtractionRequest)"/> with no other logic, so it follows that
        /// contract exactly (including its exceptions; an undetected format throws
        /// <see cref="Exceptions.UnsupportedFormatException"/>).
        /// </summary>
        [Obsolete("Detect the mime type and call Extract(ExtractionRequest) instead.")]
        public DocumentExtractionResult Extract(byte[] fileBytes)
        {
            ArgumentNullException.ThrowIfNull(fileBytes);
            var format = _helper.DetectMimeTypeFromBytes(fileBytes);
            return Extract(new ExtractionRequest
            {
                FileBytes = fileBytes,
                FileName = string.Empty,
                DetectedMimeType = _helper.MimeFor(format),
            });
        }

        // Reuses what the open strategy already parsed (#168, #167) when the extractor can take
        // it; otherwise extracts from the raw bytes, read only on that fallback.
        private static string ExtractText(ITextExtractor extractor, DocumentExtractionResult result, Func<byte[]> fileBytes,
            Action<Exception> onPartFailure)
        {
            object? parsed = result.ParsedDocument;

            string? text = parsed != null && extractor is IParsedDocumentTextExtractor parsedExtractor
                ? parsedExtractor.ExtractText(parsed, onPartFailure)
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
    }
}
