namespace SampleGenerator.Abstractions
{
    /// <summary>
    /// The six formats the generator harness targets (doc/xls/ppt legacy OLE,
    /// docx/xlsx/pptx OOXML). Mirrors ExtractorOLE's supported-format floor
    /// (docs/project-rules.md) without depending on that project's enum.
    /// </summary>
    public enum SampleFormat
    {
        Doc,
        Xls,
        Ppt,
        Docx,
        Xlsx,
        Pptx
    }
}
