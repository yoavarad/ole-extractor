namespace SampleGenerator.Fixtures
{
    /// <summary>
    /// Shared multilingual/mixed-script text and emoji fixtures reused across
    /// every format's generated body text and metadata fields, per
    /// docs/specs/extraction.md's [ydk:req:extraction/unicode-fidelity] and
    /// docs/specs/dataset-curation.md's synthetic-sample composition rules
    /// (every synthetic sample must carry multilingual/emoji text in both
    /// body text and at least 2 metadata fields).
    /// </summary>
    public static class MultilingualFixtures
    {
        public const string Hebrew = "שלום עולם, זהו טקסט לדוגמה בעברית.";
        public const string Arabic = "مرحبا بالعالم، هذا نص عربي تجريبي.";
        public const string Persian = "سلام دنیا، این یک متن نمونه فارسی است.";
        public const string Russian = "Привет, мир! Это пример текста на русском языке.";
        public const string Latin = "Hello, world! This is a sample sentence in English.";
        public const string Cjk = "你好，世界。こんにちは世界。안녕하세요, 세계.";

        /// <summary>Simple single-grapheme emoji (no joiners/modifiers).</summary>
        public const string EmojiSimple = "😀🎉🚀";

        /// <summary>ZWJ sequence: man + ZWJ + woman + ZWJ + girl + ZWJ + boy -&gt; family emoji.</summary>
        public const string EmojiFamilyZwj = "👨‍👩‍👧‍👦";

        /// <summary>Base emoji + Fitzpatrick skin-tone modifier codepoint.</summary>
        public const string EmojiSkinTone = "👍🏽";

        /// <summary>ZWJ sequence: white flag + ZWJ + rainbow -&gt; rainbow flag.</summary>
        public const string EmojiFlagZwj = "🏳️‍🌈";

        public static readonly IReadOnlyList<string> AllScripts = new[]
        {
            Hebrew, Arabic, Persian, Russian, Latin, Cjk
        };

        public static readonly IReadOnlyList<string> AllEmoji = new[]
        {
            EmojiSimple, EmojiFamilyZwj, EmojiSkinTone, EmojiFlagZwj
        };

        /// <summary>
        /// One multi-line body combining every script and emoji fixture - the
        /// default "controllable body text" generators use unless a caller
        /// supplies its own <see cref="Abstractions.SampleSpec.BodyText"/>.
        /// </summary>
        public static string ComposeMixedBody()
        {
            var lines = new List<string>(AllScripts)
            {
                string.Join(" ", AllEmoji)
            };
            return string.Join(Environment.NewLine, lines);
        }
    }
}
