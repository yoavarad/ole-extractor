using System.Text.Json;

namespace ExtractorOLE.StressHarness
{
    /// <summary>
    /// The set of samples that are expected to fail extraction: manifest.json entries carrying an
    /// <c>expectedBehavior</c> (the corpus's deliberate adversarial failures). Both harness modes exit 1
    /// when the actual failing set differs from this set in either direction.
    /// </summary>
    internal static class ExpectedFailures
    {
        /// <summary>Sample names are paths relative to the samples directory, '/'-separated.</summary>
        public static HashSet<string> Load(string? samplesDirectory)
        {
            var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (samplesDirectory is null)
            {
                return expected;
            }

            var manifestPath = Path.Combine(samplesDirectory, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return expected;
            }

            using var doc = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
            if (!doc.RootElement.TryGetProperty("samples", out var entries))
            {
                return expected;
            }

            foreach (var entry in entries.EnumerateArray())
            {
                if (entry.TryGetProperty("expectedBehavior", out _) && entry.TryGetProperty("path", out var path))
                {
                    var name = path.GetString()!.Replace(Path.DirectorySeparatorChar, '/');
                    expected.Add(name.StartsWith("samples/", StringComparison.OrdinalIgnoreCase) ? name["samples/".Length..] : name);
                }
            }

            return expected;
        }

        /// <summary>0 when the failing sets match; 1 (with the differences printed) otherwise.</summary>
        public static int ExitCodeFor(IReadOnlySet<string> expected, IReadOnlySet<string> actual)
        {
            var unexpectedFailures = actual.Where(a => !expected.Contains(a)).OrderBy(a => a, StringComparer.Ordinal).ToList();
            var unexpectedPasses = expected.Where(e => !actual.Contains(e)).OrderBy(e => e, StringComparer.Ordinal).ToList();
            if (unexpectedFailures.Count == 0 && unexpectedPasses.Count == 0)
            {
                return 0;
            }

            Console.WriteLine($"[FAIL] failing set differs from expected: unexpected failures [{string.Join(", ", unexpectedFailures)}], expected failures that passed [{string.Join(", ", unexpectedPasses)}]");
            return 1;
        }
    }
}
