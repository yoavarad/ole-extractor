using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using Microsoft.Extensions.DependencyInjection;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// #309: a flat-text failure surfaces as one FieldError at ExtractedText (empty text),
    /// while a genuinely empty body stays silent.
    /// </summary>
    public class MainExtractorTextFailureTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

        private static DocumentExtractionResult Extract(byte[] bytes, string fileName)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<MainExtractor>().Extract(
                new ExtractionRequest { FileBytes = bytes, FileName = fileName, DetectedMimeType = DocxMime });
        }

        // Valid package whose document.xml is not well-formed XML: opens fine, body read throws.
        private static byte[] CorruptBodyDocx()
        {
            var good = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = "hello" }).Content;
            using var ms = new MemoryStream();
            ms.Write(good);
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
            {
                zip.GetEntry("word/document.xml")!.Delete();
                using var w = zip.CreateEntry("word/document.xml").Open();
                w.Write(Encoding.UTF8.GetBytes("<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>oops"));
            }
            return ms.ToArray();
        }

        [Fact]
        public void CorruptBodyDocx_EmptyTextAndOneExtractedTextFieldError()
        {
            var result = Extract(CorruptBodyDocx(), "corrupt-body-text.docx");

            Assert.Equal(string.Empty, result.ExtractedText);
            var error = Assert.Single(result.FieldErrors, e => e.FieldPath == "ExtractedText");
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
        }

        [Fact]
        public void ValidEmptyBodyDocx_NoFieldError()
        {
            var bytes = new DocxSampleGenerator().Generate(new SampleSpec { BodyText = string.Empty }).Content;

            var result = Extract(bytes, "empty.docx");

            Assert.Equal(string.Empty, result.ExtractedText);
            Assert.Empty(result.FieldErrors);
        }
    }
}
