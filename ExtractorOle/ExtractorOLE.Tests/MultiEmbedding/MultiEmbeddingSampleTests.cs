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
    /// Round-trip tests for the "multi-embedding" synthetic sample scenario
    /// (docs/specs/dataset-curation.md Corpus Composition rule #1): generate
    /// via SampleGenerator, then confirm ExtractorOLE actually detects at
    /// least 3 first-layer embeddings of at least 2 kinds for docx/xlsx/pptx.
    /// </summary>
    public class MultiEmbeddingSampleTests
    {
        [Fact]
        public void Docx_MultiEmbeddingSample_DetectsAtLeastThreeEmbeddingsAcrossTwoKinds()
        {
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new DocxSampleGenerator().Generate(spec);

            var strategy = new WordOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(4, result!.EmbeddedFiles.Count);

            var extensions = result.EmbeddedFiles.Select(f => Path.GetExtension(f.FileName)).Distinct().ToList();
            Assert.True(extensions.Count >= 2, $"Expected >=2 distinct kinds, found: {string.Join(",", extensions)}");
            Assert.Contains(".png", extensions);
            Assert.Contains(".bin", extensions);
            Assert.All(result.EmbeddedFiles, f => Assert.True(f.SizeInBytes > 0));
        }

        [Fact]
        public void Pptx_MultiEmbeddingSample_DetectsAtLeastThreeEmbeddingsAcrossTwoKinds()
        {
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new PptxSampleGenerator().Generate(spec);

            var strategy = new PowerPointOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(4, result!.EmbeddedFiles.Count);

            var extensions = result.EmbeddedFiles.Select(f => Path.GetExtension(f.FileName)).Distinct().ToList();
            Assert.True(extensions.Count >= 2, $"Expected >=2 distinct kinds, found: {string.Join(",", extensions)}");
            Assert.Contains(".png", extensions);
            Assert.Contains(".bin", extensions);

            // All 4 embeddings live on the SlidePart (PresentationPart cannot hold
            // EmbeddedObjectPart/EmbeddedPackagePart/ImagePart directly, per
            // ExtractionHelper.ExtractFirstLayerEmbedded's PowerPoint scan).
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("embedded_object_")));
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("slide_image_")));
        }

        [Fact]
        public void Xlsx_MultiEmbeddingSample_DetectsAtLeastThreeEmbeddingsAcrossTwoKinds()
        {
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new XlsxSampleGenerator().Generate(spec);

            var strategy = new ExcelOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(generated.Content);

            Assert.NotNull(result);
            Assert.Equal(4, result!.EmbeddedFiles.Count);

            var extensions = result.EmbeddedFiles.Select(f => Path.GetExtension(f.FileName)).Distinct().ToList();
            Assert.True(extensions.Count >= 2, $"Expected >=2 distinct kinds, found: {string.Join(",", extensions)}");
            Assert.Contains(".png", extensions);
            Assert.Contains(".bin", extensions);

            // All 4 embeddings live on the WorksheetPart (WorkbookPart cannot hold
            // EmbeddedObjectPart/EmbeddedPackagePart/ImagePart directly, per
            // ExtractionHelper.ExtractFirstLayerEmbedded's generic depth-2 scan).
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("embedded_object_")));
            Assert.Equal(2, result.EmbeddedFiles.Count(f => f.FileName.StartsWith("slide_image_")));
        }

        [Fact]
        public void Xlsx_MultiEmbeddingSample_OneCorruptEmbedding_IsOmittedWithoutFailingExtraction()
        {
            var spec = MultiEmbeddingSampleSpecs.Build();
            var generated = new XlsxSampleGenerator().Generate(spec);

            string corruptedPartUri;
            using (var document = SpreadsheetDocument.Open(new MemoryStream(generated.Content), false))
            {
                var worksheetPart = document.WorkbookPart!.Parts
                    .Select(p => p.OpenXmlPart)
                    .OfType<WorksheetPart>()
                    .First();

                var targetPart = worksheetPart.Parts
                    .Select(p => p.OpenXmlPart)
                    .OfType<EmbeddedObjectPart>()
                    .First();

                corruptedPartUri = targetPart.Uri.OriginalString;
            }

            var corruptedBytes = CorruptZipEntry(generated.Content, corruptedPartUri);

            var strategy = new ExcelOpenStrategy(new ExtractionHelper());
            var result = strategy.Open(corruptedBytes);

            Assert.NotNull(result);
            Assert.Equal(3, result!.EmbeddedFiles.Count);
            Assert.DoesNotContain(result.EmbeddedFiles, f => f.PackagePath == corruptedPartUri);
        }

        /// <summary>
        /// Clones <paramref name="zipBytes"/> and flips a handful of bytes inside the
        /// compressed data of the ZIP local-file-header entry whose filename matches
        /// <paramref name="partUri"/> (minus its leading '/'), leaving every other byte -
        /// including the central directory - untouched, so the resulting archive still
        /// opens fine except for reading that one entry's content.
        /// </summary>
        private static byte[] CorruptZipEntry(byte[] zipBytes, string partUri)
        {
            var entryName = partUri.TrimStart('/');
            // OOXML packages always write ZIP entry names as UTF-8 (no legacy codepages involved).
            var entryNameBytes = System.Text.Encoding.UTF8.GetBytes(entryName);
            var corrupted = (byte[])zipBytes.Clone();

            for (int i = 0; i <= corrupted.Length - 30; i++)
            {
                // Local file header signature: 50 4B 03 04
                if (corrupted[i] != 0x50 || corrupted[i + 1] != 0x4B || corrupted[i + 2] != 0x03 || corrupted[i + 3] != 0x04)
                {
                    continue;
                }

                int compressedSize = BitConverter.ToInt32(corrupted, i + 18);
                int filenameLength = BitConverter.ToUInt16(corrupted, i + 26);
                int extraFieldLength = BitConverter.ToUInt16(corrupted, i + 28);

                if (filenameLength != entryNameBytes.Length)
                {
                    continue;
                }

                int filenameStart = i + 30;
                if (filenameStart + filenameLength > corrupted.Length)
                {
                    continue;
                }

                bool nameMatches = true;
                for (int j = 0; j < filenameLength; j++)
                {
                    if (corrupted[filenameStart + j] != entryNameBytes[j])
                    {
                        nameMatches = false;
                        break;
                    }
                }

                if (!nameMatches)
                {
                    continue;
                }

                int dataStart = filenameStart + filenameLength + extraFieldLength;
                int bytesToFlip = Math.Min(16, compressedSize);
                if (bytesToFlip <= 0 || dataStart + bytesToFlip > corrupted.Length)
                {
                    throw new InvalidOperationException(
                        $"Found ZIP entry for '{entryName}' but its compressed data region is unusable (compressedSize={compressedSize}).");
                }

                for (int k = 0; k < bytesToFlip; k++)
                {
                    corrupted[dataStart + k] = (byte)~corrupted[dataStart + k];
                }

                return corrupted;
            }

            throw new InvalidOperationException($"Could not find ZIP local-file-header entry for '{entryName}'.");
        }
    }
}
