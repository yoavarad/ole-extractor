using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using SampleGenerator.Abstractions;
using SampleGenerator.Fixtures;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Builds the fixed 10MB and ~100MB benchmark samples (T-f0963fc3), one per
    /// tier per format, for the extraction latency/memory benchmarks. Everything
    /// is a pure function of (format, target size): no RNG, no wall clock.
    ///
    /// Composition:
    /// - docx/xlsx/pptx: the existing OpenXml generators author a line-numbered,
    ///   multilingual body (<see cref="MultilingualFixtures.ComposeLargeBody"/>,
    ///   about target/20 words) plus first-layer embedded-object padding parts of
    ///   fixed-seed SplitMix64 bytes (incompressible, so declared uncompressed
    ///   size stays close to the file size, far under the 2 GiB cap). The zip is
    ///   then rewritten with fixed entry order/timestamps, because the OpenXml
    ///   SDK output embeds wall-clock entry times.
    /// - xls: NPOI HSSFWorkbook, one line-numbered multilingual text row per
    ///   line, row count fitted to the target (BIFF text is not compressed).
    /// - doc/ppt: no legacy writer exists in the repo (ADR-004), so the
    ///   Word-authored multilingual.doc / PowerPoint-authored with_textbox.ppt
    ///   test fixtures are the extractable content and are padded with the
    ///   stream real large files are dominated by: "Data" (doc) and "Pictures"
    ///   (ppt, valid JPEG BLIP records) filled with the same fixed-seed bytes.
    ///   Only the small template text is extracted from these two formats.
    ///
    /// The size fit lands in [<see cref="MinFillRatio"/> x target, target].
    /// </summary>
    public static class BenchmarkSampleGenerator
    {
        public const long Tier10MbBytes = 10_000_000;
        public const long Tier100MbBytes = 102_400_000; // 100,000 KiB = 97.66 MiB: inside 95-100 MiB, 2.4MB under the 104,857,600 cap
        public const double MinFillRatio = 0.99;

        private const int WordsPerLine = 400;
        private const int TargetBytesPerWord = 20;
        private const int PadChunkBytes = 1 << 20;
        private const int PaddingStreamMinBytes = 4096;
        private static readonly DateTimeOffset FixedEntryTime = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public static readonly IReadOnlyList<(string Name, long Bytes)> Tiers = new[]
        {
            ("10mb", Tier10MbBytes),
            ("100mb", Tier100MbBytes),
        };

        public static string FileName(SampleFormat format, string tier) =>
            $"benchmark-{tier}.{format.ToString().ToLowerInvariant()}";

        public static byte[] Generate(SampleFormat format, long targetBytes) => format switch
        {
            SampleFormat.Docx => GenerateOoxml(new DocxSampleGenerator(), targetBytes),
            SampleFormat.Xlsx => GenerateOoxml(new XlsxSampleGenerator(), targetBytes),
            SampleFormat.Pptx => GenerateOoxml(new PptxSampleGenerator(), targetBytes),
            SampleFormat.Xls => GenerateXls(targetBytes),
            SampleFormat.Doc => GenerateLegacyPadded(
                "ExtractorOle/ExtractorOLE.Tests/Doc/Fixtures/multilingual.doc", "Data", targetBytes, DataPadding),
            SampleFormat.Ppt => GenerateLegacyPadded(
                "ExtractorOle/ExtractorOLE.Tests/PowerPoint/Fixtures/with_textbox.ppt", "Pictures", targetBytes, BlipPadding),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };

        /// <summary>
        /// Writes all 12 samples into <paramref name="outputDir"/>, prints
        /// "path bytes sha256" per file and, when a manifest exists, compares
        /// each SHA-256 with its <c>benchmarkSamples</c> entry. Returns false on
        /// any mismatch or missing entry.
        /// </summary>
        public static bool WriteAll(string outputDir, string? manifestPath)
        {
            var expected = LoadManifestHashes(manifestPath);
            Directory.CreateDirectory(outputDir);
            var allMatch = true;

            foreach (var (tier, bytes) in Tiers)
            {
                foreach (var format in Enum.GetValues<SampleFormat>())
                {
                    var path = Path.Combine(outputDir, FileName(format, tier));
                    var content = Generate(format, bytes);
                    File.WriteAllBytes(path, content);

                    var sha = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
                    var verdict = "";
                    if (expected is not null)
                    {
                        var key = $"samples/benchmark/{FileName(format, tier)}";
                        var match = expected.TryGetValue(key, out var want) && want == sha;
                        allMatch &= match;
                        verdict = match ? " MATCHES-MANIFEST" : " MISMATCH-VS-MANIFEST";
                    }
                    Console.WriteLine($"{path} {content.Length} {sha}{verdict}");
                }
            }

            return allMatch;
        }

        private static Dictionary<string, string>? LoadManifestHashes(string? manifestPath)
        {
            if (manifestPath is null || !File.Exists(manifestPath)) return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var hashes = new Dictionary<string, string>();
            if (doc.RootElement.TryGetProperty("benchmarkSamples", out var entries))
            {
                foreach (var entry in entries.EnumerateArray())
                {
                    hashes[entry.GetProperty("path").GetString()!] = entry.GetProperty("sha256").GetString()!;
                }
            }
            return hashes;
        }

        // ---- OOXML ----

        private static byte[] GenerateOoxml(ISampleGenerator generator, long target)
        {
            var body = NumberedBody((int)(target / TargetBytesPerWord));
            return Fit(target, 0, PadChunkBytes, pad => NormalizeZip(generator.Generate(new SampleSpec
            {
                BodyText = body,
                Embeddings = PaddingParts(pad),
            }).Content));
        }

        private static List<EmbeddedContentSpec> PaddingParts(long padBytes)
        {
            var parts = new List<EmbeddedContentSpec>();
            foreach (var (index, length) in Chunks(padBytes))
            {
                parts.Add(new EmbeddedContentSpec
                {
                    FileName = $"padding-{index:D4}.bin",
                    ContentType = "application/octet-stream",
                    Content = PseudoRandomBytes(length, (ulong)index),
                });
            }
            return parts;
        }

        // Splits totalBytes into ceil(total / PadChunkBytes) near-equal chunks.
        private static IEnumerable<(int Index, int Length)> Chunks(long totalBytes)
        {
            if (totalBytes <= 0) yield break;

            var count = (int)((totalBytes + PadChunkBytes - 1) / PadChunkBytes);
            for (var i = 0; i < count; i++)
            {
                yield return (i, (int)(totalBytes / count + (i < totalBytes % count ? 1 : 0)));
            }
        }

        // Rewrites the package so the same input always yields the same bytes: [Content_Types].xml
        // first, the rest in ordinal name order, a fixed timestamp on every entry, stored-size
        // compression for entries deflate could not shrink (the padding), and stable rId1..n
        // relationship ids (System.IO.Packaging draws them from a Guid on every run) rewritten in
        // the .rels files and in the part that owns each one.
        private static byte[] NormalizeZip(byte[] zip)
        {
            using var source = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read);
            var entries = source.Entries
                .OrderBy(e => e.FullName != "[Content_Types].xml")
                .ThenBy(e => e.FullName, StringComparer.Ordinal)
                .ToList();

            var rewrittenRels = new Dictionary<string, byte[]>();
            var idMapByPart = new Dictionary<string, Dictionary<string, string>>();
            foreach (var entry in entries.Where(e => e.FullName.EndsWith(".rels", StringComparison.Ordinal)))
            {
                XDocument doc;
                using (var stream = entry.Open()) doc = XDocument.Load(stream);

                var relationships = doc.Root!.Elements()
                    .OrderBy(r => (string?)r.Attribute("Target"), StringComparer.Ordinal)
                    .ThenBy(r => (string?)r.Attribute("Type"), StringComparer.Ordinal)
                    .ToList();
                var map = new Dictionary<string, string>();
                for (var i = 0; i < relationships.Count; i++)
                {
                    var id = relationships[i].Attribute("Id")!;
                    map[id.Value] = $"rId{i + 1}";
                    id.Value = map[id.Value];
                }
                doc.Root.ReplaceNodes(relationships);

                rewrittenRels[entry.FullName] = Utf8(doc.ToString(SaveOptions.DisableFormatting), declaration: true);
                idMapByPart[OwningPart(entry.FullName)] = map;
            }

            using var output = new MemoryStream();
            using (var target = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var entry in entries)
                {
                    var level = entry.Length > 0 && entry.CompressedLength >= entry.Length
                        ? CompressionLevel.NoCompression
                        : CompressionLevel.Optimal;
                    var copy = target.CreateEntry(entry.FullName, level);
                    copy.LastWriteTime = FixedEntryTime;
                    using var to = copy.Open();

                    if (rewrittenRels.TryGetValue(entry.FullName, out var rels))
                    {
                        to.Write(rels);
                    }
                    else if (idMapByPart.TryGetValue(entry.FullName, out var map) && map.Count > 0
                             && entry.FullName.EndsWith(".xml", StringComparison.Ordinal))
                    {
                        using var reader = new StreamReader(entry.Open());
                        var xml = Regex.Replace(reader.ReadToEnd(), "\"([^\"]*)\"",
                            m => map.TryGetValue(m.Groups[1].Value, out var stable) ? $"\"{stable}\"" : m.Value);
                        to.Write(Utf8(xml, declaration: false));
                    }
                    else
                    {
                        using var from = entry.Open();
                        from.CopyTo(to);
                    }
                }
            }
            return output.ToArray();
        }

        // "word/_rels/document.xml.rels" belongs to "word/document.xml"; "_rels/.rels" to the package.
        private static string OwningPart(string relsEntryName)
        {
            var slash = relsEntryName.LastIndexOf("_rels/", StringComparison.Ordinal);
            var directory = relsEntryName[..slash];
            var file = relsEntryName[(slash + "_rels/".Length)..^".rels".Length];
            return directory + file;
        }

        private static byte[] Utf8(string xml, bool declaration) =>
            new UTF8Encoding(false).GetBytes(declaration
                ? "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + xml
                : xml);

        // ---- xls ----

        private static byte[] GenerateXls(long target) =>
            Fit(target, 0, 100, rows =>
            {
                var lines = NumberedBody((int)(rows * WordsPerLine)).Split('\n');
                using var workbook = new HSSFWorkbook();
                var sheet = workbook.CreateSheet("Benchmark");
                for (var i = 0; i < rows; i++)
                {
                    sheet.CreateRow(i).CreateCell(0).SetCellValue(lines[i]);
                }
                using var ms = new MemoryStream();
                workbook.Write(ms);
                return ms.ToArray();
            });

        // ---- doc / ppt ----

        private static byte[] GenerateLegacyPadded(
            string templateRepoPath, string streamName, long target, Func<long, byte[]> padding)
        {
            var template = File.ReadAllBytes(FindRepoFile(templateRepoPath));
            return Fit(target, 0, PadChunkBytes, pad =>
            {
                if (pad < PaddingStreamMinBytes) return template;

                var fs = new NPOIFSFileSystem(new MemoryStream(template));
                try
                {
                    fs.Root.CreateDocument(streamName, new MemoryStream(padding(pad)));
                    using var output = new MemoryStream();
                    fs.WriteFileSystem(output);
                    return output.ToArray();
                }
                finally
                {
                    fs.Close();
                }
            });
        }

        private static byte[] DataPadding(long length) => PseudoRandomBytes((int)length, 0);

        // A "Pictures" stream is a run of OfficeArt BLIP records. Each is a JPEG BLIP
        // (recInstance 0x46A, recType 0xF01D): 8-byte header + 16-byte UID + 1-byte tag + payload.
        private static byte[] BlipPadding(long length)
        {
            const int overhead = 8 + 16 + 1;
            using var ms = new MemoryStream();
            foreach (var (index, size) in Chunks(length))
            {
                var header = new byte[overhead];
                BinaryPrimitives.WriteUInt16LittleEndian(header, 0x46A0);
                BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(2), 0xF01D);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)(size - 8));
                BinaryPrimitives.WriteUInt64LittleEndian(header.AsSpan(8), (ulong)index + 1); // distinct UID per BLIP
                ms.Write(header);
                ms.Write(PseudoRandomBytes(size - overhead, (ulong)index));
            }
            return ms.ToArray();
        }

        // ---- shared ----

        // ComposeLargeBody lines repeat every 43 lines; the line number keeps every line unique so
        // BIFF shared strings cannot dedup them and the text is not a pure repeat.
        private static string NumberedBody(int wordCount)
        {
            var lines = MultilingualFixtures.ComposeLargeBody(wordCount, WordsPerLine)
                .Replace("\r\n", "\n").Split('\n');
            return string.Join("\n", lines.Select((line, i) => $"{i + 1:D7} {line}"));
        }

        // SplitMix64 with a caller-chosen fixed seed: deterministic, well mixed, not compressible.
        private static byte[] PseudoRandomBytes(int length, ulong seed)
        {
            var bytes = new byte[length];
            var state = seed;
            var word = new byte[8];
            for (var offset = 0; offset < length; offset += 8)
            {
                state += 0x9E3779B97F4A7C15UL;
                var z = state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                BinaryPrimitives.WriteUInt64LittleEndian(word, z);
                word.AsSpan(0, Math.Min(8, length - offset)).CopyTo(bytes.AsSpan(offset));
            }
            return bytes;
        }

        // Size is near-linear in the scaling parameter (padding bytes, or text rows). Two cheap
        // probes give the slope; then converge on the middle of [MinFillRatio x target, target].
        private static byte[] Fit(long target, long probeLow, long probeHigh, Func<long, byte[]> build)
        {
            var sizeLow = build(probeLow).Length;
            var sizeHigh = build(probeHigh).Length;
            var slope = (double)(sizeHigh - sizeLow) / (probeHigh - probeLow);
            var aim = (long)(target * (1 + MinFillRatio) / 2);

            var parameter = probeLow + (long)((aim - sizeLow) / slope);
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var bytes = build(parameter);
                if (bytes.Length <= target && bytes.Length >= target * MinFillRatio) return bytes;
                parameter += (long)((aim - bytes.Length) / slope);
            }
            throw new InvalidOperationException($"Could not fit a sample into [{target * MinFillRatio:F0}, {target}] bytes.");
        }

        private static string FindRepoFile(string relativePath)
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    var candidate = Path.Combine(dir.FullName, relativePath);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            throw new FileNotFoundException($"Could not locate '{relativePath}' from the base or current directory.");
        }
    }
}
