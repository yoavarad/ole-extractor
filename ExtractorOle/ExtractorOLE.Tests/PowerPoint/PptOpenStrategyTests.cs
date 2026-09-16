using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.PowerPoint
{
    /// <summary>
    /// Covers T-517f89c5: the legacy .ppt open component. Per ADR-004
    /// (docs/adrs/004-legacy-doc-ppt-parsing.md), NPOI has no working HSLFSlideShow entry
    /// point for .ppt, so FileMetadata is read directly off the OLE
    /// SummaryInformation/DocumentSummaryInformation property streams via NPOI's
    /// format-agnostic HPSF reader, independent of body/slide conversion (handled
    /// separately, best-effort, via b2xtranslator).
    ///
    /// Fixtures reuse XlsOpenStrategyTests' technique: NPOI's HSSFWorkbook is used purely as
    /// a convenient writer for the HPSF property-set streams (SummaryInformation /
    /// DocumentSummaryInformation are format-agnostic CFB streams -- the same streams a real
    /// .ppt carries), not as a stand-in for actual PowerPoint binary content. No real .ppt
    /// sample fixture exists in this repo (matching the equivalent lack of a real .xls
    /// fixture) -- see docs/research/legacy-doc-ppt-parsing.md.
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
        public void Open_PptFixtureWithoutConvertibleBody_StillPopulatesMetadataAndLeavesFormatMetadataNull()
        {
            // FileMetadata comes from HPSF (ADR-004), independent of whether b2xtranslator can
            // convert the body -- this fixture carries valid HPSF streams but no real PPT body
            // records, so body conversion is expected to fail gracefully rather than fail Open().
            var bytes = BuildPptFixtureBytes("No Body", null, null, null, null);

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Equal("No Body", result!.Metadata.Title);
            Assert.Null(result.FormatMetadata);
            Assert.Empty(result.EmbeddedFiles);
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
