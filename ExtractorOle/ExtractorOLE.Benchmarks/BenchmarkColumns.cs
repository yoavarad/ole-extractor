using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace ExtractorOLE.Benchmarks
{
    // The "Sample" param is an id like "docx-10mb"; column values below key off it.
    internal static class SampleParam
    {
        public static string? Id(BenchmarkCase benchmarkCase) =>
            benchmarkCase.Parameters["Sample"]?.ToString();
    }

    /// <summary>Allocated bytes per Extract() call divided by the input file size (memory-ceiling signal).</summary>
    internal sealed class AllocatedPerInputByteColumn : IColumn
    {
        public string Id => nameof(AllocatedPerInputByteColumn);
        public string ColumnName => "Alloc B/input B";
        public string Legend => "Allocated bytes per operation / input file size in bytes";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public bool IsAvailable(Summary summary) => true;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            string? id = SampleParam.Id(benchmarkCase);
            long? size = id == null ? null : BenchmarkSamples.SizeOf(id);
            long? allocated = summary[benchmarkCase]?.GcStats.GetBytesAllocatedPerOperation(benchmarkCase);
            if (size is null or 0 || allocated is null) return "NA";
            return ((double)allocated.Value / size.Value).ToString("0.00");
        }
    }

    /// <summary>P95 latency vs. the extraction-latency target: 500 ms at 10MB, 5 s at ~100MB.</summary>
    internal sealed class P95TargetColumn : IColumn
    {
        public string Id => nameof(P95TargetColumn);
        public string ColumnName => "P95 target";
        public string Legend => "P95 < 500 ms for the 10mb tier, P95 < 5 s for the 100mb tier (extraction-latency NFR)";
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 1;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public bool IsAvailable(Summary summary) => true;
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            string? id = SampleParam.Id(benchmarkCase);
            var stats = summary[benchmarkCase]?.ResultStatistics;
            if (id == null || stats == null) return "NA";
            double limitNs = id.EndsWith("-100mb", StringComparison.Ordinal) ? 5_000_000_000d : 500_000_000d;
            return stats.Percentiles.P95 < limitNs ? "met" : "MISSED";
        }
    }
}
