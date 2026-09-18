using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Covers T-517f89c5 (the legacy .ppt open component) and T-2c3440d2 (mapping
    /// PowerPointFormatMetadata for .ppt). Per ADR-004 (docs/adrs/004-legacy-doc-ppt-parsing.md),
    /// NPOI has no working HSLFSlideShow entry point for .ppt, so FileMetadata and
    /// PowerPointFormatMetadata.HasMacros are read directly off the original .ppt's OLE streams
    /// (SummaryInformation/DocumentSummaryInformation property streams and root storage,
    /// respectively) via NPOI's format-agnostic HPSF/POIFS readers, independent of body/slide
    /// conversion (handled separately, best-effort, via b2xtranslator -- which does not carry
    /// macro storage into the converted .pptx).
    ///
    /// Fixtures reuse XlsOpenStrategyTests' technique: NPOI's HSSFWorkbook is used purely as
    /// a convenient writer for the HPSF property-set streams (SummaryInformation /
    /// DocumentSummaryInformation are format-agnostic CFB streams -- the same streams a real
    /// .ppt carries), not as a stand-in for actual PowerPoint binary content. No real .ppt
    /// sample fixture exists in this repo (matching the equivalent lack of a real .xls
    /// fixture) -- see docs/research/legacy-doc-ppt-parsing.md. Because of this, body
    /// conversion always fails for these fixtures, so SlideCount/NotesSlideCount stay at
    /// their not-determinable defaults (0/null) in every test here; HasMacros does not
    /// depend on body conversion and is asserted directly.
    /// </summary>
    public class PptOpenStrategyTests
    {
        private static byte[] BuildPptFixtureBytes(string? title, string? author, DateTime? created, DateTime? lastSaved, string? lastAuthor)
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateInformationProperties();
            if (title != null) workbook.SummaryInformation.Title = title;
            if (author != null) workbook.SummaryInformation.Author = author;
            if (created != null) workbook.SummaryInformation.CreateDateTime = created;
            if (lastSaved != null) workbook.SummaryInformation.LastSaveDateTime = lastSaved;
            if (lastAuthor != null) workbook.SummaryInformation.LastAuthor = lastAuthor;
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        // Builds on BuildPptFixtureBytes by post-processing the raw CFB to add a "Macros"
        // storage entry at the root -- the standard marker legacy PowerPoint/Word binary files
        // use for an embedded VBA project (MS-OVBA), mirroring
        // XlsOpenStrategyTests.BuildMacroEnabledMultiSheetXlsFixtureBytes's equivalent technique
        // for .xls's own "_VBA_PROJECT_CUR" marker.
        private static byte[] BuildMacroEnabledPptFixtureBytes(string? title)
        {
            var bytes = BuildPptFixtureBytes(title, null, null, null, null);

            var raw = new NPOIFSFileSystem(new MemoryStream(bytes));
            raw.Root.CreateDirectory("Macros");
            using var outStream = new MemoryStream();
            raw.WriteFileSystem(outStream);
            raw.Close();
            return outStream.ToArray();
        }

        [Fact]
        public void Open_KnownPptFixture_PopulatesFileMetadataFromSummaryInformationWithOneNullSourceField()
        {
            var created = new DateTime(2018, 11, 2, 8, 15, 0, DateTimeKind.Utc);
            var lastSaved = new DateTime(2021, 4, 9, 13, 45, 0, DateTimeKind.Utc);
            // lastAuthor intentionally omitted -- this is the "null-source" field the format
            // doesn't provide for this fixture.
            var bytes = BuildPptFixtureBytes("Annual Kickoff Deck", "Alex Rivera", created, lastSaved, null);

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Equal("Annual Kickoff Deck", result!.Metadata.Title);
            Assert.Equal("Alex Rivera", result.Metadata.Creator);
            // NPOI's Filetime round-trips DateTime via DateTime.ToFileTime()/FromFileTime(),
            // which returns DateTimeKind.Local; normalize to UTC to compare the same instant
            // regardless of the executing machine's local timezone offset.
            Assert.Equal(created, result.Metadata.Created!.Value.ToUniversalTime());
            Assert.Equal(lastSaved, result.Metadata.Modified!.Value.ToUniversalTime());
            Assert.Null(result.Metadata.LastModifiedBy);
            Assert.Equal("application/vnd.ms-powerpoint", result.MimeType);
        }

        [Fact]
        public void Open_NonMacroPptFixtureWithoutConvertibleBody_PopulatesFormatMetadataWithNoMacrosAndNotDeterminableCounts()
        {
            // FileMetadata and FormatMetadata.HasMacros come from HPSF/POIFS (ADR-004),
            // independent of whether b2xtranslator can convert the body -- this fixture carries
            // valid HPSF streams but no real PPT body records, so body conversion is expected to
            // fail gracefully rather than fail Open(); SlideCount/NotesSlideCount are left at
            // their not-determinable defaults (0/null) as a result.
            var bytes = BuildPptFixtureBytes("No Body", null, null, null, null);

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Equal("No Body", result!.Metadata.Title);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result.FormatMetadata);
            Assert.False(formatMetadata.HasMacros);
            Assert.Equal(0, formatMetadata.SlideCount);
            Assert.Null(formatMetadata.NotesSlideCount);
            Assert.Empty(result.EmbeddedFiles);
        }

        [Fact]
        public void Open_MacroEnabledPptFixture_PopulatesHasMacrosTrue()
        {
            var bytes = BuildMacroEnabledPptFixtureBytes("Macro Deck");

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result!.FormatMetadata);
            Assert.True(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_NonMacroPptFixture_PopulatesHasMacrosFalse()
        {
            var bytes = BuildPptFixtureBytes("Plain Deck", null, null, null, null);

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var formatMetadata = Assert.IsType<PowerPointFormatMetadata>(result!.FormatMetadata);
            Assert.False(formatMetadata.HasMacros);
        }

        [Fact]
        public void Open_CorruptBytes_ReturnsNull()
        {
            var result = new PptOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.Null(result);
        }

        [Fact]
        public void ServiceRegistration_ResolvesPptOpenStrategyViaDiRegistryForPowerPointLegacy()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();

            var dict = provider.GetRequiredService<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>();

            Assert.True(dict.ContainsKey(OfficeMimeTypeEnum.PowerPointLegacy));
            Assert.IsType<PptOpenStrategy>(dict[OfficeMimeTypeEnum.PowerPointLegacy]);
        }
    }
}
