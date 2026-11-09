using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.Guardrails;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
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
            return OpenFirst(flavor, f => Strategy(f).Open(fileBytes));
        }

        // Stream input (#194, ADR-008): same flavor pick and trial order, each OOXML strategy opening the caller's
        // seekable stream directly. Rewound before every attempt; the caller owns and disposes it.
        public DocumentExtractionResult? Open(Stream input)
        {
            input.Position = 0;
            var flavor = DetectFlavor(input);
            return OpenFirst(flavor, f =>
            {
                input.Position = 0;
                return Strategy(f).Open(input);
            });
        }

        // Detected flavor first, then trial opens in the original order, skipping the flavor already
        // tried. A parse failure moves on to the next flavor; anything else (including an encrypted-document
        // exception, which Extract maps to password-protected) propagates at once (#287).
        // When every flavor fails to parse, the last parse failure propagates as the cause.
        private static DocumentExtractionResult? OpenFirst(Flavor? detected, Func<Flavor, DocumentExtractionResult?> open)
        {
            var order = new List<Flavor>(3);
            if (detected != null) order.Add(detected.Value);
            foreach (var f in new[] { Flavor.Word, Flavor.Excel, Flavor.PowerPoint })
            {
                if (f != detected) order.Add(f);
            }

            ExceptionDispatchInfo? lastFailure = null;
            foreach (var f in order)
            {
                try
                {
                    var r = open(f);
                    if (r != null) return r;
                }
                catch (Exception ex) when (ex is not NPOI.EncryptedDocumentException && ParseFailure.IsParseFailure(ex))
                {
                    lastFailure = ExceptionDispatchInfo.Capture(ex);
                }
            }

            lastFailure?.Throw();
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
            catch (Exception ex) when (ParseFailure.IsParseFailure(ex))
            {
                // Unreadable content types: fall back to trial opens.
            }
            return null;
        }
    }
}
