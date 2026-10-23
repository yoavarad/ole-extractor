using System;

namespace ExtractorOLE.Abstractions
{
    public interface ITextExtractor
    {
        string ExtractText(byte[] fileBytes);
    }
}
