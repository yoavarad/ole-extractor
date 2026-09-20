using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a legacy .xls (BIFF8, OLE-CFB) with NPOI's HSSFWorkbook (ADR-001: the self-compiled
    /// NPOI main tree owns .xls; the same approach the .xls Unicode-fidelity 15/15 baseline used).
    ///
    /// One sheet, one text cell per body line in column A (BIFF stores each string as 8-bit or
    /// UTF-16LE automatically, so multilingual and emoji text round-trips verbatim).
    /// <see cref="SampleSpec.Metadata"/> goes to SummaryInformation. Embeddings: image/png and
    /// image/jpeg content becomes an inline picture; anything else an embedded OLE package object,
    /// which needs an icon picture, so one shared 1x1 icon picture is also registered when any
    /// such object exists (ExtractorOLE reports it as a picture). A
    /// <see cref="SampleSpec.VbaProject"/> becomes a "_VBA_PROJECT_CUR" storage, the location
    /// XlsOpenStrategy reads.
    /// </summary>
    public sealed class XlsSampleGenerator : ISampleGenerator
    {
        // 1x1 transparent PNG, used as the icon of embedded OLE package objects.
        private static readonly byte[] IconPng = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

        public SampleFormat Format => SampleFormat.Xls;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");

            var rowIndex = 0;
            foreach (var line in DocxSampleGenerator.SplitLines(spec.BodyText))
            {
                var row = sheet.CreateRow(rowIndex++);
                if (line.Length > 0) row.CreateCell(0).SetCellValue(line);
            }

            AddEmbeddings(workbook, sheet, spec.Embeddings);
            ApplyMetadata(workbook, spec.Metadata);

            using var written = new MemoryStream();
            workbook.Write(written);
            var bytes = written.ToArray();

            if (spec.VbaProject is { } vbaProject)
            {
                bytes = AddVbaStorage(bytes, vbaProject);
            }

            return new GeneratedSample { FileName = "sample.xls", Content = bytes };
        }

        private static void AddEmbeddings(HSSFWorkbook workbook, ISheet sheet, IList<EmbeddedContentSpec> embeddings)
        {
            if (embeddings.Count == 0) return;

            var patriarch = (HSSFPatriarch)sheet.CreateDrawingPatriarch();
            var helper = workbook.GetCreationHelper();
            int? iconIndex = null;

            for (var i = 0; i < embeddings.Count; i++)
            {
                var embedding = embeddings[i];
                var anchor = (HSSFClientAnchor)helper.CreateClientAnchor();
                anchor.SetAnchor((short)(2 + 3 * i), 1, 0, 0, (short)(4 + 3 * i), 5, 0, 0);

                var pictureType = embedding.ContentType switch
                {
                    "image/png" => (PictureType?)PictureType.PNG,
                    "image/jpeg" => PictureType.JPEG,
                    _ => null,
                };

                if (pictureType is { } type)
                {
                    patriarch.CreatePicture(anchor, workbook.AddPicture(embedding.Content, type));
                }
                else
                {
                    iconIndex ??= workbook.AddPicture(IconPng, PictureType.PNG);
                    var storageId = workbook.AddOlePackage(
                        embedding.Content, embedding.FileName, embedding.FileName, embedding.FileName);
                    patriarch.CreateObjectData(anchor, storageId, iconIndex.Value);
                }
            }
        }

        private static void ApplyMetadata(HSSFWorkbook workbook, IDictionary<string, string> metadata)
        {
            if (metadata.Count == 0) return;

            workbook.CreateInformationProperties();
            var summary = workbook.SummaryInformation;
            foreach (var (key, value) in metadata)
            {
                switch (key)
                {
                    case MetadataFields.Title: summary.Title = value; break;
                    case MetadataFields.Author: summary.Author = value; break;
                    case MetadataFields.Subject: summary.Subject = value; break;
                    case MetadataFields.Keywords: summary.Keywords = value; break;
                    case MetadataFields.Comments: summary.Comments = value; break;
                    case MetadataFields.LastModifiedBy: summary.LastAuthor = value; break;
                    case MetadataFields.RevisionNumber: summary.RevNumber = value; break;
                    case MetadataFields.Created: summary.CreateDateTime = LegacyCfbHelper.ParseUtc(value); break;
                    case MetadataFields.Modified: summary.LastSaveDateTime = LegacyCfbHelper.ParseUtc(value); break;
                }
            }
        }

        private static byte[] AddVbaStorage(byte[] workbookBytes, byte[] vbaProject)
        {
            var fs = new NPOIFSFileSystem(new MemoryStream(workbookBytes));
            try
            {
                LegacyCfbHelper.AddVbaProject(fs.Root, vbaProject, LegacyCfbHelper.XlsMacrosStorage);
                using var output = new MemoryStream();
                fs.WriteFileSystem(output);
                return output.ToArray();
            }
            finally
            {
                fs.Close();
            }
        }
    }
}
