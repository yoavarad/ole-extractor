using DocumentFormat.OpenXml.Packaging;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace ExtractorOLE.Helpers
{
    /// <summary>
    /// First-layer embedded-file (and OOXML package metadata) extraction for OOXML parts,
    /// HSSF workbooks, raw CFB (.ppt) file systems and the .doc ObjectPool storage.
    /// </summary>
    public interface IEmbeddedFileExtractor
    {
        void ExtractMetadataAndEmbedded(OpenXmlPackage package, OpenXmlPart? rootPart, DocumentExtractionResult result);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart rootPart);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, HSSFWorkbook workbook);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, NPOIFSFileSystem fs);
        void ExtractFirstLayerEmbedded(DocumentExtractionResult result, DirectoryEntry root);
    }

    public class EmbeddedFileExtractor : IEmbeddedFileExtractor
    {
        private readonly IMimeTypeMapper _mapper;
        private readonly long _maxItemBytes;

        public EmbeddedFileExtractor(IMimeTypeMapper mapper, MimeDetectionLimits? limits = null)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _maxItemBytes = (limits ?? new MimeDetectionLimits()).MaxDeclaredEntryBytes;
        }
        public void ExtractMetadataAndEmbedded(OpenXmlPackage package, OpenXmlPart? rootPart, DocumentExtractionResult result)
        {
            if (package == null) return;

            // [ydk:req:extraction/per-field-error-isolation]: each property is its own leaf at
            // "Metadata.<CSharpName>"; the part is read inside each leaf so an unreadable part
            // costs exactly the properties it sources.
            var m = result.Metadata;
            var extendedPropsPart = package.GetPartsOfType<ExtendedFilePropertiesPart>().FirstOrDefault();
            if (extendedPropsPart != null)
            {
                PopulateMetadata(result, nameof(FileMetadata.EditingDurationMinutes), () => m.EditingDurationMinutes = MetadataConversions.ParseInt(extendedPropsPart.Properties?.TotalTime?.Text));
                PopulateMetadata(result, nameof(FileMetadata.ApplicationName), () => m.ApplicationName = MetadataConversions.NullIfEmpty(extendedPropsPart.Properties?.Application?.Text));
            }

            var corePart = package.GetPartsOfType<CoreFilePropertiesPart>().FirstOrDefault();

            if (corePart != null)
            {
                // The SDK's CoreFileProperties parses every element at once, so one malformed
                // value would cost all ten; each element is read on its own instead. The part is
                // loaded once, and a part that is not well-formed fails every leaf it sources.
                var core = new Lazy<XElement?>(() => LoadCoreProperties(corePart));
                PopulateMetadata(result, nameof(FileMetadata.Title), () => m.Title = CoreText(core.Value, Dc + "title"));
                PopulateMetadata(result, nameof(FileMetadata.Creator), () => m.Creator = CoreText(core.Value, Dc + "creator"));
                PopulateMetadata(result, nameof(FileMetadata.Created), () => m.Created = CoreDate(core.Value, DcTerms + "created"));
                PopulateMetadata(result, nameof(FileMetadata.Modified), () => m.Modified = CoreDate(core.Value, DcTerms + "modified"));
                PopulateMetadata(result, nameof(FileMetadata.LastModifiedBy), () => m.LastModifiedBy = CoreText(core.Value, Cp + "lastModifiedBy"));
                PopulateMetadata(result, nameof(FileMetadata.Subject), () => m.Subject = MetadataConversions.NullIfEmpty(CoreText(core.Value, Dc + "subject")));
                PopulateMetadata(result, nameof(FileMetadata.Comments), () => m.Comments = MetadataConversions.NullIfEmpty(CoreText(core.Value, Dc + "description")));
                PopulateMetadata(result, nameof(FileMetadata.RevisionNumber), () => m.RevisionNumber = MetadataConversions.ParseInt(CoreText(core.Value, Cp + "revision")));
                PopulateMetadata(result, nameof(FileMetadata.LastPrinted), () => m.LastPrinted = CoreDate(core.Value, Cp + "lastPrinted"));
                PopulateMetadata(result, nameof(FileMetadata.Keywords), () => m.Keywords = MetadataConversions.NullIfEmpty(CoreText(core.Value, Cp + "keywords")));

                if (string.IsNullOrEmpty(result.MimeType) && !string.IsNullOrEmpty(corePart.ContentType))
                {
                    result.MimeType = corePart.ContentType;
                }
            }

            if (rootPart != null)
            {
                ExtractFirstLayerEmbedded(result, rootPart);
            }
        }

        private static readonly XNamespace Cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
        private static readonly XNamespace Dc = "http://purl.org/dc/elements/1.1/";
        private static readonly XNamespace DcTerms = "http://purl.org/dc/terms/";

        // core.xml is a handful of short elements; anything larger is not a real core part.
        private const long MaxCorePropertiesChars = 1_000_000;

        private static XElement? LoadCoreProperties(CoreFilePropertiesPart corePart)
        {
            using var stream = corePart.GetStream(FileMode.Open, FileAccess.Read);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxCorePropertiesChars });
            return XDocument.Load(reader).Root;
        }

        // Absent element -> null; present element -> its text, as the SDK reports it.
        private static string? CoreText(XElement? core, XName name) => core?.Element(name)?.Value;

        // W3CDTF timestamp, parsed as the SDK did (zoned -> Local, no zone -> Unspecified); an
        // unparsable value throws FormatException (a malformed source record).
        private static DateTime? CoreDate(XElement? core, XName name)
        {
            var text = CoreText(core, name);
            return string.IsNullOrWhiteSpace(text)
                ? null
                : DateTime.Parse(text.Trim(), CultureInfo.InvariantCulture);
        }

        private static void PopulateMetadata(DocumentExtractionResult result, string propertyName, Action populate) =>
            FieldIsolation.TryPopulate(result, MetadataConversions.MetadataPath(propertyName), populate);

        // A subfile is a distinct package part ([ydk:req:extraction/subfile-scope] speaks of
        // embeddings/ and media parts, not relationships): every image / OLE-embedding part
        // reachable from the root is listed exactly once by part URI, however many hosts relate
        // to it. The walk is breadth-first so the parts directly on the root come first, then
        // parts one hop down (slides, worksheets, headers), then deeper hosts (slide layouts,
        // vmlDrawing OLE previews). Only containers are descended into; an embedding is never
        // unpacked. The visited set also breaks relationship cycles (slide <-> layout <-> master).
        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, OpenXmlPart rootPart)
        {
            // An enumeration fault ends the walk: one bare EmbeddedFiles entry, items past the
            // fault are not reported. Per-item failures are isolated inside ExtractPartData.
            FieldIsolation.TryPopulate(result, FieldIsolation.EmbeddedFilesPath, () => WalkParts(result, rootPart));
        }

        private void WalkParts(DocumentExtractionResult result, OpenXmlPart rootPart)
        {
            int index = 1;
            var seen = new HashSet<Uri> { rootPart.Uri };
            var hosts = new List<OpenXmlPart> { rootPart };
            bool isRootLevel = true;

            while (hosts.Count > 0)
            {
                var nextHosts = new List<OpenXmlPart>();
                foreach (var host in hosts)
                {
                    foreach (var partPair in host.Parts)
                    {
                        var nestedPart = partPair.OpenXmlPart;
                        if (!seen.Add(nestedPart.Uri)) continue;

                        if (nestedPart is ImagePart)
                        {
                            // Images directly on the root keep the embedded_object prefix; images
                            // one or more hops down are slide_image.
                            ExtractPartData(nestedPart, result, ref index, isRootLevel ? "embedded_object" : "slide_image");
                        }
                        else if (nestedPart is EmbeddedObjectPart || nestedPart is EmbeddedPackagePart)
                        {
                            ExtractPartData(nestedPart, result, ref index, "embedded_object");
                        }
                        else
                        {
                            nextHosts.Add(nestedPart);
                        }
                    }
                }

                hosts = nextHosts;
                isRootLevel = false;
            }
        }

        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, HSSFWorkbook workbook)
        {
            int index = 1;

            FieldIsolation.TryPopulate(result, FieldIsolation.EmbeddedFilesPath, () =>
            {
                foreach (HSSFObjectData obj in workbook.GetAllEmbeddedObjects())
                {
                    ExtractHssfObjectData(obj, result, ref index);
                }
            });

            FieldIsolation.TryPopulate(result, FieldIsolation.EmbeddedFilesPath, () =>
            {
                foreach (HSSFPictureData pic in workbook.GetAllPictures())
                {
                    ExtractHssfPictureData(pic, result, ref index);
                }
            });
        }

        // Legacy .ppt has no per-format API like HSSFWorkbook.GetAllEmbeddedObjects() to
        // enumerate embeddings -- NPOI has no working HSLF entry point (ADR-004). Embedded
        // OLE objects show up as CFB storages directly under the file's root, so this walks
        // the root directory entries generically: any top-level storage is one opaque
        // embedded object, named by its own CFB storage name (per ydk:req:extraction/subfile-scope's
        // ppt-specific acceptance criterion). The standard root streams a real .ppt carries
        // (PowerPoint Document, SummaryInformation, DocumentSummaryInformation, Current User)
        // are never directories, so they never collide with this check. Inline media -- legacy
        // .ppt concatenates every slide image into one root "Pictures" stream -- is returned as
        // one further opaque subfile rather than parsed apart into individual images (no
        // acceptance criterion asks for splitting it, and it fits the same "container returned
        // as one opaque item, not recursively unpacked" rule already established for OLE objects).
        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, NPOIFSFileSystem fs)
        {
            var root = fs.Root;
            var topLevelEntries = new List<Entry>();
            foreach (Entry entry in root)
            {
                topLevelEntries.Add(entry);
            }

            foreach (var entry in topLevelEntries)
            {
                if (!entry.IsDirectoryEntry) continue;

                FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath(entry.Name),
                    () => ExtractCfbStorage((DirectoryEntry)entry, result.EmbeddedFiles));
            }

            // Guarded with an `is` check rather than relying on the catch below: "Pictures" is
            // always a stream in a real .ppt, but if it were somehow a storage it would already
            // have been picked up (and, if unreadable, reported) by the storage loop above -- an
            // unconditional cast here would just re-attempt and re-report it as a second, unrelated
            // failure.
            if (root.HasEntry("Pictures") && root.GetEntry("Pictures") is DocumentEntry picturesEntry)
            {
                FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath(picturesEntry.Name),
                    () => ExtractCfbStream(picturesEntry, result.EmbeddedFiles));
            }
        }

        // Reads a stream to the end. Never sizes the buffer from a declared length (CFB directory
        // size, zip entry length): those come from untrusted headers, and a lying value would
        // force a huge up-front allocation. The MemoryStream grows only with bytes actually read.
        private static byte[] ReadAllBytes(Stream input)
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            return TakeBytes(ms);
        }

        // Same, but capped at the per-item limit: checked up front when the stream reports a
        // length, and again as bytes actually arrive, so a stream that cannot report (or lies
        // about) its length still stops at the cap.
        private byte[] ReadBoundedBytes(Stream input)
        {
            if (input.CanSeek) EnsureWithinLimit(input.Length);
            else if (TryGetDeclaredZipEntryLength(input) is long declared) EnsureWithinLimit(declared);

            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                EnsureWithinLimit(ms.Length + read);
                ms.Write(buffer, 0, read);
            }
            return TakeBytes(ms);
        }

        // The SDK hands out non-seekable System.IO.Packaging.ZipWrappingStream parts, which hide the
        // zip entry's declared uncompressed size. It is read from the wrapper's backing
        // ZipArchiveEntry (best effort: null when the layout differs, in which case the
        // bytes-actually-read cap in ReadBoundedBytes still applies).
        private static long? TryGetDeclaredZipEntryLength(Stream input)
        {
            try
            {
                var field = input.GetType().GetField("_zipArchiveEntry", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                return (field?.GetValue(input) as System.IO.Compression.ZipArchiveEntry)?.Length;
            }
            catch (Exception ex) when (ex is System.Reflection.TargetException or MemberAccessException or InvalidOperationException)
            {
                return null;
            }
        }

        // [ydk:nfr:extraction/nesting-depth-guard]: the per-item cap is the per-entry limit.
        private void EnsureWithinLimit(long declaredBytes)
        {
            if (declaredBytes > _maxItemBytes)
            {
                throw new OversizedNestedContentException(
                    $"embedded item declares {declaredBytes} bytes (per-item limit {_maxItemBytes})");
            }
        }

        // Sum of the declared stream sizes under a storage, read from the directory entries
        // before anything is copied or allocated. Iterative (no stack growth on a deep tree) and
        // stops as soon as the per-item cap is exceeded.
        private long DeclaredSize(DirectoryEntry storage)
        {
            long total = 0;
            var pending = new Stack<DirectoryEntry>();
            pending.Push(storage);
            while (pending.Count > 0)
            {
                foreach (Entry child in pending.Pop())
                {
                    if (child is DirectoryEntry dir) pending.Push(dir);
                    else if (child is DocumentEntry doc) total += doc.Size;
                    if (total > _maxItemBytes) return total;
                }
            }
            return total;
        }

        // Hands off the MemoryStream's backing array when it is exactly full (no copy);
        // otherwise copies once.
        private static byte[] TakeBytes(MemoryStream ms)
        {
            if (ms.TryGetBuffer(out var seg) && seg.Offset == 0 && seg.Array!.Length == seg.Count)
            {
                return seg.Array;
            }

            return ms.ToArray();
        }

        // Copies one CFB storage's whole subtree into a standalone POIFS filesystem and
        // serializes it to bytes -- the same opaque, no-recursive-unpacking technique
        // ExtractHssfObjectData uses for .xls embedded objects. Named by the storage's own
        // CFB name.
        private void ExtractCfbStorage(DirectoryEntry storage, List<EmbeddedFileItem> fileList)
        {
            EnsureWithinLimit(DeclaredSize(storage));
            var target = new NPOIFSFileSystem();
            try
            {
                EntryUtils.CopyNodes(storage, target.Root);
                byte[] extractedBytes;
                using (var outStream = new MemoryStream())
                {
                    target.WriteFileSystem(outStream);
                    extractedBytes = TakeBytes(outStream);
                }

                fileList.Add(new EmbeddedFileItem
                {
                    BinaryData = extractedBytes,
                    PackagePath = storage.Name,
                    SizeInBytes = extractedBytes.LongLength,
                    FileName = storage.Name,
                });
            }
            finally
            {
                target.Close();
            }
        }

        // Reads one root-level CFB stream (the "Pictures" media blob) as one opaque subfile.
        private void ExtractCfbStream(DocumentEntry stream, List<EmbeddedFileItem> fileList)
        {
            EnsureWithinLimit(stream.Size);
            using var input = new DocumentInputStream(stream);
            byte[] extractedBytes = ReadAllBytes(input);

            fileList.Add(new EmbeddedFileItem
            {
                BinaryData = extractedBytes,
                PackagePath = stream.Name,
                SizeInBytes = extractedBytes.LongLength,
                FileName = stream.Name,
            });
        }

        // Legacy .doc has no HWPFDocument-level "get all embedded objects" convenience API
        // (ADR-004: HWPF lives only in NPOI's un-compiled scratchpad tree). Per the Word/OLE
        // storage convention (mirrored from Java POI, documented in docs/research/npoi.md),
        // first-layer OLE-embedded objects live as child storages of a root-level "ObjectPool"
        // directory - so this walks that storage directly off the raw CFB tree, independent of
        // any body-text conversion path. Each child directory is copied out and serialized as
        // one opaque blob (same technique as ExtractHssfObjectData), never unpacked further.
        public void ExtractFirstLayerEmbedded(DocumentExtractionResult result, DirectoryEntry root)
        {
            if (!root.HasEntry("ObjectPool")) return;
            if (root.GetEntry("ObjectPool") is not DirectoryEntry objectPool) return;

            int index = 1;
            FieldIsolation.TryPopulate(result, FieldIsolation.EmbeddedFilesPath, () =>
            {
                foreach (var entry in objectPool)
                {
                    ExtractDocObjectPoolEntry(entry, result, ref index);
                }
            });
        }

        // Reads one ObjectPool child. A corrupt/malformed entry (e.g. one that isn't actually a
        // directory storage) is reported at EmbeddedFiles[ObjectPool/<name>] (bare when the name
        // is empty) without failing the whole walk.
        private void ExtractDocObjectPoolEntry(Entry entry, DocumentExtractionResult result, ref int index)
        {
            var fileList = result.EmbeddedFiles;
            int current = index;
            var path = string.IsNullOrEmpty(entry.Name) ? null : $"ObjectPool/{entry.Name}";
            if (FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath(path), () =>
            {
                if (entry is not DirectoryEntry objectDir)
                {
                    throw new IOException($"ObjectPool entry '{entry.Name}' is not a directory storage");
                }

                EnsureWithinLimit(DeclaredSize(objectDir));

                var target = new NPOIFSFileSystem();
                try
                {
                    EntryUtils.CopyNodes(objectDir, target.Root);
                    using (var outStream = new MemoryStream())
                    {
                        target.WriteFileSystem(outStream);
                        var extractedBytes = TakeBytes(outStream);

                        var item = new EmbeddedFileItem
                        {
                            BinaryData = extractedBytes,
                            PackagePath = $"ObjectPool/{objectDir.Name}",
                            SizeInBytes = extractedBytes.LongLength,
                            FileName = objectDir.Name,
                        };

                        fileList.Add(item);
                    }
                }
                finally
                {
                    target.Close();
                }
            }))
            {
                index = current + 1;
            }
        }

        // Reads one embedded OLE object. A corrupt item (e.g. a POIFS directory entry that doesn't
        // resolve to a real DirectoryEntry) is reported at EmbeddedFiles[embedded_object_<n>]
        // without failing the whole extraction.
        private void ExtractHssfObjectData(HSSFObjectData obj, DocumentExtractionResult result, ref int index)
        {
            var fileList = result.EmbeddedFiles;
            int current = index;
            if (FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath($"embedded_object_{current}"), () =>
            {
                byte[] extractedBytes;

                if (obj.HasDirectoryEntry())
                {
                    // Copy the container's directory tree into a standalone POIFS filesystem and
                    // serialize it to bytes. This returns the whole embedded object as one opaque
                    // blob - its internal streams (e.g. "Ole", "Ole10Native") are never unpacked
                    // or listed separately.
                    EnsureWithinLimit(DeclaredSize(obj.Directory));
                    var target = new NPOIFSFileSystem();
                    try
                    {
                        EntryUtils.CopyNodes(obj.Directory, target.Root);
                        using (var outStream = new MemoryStream())
                        {
                            target.WriteFileSystem(outStream);
                            extractedBytes = TakeBytes(outStream);
                        }
                    }
                    finally
                    {
                        target.Close();
                    }
                }
                else
                {
                    extractedBytes = obj.ObjectData;
                }

                var item = new EmbeddedFileItem
                {
                    BinaryData = extractedBytes,
                    PackagePath = $"embedded_object_{current}",
                    SizeInBytes = extractedBytes.LongLength,
                    FileName = $"embedded_object_{current}.bin"
                };

                fileList.Add(item);
            }))
            {
                index = current + 1;
            }
        }

        // Reads one inline picture, reported at EmbeddedFiles[picture_<n>] on failure, matching
        // ExtractHssfObjectData's per-item behavior.
        private static void ExtractHssfPictureData(HSSFPictureData pic, DocumentExtractionResult result, ref int index)
        {
            var fileList = result.EmbeddedFiles;
            int current = index;
            if (FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath($"picture_{current}"), () =>
            {
                byte[] extractedBytes = pic.Data;
                string extension = pic.SuggestFileExtension();
                string fileName = string.IsNullOrEmpty(extension)
                    ? $"picture_{current}.bin"
                    : $"picture_{current}.{extension}";

                fileList.Add(new EmbeddedFileItem
                {
                    BinaryData = extractedBytes,
                    PackagePath = $"picture_{current}",
                    SizeInBytes = extractedBytes.LongLength,
                    FileName = fileName
                });
            }))
            {
                index = current + 1;
            }
        }

        private void ExtractPartData(OpenXmlPart part, DocumentExtractionResult result, ref int index, string prefix)
        {
            int current = index;
            if (FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath(part.Uri.ToString()), () =>
            {
                using Stream partStream = part.GetStream();
                byte[] extractedBytes = ReadBoundedBytes(partStream);

                result.EmbeddedFiles.Add(new EmbeddedFileItem
                {
                    BinaryData = extractedBytes,
                    PackagePath = part.Uri.ToString(),
                    SizeInBytes = extractedBytes.LongLength,
                    FileName = $"{prefix}_{current}{_mapper.GetExtensionFromContentType(part.ContentType)}"
                });
            }))
            {
                index = current + 1;
            }
        }
    }
}
