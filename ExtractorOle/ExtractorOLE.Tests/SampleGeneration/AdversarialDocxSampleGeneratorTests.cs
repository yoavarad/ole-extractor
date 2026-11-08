using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.Guardrails;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.SampleGeneration
{
    // Raw-file properties of the committed per-field-error-isolation docx fixtures in
    // samples/adversarial (ydk:req:extraction/per-field-error-isolation): each opens as a docx
    // with no open-time throw, then fails on exactly one leaf.
    public class AdversarialDocxSampleGeneratorTests
    {
        private static byte[] Sample(string fileName)
        {
            var relative = Path.Combine("samples", "adversarial", fileName);
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Could not locate '{relative}' walking up from {AppContext.BaseDirectory}");
        }

        [Fact]
        public void CorruptBodyText_OpensButBodyCannotBeFlattened()
        {
            using var stream = new MemoryStream(Sample("corrupt-body-text.docx"));
            using var word = WordprocessingDocument.Open(stream, false);

            Assert.NotNull(word.MainDocumentPart);
            Assert.NotEmpty(word.MainDocumentPart!.Parts);
            var ex = Assert.ThrowsAny<Exception>(() => word.MainDocumentPart.Document.Body!.InnerText.ToString());
        }

        [Fact]
        public void OversizedEmbeddedObject_DeclaresOverPerEntryCapYetPassesOpenTimeGuard()
        {
            var bytes = Sample("oversized-embedded-object.docx");
            var limits = new MimeDetectionLimits();

            using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
            {
                var oversized = archive.GetEntry(AdversarialDocxSampleGenerator.OversizedEmbeddingEntry)!;
                Assert.True(oversized.Length > limits.MaxDeclaredEntryBytes);
                Assert.True(archive.GetEntry(AdversarialDocxSampleGenerator.GoodEmbeddingEntry)!.Length < limits.MaxDeclaredEntryBytes);
            }

            var guardrails = new ExtractionGuardrails(limits, _ => { });
            guardrails.Enforce(new ExtractionRequest { FileName = "oversized-embedded-object.docx", FileBytes = bytes });

            using var stream = new MemoryStream(bytes);
            using var word = WordprocessingDocument.Open(stream, false);
            Assert.Equal(2, word.MainDocumentPart!.Parts.Count(p => p.OpenXmlPart is EmbeddedObjectPart));
        }

        [Fact]
        public void FaultingEmbeddedEnumeration_OpensButEnumerationFaultsAfterAGoodItem()
        {
            using var stream = new MemoryStream(Sample("faulting-embedded-enumeration.docx"));
            using var word = WordprocessingDocument.Open(stream, false);

            var result = new DocumentExtractionResult();
            var ex = Assert.ThrowsAny<Exception>(() =>
                new EmbeddedFileExtractor(new MimeTypeMapper()).ExtractFirstLayerEmbedded(result, word.MainDocumentPart!));
            Assert.True(result.EmbeddedFiles.Count > 0, ex.ToString());
        }
    }
}
