using B2xPresentationDocument = b2xtranslator.OpenXmlLib.PresentationML.PresentationDocument;
using b2xtranslator.OpenXmlLib;
using b2xtranslator.PptFileFormat;
using b2xtranslator.PresentationMLMapping;
using b2xtranslator.StructuredStorage.Reader;
using System;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Converts legacy .ppt bytes to .pptx bytes in-process via b2xtranslator's Ppt module
    // (ADR-004: docs/adrs/004-legacy-doc-ppt-parsing.md). Shared by PptOpenStrategy (slide-count
    // and embedded-file discovery) and PptTextExtractor (flat body text) so both read the same
    // converted package through the project's existing DocumentFormat.OpenXml path.
    //
    // The conversion runs entirely in memory (#169): b2xtranslator's public Create() only targets a
    // file path, so InMemoryPresentationDocument overrides the package's Close() to serialize into a
    // MemoryStream instead. Throws on any conversion failure -- callers decide whether that is fatal
    // (PptOpenStrategy: best-effort, PptTextExtractor: empty text).
    internal static class PptToPptxConverter
    {
        // Test hook (#168): counts Convert calls on the current thread, so a test can assert one
        // conversion per Extract() without racing tests running in parallel on other threads.
        [ThreadStatic]
        internal static int ConvertCallCount;

        public static byte[] Convert(byte[] pptBytes)
        {
            ConvertCallCount++;
            var output = new MemoryStream();
            using (var reader = new StructuredStorageReader(new MemoryStream(pptBytes)))
            {
                var ppt = new PowerpointDocument(reader);
                var outType = Converter.DetectOutputType(ppt);

                // No `using` here: Converter.Convert disposes (closes) the package itself, and a
                // second Close() would serialize the package a second time.
                var pptx = new InMemoryPresentationDocument(output, outType);
                Converter.Convert(ppt, pptx);
            }

            // The zip archive disposes `output` on close; ToArray() still works on a disposed MemoryStream.
            return output.ToArray();
        }

        private sealed class InMemoryPresentationDocument : B2xPresentationDocument
        {
            private readonly Stream _output;

            public InMemoryPresentationDocument(Stream output, DocumentType type)
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
