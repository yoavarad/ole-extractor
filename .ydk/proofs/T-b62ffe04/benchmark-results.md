# T-b62ffe04 benchmark results (Extract(ExtractionRequest), warm steady state)

Run: 2026-09-20, `dotnet run -c Release --project ExtractorOle/ExtractorOLE.Benchmarks --no-build -- --filter "*Extract10Mb*"` then `"*Extract100Mb*"`.
Machine: Intel Core Ultra 7 155H (16C/22T), 31 GB RAM, Windows 11 10.0.26200, .NET 8.0.30 (host SDK 9.0.317), X64 RyuJIT AVX2, BenchmarkDotNet 0.14.0, default (workstation concurrent) GC.
Job: RunStrategy.Monitoring (1 invocation/iteration); 10MB: 3 warmup + 30 iterations; 100MB: 1 warmup + 15 iterations (P95 over 15 samples is close to the max).
Caveats: sibling ydk tasks ran builds/tests on the same machine concurrently (noise); warmup iterations exclude cold-start cost (one cold docx-10mb call measured ~166 ms; one cold xls-100mb call measured 2.38 s vs 2.04 s warm mean, so the 100MB xls is well under 5 s and the ~14 s seen in an earlier one-off run was not reproduced in a Release build under BenchmarkDotNet); doc/ppt samples are padded fixtures whose extracted text is tiny (283 / 74 chars), so they measure container open + tiny body, not text-proportional work (T-b856b14e builds real legacy generators); "Alloc B/input B" = BDN allocated bytes/op divided by sample file size.

## 10MB tier (target P95 < 500 ms)
| Method  | Sample    | Mean      | Error     | StdDev    | Median    | Min        | Max       | P95       | Alloc B/input B | P95 target | Gen0       | Gen1      | Gen2      | Allocated |
|-------- |---------- |----------:|----------:|----------:|----------:|-----------:|----------:|----------:|----------------:|----------- |-----------:|----------:|----------:|----------:|
| **Extract** | **doc-10mb**  |  **11.32 ms** |  **0.599 ms** |  **0.897 ms** |  **11.18 ms** |   **9.904 ms** |  **14.24 ms** |  **12.46 ms** |            **1.29** | **met**        |          **-** |         **-** |         **-** |  **12.29 MB** |
| **Extract** | **docx-10mb** |  **71.91 ms** |  **4.940 ms** |  **7.394 ms** |  **72.61 ms** |  **62.133 ms** |  **85.99 ms** |  **82.89 ms** |            **8.61** | **met**        |  **3000.0000** | **2000.0000** | **1000.0000** |  **81.65 MB** |
| **Extract** | **ppt-10mb**  |  **56.00 ms** |  **4.940 ms** |  **7.394 ms** |  **54.02 ms** |  **47.006 ms** |  **71.92 ms** |  **69.66 ms** |           **14.83** | **met**        |  **3000.0000** | **3000.0000** | **3000.0000** | **140.78 MB** |
| **Extract** | **pptx-10mb** |  **96.96 ms** | **18.549 ms** | **27.763 ms** | **110.98 ms** |  **46.145 ms** | **135.92 ms** | **128.04 ms** |            **6.07** | **met**        |  **2000.0000** | **1000.0000** | **1000.0000** |  **57.63 MB** |
| **Extract** | **xls-10mb**  | **156.77 ms** |  **5.643 ms** |  **8.447 ms** | **154.99 ms** | **145.807 ms** | **180.22 ms** | **174.78 ms** |           **39.48** | **met**        | **29000.0000** | **6000.0000** | **1000.0000** | **375.48 MB** |
| **Extract** | **xlsx-10mb** |  **59.00 ms** |  **8.204 ms** | **12.280 ms** |  **53.92 ms** |  **50.197 ms** |  **92.41 ms** |  **89.27 ms** |            **5.82** | **met**        |  **1000.0000** |         **-** |         **-** |  **55.21 MB** |

## 100MB tier (target P95 < 5 s)
| Method  | Sample     | Mean        | Error      | StdDev     | Min         | Median      | Max         | P95         | Alloc B/input B | P95 target | Gen0        | Gen1       | Gen2       | Allocated  |
|-------- |----------- |------------:|-----------:|-----------:|------------:|------------:|------------:|------------:|----------------:|----------- |------------:|-----------:|-----------:|-----------:|
| **Extract** | **doc-100mb**  |    **38.21 ms** |   **2.617 ms** |   **2.448 ms** |    **35.50 ms** |    **38.02 ms** |    **43.65 ms** |    **42.09 ms** |            **1.13** | **met**        |           **-** |          **-** |          **-** |  **109.52 MB** |
| **Extract** | **docx-100mb** |   **468.87 ms** |  **30.776 ms** |  **28.788 ms** |   **427.45 ms** |   **464.05 ms** |   **540.62 ms** |   **522.39 ms** |            **8.09** | **met**        |  **27000.0000** |  **7000.0000** |  **1000.0000** |  **786.21 MB** |
| **Extract** | **ppt-100mb**  |   **458.56 ms** |  **40.586 ms** |  **37.964 ms** |   **397.96 ms** |   **451.77 ms** |   **558.93 ms** |   **519.50 ms** |           **14.71** | **met**        |   **5000.0000** |  **4000.0000** |  **3000.0000** | **1429.38 MB** |
| **Extract** | **pptx-100mb** |   **706.48 ms** |  **60.486 ms** |  **56.579 ms** |   **623.98 ms** |   **715.44 ms** |   **808.77 ms** |   **777.73 ms** |            **5.90** | **met**        |  **17000.0000** | **16000.0000** |  **3000.0000** |  **573.27 MB** |
| **Extract** | **xls-100mb**  | **2,040.69 ms** | **289.009 ms** | **270.340 ms** | **1,797.61 ms** | **1,887.09 ms** | **2,718.38 ms** | **2,461.36 ms** |           **39.48** | **met**        | **338000.0000** | **95000.0000** | **42000.0000** | **3845.41 MB** |
| **Extract** | **xlsx-100mb** |   **562.23 ms** |  **66.776 ms** |  **62.463 ms** |   **505.62 ms** |   **553.91 ms** |   **724.76 ms** |   **693.69 ms** |            **5.66** | **met**        |  **18000.0000** | **12000.0000** |  **1000.0000** |  **549.55 MB** |
