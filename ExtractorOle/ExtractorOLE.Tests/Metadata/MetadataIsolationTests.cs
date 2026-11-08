using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using NPOI.HPSF;
using NPOI.HPSF.Wellknown;
using NPOI.HSSF.UserModel;
using Xunit;

namespace ExtractorOLE.Tests.Metadata
{
    // #304: each of the twelve source-derived FileMetadata properties is its own isolated leaf.
    public class MetadataIsolationTests
    {
        private static readonly DateTime Created = new(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        private static readonly DateTime Modified = new(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);
        private static readonly DateTime LastPrinted = new(2024, 3, 4, 5, 6, 7, DateTimeKind.Utc);

        // A stored value no string getter can render: stands in for an undecodable record.
        private sealed class UnreadableValue
        {
            public override string ToString() => throw new FormatException("undecodable property value");
        }

        // FileMetadata property -> (HPSF property id, variant type, a value whose source read fails).
        // The four FILETIME properties fail on a real type mismatch (VT_I4 where VT_FILETIME belongs).
        public static TheoryData<string, int, long, object> FailingSources => new()
        {
            { nameof(FileMetadata.Title), PropertyIDMap.PID_TITLE, Variant.VT_LPSTR, new UnreadableValue() },
            { nameof(FileMetadata.Creator), PropertyIDMap.PID_AUTHOR, Variant.VT_LPSTR, new UnreadableValue() },
            { nameof(FileMetadata.Created), PropertyIDMap.PID_CREATE_DTM, Variant.VT_I4, 7 },
            { nameof(FileMetadata.Modified), PropertyIDMap.PID_LASTSAVE_DTM, Variant.VT_I4, 7 },
            { nameof(FileMetadata.LastModifiedBy), PropertyIDMap.PID_LASTAUTHOR, Variant.VT_LPSTR, new UnreadableValue() },
            { nameof(FileMetadata.Subject), PropertyIDMap.PID_SUBJECT, Variant.VT_LPSTR, new UnreadableValue() },
            { nameof(FileMetadata.Comments), PropertyIDMap.PID_COMMENTS, Variant.VT_LPSTR, new UnreadableValue() },
            { nameof(FileMetadata.RevisionNumber), PropertyIDMap.PID_REVNUMBER, Variant.VT_LPSTR, new UnreadableValue() },
            { nameof(FileMetadata.LastPrinted), PropertyIDMap.PID_LASTPRINTED, Variant.VT_I4, 7 },
            { nameof(FileMetadata.EditingDurationMinutes), PropertyIDMap.PID_EDITTIME, Variant.VT_I4, 7 },
            { nameof(FileMetadata.Keywords), PropertyIDMap.PID_KEYWORDS, Variant.VT_LPSTR, new UnreadableValue() },
            { nameof(FileMetadata.ApplicationName), PropertyIDMap.PID_APPNAME, Variant.VT_LPSTR, new UnreadableValue() },
        };

        private static SummaryInformation FullSummary() => new()
        {
            Title = "T",
            Author = "A",
            CreateDateTime = Created,
            LastSaveDateTime = Modified,
            LastAuthor = "L",
            Subject = "S",
            Comments = "C",
            RevNumber = "3",
            LastPrinted = LastPrinted,
            EditTime = 5L * 600_000_000L,
            Keywords = "K",
            ApplicationName = "App",
        };

        private static void AssertOnlyFailed(DocumentExtractionResult result, string failed)
        {
            var error = Assert.Single(result.FieldErrors);
            Assert.Equal($"Metadata.{failed}", error.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
            foreach (var p in typeof(FileMetadata).GetProperties())
            {
                if (p.Name == failed) Assert.Null(p.GetValue(result.Metadata));
                else Assert.NotNull(p.GetValue(result.Metadata));
            }
        }

        [Fact]
        public void Apply_AllReadable_PopulatesTwelveWithNoErrors()
        {
            var result = new DocumentExtractionResult();

            MetadataConversions.Apply(FullSummary(), result);

            Assert.Empty(result.FieldErrors);
            Assert.Equal(12, typeof(FileMetadata).GetProperties().Length);
            Assert.All(typeof(FileMetadata).GetProperties(), p => Assert.NotNull(p.GetValue(result.Metadata)));
        }

        [Theory]
        [MemberData(nameof(FailingSources))]
        public void Apply_OneUnreadableProperty_LeavesItNullAndPopulatesTheOtherEleven(string property, int pid, long variantType, object badValue)
        {
            var summary = FullSummary();
            summary.FirstSection.SetProperty(pid, variantType, badValue);
            var result = new DocumentExtractionResult();

            MetadataConversions.Apply(summary, result);

            AssertOnlyFailed(result, property);
        }

        [Fact]
        public void DocOpen_CorruptMetadataPropertyFixture_LosesOnlyCreated()
        {
            var bytes = File.ReadAllBytes(FindRepoRootFile("samples/adversarial/corrupt-metadata-property.doc"));

            var result = new DocOpenStrategy(new ExtractionHelper()).Open(bytes);

            Assert.NotNull(result);
            var error = Assert.Single(result!.FieldErrors);
            Assert.Equal("Metadata.Created", error.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
            Assert.Null(result.Metadata.Created);
            Assert.Equal("Adversarial title", result.Metadata.Title);
            Assert.Equal("Adversarial author", result.Metadata.Creator);
            Assert.Equal("Adversarial subject", result.Metadata.Subject);
            Assert.Equal("adversarial, corrupt", result.Metadata.Keywords);
            Assert.Equal("Adversarial comments", result.Metadata.Comments);
            Assert.Equal("Adversarial editor", result.Metadata.LastModifiedBy);
            Assert.Equal(3, result.Metadata.RevisionNumber);
            Assert.NotNull(result.Metadata.Modified);
        }

        [Fact]
        public void PptOpen_OneUnreadableProperty_LosesOnlyThatProperty()
        {
            var workbook = new HSSFWorkbook();
            workbook.CreateInformationProperties();
            workbook.SummaryInformation.Title = "Deck";
            workbook.SummaryInformation.Author = "Author";
            workbook.SummaryInformation.LastSaveDateTime = Modified;
            workbook.SummaryInformation.FirstSection.SetProperty(PropertyIDMap.PID_CREATE_DTM, Variant.VT_I4, 7);
            using var ms = new MemoryStream();
            workbook.Write(ms);

            var result = new PptOpenStrategy(new ExtractionHelper()).Open(ms.ToArray());

            Assert.NotNull(result);
            var error = Assert.Single(result!.FieldErrors);
            Assert.Equal("Metadata.Created", error.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
            Assert.Null(result.Metadata.Created);
            Assert.Equal("Deck", result.Metadata.Title);
            Assert.Equal("Author", result.Metadata.Creator);
            Assert.NotNull(result.Metadata.Modified);
        }

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
    }
}
