// Task 237 spike: phase-instrumented copy of DocTextExtractor.ConvertToDocx.
// Usage: DocProfile <file.doc> [mode]   mode: phases (default) | alloc (GC AllocationTick by type)
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using b2xtranslator.DocFileFormat;
using b2xtranslator.OpenXmlLib;
using b2xtranslator.StructuredStorage.Reader;
using b2xtranslator.WordprocessingMLMapping;
using B2x = b2xtranslator.OpenXmlLib.WordprocessingML.WordprocessingDocument;

static class P
{
    static long peakWs, peakPriv;
    static void Report(string tag)
    {
        var p = Process.GetCurrentProcess();
        Console.WriteLine($"{tag,-34} alloc={GC.GetTotalAllocatedBytes() / 1048576,6}MB  heap={GC.GetTotalMemory(false) / 1048576,6}MB  ws={p.WorkingSet64 / 1048576,6}MB  peakWs(sampled)={peakWs / 1048576,6}MB  gc0/1/2={GC.CollectionCount(0)}/{GC.CollectionCount(1)}/{GC.CollectionCount(2)}");
    }

    sealed class AllocListener : EventListener
    {
        public readonly ConcurrentDictionary<string, long> Bytes = new();
        public long Total;
        protected override void OnEventSourceCreated(EventSource s)
        {
            if (s.Name == "Microsoft-Windows-DotNETRuntime")
                EnableEvents(s, EventLevel.Verbose, (EventKeywords)0x1 /*GC*/);
        }
        protected override void OnEventWritten(EventWrittenEventArgs e)
        {
            if (e.EventName == null || !e.EventName.StartsWith("GCAllocationTick", StringComparison.Ordinal)) return;
            string name = "?"; long amt = 0;
            for (int i = 0; i < e.PayloadNames.Count; i++)
            {
                if (e.PayloadNames[i] == "TypeName") name = e.Payload[i] as string ?? "?";
                if (e.PayloadNames[i] == "AllocationAmount64") amt = Convert.ToInt64(e.Payload[i], System.Globalization.CultureInfo.InvariantCulture);
            }
            Bytes.AddOrUpdate(name, amt, (_, v) => v + amt);
            Interlocked.Add(ref Total, amt);
        }
    }

    sealed class InMem : B2x
    {
        readonly Stream _o;
        public InMem(Stream o, DocumentType t) : base(string.Empty, t) { _o = o; }
        public override void Close()
        {
            Report("convert done, before package write");
            var w = new OpenXmlWriter(); w.Open(_o); WritePackage(w); w.Close();
            Report("package written");
        }
    }

    static void Main(string[] a)
    {
        var path = a[0];
        var mode = a.Length > 1 ? a[1] : "phases";
        AllocListener l = mode == "alloc" ? new AllocListener() : null;
        var cts = new CancellationTokenSource();
        var sampler = new Thread(() =>
        {
            var p = Process.GetCurrentProcess();
            while (!cts.IsCancellationRequested) { p.Refresh(); peakWs = Math.Max(peakWs, p.WorkingSet64); peakPriv = Math.Max(peakPriv, p.PrivateMemorySize64); Thread.Sleep(5); }
        })
        { IsBackground = true };
        sampler.Start();

        Report("start");
        var bytes = File.ReadAllBytes(path);               // mirrors caller byte[] mode; use FileStream for stream mode
        Stream src = mode == "file" ? File.OpenRead(path) : new MemoryStream(bytes);
        if (mode == "file") bytes = null;
        Report("input loaded");
        var output = new MemoryStream();
        var sw = Stopwatch.StartNew();
        using (var reader = new StructuredStorageReader(src))
        {
            Report("StructuredStorageReader ctor");
            var doc = new WordDocument(reader);
            Report("WordDocument ctor");
            var t = Converter.DetectOutputType(doc);
            var docx = new InMem(output, t);
            Converter.Convert(doc, docx);
            Report("Converter.Convert returned");
        }
        var arr = output.ToArray();
        Report($"ToArray ({arr.Length / 1048576}MB docx)");
        sw.Stop();
        Console.WriteLine($"elapsed={sw.Elapsed.TotalSeconds:F1}s peakWs={peakWs / 1048576}MB peakPrivate={peakPriv / 1048576}MB");
        cts.Cancel();
        if (l != null)
        {
            Console.WriteLine($"sampled alloc total={l.Total / 1048576}MB");
            foreach (var kv in l.Bytes.OrderByDescending(k => k.Value).Take(25))
                Console.WriteLine($"{kv.Value / 1048576,8}MB {100.0 * kv.Value / l.Total,5:F1}%  {kv.Key}");
        }
    }
}
