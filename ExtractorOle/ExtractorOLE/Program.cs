using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);

            using var provider = services.BuildServiceProvider();

            var helper = provider.GetRequiredService<IExtractionHelper>();
            var extractor = provider.GetRequiredService<MainExtractor>();

            string? filePath = args.Length > 0 ? args[0] : FindDefaultSampleFile();
            if (filePath == null || !File.Exists(filePath))
            {
                Console.WriteLine($"Could not find a sample file to run against. Pass a file path as an argument, e.g.:");
                Console.WriteLine("  ExtractorOLE.exe path\\to\\file.docx");
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            Console.WriteLine($"File: {Path.GetFullPath(filePath)} ({fileBytes.Length} bytes)");

            // 1) Detection
            var detectedMime = helper.DetectMimeTypeFromBytes(fileBytes);
            Console.WriteLine($"Detected format: {detectedMime} ({helper.MimeFor(detectedMime)})");

            // 2) Extraction
            var result = extractor.Extract(fileBytes);
            Console.WriteLine($"Extraction MIME type: {result.MimeType}");
            Console.WriteLine($"Title: {result.Metadata.Title}");
            Console.WriteLine($"Creator: {result.Metadata.Creator}");
            Console.WriteLine($"Created: {result.Metadata.Created}");
            Console.WriteLine($"Extracted text length: {result.ExtractedText.Length} chars");
            Console.WriteLine($"Extracted text preview: {Preview(result.ExtractedText)}");
            Console.WriteLine($"Embedded files: {result.EmbeddedFiles.Count}");
            foreach (var embedded in result.EmbeddedFiles)
            {
                Console.WriteLine($"  - {embedded.FileName} ({embedded.SizeInBytes} bytes) [{embedded.PackagePath}]");
            }
        }

        private static string Preview(string text)
        {
            const int maxLength = 200;
            if (string.IsNullOrEmpty(text)) return "(empty)";
            var flattened = text.Replace("\r", " ").Replace("\n", " ");
            return flattened.Length > maxLength ? flattened.Substring(0, maxLength) + "..." : flattened;
        }

        // No sample file was passed on the command line: walk up from the executable's
        // location looking for the repo's checked-in samples/ directory.
        private static string? FindDefaultSampleFile()
        {
            const string relativeSample = "samples/curated/docx/invoice-acme-corp.docx";

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativeSample);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            return null;
        }
    }
}
