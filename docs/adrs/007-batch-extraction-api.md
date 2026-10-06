# ADR-007: Batch extraction API with bounded parallelism

## Status
Accepted (2026-10-07)

## Context
`MainExtractor.Extract(ExtractionRequest)` processes one file on the calling thread. Callers with
many files leave cores idle unless they write their own fan-out. Since #172, `Extract()` touches
no shared mutable state and is safe for concurrent callers (README, "Concurrency"), so a batch
entry point only needs to schedule calls, not add locking. Open questions were the API shape
(sync vs async), result ordering, per-item error reporting, and memory under concurrent large
inputs (up to the 100 MB guardrail each).

## Decision
Add one method to `MainExtractor`:

```csharp
Task<IReadOnlyList<BatchExtractionResult>> ExtractManyAsync(
    IEnumerable<ExtractionRequest> requests,
    int? maxDegreeOfParallelism = null,
    CancellationToken cancellationToken = default)
```

- **Async.** Implemented with `Parallel.ForEachAsync`. Each item calls the existing synchronous
  `Extract(request)`, so batch output is by construction the same as serial output. The `Async`
  suffix follows .NET naming; the task description's `ExtractMany` refers to this method.
- **Ordering.** The returned list is in input order. Each `BatchExtractionResult` also carries
  its input `Index` and the request's `FileName`.
- **Per-item errors.** An exception from one item (including `ArgumentNullException` for a null
  request) is caught and stored in that item's `Error`; `Result` is then null. Other items
  are not affected. Only a null `requests` sequence or an invalid `maxDegreeOfParallelism`
  throws up front. Two exceptions fail the whole batch instead of one item: an
  `OutOfMemoryException`, which is never captured because the process is then unreliable,
  and any exception thrown by the caller's `requests` enumerator while it is being read.
- **Cancellation.** Cancelling the token stops new items from starting, and the returned task
  ends with `OperationCanceledException`. No partial result list is returned. An item already
  running finishes, because `Extract()` takes no token.
- **Parallelism.** `maxDegreeOfParallelism` defaults to `Environment.ProcessorCount`. A value of
  0 or less throws `ArgumentOutOfRangeException`. Items run on thread-pool threads. If a caller
  asks for a MaxDOP well above the pool's minimum thread count, the pool adds threads
  gradually, so parallelism ramps up slowly. Raise `ThreadPool.SetMinThreads` if that matters.
- **Memory.** The batch API never copies or loads input bytes. Callers own the `byte[]` in each
  request. `Parallel.ForEachAsync` pulls from the sequence lazily, so at most
  `maxDegreeOfParallelism` extractions (and their parse-time working memory) run at once. A
  caller that yields requests lazily (reading each file on demand) keeps at most about
  MaxDOP inputs in memory. Concurrent 100 MB inputs are therefore capped by the MaxDOP the
  caller chooses. We add no separate byte-budget cap (YAGNI). Results are kept until the
  batch completes. Results hold extracted text and embedded files, not the input bytes.

## Alternatives Considered
- **Synchronous `ExtractMany` with `Parallel.ForEach`**: blocks the caller's thread for the whole
  batch. Async composes better with server callers and with cancellation.
- **Streaming `IAsyncEnumerable` in completion order**: gives lower memory for huge batches,
  but callers must re-key results and the ordering guarantee goes away. Not needed yet.
- **Byte-budget semaphore across in-flight inputs**: adds a second tuning knob and does not help
  when the caller has already materialized every input. MaxDOP is already a bound.

## Consequences
- One new public method plus one result DTO. No change to `Extract()`.
- Fail-fast callers must check `Error` on each item. The batch does not throw for item failures.
- `ExtractorOLE.StressHarness batch` compares throughput at MaxDOP=1 and
  MaxDOP=ProcessorCount on `samples/`, and checks the batch output against serial `Extract()`.

## Measured throughput
`dotnet run -c Release --project ExtractorOLE.StressHarness -- batch 20 ../samples`. The run
used 39 samples repeated 20 times, for 780 items, on a 22-core machine. An untimed serial
baseline runs first and also warms up the JIT. 100 items are adversarial samples that
deliberately fail per item. They fail the same way in serial and batch runs.

| Run | MaxDOP | Wall-clock | Throughput | Mismatches vs serial |
|---|---|---|---|---|
| 1 | 1 | 39.4 s | 19.8 items/s | 0 |
| 1 | 22 (ProcessorCount) | 17.9 s | 43.5 items/s | 0 |
| 2 | 1 | 39.8 s | 19.6 items/s | 0 |
| 2 | 22 (ProcessorCount) | 24.1 s | 32.3 items/s | 0 |

The speedup was about 1.6x to 2.2x across two runs on the same machine. That is well below
the core count. The cause was not investigated here.
