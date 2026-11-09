using Microsoft.Extensions.Logging;

namespace ExtractorOLE.Tests
{
    /// <summary>Test ILogger that records each formatted message and its structured state.</summary>
    public sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public List<IReadOnlyList<KeyValuePair<string, object?>>> States { get; } = new();

        public List<LogLevel> Levels { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            Messages.Add(formatter(state, exception));
            Levels.Add(logLevel);
            States.Add(state as IReadOnlyList<KeyValuePair<string, object?>> ?? Array.Empty<KeyValuePair<string, object?>>());
        }
    }
}
