using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace ExtractorOLE.StressHarness
{
    /// <summary>
    /// <c>password</c> mode (ADR-012): runs encrypted and unencrypted xls with the correct, a wrong and no
    /// password through <see cref="MainExtractor.ExtractManyAsync"/> and
    /// <see cref="MainExtractor.ExtractManyStreamingAsync"/> at MaxDOP above the core count, and checks each
    /// item's outcome against its manifest expectation. With <c>--cross-wire</c> the passwords of the
    /// password-protected samples are swapped while the expectations are kept, so the run must fail.
    /// Usage: password [repeat] [samplesDir] [--cross-wire]
    /// </summary>
    internal static class PasswordBatchMode
    {
        private const int DefaultRepeat = 20;
        private const string WrongPassword = "not-the-password";
        private const string XlsMime = "application/vnd.ms-excel";

        internal sealed record PasswordCase(string Label, byte[] Bytes, string? Password, string? ExpectedText, string? ExpectedError);

        public static async Task RunAsync(string[] args)
        {
            bool crossWire = args.Contains("--cross-wire", StringComparer.Ordinal);
            var positional = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToArray();
            int repeat = positional.Length > 0 && int.TryParse(positional[0], out var r) ? r : DefaultRepeat;
            string? samplesDir = positional.Length > 1 ? positional[1] : null;

            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var extractor = provider.GetRequiredService<MainExtractor>();

            var directory = Program.ResolveSamplesDirectory(samplesDir);
            if (directory is null)
            {
                Console.WriteLine("[FAIL] samples directory not found");
                Environment.ExitCode = 1;
                return;
            }

            var cases = BuildCases(directory, twin => extractor.Extract(Request(twin, null)).ExtractedText, crossWire);
            if (cases.Count == 0)
            {
                Console.WriteLine("[FAIL] no password-protected xls samples in the manifest");
                Environment.ExitCode = 1;
                return;
            }

            var requests = Enumerable.Range(0, repeat)
                .SelectMany(_ => cases.Select(c => Request(c.Bytes, c.Password)))
                .ToList();
            int dop = Math.Max(2, Environment.ProcessorCount * 2);
            Console.WriteLine($"Password mode | Cases: {cases.Count} | Repeat: {repeat} | Items: {requests.Count} | MaxDOP: {dop} | ProcessorCount: {Environment.ProcessorCount}{(crossWire ? " | cross-wired" : string.Empty)}");

            var listBatch = await extractor.ExtractManyAsync(requests, dop);
            long mismatches = Check("ExtractManyAsync", listBatch, cases, requests.Count);

            var streamed = new List<BatchExtractionResult>();
            await foreach (var item in extractor.ExtractManyStreamingAsync(requests, dop))
            {
                streamed.Add(item);
            }

            mismatches += Check("ExtractManyStreamingAsync", streamed, cases, requests.Count);
            Console.WriteLine(mismatches == 0 ? "[PASS] every outcome matches its manifest expectation" : "[FAIL] outcomes differ from manifest expectations");
            Environment.ExitCode = mismatches == 0 ? 0 : 1;
        }

        private static long Check(string api, IReadOnlyList<BatchExtractionResult> items, List<PasswordCase> cases, int expectedCount)
        {
            long mismatches = items.Count == expectedCount ? 0 : 1;
            foreach (var item in items)
            {
                var c = cases[item.Index % cases.Count];
                if (!Matches(c, item))
                {
                    mismatches++;
                    Console.WriteLine($"[mismatch] {api} item {item.Index} ({c.Label}): {Describe(item)}");
                }
            }

            Console.WriteLine($"{api,-27} | items: {items.Count} | mismatches: {mismatches}");
            return mismatches;
        }

        internal static bool Matches(PasswordCase c, BatchExtractionResult item) =>
            item.Succeeded
                ? c.ExpectedError is null && string.Equals(item.Result!.ExtractedText, c.ExpectedText, StringComparison.Ordinal)
                : c.ExpectedError is not null && string.Equals(item.Error!.GetType().Name, c.ExpectedError, StringComparison.Ordinal);

        private static string Describe(BatchExtractionResult item) =>
            item.Succeeded ? "succeeded with unexpected text" : $"failed with {item.Error!.GetType().Name}";

        private static ExtractionRequest Request(byte[] bytes, string? password) =>
            new() { FileBytes = bytes, FileName = "fixture.xls", DetectedMimeType = XlsMime, Password = password };

        /// <summary>
        /// Cases from xls manifest entries that carry an <c>unencryptedTwin</c>: the twin itself (password
        /// ignored), password-protected entries (correct, wrong, none), and default-key entries (decrypt
        /// whatever password is supplied). Expected text is the twin's own extraction.
        /// </summary>
        internal static List<PasswordCase> BuildCases(string samplesDirectory, Func<byte[], string> extractTwinText, bool crossWire)
        {
            var root = Path.GetDirectoryName(Path.GetFullPath(samplesDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))!;
            using var doc = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(samplesDirectory, "manifest.json")));
            var cases = new List<PasswordCase>();
            var protectedEntries = new List<(string Name, byte[] Bytes, string Password, string TwinText)>();
            var twinTexts = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var entry in doc.RootElement.GetProperty("samples").EnumerateArray())
            {
                if (!entry.TryGetProperty("path", out var pathElement) || !entry.TryGetProperty("unencryptedTwin", out var twinElement)
                    || entry.GetProperty("format").GetString() != "xls")
                {
                    continue;
                }

                var path = pathElement.GetString()!;
                var twinPath = twinElement.GetString()!;
                if (!twinTexts.TryGetValue(twinPath, out var twinText))
                {
                    var twinBytes = File.ReadAllBytes(Path.Combine(root, twinPath));
                    twinText = extractTwinText(twinBytes);
                    twinTexts[twinPath] = twinText;
                    cases.Add(new PasswordCase($"{twinPath} (unencrypted) + wrong password", twinBytes, WrongPassword, twinText, null));
                    cases.Add(new PasswordCase($"{twinPath} (unencrypted) + no password", twinBytes, null, twinText, null));
                }

                var bytes = File.ReadAllBytes(Path.Combine(root, path));
                if (entry.TryGetProperty("password", out var password))
                {
                    protectedEntries.Add((path, bytes, password.GetString()!, twinText));
                }
                else
                {
                    cases.Add(new PasswordCase($"{path} (default key) + wrong password", bytes, WrongPassword, twinText, null));
                    cases.Add(new PasswordCase($"{path} (default key) + no password", bytes, null, twinText, null));
                }
            }

            for (int i = 0; i < protectedEntries.Count; i++)
            {
                var (name, bytes, correct, twinText) = protectedEntries[i];
                // Cross-wired: the correct-password item receives the next sample's password, expectation unchanged.
                var supplied = crossWire ? protectedEntries[(i + 1) % protectedEntries.Count].Password : correct;
                cases.Add(new PasswordCase($"{name} + correct password", bytes, supplied, twinText, null));
                cases.Add(new PasswordCase($"{name} + wrong password", bytes, WrongPassword, null, nameof(InvalidPasswordException)));
                cases.Add(new PasswordCase($"{name} + no password", bytes, null, null, nameof(PasswordProtectedException)));
            }

            return cases;
        }
    }
}
