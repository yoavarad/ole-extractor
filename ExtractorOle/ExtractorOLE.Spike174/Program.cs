// Spike 174: peak memory of byte[] vs Stream input per parser. Usage:
//   Spike174 <mode> <path>      -> runs one mode in-process, prints one result line
//   Spike174 --all <samplesDir> -> spawns one child per mode (clean peak working set each)
using System.Diagnostics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using b2xtranslator.StructuredStorage.Reader;

if (args[0] == "--all")
{
    string dir = args[1];
    var modes = new (string Mode, string File)[]
    {
        ("docx-bytes", "benchmark-100mb.docx"), ("docx-stream", "benchmark-100mb.docx"),
        ("xlsx-bytes", "benchmark-100mb.xlsx"), ("xlsx-stream", "benchmark-100mb.xlsx"),
        ("xls-bytes", "benchmark-100mb.xls"), ("xls-npoifs-filestream", "benchmark-100mb.xls"),
        ("doc-b2x-bytes", "benchmark-100mb.doc"), ("doc-b2x-stream", "benchmark-100mb.doc"),
        ("cfb-openmcdf-bytes", "benchmark-100mb.doc"), ("cfb-openmcdf-stream", "benchmark-100mb.doc"),
    };
    Console.WriteLine("mode | peakWS MB | allocated MB | LOH-ish note");
    foreach (var (mode, file) in modes)
    {
        var psi = new ProcessStartInfo(Environment.ProcessPath!, $"\"{typeof(Program).Assembly.Location}\" {mode} \"{Path.Combine(dir, file)}\"")
        { RedirectStandardOutput = true };
        using var p = Process.Start(psi)!;
        string o = p.StandardOutput.ReadToEnd().Trim();
        p.WaitForExit();
        Console.WriteLine(o);
    }
    return;
}

string path = args[1];
long before = GC.GetTotalAllocatedBytes(true);
long chars = 0;
switch (args[0])
{
    case "docx-bytes":
    {
        byte[] b = File.ReadAllBytes(path);
        using var d = WordprocessingDocument.Open(new MemoryStream(b, false), false);
        chars = WordText(d);
        break;
    }
    case "docx-stream":
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var d = WordprocessingDocument.Open(fs, false);
        chars = WordText(d);
        break;
    }
    case "xlsx-bytes":
    {
        byte[] b = File.ReadAllBytes(path);
        using var d = SpreadsheetDocument.Open(new MemoryStream(b, false), false);
        chars = XlsxText(d);
        break;
    }
    case "xlsx-stream":
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var d = SpreadsheetDocument.Open(fs, false);
        chars = XlsxText(d);
        break;
    }
    case "xls-bytes":
    {
        byte[] b = File.ReadAllBytes(path);
        var wb = new HSSFWorkbook(new MemoryStream(b));
        chars = wb.NumberOfSheets;
        break;
    }
    case "xls-npoifs-filestream":
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var wb = new HSSFWorkbook(new NPOIFSFileSystem(fs));
        chars = wb.NumberOfSheets;
        break;
    }
    case "doc-b2x-bytes":
    {
        byte[] b = File.ReadAllBytes(path);
        using var r = new StructuredStorageReader(new MemoryStream(b));
        chars = r.FullNameOfAllStreamEntries.Count;
        break;
    }
    case "doc-b2x-stream":
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var r = new StructuredStorageReader(fs);
        chars = r.FullNameOfAllStreamEntries.Count;
        break;
    }
    case "cfb-openmcdf-bytes":
    {
        byte[] b = File.ReadAllBytes(path);
        using var root = OpenMcdf.RootStorage.Open(new MemoryStream(b));
        chars = root.EnumerateEntries().Count();
        break;
    }
    case "cfb-openmcdf-stream":
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var root = OpenMcdf.RootStorage.Open(fs);
        chars = root.EnumerateEntries().Count();
        break;
    }
    default: throw new ArgumentException(args[0]);
}
long alloc = GC.GetTotalAllocatedBytes(true) - before;
var proc = Process.GetCurrentProcess();
Console.WriteLine($"{args[0]} | {proc.PeakWorkingSet64 / 1048576.0:F0} | {alloc / 1048576.0:F0} | result={chars}");

static long WordText(WordprocessingDocument d) =>
    d.MainDocumentPart?.Document?.Body is { } body
        ? string.Join('\n', body.Descendants<Paragraph>().Select(p => p.InnerText)).Length : 0;

static long XlsxText(SpreadsheetDocument d)
{
    long n = 0;
    var sst = d.WorkbookPart?.SharedStringTablePart?.SharedStringTable;
    if (sst != null) foreach (var i in sst.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>()) n += i.InnerText.Length;
    return n;
}
