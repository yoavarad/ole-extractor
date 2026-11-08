using System.Diagnostics;
using System.Diagnostics.Metrics;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Telemetry
{
    public class ExtractionTelemetryTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private static readonly string[] Stages = { "guardrails", "preflight", "open", "extract_text" };

        private static byte[] Sample()
        {
            var relative = Path.Combine("samples", "synthetic", "multi-embedding", "sample.docx");
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
                dir = dir.Parent;
            }
            throw new FileNotFoundException(relative);
        }

        private static MainExtractor Build()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        }

        [Fact]
        public void Extract_EmitsOneActivityPerStage_WithFormatAndSizeTags()
        {
            var bytes = Sample();
            var activities = new List<Activity>();
            // ActivityListener is process-wide; only count activities from this test's async flow
            // so concurrently running tests don't add extra stage activities.
            var inFlow = new AsyncLocal<bool>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == ExtractionTelemetry.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = a => { if (inFlow.Value) lock (activities) activities.Add(a); },
            };
            ActivitySource.AddActivityListener(listener);

            inFlow.Value = true;
            Build().Extract(new ExtractionRequest { FileBytes = bytes, FileName = "s.docx", DetectedMimeType = DocxMime });

            List<Activity> mine;
            lock (activities) mine = activities.Where(a => a.Source.Name == ExtractionTelemetry.SourceName).ToList();
            foreach (var stage in Stages)
            {
                var a = Assert.Single(mine, x => x.OperationName == stage);
                Assert.Equal("Word", a.GetTagItem("format"));
                Assert.Equal((long)bytes.Length, a.GetTagItem("size"));
            }
        }

        [Fact]
        public void Extract_RecordsDurationHistogramPerStage_AndBytesCounter()
        {
            var bytes = Sample();
            var stagesSeen = new HashSet<string>();
            long total = 0;
            var inFlow = new AsyncLocal<bool>();
            using var listener = new MeterListener
            {
                InstrumentPublished = (i, l) => { if (i.Meter.Name == ExtractionTelemetry.MeterName) l.EnableMeasurementEvents(i); },
            };
            listener.SetMeasurementEventCallback<double>((inst, m, tags, _) =>
            {
                if (inst.Name != "extractor.stage.duration") return;
                foreach (var t in tags) if (t.Key == "stage") lock (stagesSeen) stagesSeen.Add((string)t.Value!);
            });
            listener.SetMeasurementEventCallback<long>((inst, m, tags, _) =>
            {
                if (inFlow.Value && inst.Name == "extractor.bytes.processed") Interlocked.Add(ref total, m);
            });
            listener.Start();

            inFlow.Value = true;
            Build().Extract(new ExtractionRequest { FileBytes = bytes, FileName = "s.docx", DetectedMimeType = DocxMime });

            foreach (var stage in Stages) Assert.Contains(stage, stagesSeen);
            Assert.Equal(bytes.Length, Interlocked.Read(ref total));
        }

        [Fact]
        public void Extract_ArbitraryMime_UsesFixedTag_AndEchoIsCapped()
        {
            var mime = new string('x', 5000);
            var formatTags = new HashSet<string>();
            var inFlow = new AsyncLocal<bool>();
            using var listener = new MeterListener
            {
                InstrumentPublished = (i, l) => { if (i.Meter.Name == ExtractionTelemetry.MeterName) l.EnableMeasurementEvents(i); },
            };
            listener.SetMeasurementEventCallback<double>((inst, m, tags, _) =>
            {
                if (!inFlow.Value) return;
                foreach (var t in tags) if (t.Key == "format") lock (formatTags) formatTags.Add((string)t.Value!);
            });
            listener.SetMeasurementEventCallback<long>((inst, m, tags, _) =>
            {
                if (!inFlow.Value) return;
                foreach (var t in tags) if (t.Key == "format") lock (formatTags) formatTags.Add((string)t.Value!);
            });
            listener.Start();

            inFlow.Value = true;
            var ex = Assert.Throws<ExtractorOLE.Exceptions.UnsupportedFormatException>(() =>
                Build().Extract(new ExtractionRequest { FileBytes = Sample(), FileName = "s.bin", DetectedMimeType = mime }));

            Assert.Equal("unsupported", Assert.Single(formatTags));
            Assert.True(ex.DetectedMimeType.Length <= 100);
            Assert.True(ex.Message.Length < 300);
        }
    }
}
