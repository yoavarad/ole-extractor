using System.Runtime.CompilerServices;
using System.Threading;

namespace ExtractorOLE.Helpers.MimeDetection
{
    /// <summary>
    /// Test-observability counter scoped to the current async flow (AsyncLocal),
    /// so concurrent callers/tests never see each other's counts. Increments are
    /// atomic, so parallel work spawned inside a scope counts exactly. Outside a
    /// scope (production, nobody reading) increments are a no-op.
    /// </summary>
    internal sealed class AttemptCounter
    {
        private sealed class Box { public int Value; }

        private readonly AsyncLocal<Box?> _box = new();

        public int Value
        {
            get => _box.Value?.Value ?? 0;
            set => _box.Value = new Box { Value = value };
        }

        public void Increment()
        {
            var box = _box.Value;
            if (box != null) Interlocked.Increment(ref box.Value);
        }
    }
}
