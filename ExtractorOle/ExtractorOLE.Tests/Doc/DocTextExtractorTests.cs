using ExtractorOLE.Handlers;
using SampleGenerator.Fixtures;
using System;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Doc
{
    // [ydk:req:extraction/unicode-fidelity] for legacy .doc flat-text extraction
    // (T-a8b6b4ec). multilingual.doc / empty-body.doc under Doc/Fixtures were authored with real
    // Microsoft Word ("Word 97-2003 Document" save format) rather than synthesized by hand, so
    // they exercise the real binary WordDocument/piece-table encoding b2xtranslator has to parse.
    public class DocTextExtractorTests
    {
        private const string MultilingualFixturePath = "ExtractorOle/ExtractorOLE.Tests/Doc/Fixtures/multilingual.doc";
        private const string EmptyBodyFixturePath = "ExtractorOle/ExtractorOLE.Tests/Doc/Fixtures/empty-body.doc";
        private const string KnownDocFixturePath = "samples/curated/doc/us-dept-of-education-teacher-quality-grantees-1999-2000.doc";

        // Decomposed 'e' + COMBINING ACUTE ACCENT followed by precomposed 'é': if any stage
        // (b2xtranslator conversion, OpenXml read) applied Unicode normalization, the two would
        // collapse to the same form and this exact sequence would no longer match.
        private const string CombiningLine = "café café";

        private static string FindRepoRootFile(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
        }

        // The multilingual.doc body, one paragraph per entry, in document order - built from the
        // same MultilingualFixtures constants the fixture was authored from.
        private static readonly string[] ExpectedParagraphs =
        {
            MultilingualFixtures.Hebrew,
            MultilingualFixtures.Arabic,
            MultilingualFixtures.Persian,
            MultilingualFixtures.Russian,
            MultilingualFixtures.Latin,
            MultilingualFixtures.Cjk,
            string.Join(" ", MultilingualFixtures.EmojiSimple, MultilingualFixtures.EmojiFamilyZwj,
                MultilingualFixtures.EmojiSkinTone, MultilingualFixtures.EmojiFlagZwj),
            CombiningLine,
        };

        [Fact]
        public void ExtractText_MultilingualDoc_MatchesExpectedStringExactly()
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile(MultilingualFixturePath));

            var text = new DocTextExtractor().ExtractText(bytes);

            Assert.Equal(string.Join(Environment.NewLine, ExpectedParagraphs), text);
        }

        [Theory]
        [InlineData(0)] // Hebrew
        [InlineData(1)] // Arabic
        [InlineData(2)] // Persian
        [InlineData(3)] // Russian
        [InlineData(4)] // Latin
        [InlineData(5)] // CJK
        [InlineData(6)] // emoji: simple, family ZWJ, skin-tone modifier, flag ZWJ
        [InlineData(7)] // combining-character sequence, no normalization
        public void ExtractText_MultilingualDoc_EachScriptFamilyRoundTripsExactly(int paragraphIndex)
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile(MultilingualFixturePath));

            var text = new DocTextExtractor().ExtractText(bytes);

            Assert.Contains(ExpectedParagraphs[paragraphIndex], text, StringComparison.Ordinal);
        }

        [Fact]
        public void ExtractText_DoesNotApplyUnicodeNormalization()
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile(MultilingualFixturePath));

            var text = new DocTextExtractor().ExtractText(bytes);

            Assert.Contains("café", text, StringComparison.Ordinal);
            Assert.Contains("café", text, StringComparison.Ordinal);
            Assert.DoesNotContain("café café", text, StringComparison.Ordinal);
            Assert.DoesNotContain("café café", text, StringComparison.Ordinal);
        }

        [Fact]
        public void ExtractText_DocWithNoBodyText_ReturnsEmptyStringNotNull()
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile(EmptyBodyFixturePath));

            var text = new DocTextExtractor().ExtractText(bytes);

            Assert.NotNull(text);
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void ExtractText_RealWorldCuratedDoc_ReturnsNonEmptyBodyText()
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile(KnownDocFixturePath));

            var text = new DocTextExtractor().ExtractText(bytes);

            Assert.False(string.IsNullOrWhiteSpace(text));
        }

        [Fact]
        public void ExtractText_CorruptBytes_ReturnsEmptyStringNotThrow()
        {
            var text = new DocTextExtractor().ExtractText(new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF });

            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void ExtractText_EmptyBytes_ReturnsEmptyStringNotThrow()
        {
            var text = new DocTextExtractor().ExtractText(Array.Empty<byte>());

            Assert.Equal(string.Empty, text);
        }
    }
}
