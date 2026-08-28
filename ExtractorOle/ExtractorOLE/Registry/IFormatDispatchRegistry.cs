using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.FileTypeStrategy;

namespace ExtractorOLE.Registry
{
    /// <summary>
    /// DI-registered lookup for the open/text-extraction components backing each
    /// supported format ([ydk:req:extraction/format-extensibility]). Orchestration
    /// code (MainExtractor) depends only on this interface plus the
    /// IOpenStrategy/ITextExtractor interfaces it hands back - never on a specific
    /// backing library directly ([ydk:req:extraction/dependency-injection]). Adding
    /// a format is a matter of registering new components under a new key here, not
    /// changing dispatch code.
    /// </summary>
    public interface IFormatDispatchRegistry
    {
        IOpenStrategy? GetOpenStrategy(OfficeMimeTypeEnum format);

        ITextExtractor? GetTextExtractor(OfficeMimeTypeEnum format);
    }
}
