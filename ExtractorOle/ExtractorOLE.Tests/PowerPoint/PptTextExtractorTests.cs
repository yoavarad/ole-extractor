using ExtractorOLE.DTOs;
using ExtractorOLE.Handlers;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using Xunit;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Covers T-40fc15b4 (legacy .ppt flat-text extraction with Unicode fidelity,
    /// [ydk:req:extraction/unicode-fidelity]). Per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md)
    /// the .ppt is converted in-process to .pptx via b2xtranslator and read through the existing
    /// OpenXml path, so these tests drive real .ppt binaries end to end.
    ///
    /// Fixtures are built by PptTextFixtureBuilder: a genuine PowerPoint-authored .ppt
    /// (PowerPoint/Fixtures/with_textbox.ppt, four text boxes) with each text box's text swapped for
    /// a same-length multilingual string, so the expected ExtractedText is known exactly. The source
    /// file's original texts are:
    ///   "Hello, World!!!" (15), "I am just a poor boy" (20), "This is Times New Roman" (23), "Plain Text " (11)
    /// (lengths in UTF-16 code units). Extraction emits one line per text run in slide order.
    /// </summary>
    public class PptTextExtractorTests
    {
        private const string OriginalText1 = "Hello, World!!!";
        private const string OriginalText2 = "I am just a poor boy";
        private const string OriginalText3 = "This is Times New Roman";
        private const string OriginalText4 = "Plain Text ";

        private static byte[] LoadBaseFixture() =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "PowerPoint", "Fixtures", "with_textbox.ppt"));

        private static byte[] BuildFixture(string text1, string text2, string text3, string text4) =>
            PptTextFixtureBuilder.ReplaceText(LoadBaseFixture(), new Dictionary<string, string>
            {
                [OriginalText1] = text1,
                [OriginalText2] = text2,
                [OriginalText3] = text3,
                [OriginalText4] = text4,
            });

        private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

        // 15 UTF-16 code units: Hebrew + Arabic (RTL).
        private const string HebrewArabic = "שלום עולם مرحبا";

        // 20 code units: Persian + Russian (Cyrillic).
        private const string PersianRussian = "سلام دنیا Привет мир";

        // 23 code units: Chinese + Japanese + skin-tone thumbs-up + regional-indicator flag (both
        // 4 code units, surrogate pairs) + a decomposed "é" (e + U+0301) next to a precomposed "é",
        // so any NFC/NFD normalization would alter the string.
        private const string CjkEmojiCombining = "你好 こんにちは 👍🏽🇮🇱 e\u0301éñü";

        // 11 code units: ZWJ family emoji (man, ZWJ, woman, ZWJ, girl = 8 code units) + grinning face.
        private const string ZwjEmoji = "👨\u200d👩\u200d👧 😀";

        [Fact]
        public void ExtractText_MultilingualPptFixture_EqualsExpectedUnicodeStringExactly()
        {
            var bytes = BuildFixture(HebrewArabic, PersianRussian, CjkEmojiCombining, ZwjEmoji);

            var text = new PptTextExtractor().ExtractText(bytes);

            Assert.Equal(Lines(HebrewArabic, PersianRussian, CjkEmojiCombining, ZwjEmoji), text);
        }

        [Fact]
        public void ExtractText_MultilingualPptFixture_AppliesNoUnicodeNormalization()
        {
            var bytes = BuildFixture(HebrewArabic, PersianRussian, CjkEmojiCombining, ZwjEmoji);

            var text = new PptTextExtractor().ExtractText(bytes);

            // Decomposed and precomposed "é" must both survive as authored; the ZWJ sequence and
            // the surrogate-pair emoji must not be split, stripped or recomposed.
            Assert.Contains("e\u0301é", text, StringComparison.Ordinal);
            Assert.Contains("👨\u200d👩\u200d👧", text, StringComparison.Ordinal);
            Assert.Contains("👍🏽🇮🇱", text, StringComparison.Ordinal);
        }

        [Fact]
        public void ExtractText_Latin1AccentedPptFixtureStoredAsEightBitTextBytesAtoms_EqualsExpectedString()
        {
            // All chars are within U+0000..U+00FF, so the fixture stores these as TextBytesAtom
            // (ISO-8859-1), the legacy 8-bit path -- the one most prone to mojibake.
            const string t1 = "Zoë Ünïcödé ça!";
            const string t2 = "Ångström señor Ñandú";
            const string t3 = "Crème brûlée à la façon";
            const string t4 = "¡Olé! ½ £5©";
            var bytes = BuildFixture(t1, t2, t3, t4);

            var text = new PptTextExtractor().ExtractText(bytes);

            Assert.Equal(Lines(t1, t2, t3, t4), text);
        }

        [Fact]
        public void ExtractText_UnmodifiedRealPptFixture_ReturnsOriginalSlideText()
        {
            var text = new PptTextExtractor().ExtractText(LoadBaseFixture());

            // Trailing space of the last text box is trimmed only because it is the end of the result.
            Assert.Equal(Lines(OriginalText1, OriginalText2, OriginalText3, OriginalText4.TrimEnd()), text);
        }

        [Fact]
        public void ExtractText_PptWithOnlyWhitespaceText_ReturnsEmptyStringNotNull()
        {
            var bytes = BuildFixture(
                new string(' ', OriginalText1.Length),
                new string(' ', OriginalText2.Length),
                new string(' ', OriginalText3.Length),
                new string(' ', OriginalText4.Length));

            var text = new PptTextExtractor().ExtractText(bytes);

            Assert.NotNull(text);
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void ExtractText_CompoundFileWithoutPresentationStreams_ReturnsEmptyStringNotNull()
        {
            // Valid CFB (HPSF property streams only) but no "PowerPoint Document"/"Current User" -- the
            // shape PptOpenStrategyTests' metadata fixtures use; body conversion cannot succeed.
            var workbook = new HSSFWorkbook();
            workbook.CreateInformationProperties();
            using var ms = new MemoryStream();
            workbook.Write(ms);

            var text = new PptTextExtractor().ExtractText(ms.ToArray());

            Assert.NotNull(text);
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void ExtractText_CorruptBytes_ReturnsEmptyStringNotNull()
        {
            var text = new PptTextExtractor().ExtractText(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.NotNull(text);
            Assert.Equal(string.Empty, text);
        }

        [Fact]
        public void ServiceRegistration_ResolvesPptTextExtractorViaDiRegistryForPowerPointLegacy()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();

            var dict = provider.GetRequiredService<IDictionary<OfficeMimeTypeEnum, ITextExtractor>>();

            Assert.True(dict.ContainsKey(OfficeMimeTypeEnum.PowerPointLegacy));
            Assert.IsType<PptTextExtractor>(dict[OfficeMimeTypeEnum.PowerPointLegacy]);
        }

        [Fact]
        public void MainExtractor_MultilingualPptFixture_PopulatesExtractedTextThroughRegistry()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var extractor = services.BuildServiceProvider().GetRequiredService<MainExtractor>();
            var bytes = BuildFixture(HebrewArabic, PersianRussian, CjkEmojiCombining, ZwjEmoji);

            var result = extractor.Extract(bytes);

            Assert.Equal("application/vnd.ms-powerpoint", result.MimeType);
            Assert.Equal(Lines(HebrewArabic, PersianRussian, CjkEmojiCombining, ZwjEmoji), result.ExtractedText);
        }

        [Fact]
        public void PptOpenStrategy_RealPptFixture_ConvertsBodyAndReportsSlideCount()
        {
            // Regression for the shared .ppt -> .pptx conversion: real .ppt bodies previously failed
            // to convert (b2xtranslator's default layout/theme resources were not embedded, and the
            // output package was closed twice), leaving SlideCount at its 0 default.
            var result = new PptOpenStrategy(new ExtractionHelper()).Open(LoadBaseFixture());

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result!.FormatMetadata);
            Assert.Equal(1, formatMetadata.SlideCount);
        }
    }
}
