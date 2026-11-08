using ExtractorOLE.DTOs;
using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

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
            // Pick the flavor from [Content_Types].xml so a pptx pays one package open, not three.
            var flavor = DetectFlavor(new MemoryStream(fileBytes, false));
            if (flavor != null)
            {
                var r = Strategy(flavor.Value).Open(fileBytes);
                if (r != null) return r;
            }

            // Last resort: trial opens in the original order, skipping the flavor already tried.
            foreach (var f in new[] { Flavor.Word, Flavor.Excel, Flavor.PowerPoint })
            {
                if (f == flavor) continue;
                var r = Strategy(f).Open(fileBytes);
                if (r != null) return r;
            }
            return null;
        }

        // Stream input (#194, ADR-008): same flavor pick and trial order, each OOXML strategy opening the caller's
        // seekable stream directly. Rewound before every attempt; the caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input)
        {
            input.Position = 0;
            var flavor = DetectFlavor(input);
            if (flavor != null)
            {
                input.Position = 0;
                var r = Strategy(flavor.Value).Open(input);
                if (r != null) return r;
            }
            foreach (var f in new[] { Flavor.Word, Flavor.Excel, Flavor.PowerPoint })
            {
                if (f == flavor) continue;
                input.Position = 0;
                var r = Strategy(f).Open(input);
                if (r != null) return r;
            }
            return null;
        }

        internal enum Flavor { Word, Excel, PowerPoint }

        private IOpenStrategy Strategy(Flavor f) => f switch
        {
            Flavor.Word => _word,
            Flavor.Excel => _excel,
            _ => _ppt,
        };

        // [Content_Types].xml is tiny in any real package; anything past this is not parsed.
        private const int MaxContentTypesBytes = 1024 * 1024;

        internal static Flavor? DetectFlavor(Stream input)
        {
            try
            {
                using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
                var entry = zip.GetEntry("[Content_Types].xml");
                if (entry == null) return null;
                using var stream = entry.Open();
                var capped = new MemoryStream();
                var buf = new byte[8192];
                int n;
                while ((n = stream.Read(buf, 0, buf.Length)) > 0)
                {
                    if (capped.Length + n > MaxContentTypesBytes) return null; // oversized: trial opens instead
                    capped.Write(buf, 0, n);
                }
                capped.Position = 0;
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
                using var reader = XmlReader.Create(capped, settings);
                var doc = XDocument.Load(reader);
                foreach (var e in doc.Descendants())
                {
                    var ct = (string?)e.Attribute("ContentType");
                    if (ct == null || !ct.Contains(".main+xml", StringComparison.OrdinalIgnoreCase)) continue;
                    if (ct.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase)) return Flavor.Word;
                    if (ct.Contains("spreadsheetml", StringComparison.OrdinalIgnoreCase)) return Flavor.Excel;
                    if (ct.Contains("presentationml", StringComparison.OrdinalIgnoreCase)) return Flavor.PowerPoint;
                }
            }
            catch
            {
                // Unreadable content types: fall back to trial opens.
            }
            return null;
        }
    }
}
