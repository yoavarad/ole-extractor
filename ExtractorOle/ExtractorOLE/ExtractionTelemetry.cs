using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ExtractorOLE
{
    /// <summary>
    /// Per-stage timing instrumentation for <see cref="MainExtractor"/>. Zero-cost when no
    /// ActivityListener / MeterListener is attached: no Activity, Stopwatch or tag allocation.
    /// </summary>
    public static class ExtractionTelemetry
    {
        public const string SourceName = "ExtractorOLE";
        public const string MeterName = "ExtractorOLE";

        internal static readonly ActivitySource Source = new(SourceName);
        private static readonly Meter Meter = new(MeterName);
        private static readonly Histogram<double> StageDuration =
            Meter.CreateHistogram<double>("extractor.stage.duration", "ms", "Duration of an extraction stage");
        private static readonly Counter<long> BytesProcessed =
            Meter.CreateCounter<long>("extractor.bytes.processed", "By", "Input bytes processed");

        internal static StageScope Stage(string stage, string format, long size) => new(stage, format, size);

        internal readonly struct StageScope : IDisposable
        {
            private readonly Activity? _activity;
            private readonly string _stage;
            private readonly string _format;
            private readonly long _size;
            private readonly long _start;
            private readonly bool _metrics;

            internal StageScope(string stage, string format, long size)
            {
                _stage = stage;
                _format = format;
                _size = size;
                _metrics = StageDuration.Enabled || BytesProcessed.Enabled;
                _activity = Source.HasListeners() ? Source.StartActivity(stage) : null;
                _activity?.SetTag("format", format);
                _activity?.SetTag("size", size);
                _start = _metrics ? Stopwatch.GetTimestamp() : 0;
            }

            public void Dispose()
            {
                _activity?.Dispose();
                if (!_metrics) return;
                var ms = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
                StageDuration.Record(ms,
                    new KeyValuePair<string, object?>("stage", _stage),
                    new KeyValuePair<string, object?>("format", _format));
                BytesProcessed.Add(_size, new KeyValuePair<string, object?>("stage", _stage));
            }
        }
    }
}
