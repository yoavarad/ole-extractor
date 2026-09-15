using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Covers T-90924b6d / [ydk:req:extraction/format-extensibility] and
    /// [ydk:req:extraction/dependency-injection]: format dispatch must be resolved
    /// via a DI-registered registry keyed by format, and MainExtractor's dispatch
    /// code must never need to change to pick up a newly-registered format
    /// component - it only ever talks to IOpenStrategy/ITextExtractor via the
    /// registry.
    /// </summary>
    public class FormatDispatchRegistryTests
    {
        private sealed class FakeOpenStrategy : IOpenStrategy
        {
            public DocumentExtractionResult? Open(byte[] fileBytes) =>
                new DocumentExtractionResult { MimeType = "application/x-fake-format" };
        }

        private sealed class FakeTextExtractor : ITextExtractor
        {
            public string ExtractText(byte[] fileBytes) => "FAKE_TEXT";
        }

        // Stands in for the real ExtractionHelper so this test never has to build a
        // real docx/xlsx/xls fixture - it only needs to route a fake format value
        // through the real, unmodified MainExtractor dispatch code.
        private sealed class FakeExtractionHelper : IExtractionHelper
        {
            public string MimeFor(OfficeMimeTypeEnum type) => "application/x-fake-format";

            public OfficeMimeTypeEnum ParseOfficeMimeType(string? mime) => OfficeMimeTypeEnum.Word;

            public void ExtractMetadataAndEmbedded(OpenXmlPackage package, OpenXmlPart? rootPart, DocumentExtractionResult result) =>
                throw new NotSupportedException("Not exercised by this test - FakeOpenStrategy handles Open() directly.");

            public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart rootPart) =>
                throw new NotSupportedException("Not exercised by this test.");

            public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, HSSFWorkbook workbook) =>
                throw new NotSupportedException("Not exercised by this test.");

            public OfficeMimeTypeEnum DetectMimeTypeFromBytes(byte[] fileBytes) => OfficeMimeTypeEnum.Word;

            public MimeDetectionResult DetectMimeType(MimeDetectionRequest request) =>
                throw new NotSupportedException("Not exercised by this test.");

            public string GetExtensionFromContentType(string contentType) => ".fake";

            public string DetectMimeFromMagicBytes(byte[] bytes) => "application/x-fake-format";
        }

        [Fact]
        public void DiContainer_ResolvesFakeFormatComponents_ViaRegistry()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IOpenStrategy, FakeOpenStrategy>();
            services.AddSingleton<ITextExtractor, FakeTextExtractor>();
            services.AddSingleton<IFormatDispatchRegistry>(sp => new FormatDispatchRegistry(
                new Dictionary<OfficeMimeTypeEnum, IOpenStrategy> { [OfficeMimeTypeEnum.Word] = sp.GetRequiredService<IOpenStrategy>() },
                new Dictionary<OfficeMimeTypeEnum, ITextExtractor> { [OfficeMimeTypeEnum.Word] = sp.GetRequiredService<ITextExtractor>() }));

            using var provider = services.BuildServiceProvider();
            var registry = provider.GetRequiredService<IFormatDispatchRegistry>();

            Assert.IsType<FakeOpenStrategy>(registry.GetOpenStrategy(OfficeMimeTypeEnum.Word));
            Assert.IsType<FakeTextExtractor>(registry.GetTextExtractor(OfficeMimeTypeEnum.Word));
        }

        [Fact]
        public void UnregisteredFormat_ReturnsNullFromBothLookups()
        {
            var registry = new FormatDispatchRegistry(
                new Dictionary<OfficeMimeTypeEnum, IOpenStrategy>(),
                new Dictionary<OfficeMimeTypeEnum, ITextExtractor>());

            Assert.Null(registry.GetOpenStrategy(OfficeMimeTypeEnum.Excel));
            Assert.Null(registry.GetTextExtractor(OfficeMimeTypeEnum.Excel));
        }

        [Fact]
        public void MainExtractor_DispatchesToDiRegisteredFakeFormatComponents_WithoutDispatchCodeChanges()
        {
            // This test registers a brand-new fake format component set in the DI
            // container and confirms MainExtractor's real, unmodified dispatch code
            // resolves and uses it purely through the registry - proving dispatch is
            // registry-driven, not a hardcoded chain baked into MainExtractor.
            var services = new ServiceCollection();
            services.AddSingleton<IExtractionHelper, FakeExtractionHelper>();
            services.AddSingleton<IOpenStrategy, FakeOpenStrategy>();
            services.AddSingleton<ITextExtractor, FakeTextExtractor>();
            services.AddSingleton<IFormatDispatchRegistry>(sp => new FormatDispatchRegistry(
                new Dictionary<OfficeMimeTypeEnum, IOpenStrategy> { [OfficeMimeTypeEnum.Word] = sp.GetRequiredService<IOpenStrategy>() },
                new Dictionary<OfficeMimeTypeEnum, ITextExtractor> { [OfficeMimeTypeEnum.Word] = sp.GetRequiredService<ITextExtractor>() }));
            services.AddSingleton<MainExtractor>();

            using var provider = services.BuildServiceProvider();
            var mainExtractor = provider.GetRequiredService<MainExtractor>();

            var result = mainExtractor.Extract(new byte[] { 0x01, 0x02, 0x03 });

            Assert.Equal("FAKE_TEXT", result.ExtractedText);
            Assert.Equal("application/x-fake-format", result.MimeType);
        }
    }
}
