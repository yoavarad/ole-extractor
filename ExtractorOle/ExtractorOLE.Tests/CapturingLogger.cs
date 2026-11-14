using Microsoft.Extensions.Logging;

namespace ExtractorOLE.Tests
{
    /// <summary>Test ILogger that records each formatted message and its structured state.</summary>
    public sealed class CapturingLogger<T> : ILogger<T>
    {
        private readonly object _gate = new();

        public List<string> Messages { get; } = new();

        public List<IReadOnlyList<KeyValuePair<string, object?>>> States { get; } = new();

        public List<LogLevel> Levels { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            var message = formatter(state, exception);
            var kvs = state as IReadOnlyList<KeyValuePair<string, object?>> ?? Array.Empty<KeyValuePair<string, object?>>();
            lock (_gate)
            {
                Messages.Add(message);
                Levels.Add(logLevel);
                States.Add(kvs);
            }
        }

        /// <summary>A consistent copy of what has been logged so far, safe while other threads still log.</summary>
        public (string[] Messages, IReadOnlyList<KeyValuePair<string, object?>>[] States) Snapshot()
        {
            lock (_gate) return (Messages.ToArray(), States.ToArray());
        }
    }
}
