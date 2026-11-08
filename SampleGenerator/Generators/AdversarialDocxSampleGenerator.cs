using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Builds the docx adversarial fixtures for per-field error isolation
    /// (ydk:req:extraction/per-field-error-isolation): each opens as a docx
    /// without an open-time throw but fails on exactly one leaf afterwards.
    /// </summary>
    public static class AdversarialDocxSampleGenerator
    {
        /// <summary>Declared uncompressed size forged into the central directory: 1.5 GB, over the 1 GB per-entry cap.</summary>
        public const uint ForgedEntrySize = 0x60000000;

        public const string GoodEmbeddingEntry = "word/embeddings/embeddedObject.bin";
        public const string OversizedEmbeddingEntry = "word/embeddings/embeddedObject2.bin";

        private const string ImageRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image";
        private const string HeaderRelationshipType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/header";

        private static SampleSpec BuildSpec(string bodyText, int embeddingCount)
        {
            var spec = new SampleSpec { BodyText = bodyText };
            for (var i = 1; i <= embeddingCount; i++)
            {
                spec.Embeddings.Add(new EmbeddedContentSpec
                {
                    FileName = $"oleObject{i}.bin",
                    Content = Encoding.UTF8.GetBytes($"embedded object payload {i}"),
                    ContentType = "application/vnd.openxmlformats-officedocument.oleObject",
                });
            }

            return spec;
        }

        /// <summary>A docx whose word/document.xml compressed data is garbage: the package and its parts open, the body cannot be read.</summary>
        public static byte[] BuildCorruptBodyText()
        {
            var good = new DocxSampleGenerator().Generate(BuildSpec("This body text is stored in a document part that cannot be inflated.", 1)).Content;
            return ZipEntryCorruptor.CorruptEntryData(good, "word/document.xml");
        }

        /// <summary>
        /// A docx with a good embedding and a second one whose central-directory uncompressed size is forged over the
        /// per-entry cap. The local file header (all the open-time guard reads) keeps the true size.
        /// </summary>
        public static byte[] BuildOversizedEmbeddedObject()
        {
            var good = new DocxSampleGenerator().Generate(BuildSpec("One embedded object declares more than the per-entry cap.", 2)).Content;
            var bytes = (byte[])good.Clone();

            var eocd = FindEndOfCentralDirectory(bytes);
            var offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(eocd + 16, 4));
            var total = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(eocd + 10, 2));
            for (var i = 0; i < total; i++)
            {
                var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 28, 2));
                var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 30, 2));
                var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 32, 2));
                var name = Encoding.UTF8.GetString(bytes, offset + 46, nameLength);
                if (name == OversizedEmbeddingEntry)
                {
                    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + 24, 4), ForgedEntrySize);
                    return bytes;
                }

                offset += 46 + nameLength + extraLength + commentLength;
            }

            throw new InvalidOperationException($"Central directory entry '{OversizedEmbeddingEntry}' not found.");
        }

        /// <summary>
        /// A docx with a good embedding on the main part and a header part (one hop down) that relates an image (two hops down)
        /// whose relationships part is not well-formed XML, so enumerating the subfiles faults after the first item.
        /// </summary>
        public static byte[] BuildFaultingEmbeddedEnumeration()
        {
            var good = new DocxSampleGenerator().Generate(BuildSpec("The subfile enumeration of this document faults after its first item.", 1)).Content;

            using var stream = new MemoryStream();
            stream.Write(good);
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
            {
                var relsEntry = archive.GetEntry("word/_rels/document.xml.rels")!;
                string rels;
                using (var reader = new StreamReader(relsEntry.Open(), Encoding.UTF8))
                {
                    rels = reader.ReadToEnd();
                }

                relsEntry.Delete();
                var header = $"<Relationship Id=\"rIdHeader\" Type=\"{HeaderRelationshipType}\" Target=\"header1.xml\" />";
                rels = rels.Replace("</Relationships>", header + "</Relationships>", StringComparison.Ordinal);
                WriteEntry(archive, "word/_rels/document.xml.rels", rels);
                WriteEntry(archive, "word/header1.xml", "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p/></w:hdr>");
                WriteEntry(archive, "word/_rels/header1.xml.rels", $"<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"{ImageRelationshipType}\" Target=\"media/headerImage.png\" /></Relationships>");
                WriteEntry(archive, "word/media/headerImage.png", "not really a png");
                WriteEntry(archive, "word/media/_rels/headerImage.png.rels", "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\"");

                var contentTypes = archive.GetEntry("[Content_Types].xml")!;
                string types;
                using (var reader = new StreamReader(contentTypes.Open(), Encoding.UTF8))
                {
                    types = reader.ReadToEnd();
                }

                contentTypes.Delete();
                types = types.Replace("</Types>", "<Override PartName=\"/word/media/headerImage.png\" ContentType=\"image/png\" /><Override PartName=\"/word/header1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.header+xml\" /></Types>", StringComparison.Ordinal);
                WriteEntry(archive, "[Content_Types].xml", types);
            }

            return stream.ToArray();
        }

        private static void WriteEntry(ZipArchive archive, string name, string content)
        {
            using var writer = new StreamWriter(archive.CreateEntry(name).Open(), new UTF8Encoding(false));
            writer.Write(content);
        }

        private static int FindEndOfCentralDirectory(byte[] bytes)
        {
            for (var i = bytes.Length - 22; i >= 0; i--)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(i, 4)) == 0x06054b50)
                {
                    return i;
                }
            }

            throw new InvalidOperationException("End of Central Directory record not found.");
        }
    }
}
