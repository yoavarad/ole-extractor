using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Guardrails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NPOI.HSSF.Record.Crypto;
using Xunit;

namespace ExtractorOLE.Tests.Encryption;

/// <summary>
/// Reusable "the password never leaks" assertions (#385). Every method takes the secret under test and
/// fails with an <see cref="Xunit.Sdk.XunitException"/> naming where it was found. Format-agnostic, so
/// the doc/ppt suites can reuse them together with <see cref="ConfidentialityRig"/>.
/// </summary>
internal static class ConfidentialityAssertions
{
    private const int MaxNodes = 2_000_000;

    // ---- text ----

    private static bool Leaks(string? text, string secret) =>
        text is not null && text.Contains(secret, StringComparison.OrdinalIgnoreCase);

    internal static void AssertTextClean(string? text, string secret, string where)
    {
        Assert.False(Leaks(text, secret), $"secret found in {where}");
    }

    // ---- logger ----

    // Every formatted message, plus each structured state key and value, of the one logger given
    // (for the extractor rig: the guardrail logger, the only ILogger the library writes to).
    internal static void AssertLoggerClean<T>(CapturingLogger<T> logger, string secret)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var (messages, states) = logger.Snapshot();
        for (var i = 0; i < messages.Length; i++)
        {
            AssertTextClean(messages[i], secret, $"log message #{i}");
            foreach (var pair in states[i])
            {
                AssertTextClean(pair.Key, secret, $"log state key #{i}");
                AssertTextClean(Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture), secret, $"log state value '{pair.Key}' #{i}");
            }
        }
    }

    // ---- exceptions ----

    // Message, ToString(), Data keys and values, then the InnerException chain and every
    // AggregateException inner exception, recursively.
    internal static void AssertExceptionClean(Exception? exception, string secret)
    {
        var pending = new Stack<Exception>();
        var seen = new HashSet<Exception>(ReferenceEqualityComparer.Instance);
        if (exception is not null) pending.Push(exception);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!seen.Add(current)) continue;
            var name = current.GetType().Name;
            AssertTextClean(current.Message, secret, $"{name}.Message");
            AssertTextClean(current.ToString(), secret, $"{name}.ToString()");
            foreach (DictionaryEntry entry in current.Data)
            {
                AssertTextClean(Convert.ToString(entry.Key, System.Globalization.CultureInfo.InvariantCulture), secret, $"{name}.Data key");
                AssertTextClean(Convert.ToString(entry.Value, System.Globalization.CultureInfo.InvariantCulture), secret, $"{name}.Data['{entry.Key}']");
            }

            if (current.InnerException is not null) pending.Push(current.InnerException);
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions) pending.Push(inner);
            }
        }
    }

    // ---- results ----

    internal static void AssertJsonClean(object? value, string secret, string where)
    {
        var json = JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object));
        AssertTextClean(json, secret, where + " (JSON)");
    }

    // Public leaves (embedded bytes included), FormatMetadata by its runtime type, each FieldError and
    // the raw object graph (JSON hides byte[] content behind base64, so UTF-8/UTF-16 needles are searched too).
    internal static void AssertResultClean(DocumentExtractionResult? result, string secret)
    {
        if (result is null) return;
        AssertJsonClean(result, secret, nameof(DocumentExtractionResult));
        AssertJsonClean(result.FormatMetadata, secret, "FormatMetadata");
        foreach (var error in result.FieldErrors)
        {
            AssertTextClean(error.FieldPath, secret, "FieldError.FieldPath");
            AssertTextClean(error.Message, secret, "FieldError.Message");
        }

        AssertNoSecretInGraph(secret, nameof(DocumentExtractionResult), result);
    }

    internal static void AssertDetectionClean(MimeDetectionResult? result, string secret)
    {
        AssertJsonClean(result, secret, nameof(MimeDetectionResult));
        AssertNoSecretInGraph(secret, nameof(MimeDetectionResult), result);
    }

    // The item serialized as a whole (an Exception may not be serializable, then its parts), the result
    // and the error graph.
    internal static void AssertBatchItemClean(BatchExtractionResult item, string secret)
    {
        ArgumentNullException.ThrowIfNull(item);
        string json;
        try
        {
            json = JsonSerializer.Serialize(item);
        }
        catch (NotSupportedException)
        {
            json = JsonSerializer.Serialize(new { item.Index, item.FileName, item.Succeeded, item.Result, ErrorType = item.Error?.GetType().FullName });
        }

        AssertTextClean(json, secret, nameof(BatchExtractionResult) + " (JSON)");
        AssertResultClean(item.Result, secret);
        AssertExceptionClean(item.Error, secret);
    }

    // ---- retained state ----

    // The NPOI slot the xls open sets around the HSSF read is [ThreadStatic] ("for the current thread"):
    // this checks the calling thread's slot, so it must run on the thread that drove the call.
    internal static void AssertHssfSlotCleared() =>
        Assert.Null(Biff8EncryptionKey.CurrentUserPassword);

    // Walks the given root (bounded, cycle-safe) for any string, char[] or byte[] (UTF-8 or UTF-16LE)
    // that contains the secret. Value types and non-text primitives are skipped. Running out of depth
    // or node budget FAILS the assertion: a truncated walk proves nothing.
    internal static void AssertNoSecretInGraph(string secret, string rootName, object? root, int maxDepth = 24)
    {
        var walker = new Walker(secret, maxDepth);
        walker.Visit(root, rootName, 0);
        walker.AssertConclusive();
    }

    private static readonly ConcurrentDictionary<Assembly, FieldInfo[]> StaticFields = new();

    private static FieldInfo[] StaticFieldsOf(Assembly assembly) => StaticFields.GetOrAdd(assembly, a =>
    {
        Type[] types;
        try
        {
            types = a.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }

        return types.Where(t => !t.ContainsGenericParameters)
            .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(f => !f.IsLiteral && Walker.IsWalkable(f.FieldType))
            .ToArray();
    });

    // Every static (incl. thread-static, of the calling thread) field of every type in the assembly, and
    // what they reference. A field whose type initializer fails is skipped: it holds nothing readable.
    internal static void AssertNoSecretInStatics(string secret, Assembly assembly, int maxDepth = 24)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var walker = new Walker(secret, maxDepth);
        foreach (var field in StaticFieldsOf(assembly))
        {
            object? value;
            try
            {
                value = field.GetValue(null);
            }
            catch (Exception ex) when (ex is TypeInitializationException or TargetInvocationException or NotSupportedException)
            {
                continue;
            }

            walker.Visit(value, $"{field.DeclaringType!.FullName}.{field.Name}", 0);
        }

        walker.AssertConclusive();
    }

    private sealed class Walker
    {
        private readonly byte[] _utf8;
        private readonly byte[] _utf16;
        private readonly int _maxDepth;
        private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);
        private readonly string _secret;
        private readonly List<string> _hits = new();
        private readonly List<string> _truncated = new();
        private int _nodes;

        internal Walker(string secret, int maxDepth)
        {
            _secret = secret;
            _utf8 = Encoding.UTF8.GetBytes(secret);
            _utf16 = Encoding.Unicode.GetBytes(secret);
            _maxDepth = maxDepth;
        }

        internal static bool IsWalkable(Type type) => !type.IsPointer && !type.IsFunctionPointer && !type.IsByRefLike && !type.IsPrimitive && !type.IsEnum;

        internal void AssertConclusive()
        {
            Assert.True(_hits.Count == 0, "secret retained at: " + string.Join("; ", _hits));
            Assert.True(_truncated.Count == 0, $"walk inconclusive, budget exhausted at {_truncated.Count} node(s), e.g. " + string.Join("; ", _truncated.Take(3)));
        }

        internal void Visit(object? value, string path, int depth)
        {
            if (value is null) return;
            var type = value.GetType();
            if (type.IsPrimitive || type.IsEnum || IsOpaque(value, type)) return;

            if (value is string text)
            {
                if (Leaks(text, _secret)) _hits.Add(path);
                return;
            }

            if (!type.IsValueType && !_seen.Add(value)) return;

            switch (value)
            {
                case byte[] bytes:
                    if (bytes.AsSpan().IndexOf(_utf8) >= 0 || bytes.AsSpan().IndexOf(_utf16) >= 0) _hits.Add(path);
                    return;
                case char[] chars:
                    if (Leaks(new string(chars), _secret)) _hits.Add(path);
                    return;
            }

            if (depth > _maxDepth || ++_nodes > MaxNodes)
            {
                _truncated.Add(path);
                return;
            }

            switch (value)
            {
                case Array array:
                    if (array.GetType().GetElementType()!.IsPrimitive) return;
                    var index = 0;
                    foreach (var element in array) Visit(element, $"{path}[{index++}]", depth + 1);
                    return;
                case Delegate del:
                    Visit(del.Target, path + ".Target", depth + 1);
                    return;
            }

            for (var t = type; t is not null; t = t.BaseType)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!IsWalkable(field.FieldType)) continue;
                    Visit(field.GetValue(value), $"{path}.{field.Name}", depth + 1);
                }
            }
        }

        // Runtime plumbing (and the telemetry listeners test sinks attach to the static source/meter) that cannot hold the password and would drag the walk through the whole runtime.
        private static bool IsOpaque(object value, Type type) =>
            value is Type or MemberInfo or Assembly or Module or ParameterInfo or Thread or ExecutionContext
                or ActivityListener or MeterListener
                or SynchronizationContext or Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
            || (type.Namespace?.StartsWith("System.Reflection", StringComparison.Ordinal) ?? false)
            || (type.Namespace?.StartsWith("System.Runtime.Loader", StringComparison.Ordinal) ?? false);
    }

    // ---- telemetry ----

    /// <summary>
    /// Captures what <see cref="ExtractionTelemetry"/> emits while attached: activity names, tags and
    /// events (the exception event carries type, message and stack) and every metric measurement's tags.
    /// Only telemetry produced in the async flow that constructed the sink is kept, so tests running in
    /// parallel neither pollute it nor see each other's secrets. Source and meter names are parameters
    /// so a negative control can use private names instead of the library's.
    /// </summary>
    internal sealed class TelemetrySink : IDisposable
    {
        private readonly ActivityListener _activities;
        private readonly MeterListener _meters;
        private readonly ConcurrentQueue<string> _lines = new();
        private readonly AsyncLocal<bool> _inFlow = new();

        internal TelemetrySink(string sourceName = ExtractionTelemetry.SourceName, string meterName = ExtractionTelemetry.MeterName)
        {
            _inFlow.Value = true;
            _activities = new ActivityListener
            {
                ShouldListenTo = s => s.Name == sourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = Capture,
            };
            ActivitySource.AddActivityListener(_activities);

            _meters = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == meterName) listener.EnableMeasurementEvents(instrument);
                },
            };
            _meters.SetMeasurementEventCallback<long>((i, m, tags, _) => Record(i, m, tags));
            _meters.SetMeasurementEventCallback<double>((i, m, tags, _) => Record(i, m, tags));
            _meters.Start();
        }

        internal int ActivityCount => _lines.Count(l => l.StartsWith("activity ", StringComparison.Ordinal));

        internal int MeasurementCount => _lines.Count(l => l.StartsWith("metric ", StringComparison.Ordinal));

        internal bool HasExceptionEvent => _lines.Any(l => l.StartsWith("event exception", StringComparison.Ordinal));

        private void Record<TValue>(Instrument instrument, TValue value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            if (!_inFlow.Value) return;
            var text = new StringBuilder($"metric {instrument.Name} {value}");
            foreach (var tag in tags) text.Append(' ').Append(tag.Key).Append('=').Append(Convert.ToString(tag.Value, System.Globalization.CultureInfo.InvariantCulture));
            _lines.Enqueue(text.ToString());
        }

        private void Capture(Activity activity)
        {
            if (!_inFlow.Value) return;
            var text = new StringBuilder($"activity {activity.OperationName} {activity.DisplayName} {activity.StatusDescription}");
            foreach (var tag in activity.TagObjects) text.Append(' ').Append(tag.Key).Append('=').Append(Convert.ToString(tag.Value, System.Globalization.CultureInfo.InvariantCulture));
            foreach (var baggage in activity.Baggage) text.Append(' ').Append(baggage.Key).Append('=').Append(baggage.Value);
            _lines.Enqueue(text.ToString());
            foreach (var evt in activity.Events)
            {
                var line = new StringBuilder($"event {evt.Name}");
                foreach (var tag in evt.Tags) line.Append(' ').Append(tag.Key).Append('=').Append(Convert.ToString(tag.Value, System.Globalization.CultureInfo.InvariantCulture));
                _lines.Enqueue(line.ToString());
            }
        }

        internal void AssertClean(string secret)
        {
            foreach (var line in _lines) AssertTextClean(line, secret, "telemetry");
        }

        public void Dispose()
        {
            _activities.Dispose();
            _meters.Dispose();
        }
    }
}

/// <summary>One Extract call to drive: the request, the secret under test and the pinned outcome.</summary>
internal sealed record ConfidentialityScenario(
    string Name,
    string Secret,
    Func<ExtractionRequest> Request,
    Type? ExpectedException,
    bool ExpectFieldErrors = false,
    MimeDetectionLimits? Limits = null);

/// <summary>
/// A real extractor wired through <see cref="ServiceRegistration"/> with a capturing guardrail logger
/// (the only ILogger the library writes to) and a per-flow <see cref="ConfidentialityAssertions.TelemetrySink"/>.
/// Format-agnostic: doc/ppt suites build requests and reuse everything here.
/// </summary>
internal sealed class ConfidentialityRig : IDisposable
{
    private readonly ServiceProvider _provider;

    internal ConfidentialityRig(MimeDetectionLimits? limits = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<ExtractionGuardrails>>(Logger);
        ServiceRegistration.Register(services, limits);
        _provider = services.BuildServiceProvider();
        Extractor = _provider.GetRequiredService<MainExtractor>();
        Helper = _provider.GetRequiredService<IExtractionHelper>();
    }

    internal CapturingLogger<ExtractionGuardrails> Logger { get; } = new();

    internal ConfidentialityAssertions.TelemetrySink Sink { get; } = new();

    internal MainExtractor Extractor { get; }

    internal IExtractionHelper Helper { get; }

    internal (DocumentExtractionResult? Result, Exception? Error) Run(ExtractionRequest request)
    {
        try
        {
            return (Extractor.Extract(request), null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }

    // Runs the scenario and pins its expected outcome (and that telemetry saw the call), so a clean
    // result is never a vacuous one.
    internal static (ConfidentialityScenario Scenario, ConfidentialityRig Rig, DocumentExtractionResult? Result, Exception? Error) Execute(ConfidentialityScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var rig = new ConfidentialityRig(scenario.Limits);
        var (result, error) = rig.Run(scenario.Request());

        if (scenario.ExpectedException is null)
        {
            Assert.True(error is null, $"{scenario.Name}: unexpected {error}");
            Assert.NotNull(result);
            Assert.Equal(scenario.ExpectFieldErrors, result.FieldErrors.Count > 0);
        }
        else
        {
            Assert.NotNull(error);
            Assert.True(scenario.ExpectedException == error.GetType(), $"{scenario.Name}: expected {scenario.ExpectedException.Name}, got {error.GetType().Name}");
        }

        Assert.True(rig.Sink.ActivityCount > 0, $"{scenario.Name}: no telemetry captured for this call");
        return (scenario, rig, result, error);
    }

    // Everything at once: guardrail log, exception graph, serialized result and graph, telemetry,
    // the extractor and service-provider object graphs, ExtractorOLE statics, and the HSSF slot.
    internal void AssertAllClean(string secret, DocumentExtractionResult? result, Exception? error)
    {
        ConfidentialityAssertions.AssertLoggerClean(Logger, secret);
        ConfidentialityAssertions.AssertExceptionClean(error, secret);
        ConfidentialityAssertions.AssertResultClean(result, secret);
        Sink.AssertClean(secret);
        ConfidentialityAssertions.AssertNoSecretInGraph(secret, nameof(MainExtractor), Extractor);
        ConfidentialityAssertions.AssertNoSecretInGraph(secret, nameof(ServiceProvider), _provider);
        ConfidentialityAssertions.AssertNoSecretInStatics(secret, typeof(MainExtractor).Assembly);
        ConfidentialityAssertions.AssertHssfSlotCleared();
    }

    public void Dispose()
    {
        Sink.Dispose();
        _provider.Dispose();
    }
}
