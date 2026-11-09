using System;
using System.Threading;

namespace ExtractorOLE.Helpers
{
    // The cancellation token of the Extract call running on this flow (#290). MainExtractor
    // enters a scope for the whole call; the built-in strategies, extractors and the embedded
    // walk check it at their sheet/slide/embedded loops. Ambient rather than a parameter so the
    // public IOpenStrategy / ITextExtractor / IExtractionHelper signatures stay unchanged.
    internal static class ExtractionCancellation
    {
        private static readonly AsyncLocal<CancellationToken> Current = new();

        internal static CancellationToken Token => Current.Value;

        // Sets the token for the current flow until the returned scope is disposed, which
        // restores the token that was current before.
        internal static IDisposable Enter(CancellationToken cancellationToken)
        {
            var previous = Current.Value;
            Current.Value = cancellationToken;
            return new Scope(previous);
        }

        internal static void ThrowIfCancellationRequested() => Current.Value.ThrowIfCancellationRequested();

        private sealed class Scope : IDisposable
        {
            private readonly CancellationToken _previous;

            public Scope(CancellationToken previous) => _previous = previous;

            public void Dispose() => Current.Value = _previous;
        }
    }
}
