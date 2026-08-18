using ExtractorOLE.DTOs;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public class ZipFallbackOpenStrategy : IOpenStrategy
    {
        private readonly WordOpenStrategy _word;
        private readonly ExcelOpenStrategy _excel;
        private readonly PowerPointOpenStrategy _ppt;

        public ZipFallbackOpenStrategy(WordOpenStrategy word, ExcelOpenStrategy excel, PowerPointOpenStrategy ppt)
        {
            _word = word;
            _excel = excel;
            _ppt = ppt;
        }

        public DocumentExtractionResult? Open(byte[] fileBytes)
        {
            var r = _word.Open(fileBytes);
            if (r != null) return r;
            r = _excel.Open(fileBytes);
            if (r != null) return r;
            return _ppt.Open(fileBytes);
        }
    }
}
