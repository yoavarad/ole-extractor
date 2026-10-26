namespace ExtractorOLE.DTOs
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Public API type name; renaming is a breaking change.")]
    public enum DetectedFormatEnum
    {
        Doc,
        Xls,
        Ppt,
        Docx,
        Xlsx,
        Pptx,
        Unknown
    }
}
