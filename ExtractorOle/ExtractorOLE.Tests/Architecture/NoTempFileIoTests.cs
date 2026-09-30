using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ExtractorOLE.Tests.Architecture
{
    // #169: the doc/ppt b2xtranslator conversions run in memory. Guards against temp-file IO
    // creeping back into the extraction handlers or the b2x converters.
    public class NoTempFileIoTests
    {
        private static readonly string[] ForbiddenApis =
        {
            "Path.GetTempFileName",
            "Path.GetTempPath",
            "File.WriteAllBytes",
            "File.ReadAllBytes",
        };

        private static string FindProductDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "ExtractorOle", "ExtractorOLE");
                if (Directory.Exists(Path.Combine(candidate, "Handlers"))) return candidate;
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException($"Could not locate ExtractorOle/ExtractorOLE by walking up from {AppContext.BaseDirectory}");
        }

        private static IEnumerable<string> GuardedSourceFiles()
        {
            var productDir = FindProductDir();
            return Directory.GetFiles(Path.Combine(productDir, "Handlers"), "*.cs", SearchOption.AllDirectories)
                .Append(Path.Combine(productDir, "Helpers", "FileTypeStrategy", "PptToPptxConverter.cs"));
        }

        [Fact]
        public void HandlersAndB2xConverters_DoNotUseTempFileApis()
        {
            var violations = GuardedSourceFiles()
                .SelectMany(file => File.ReadAllLines(file)
                    .Select((line, i) => (file, line, lineNo: i + 1)))
                .Where(x => !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                            && ForbiddenApis.Any(api => x.line.Contains(api, StringComparison.Ordinal)))
                .Select(x => $"{Path.GetFileName(x.file)}:{x.lineNo}: {x.line.Trim()}")
                .ToList();

            Assert.True(violations.Count == 0, "Temp-file IO found:\n" + string.Join("\n", violations));
        }
    }
}
