using System.Security.Cryptography;
using System.Text.Json;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Encryption.Legacy;
using Microsoft.Extensions.DependencyInjection;
using NPOI.POIFS.FileSystem;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy
{
    public class PlainCompoundFileBuilderTests
    {
        private const string DocMime = "application/msword";
        private const string PptMime = "application/vnd.ms-powerpoint";
        private const string PowerPointClsid = "{64818D10-4F9B-11CF-86EA-00AA00B929E8}";
        private const long Cap = 1L << 30;
        private static readonly IReadOnlyDictionary<string, byte[]?> NoChanges = new Dictionary<string, byte[]?>();

        private static readonly Lazy<MainExtractor> Extractor = new(() =>
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        });

        public static TheoryData<string> RoundTripFiles => new()
        {
            "samples/synthetic/macro-embed/sample.doc",
            "samples/synthetic/multi-embedding/sample.doc",
            "samples/adversarial/unnamed-embedded-storage.doc",
        };

        private static byte[] Read(string relativePath) => File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(relativePath));

        private static byte[] Rebuild(byte[] source, IReadOnlyDictionary<string, byte[]?> changes, string mime = DocMime, long cap = Cap)
        {
            using var fs = new NPOIFSFileSystem(new MemoryStream(source));
            return PlainCompoundFileBuilder.Build(fs.Root, changes, cap, mime);
        }

        private static byte[] StreamBytes(DirectoryNode parent, string name)
        {
            var doc = (DocumentNode)parent.GetEntry(name);
            var bytes = new byte[doc.Size];
            using var input = parent.CreatePOIFSDocumentReader(name);
            input.ReadFully(bytes);
            return bytes;
        }

        private static void Describe(DirectoryNode node, string path, SortedDictionary<string, string> sink)
        {
            sink[path + "|clsid"] = node.StorageClsid.ToString();
            var entries = node.Entries;
            while (entries.MoveNext())
            {
                var childPath = path + "/" + entries.Current.Name;
                if (entries.Current is DirectoryNode dir)
                {
                    Describe(dir, childPath, sink);
                }
                else
                {
                    var bytes = StreamBytes(node, entries.Current.Name);
                    sink[childPath] = Convert.ToHexString(SHA256.HashData(bytes)) + ":" + bytes.Length;
                }
            }
        }

        private static SortedDictionary<string, string> Tree(byte[] bytes)
        {
            using var fs = new NPOIFSFileSystem(new MemoryStream(bytes));
            var sink = new SortedDictionary<string, string>(StringComparer.Ordinal);
            Describe(fs.Root, string.Empty, sink);
            return sink;
        }

        private static string ExtractJson(byte[] bytes)
        {
            var r = Extractor.Value.Extract(new ExtractionRequest { FileBytes = bytes, FileName = "f.doc", DetectedMimeType = DocMime });
            return JsonSerializer.Serialize(new { r.ExtractedText, r.Metadata, r.FormatMetadata, r.EmbeddedFiles, r.FieldErrors });
        }

        private static void AssertUnchangedEntriesEqual(byte[] source, byte[] rebuilt, params string[] changed)
        {
            var expected = Tree(source).Where(kv => !changed.Any(c => kv.Key.StartsWith(c, StringComparison.Ordinal)));
            var actual = Tree(rebuilt).Where(kv => !changed.Any(c => kv.Key.StartsWith(c, StringComparison.Ordinal)));
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(RoundTripFiles))]
        public void Build_EmptyChanges_KeepsTreeClsidsAndStreams(string path)
        {
            var source = Read(path);

            var rebuilt = Rebuild(source, NoChanges);

            Assert.Equal(Tree(source), Tree(rebuilt));
            Assert.Equal(ExtractJson(source), ExtractJson(rebuilt));
        }

        [Fact]
        public void Build_NoMiniStreamPpt_ReplaceCurrentUser_OpensInStructuredStorageReader()
        {
            var source = Read("samples/encrypted/cryptoapi-proc2356.ppt");
            byte[] own;
            using (var fs = new NPOIFSFileSystem(new MemoryStream(source)))
            {
                own = StreamBytes(fs.Root, "Current User");
            }

            var replacement = own.Concat(new byte[] { 7 }).ToArray();

            var rebuilt = Rebuild(source, new Dictionary<string, byte[]?> { ["Current User"] = replacement }, PptMime);

            using var reader = new b2xtranslator.StructuredStorage.Reader.StructuredStorageReader(new MemoryStream(rebuilt));
            using var copy = new NPOIFSFileSystem(new MemoryStream(rebuilt));
            Assert.Equal(PowerPointClsid, copy.Root.StorageClsid.ToString());
            Assert.Equal(replacement, StreamBytes(copy.Root, "Current User"));
            AssertUnchangedEntriesEqual(source, rebuilt, "/Current User");
        }

        [Fact]
        public void Build_ShrinkingMiniStreamPpt_RemoveAndAdd_OpensInStructuredStorageReader()
        {
            var source = Read("samples/encrypted/Password_Protected-hello.ppt");
            var changes = new Dictionary<string, byte[]?>
            {
                ["EncryptedSummary"] = null,
                ["\u0005SummaryInformation"] = new byte[72],
                ["\u0005DocumentSummaryInformation"] = new byte[144],
            };

            var rebuilt = Rebuild(source, changes, PptMime);

            using var reader = new b2xtranslator.StructuredStorage.Reader.StructuredStorageReader(new MemoryStream(rebuilt));
            var tree = Tree(rebuilt);
            Assert.DoesNotContain("/EncryptedSummary", tree.Keys);
            AssertUnchangedEntriesEqual(source, rebuilt, "/EncryptedSummary", "/SummaryInformation", "/DocumentSummaryInformation");
            Assert.EndsWith(":72", tree["/\u0005SummaryInformation"], StringComparison.Ordinal);
            Assert.EndsWith(":144", tree["/\u0005DocumentSummaryInformation"], StringComparison.Ordinal);
        }

        [Fact]
        public void Build_ReplacementExactlyAtCap_Succeeds()
        {
            var source = Read("samples/synthetic/macro-embed/sample.doc");
            var changes = new Dictionary<string, byte[]?> { ["x"] = new byte[10] };

            var rebuilt = Rebuild(source, changes, cap: 10);

            Assert.EndsWith(":10", Tree(rebuilt)["/x"], StringComparison.Ordinal);
        }

        [Fact]
        public void Build_ReplacementOverCap_ThrowsOversizedNestedContent()
        {
            var source = Read("samples/synthetic/macro-embed/sample.doc");
            var changes = new Dictionary<string, byte[]?> { ["x"] = new byte[11] };

            var ex = Assert.Throws<OversizedNestedContentException>(() => Rebuild(source, changes, cap: 10));

            Assert.Equal("OVERSIZED_NESTED_CONTENT", ex.Code);
        }
    }
}
