# ADR-011: Streaming batch extraction to bound batch memory

## Status
Accepted (2026-11-10). Task #297.

## Context
`MainExtractor.ExtractManyAsync` (ADR-007) keeps every `BatchExtractionResult` in a list until the
whole batch returns. A result holds the extracted text and every embedded file's `BinaryData`, so a
batch's retained memory grows with the number of items, not with `maxDegreeOfParallelism`. Each
input may be up to the 100 MB guardrail, and its embedded files can add copies of that size. A large
batch can therefore exhaust memory even though at most MaxDOP extractions run at once. Task #297
asked for one of three options: a byte budget, a streaming `IAsyncEnumerable` overload, or a
documented bound.

## Decision
Add a streaming overload next to `ExtractManyAsync` and document the bound of each:

```csharp
IAsyncEnumerable<BatchExtractionResult> ExtractManyStreamingAsync(
    IEnumerable<ExtractionRequest> requests,
    int? maxDegreeOfParallelism = null,
    CancellationToken cancellationToken = default)
```

- **Order.** Items are yielded in completion order. Each item carries its input `Index`, so a caller
  can re-key results.
- **Bound.** Workers write into a bounded channel with capacity MaxDOP. A worker that finds the
  channel full waits, still holding its finished result, before it takes the next request. Each of
  the MaxDOP workers therefore holds at most one result (being extracted, or finished and waiting),
  at most MaxDOP results are queued, and the consumer handles one. That makes at most
  2 x MaxDOP + 1 results alive at once, plus any results the caller keeps, whatever the batch size.
  A slow consumer pauses extraction instead of letting results pile up. When requests are yielded
  lazily, at most 2 x MaxDOP + 1 requests are pulled ahead of the consumer.
- **Same item semantics as `ExtractManyAsync`.** Both methods share one per-item routine. An item's
  exception becomes its `Error`. `OutOfMemoryException`, an exception from the `requests`
  enumerator, and cancellation fail the stream and are thrown from the enumeration.
- **Early stop.** Disposing the enumerator early (for example `break` inside `await foreach`) stops
  new items from starting and waits for in-flight items to end. A batch failure that happened before
  the dispose is still thrown, not lost. A caller that abandons the enumerator without disposing it
  leaves the workers blocked, as with any async iterator. The token passed to the method and the token
  from `WithCancellation` both cancel the stream at once; results already queued are not yielded.
- **Argument errors** (null `requests`, MaxDOP of 0 or less) throw when the method is called, not on
  the first `MoveNextAsync`.
- **`ExtractManyAsync` is unchanged.** Its retained memory is documented as O(batch size). It stays
  the simple choice for batches whose results fit in memory together.

## Alternatives Considered
- **Byte budget across in-flight items.** This adds a second tuning knob, and it does not limit the
  results the list API keeps. The size of a result is only known after extraction, so a budget
  cannot be checked before an item starts.
- **Documented bound only.** This leaves no supported way to run a batch larger than memory.
- **Change `ExtractManyAsync` to stream.** That would break its input-order list contract
  (ADR-007), which existing callers rely on.
- **Yield in input order.** One slow item would hold back every later finished result, and the
  buffer of waiting results would no longer be bounded by MaxDOP.

## Consequences
- One new public method. No DTO change. `ExtractManyAsync` now calls the shared per-item routine, and
  its behaviour does not change.
- A streaming caller must re-key by `Index` if it needs input order.
- Tests: `ExtractManyStreamingAsyncTests` checks that the streaming items match `ExtractManyAsync`, and
  that with a stalled consumer at most 2 x MaxDOP + 1 requests are pulled from the input (MaxDOP 1
  and 3). It also covers early dispose, early `break`, both cancellation tokens, enumerator failure,
  and argument errors.
- `StressHarness -- batch` reports the peak managed heap of both methods at MaxDOP=ProcessorCount.

## Measured peak heap
`dotnet run -c Release --project ExtractorOLE.StressHarness -- batch 20 ../samples`: 45 samples
repeated 20 times, 900 items, MaxDOP 22, on a 22-core machine. The baseline is the managed heap
after a full GC before the run. It includes the 900 materialized requests and the serial outcomes.

| Method | Run | Baseline | Peak | Peak above baseline | Retained at return |
|---|---|---|---|---|---|
| `ExtractManyAsync` | 1 | 911.9 MB | 1790.4 MB | 878.5 MB | 929.6 MB (+17.7 MB results) |
| `ExtractManyStreamingAsync` | 1 | 929.6 MB | 1412.7 MB | 483.1 MB | n/a (results dropped by the consumer) |
| `ExtractManyAsync` | 2 | 899.8 MB | 1921.1 MB | 1021.3 MB | 923.1 MB (+23.3 MB results) |
| `ExtractManyStreamingAsync` | 2 | 923.1 MB | 1326.0 MB | 402.9 MB | n/a |

The streaming baseline still shows the list run's retained bytes, even though the harness clears its
reference first; that only shifts the baseline by about 20 MB. The corpus samples are small, so the
retained results here are only about 20 MB. The gap in retained memory grows with batch size and with
embedded-file size, while the streaming peak does not. The peak is sampled every 10 ms from
`GC.GetTotalMemory(false)` and includes uncollected garbage, so it is approximate and varies between
runs, as the two runs show.
