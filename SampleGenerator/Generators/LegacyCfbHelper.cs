using System.Globalization;
using NPOI.HPSF;
using NPOI.POIFS.FileSystem;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// CFB-level pieces shared by the legacy doc/xls/ppt generators: the
    /// SummaryInformation property stream, opaque OLE-object storages and a
    /// VBA-project storage. Uses the self-compiled NPOI POIFS/HPSF (ADR-001);
    /// none of this is format-body content, which each generator owns.
    /// </summary>
    internal static class LegacyCfbHelper
    {
        // Root-storage names the ExtractorOLE open strategies look for
        // (DocOpenStrategy/PptOpenStrategy: "Macros"; XlsOpenStrategy: "_VBA_PROJECT_CUR").
        public const string WordPptMacrosStorage = "Macros";
        public const string XlsMacrosStorage = "_VBA_PROJECT_CUR";

        /// <summary>
        /// Writes "\005SummaryInformation" from <see cref="SampleSpec.Metadata"/>.
        /// Nothing is written when the spec has no metadata.
        /// </summary>
        public static void WriteSummaryInformation(NPOIFSFileSystem fs, IDictionary<string, string> metadata)
        {
            if (metadata.Count == 0) return;

            var summary = PropertySetFactory.CreateSummaryInformation();
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
                    case MetadataFields.Created: summary.CreateDateTime = ParseUtc(value); break;
                    case MetadataFields.Modified: summary.LastSaveDateTime = ParseUtc(value); break;
                }
            }

            // MutablePropertySet.Write closes the stream it is given; ToArray still works on a closed MemoryStream.
            var written = new MemoryStream();
#pragma warning disable CS0618 // MutablePropertySet is what NPOI itself uses to serialize a PropertySet
            new MutablePropertySet(summary).Write(written);
#pragma warning restore CS0618
            fs.Root.CreateDocument(SummaryInformation.DEFAULT_STREAM_NAME, new MemoryStream(written.ToArray()));
        }

        /// <summary>
        /// Adds each embedding as one opaque storage named <c>prefix + index</c> under
        /// <paramref name="parent"/>, holding the content in an "Ole10Native"-named stream.
        /// The ExtractorOLE walk returns such a storage as one opaque subfile.
        /// </summary>
        public static void AddEmbeddedObjects(
            DirectoryEntry parent, IEnumerable<EmbeddedContentSpec> embeddings, string namePrefix)
        {
            var index = 1;
            foreach (var embedding in embeddings)
            {
                var storage = parent.CreateDirectory($"{namePrefix}{index++}");
                storage.CreateDocument("\u0001Ole10Native", new MemoryStream(embedding.Content));
            }
        }

        /// <summary>Adds the VBA project bytes as a single stream under a storage named <paramref name="storageName"/>.</summary>
        public static void AddVbaProject(DirectoryEntry root, byte[] vbaProject, string storageName)
        {
            var storage = root.CreateDirectory(storageName);
            storage.CreateDocument("VBA", new MemoryStream(vbaProject));
        }

        internal static DateTime ParseUtc(string iso) =>
            DateTime.Parse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
    }
}
