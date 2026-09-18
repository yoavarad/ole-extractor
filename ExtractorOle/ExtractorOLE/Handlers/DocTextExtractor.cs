using b2xtranslator.DocFileFormat;
using b2xtranslator.StructuredStorage.Reader;
using b2xtranslator.WordprocessingMLMapping;
using System;
using System.IO;
using B2xWordprocessingDocument = b2xtranslator.OpenXmlLib.WordprocessingML.WordprocessingDocument;

namespace ExtractorOLE.Handlers
{
    // Legacy .doc (OLE-CFB, Word 97-2003) text extractor.
    //
    // Per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md), NPOI's HWPFDocument (the
    // alternative body-text parser) lives only in NPOI's scratchpad tree, which this project does
    // not self-compile. Body text is instead sourced by converting the .doc to a real .docx
    // in-process via b2xtranslator's Doc module, then reading that .docx through the project's
    // existing OpenXml flat-text path (DocxTextExtractor) - no separate doc-specific body parser.
    //
    // Unicode fidelity ([ydk:req:extraction/unicode-fidelity]): neither hop applies any Unicode
    // normalization. b2xtranslator decodes the piece table's UTF-16 / code-page runs into .NET
    // strings verbatim and writes them as-is into document.xml; DocxTextExtractor then joins the
    // decoded paragraph text unchanged. DocTextExtractorTests pins this against a Word-authored
    // multilingual fixture (Hebrew/Arabic/Persian/Russian/Latin/CJK, emoji incl. ZWJ/skin-tone
    // sequences, and a decomposed combining-character sequence).
    //
    // An unreadable or unconvertible .doc yields string.Empty rather than throwing: b2xtranslator
    // is unmaintained since 2018 (ADR-004), so a record shape it doesn't handle must not take down
    // the extraction. Never returns null.
    internal class DocTextExtractor : ITextExtractor
    {
        private readonly DocxTextExtractor _docxTextExtractor = new();

        public string ExtractText(byte[] fileBytes)
        {
            try
            {
                return _docxTextExtractor.ExtractText(ConvertToDocx(fileBytes));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Doc body conversion failed, returning empty text: {ex.Message}");
                return string.Empty;
            }
        }

        // b2xtranslator's WordprocessingDocument.Create only writes to a file path (no stream
        // overload), so the conversion goes through a temp file that is always cleaned up.
        private static byte[] ConvertToDocx(byte[] docBytes)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"ole-extractor-doc2x-{Guid.NewGuid():N}.docx");
            string? writtenPath = null;
            try
            {
                using (var reader = new StructuredStorageReader(new MemoryStream(docBytes)))
                {
                    var doc = new WordDocument(reader);
                    var outType = Converter.DetectOutputType(doc);

                    // A macro-enabled/template document is written under its own extension
                    // (.docm/.dotx/.dotm) - track the real path so cleanup and read-back match.
                    writtenPath = Converter.GetConformFilename(tempPath, outType);
                    // Converter.Convert disposes (closes) the output package itself, so no `using`
                    // here - a second Close would append a duplicate copy of every zip entry and
                    // leave a package the OpenXml SDK rejects as malformed.
                    var docx = B2xWordprocessingDocument.Create(writtenPath, outType);
                    Converter.Convert(doc, docx);
                }

                return File.ReadAllBytes(writtenPath);
            }
            finally
            {
                foreach (var path in new[] { tempPath, writtenPath })
                {
                    if (path != null && File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
        }
    }
}
