using ExtractorOLE.DTOs;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public interface IOpenStrategy
    {
        DocumentExtractionResult? Open(byte[] fileBytes);
    }
}
