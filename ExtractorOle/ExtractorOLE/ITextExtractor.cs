using System;

namespace ExtractorOLE
{
    public interface ITextExtractor
    {
        string ExtractText(byte[] fileBytes);
    }
}
