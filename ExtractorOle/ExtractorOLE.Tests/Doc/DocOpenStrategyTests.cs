using ExtractorOLE.Handlers;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.DTOs;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Doc
{
    public class DocOpenStrategyTests
    {
        private const string KnownDocFixturePath = "samples/curated/doc/us-dept-of-education-teacher-quality-grantees-1999-2000.doc";

        // Walks up from the test assembly's location looking for the repo's checked-in
        // sample, mirroring the pattern used by
        // ExtractorOle/ExtractorOLE.Tests/Integration/DetectMimeTypeCorpusManifestTests.cs.
        private static string FindRepoRootFile(string relativePath)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            throw new FileNotFoundException($"Could not locate '{relativePath}' by walking up from {AppContext.BaseDirectory}");
        }

        // Builds a minimal, from-scratch legacy .doc-shaped CFB fixture: a WordDocument
        // stream (so this looks like a real .doc to structural detection) plus a
        // SummaryInformation property-set stream carrying only the fields passed in -
        // any field left null here is never written to the stream at all, exercising
        // the same "absent property, not placeholder" case
        // XlsOpenStrategyTests.BuildXlsFixtureBytes does for .xls.
        private static byte[] BuildDocFixtureBytes(string? title, string? author, DateTime? created, DateTime? lastSaved, string? lastAuthor)
        {
            var fs = new NPOIFSFileSystem();
            try
            {
                fs.Root.CreateDocument("WordDocument", new MemoryStream(new byte[] { 0x00 }));

                var summary = PropertySetFactory.NewSummaryInformation();
                if (title != null) summary.Title = title;
                if (author != null) summary.Author = author;
                if (created != null) summary.CreateDateTime = created;
                if (lastSaved != null) summary.LastSaveDateTime = lastSaved;
                if (lastAuthor != null) summary.LastAuthor = lastAuthor;
                summary.Write(fs.Root, SummaryInformation.DEFAULT_STREAM_NAME);

                using var ms = new MemoryStream();
                fs.WriteFileSystem(ms);
                return ms.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }

        [Fact]
        public void Open_KnownDocFixture_PopulatesFileMetadataFromSummaryInformation()
        {
            // Expected values per samples/manifest.json's "doc" curated entry
            // (us-dept-of-education-teacher-quality-grantees-1999-2000.doc), verified
            // directly against the file's OLE SummaryInformation stream.
            var bytes = File.ReadAllBytes(FindRepoRootFile(KnownDocFixturePath));

            var result = new DocOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Equal("Teacher Quality Enhancement Program Grantees - 1999-2000 (Word)", result!.Metadata.Title);
            Assert.Equal("Sharon Easterling", result.Metadata.Creator);
            Assert.Equal("Philip.Schulz", result.Metadata.LastModifiedBy);
            Assert.Equal(
                new DateTime(2003, 2, 26, 17, 44, 0, DateTimeKind.Utc),
                result.Metadata.Created!.Value.ToUniversalTime());
            Assert.Equal(
                new DateTime(2004, 7, 23, 18, 32, 0, DateTimeKind.Utc),
                result.Metadata.Modified!.Value.ToUniversalTime());
            Assert.Equal("application/msword", result.MimeType);
        }

        [Fact]
        public void Open_SyntheticDocFixtureWithMissingLastAuthor_LeavesLastModifiedByNull()
        {
            var created = new DateTime(2019, 3, 14, 9, 0, 0, DateTimeKind.Utc);
            var lastSaved = new DateTime(2020, 6, 1, 17, 30, 0, DateTimeKind.Utc);
            var bytes = BuildDocFixtureBytes("Quarterly Report", "Jane Doe", created, lastSaved, lastAuthor: null);

            var result = new DocOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            Assert.Equal("Quarterly Report", result!.Metadata.Title);
            Assert.Equal("Jane Doe", result.Metadata.Creator);
            Assert.Equal(created, result.Metadata.Created!.Value.ToUniversalTime());
            Assert.Equal(lastSaved, result.Metadata.Modified!.Value.ToUniversalTime());
            // Never a placeholder string ([ydk:entity:extraction/FileMetadata]) - the
            // SummaryInformation stream never had a LastAuthor property written to it.
            Assert.Null(result.Metadata.LastModifiedBy);
            Assert.Equal("application/msword", result.MimeType);
        }

        [Fact]
        public void Open_CorruptBytes_ReturnsNull()
        {
            var result = new DocOpenStrategy(new ExtractionHelper()).Open(new byte[] { 0x00, 0x01, 0x02, 0x03 });

            Assert.Null(result);
        }

        [Fact]
        public void ServiceRegistration_ResolvesDocOpenStrategyViaDiRegistryForWordLegacy()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();

            var openStrategies = provider.GetRequiredService<IDictionary<OfficeMimeTypeEnum, IOpenStrategy>>();
            Assert.True(openStrategies.ContainsKey(OfficeMimeTypeEnum.WordLegacy));
            Assert.IsType<DocOpenStrategy>(openStrategies[OfficeMimeTypeEnum.WordLegacy]);

            var textExtractors = provider.GetRequiredService<IDictionary<OfficeMimeTypeEnum, ITextExtractor>>();
            Assert.True(textExtractors.ContainsKey(OfficeMimeTypeEnum.WordLegacy));
            Assert.IsType<DocTextExtractor>(textExtractors[OfficeMimeTypeEnum.WordLegacy]);
        }
    }
}
