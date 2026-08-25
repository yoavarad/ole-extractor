using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Excel
{
    public class XlsOpenStrategyTests
    {
        private static byte[] BuildXlsFixtureBytes(string? title, string? author, DateTime? created, DateTime? lastSaved, string? lastAuthor)
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateSheet("Sheet1");
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
        public void Open_KnownXlsFixture_PopulatesFileMetadataFromSummaryInformation()
        {
            var created = new DateTime(2019, 3, 14, 9, 0, 0, DateTimeKind.Utc);
            var lastSaved = new DateTime(2020, 6, 1, 17, 30, 0, DateTimeKind.Utc);
            var bytes = BuildXlsFixtureBytes("Quarterly Report", "Jane Doe", created, lastSaved, null);

            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Equal("Quarterly Report", result!.Metadata.Title);
            Assert.Equal("Jane Doe", result.Metadata.Creator);
            // NPOI's Filetime round-trips DateTime via DateTime.ToFileTime()/FromFileTime(),
            // which returns DateTimeKind.Local; normalize to UTC to compare the same instant
            // regardless of the executing machine's local timezone offset.
            Assert.Equal(created, result.Metadata.Created!.Value.ToUniversalTime());
            Assert.Equal(lastSaved, result.Metadata.Modified!.Value.ToUniversalTime());
            Assert.Null(result.Metadata.LastModifiedBy);
            Assert.Equal("application/vnd.ms-excel", result.MimeType);
        }

        [Fact]
        public void Open_CorruptBytes_ReturnsNull()
        {
            var result = new XlsOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.Null(result);
        }

        [Fact]
        public void ServiceRegistration_ResolvesXlsOpenStrategyViaDiRegistryForExcelLegacy()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();

            var dict = provider.GetRequiredService<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>();

            Assert.True(dict.ContainsKey(OfficeMimeTypeEnum.ExcelLegacy));
            Assert.IsType<XlsOpenStrategy>(dict[OfficeMimeTypeEnum.ExcelLegacy]);
        }
    }
}
