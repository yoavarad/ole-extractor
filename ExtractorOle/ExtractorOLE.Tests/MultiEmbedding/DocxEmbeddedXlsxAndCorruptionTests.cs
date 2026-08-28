using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using SampleGenerator.Fixtures;
using SampleGenerator.Generators;
using Xunit;

namespace ExtractorOLE.Tests.MultiEmbedding
{
    /// <summary>
    /// Docx-specific proof that the generalized first-layer embedded walk
    /// (ExtractionHelper.ExtractFirstLayerEmbedded) handles a real embedded
    /// xlsx object plus an inline PNG, excludes internal package XML parts,
    /// and survives a single corrupt embedding without failing the whole
    /// extraction call.
    /// </summary>
    public class DocxEmbeddedXlsxAndCorruptionTests
    {
        [Fact]
        public void Docx_EmbeddedXlsxAndInlinePng_AppearInSubfiles_InternalXmlPartsExcluded()
        {
            var spec = DocxEmbeddedXlsxSampleSpecs.Build();
            var originalXlsxBytes = spec.Embeddings.Single(e => e.ContentType.Contains("spreadsheetml")).Content;

            var generated = new DocxSampleGenerator().Generate(spec);

            var strategy = new WordOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(2, result!.EmbeddedFiles.Count);

            var xlsxItem = Assert.Single(result.EmbeddedFiles, f => f.FileName.EndsWith(".xlsx"));
            Assert.True(xlsxItem.SizeInBytes > 0);
            Assert.Equal(originalXlsxBytes, xlsxItem.BinaryData);

            Assert.Single(result.EmbeddedFiles, f => f.FileName.EndsWith(".png"));

            var forbiddenFragments = new[]
            {
                "styles.xml", "theme", "numbering.xml", "settings.xml",
                "docProps", "[Content_Types].xml", "_rels"
            };

            foreach (var item in result.EmbeddedFiles)
            {
                foreach (var fragment in forbiddenFragments)
                {
                    Assert.DoesNotContain(fragment, item.PackagePath);
                }
            }
        }

        [Fact]
        public void Docx_CorruptedEmbeddedPart_IsOmittedAndLogged_WithoutFailingExtraction()
        {
            var spec = DocxEmbeddedXlsxSampleSpecs.BuildWithCorruptTarget();
            var goodBytes = new DocxSampleGenerator().Generate(spec).Content;

            string corruptTargetUri;
            using (var ms = new MemoryStream(goodBytes))
            using (var word = WordprocessingDocument.Open(ms, false))
            {
                var corruptPart = word.MainDocumentPart!.Parts
                    .Select(p => p.OpenXmlPart)
                    .Single(p => p.ContentType == DocxEmbeddedXlsxSampleSpecs.CorruptTargetContentType);
                corruptTargetUri = corruptPart.Uri.ToString();
            }

            var entryName = corruptTargetUri.TrimStart('/');
            var corruptedBytes = ZipEntryCorruptor.CorruptEntryData(goodBytes, entryName);

            var originalError = Console.Error;
            var capturedError = new StringWriter();
            Console.SetError(capturedError);

            ExtractorOLE.DTOs.DocumentExtractionResult? result;
            try
            {
                var strategy = new WordOpenStrategy(new ExtractionHelper());
                result = strategy.Open(corruptedBytes);
            }
            finally
            {
                Console.SetError(originalError);
            }

            Assert.NotNull(result);
            Assert.Equal(2, result!.EmbeddedFiles.Count);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.PackagePath == corruptTargetUri);
            Assert.False(string.IsNullOrEmpty(capturedError.ToString()));
        }
    }
}
