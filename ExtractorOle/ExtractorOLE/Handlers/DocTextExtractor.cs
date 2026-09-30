using b2xtranslator.DocFileFormat;
using b2xtranslator.OpenXmlLib;
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

        // Runs entirely in memory (#169): b2xtranslator's public Create() only targets a file path,
        // so InMemoryWordprocessingDocument overrides the package's Close() to serialize into a
        // MemoryStream instead.
        private static byte[] ConvertToDocx(byte[] docBytes)
        {
            var output = new MemoryStream();
            using (var reader = new StructuredStorageReader(new MemoryStream(docBytes)))
            {
                var doc = new WordDocument(reader);
                var outType = Converter.DetectOutputType(doc);

                // Converter.Convert disposes (closes) the output package itself, so no `using`
                // here - a second Close would serialize the package a second time.
                var docx = new InMemoryWordprocessingDocument(output, outType);
                Converter.Convert(doc, docx);
            }

            // The zip archive disposes `output` on close; ToArray() still works on a disposed MemoryStream.
            return output.ToArray();
        }

        private sealed class InMemoryWordprocessingDocument : B2xWordprocessingDocument
        {
            private readonly Stream _output;

            public InMemoryWordprocessingDocument(Stream output, DocumentType type)
                : base(string.Empty, type)
            {
                _output = output;
            }

            public override void Close()
            {
                var writer = new OpenXmlWriter();
                writer.Open(_output);
                WritePackage(writer);
                writer.Close();
            }
        }
    }
}
