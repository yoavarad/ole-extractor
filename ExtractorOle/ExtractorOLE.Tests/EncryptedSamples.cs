using System.Text.Json;

namespace ExtractorOLE.Tests
{
    // Locates the encrypted corpus (docs/specs/dataset-curation.md): a sample is encrypted iff it lives
    // under samples/encrypted/. The password and unencrypted twin are read from samples/manifest.json,
    // never hard-coded.
    internal static class EncryptedSamples
    {
        internal sealed record EncryptedSample(string Path, string FullPath, string? Password, string? UnencryptedTwin);

        public static IReadOnlyList<EncryptedSample> Enumerate()
        {
            var manifestPath = FindRepoRootFile("samples/manifest.json");
            var samplesRoot = System.IO.Path.GetDirectoryName(manifestPath)!;
            var folder = System.IO.Path.Combine(samplesRoot, "encrypted");

            using var doc = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
            var entries = doc.RootElement.GetProperty("samples").EnumerateArray()
                .ToDictionary(e => e.GetProperty("path").GetString()!, e => e.Clone(), StringComparer.OrdinalIgnoreCase);

            var result = new List<EncryptedSample>();
            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                var relative = "samples/" + System.IO.Path.GetRelativePath(samplesRoot, file).Replace(System.IO.Path.DirectorySeparatorChar, '/');
                var entry = entries[relative];
                var twin = entry.TryGetProperty("unencryptedTwin", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;
                var password = entry.TryGetProperty("password", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
                result.Add(new EncryptedSample(relative, file, password, twin));
            }

            return result;
        }

        public static byte[] ReadBytes(string fileName) =>
            File.ReadAllBytes(Enumerate().Single(s => string.Equals(System.IO.Path.GetFileName(s.Path), fileName, StringComparison.OrdinalIgnoreCase)).FullPath);

        private static string FindRepoRootFile(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = System.IO.Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
        }
    }
}
