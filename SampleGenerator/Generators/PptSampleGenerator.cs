using System.Buffers.Binary;
using System.Text;
using NPOI.POIFS.FileSystem;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a legacy PowerPoint 97-2003 .ppt (OLE-CFB) from scratch
    /// (docs/adrs/005-legacy-doc-ppt-authoring.md): no OSS library in this repo can write .ppt
    /// slide content (b2xtranslator only converts to OOXML; NPOI has no HSLF "open/assemble"
    /// entry point and its scratchpad HSLF is not compiled here), so the "PowerPoint Document"
    /// and "Current User" streams are written directly per [MS-PPT] on top of NPOI's POIFS
    /// container writer.
    ///
    /// The deck is one slide on one main master. Every non-empty body line is one text box shape
    /// holding a TextCharsAtom (UTF-16LE, so multilingual and emoji text is stored verbatim;
    /// PowerPoint itself only uses the 8-bit TextBytesAtom when every char is Latin-1).
    /// ExtractorOLE's PptTextExtractor reads one line per text run in shape order.
    /// <see cref="SampleSpec.Metadata"/> goes to SummaryInformation, each embedding to one opaque
    /// root storage (PptOpenStrategy returns every top-level storage as one embedded object) and a
    /// <see cref="SampleSpec.VbaProject"/> to a "Macros" storage, which that same walk also
    /// reports as one further embedded object.
    /// </summary>
    public sealed class PptSampleGenerator : ISampleGenerator
    {
        // Record types ([MS-PPT] RecordType) and OfficeArt record types ([MS-ODRAW]).
        private const int RtDocument = 1000, RtDocumentAtom = 1001, RtEndDocumentAtom = 1002, RtSlide = 1006,
            RtSlideAtom = 1007, RtEnvironment = 1010, RtSlidePersistAtom = 1011, RtMainMaster = 1016,
            RtSlideViewInfo = 1018, RtZoomViewInfoAtom = 1021, RtSlideViewInfoAtom = 1022,
            RtTextMasterStyleAtom = 4003, RtDocInfoList = 2000, RtPpDrawingGroup = 1035, RtPpDrawing = 1036, RtColorSchemeAtom = 2032, RtTextHeaderAtom = 3999,
            RtTextCharsAtom = 4000, RtSlideListWithText = 4080, RtUserEditAtom = 4085,
            RtCurrentUserAtom = 4086, RtPersistDirectoryAtom = 6002;
        private const int OaDggContainer = 0xF000, OaDgContainer = 0xF002, OaSpgrContainer = 0xF003,
            OaSpContainer = 0xF004, OaDg = 0xF008, OaSpgr = 0xF009, OaSp = 0xF00A, OaClientTextbox = 0xF00D,
            OaOpt = 0xF00B, OaClientAnchor = 0xF010;

        private const uint MasterSlideId = 0x80000000;
        private const uint FirstSlideId = 256;
        private const int TextBoxShapeType = 202;      // msosptTextBox
        private const int TextTypeOther = 4;           // TextHeaderAtom: text box text
        private const int SlideWidth = 5760, SlideHeight = 4320; // master units (1/576 inch): 10 x 7.5 in

        public SampleFormat Format => SampleFormat.Ppt;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            var lines = DocxSampleGenerator.SplitLines(spec.BodyText).Where(l => l.Length > 0).ToList();
            var (document, currentUser) = BuildStreams(lines);

            var fs = new NPOIFSFileSystem();
            try
            {
                fs.Root.CreateDocument("PowerPoint Document", new MemoryStream(document));
                fs.Root.CreateDocument("Current User", new MemoryStream(currentUser));
                LegacyCfbHelper.WriteSummaryInformation(fs, spec.Metadata);
                LegacyCfbHelper.AddEmbeddedObjects(fs.Root, spec.Embeddings, "Embedding");
                if (spec.VbaProject is { } vbaProject)
                {
                    LegacyCfbHelper.AddVbaProject(fs.Root, vbaProject, LegacyCfbHelper.WordPptMacrosStorage);
                }

                using var output = new MemoryStream();
                fs.WriteFileSystem(output);
                return new GeneratedSample { FileName = "sample.ppt", Content = output.ToArray() };
            }
            finally
            {
                fs.Close();
            }
        }

        // Stream layout: Document | MainMaster | Slide | PersistDirectoryAtom | UserEditAtom.
        // Persist ids: 1 = Document, 2 = MainMaster, 3 = Slide.
        private static (byte[] Document, byte[] CurrentUser) BuildStreams(IReadOnlyList<string> lines)
        {
            var document = BuildDocument();
            var master = BuildMaster();
            var slide = BuildSlide(lines);

            var masterOffset = document.Length;
            var slideOffset = masterOffset + master.Length;
            var persistDirectoryOffset = slideOffset + slide.Length;

            // PersistDirectoryAtom: one entry [persistId 1, count 3] followed by the three offsets.
            var persistDirectory = Record(0, 0, RtPersistDirectoryAtom,
                U32(1 | (3 << 20)), U32(0), U32(masterOffset), U32(slideOffset));
            var userEditOffset = persistDirectoryOffset + persistDirectory.Length;

            var userEdit = Record(0, 0, RtUserEditAtom,
                U32(FirstSlideId),           // lastSlideIdRef
                U16(0), new byte[] { 0, 3 }, // version, minorVersion, majorVersion
                U32(0),                      // offsetLastEdit (no earlier edit)
                U32(persistDirectoryOffset),
                U32(1),                      // docPersistIdRef
                U32(4),                      // persistIdSeed: highest persist id + 1
                U16(1), U16(0));             // lastViewType, unused

            // Current User: CurrentUserAtom pointing at the UserEditAtom, then the user name twice
            // (ANSI inside the atom, UTF-16LE after it).
            const string userName = "SampleGenerator";
            var currentUser = Record(0, 0, RtCurrentUserAtom,
                U32(20),                    // size
                U32(0xE391C05F),            // headerToken: not encrypted
                U32(userEditOffset),        // offsetToCurrentEdit
                U16(userName.Length),
                U16(0x03F4),                // docFileVersion
                new byte[] { 3, 0 },        // majorVersion, minorVersion
                U16(0),                     // unused
                Encoding.ASCII.GetBytes(userName),
                U32(8),                     // relFileVersion
                Encoding.Unicode.GetBytes(userName));

            return (Concat(document, master, slide, persistDirectory, userEdit), currentUser);
        }

        private static byte[] BuildDocument()
        {
            var documentAtom = Record(1, 0, RtDocumentAtom,
                U32(SlideWidth), U32(SlideHeight), U32(SlideHeight), U32(SlideWidth), // slide size, notes size
                U32(5), U32(10),                                                     // server zoom 5:10
                U32(0), U32(0),                                                      // notes/handout master persist refs
                U16(1), U16(0),                                                      // first slide number, slide size type
                new byte[] { 0, 0, 0, 1 });                                          // save-with-fonts, omit-title, RTL, show-comments

            var slideList = Container(0, RtSlideListWithText,
                Record(0, 0, RtSlidePersistAtom, U32(3), U32(4), U32(0), U32(FirstSlideId), U32(0)));
            var masterList = Container(1, RtSlideListWithText,
                Record(0, 0, RtSlidePersistAtom, U32(2), U32(0), U32(0), U32(MasterSlideId), U32(0)));

            return Container(0, RtDocument,
                documentAtom,
                // Readers take the default text style from the Environment's TextMasterStyleAtom:
                // one indent level whose paragraph and character formatting masks are empty.
                Container(0, RtEnvironment, Record(0, 4, RtTextMasterStyleAtom, U16(1), U32(0), U32(0))),
                Container(0, RtPpDrawingGroup, Container(0, OaDggContainer)),
                masterList,
                DocInfoList(),
                slideList,
                Record(0, 0, RtEndDocumentAtom));
        }

        // Readers take the view scale/origin from the first SlideViewInfo's ZoomViewInfoAtom: 1:1, origin (0, 0).
        private static byte[] DocInfoList() =>
            Container(0, RtDocInfoList,
                Container(0, RtSlideViewInfo,
                    Record(0, 0, RtSlideViewInfoAtom, new byte[] { 1, 1, 0 }),
                    Record(0, 0, RtZoomViewInfoAtom,
                        U32(1), U32(1), U32(1), U32(1), // curScale: x = 1/1, y = 1/1
                        new byte[24],                   // unused
                        U32(0), U32(0),                 // origin
                        new byte[4])));                 // fUserVarScale, fDraftMode, padding

        private static byte[] BuildMaster() =>
            Container(0, RtMainMaster,
                SlideAtom(0),
                Drawing(Array.Empty<byte[]>()),
                Record(0, 1, RtColorSchemeAtom, ColorScheme()));

        private static byte[] BuildSlide(IReadOnlyList<string> lines)
        {
            // One text box per body line, stacked down the slide.
            var boxHeight = Math.Max(1, (SlideHeight - 200) / Math.Max(lines.Count, 1));
            var shapes = lines.Select((line, i) => TextBox(line, 1026 + i, 100 + i * boxHeight, boxHeight)).ToArray();

            return Container(0, RtSlide,
                SlideAtom(MasterSlideId),
                Drawing(shapes),
                Record(0, 1, RtColorSchemeAtom, ColorScheme()));
        }

        // SlideAtom: geometry, 8 placeholder-type bytes, master/notes ids, flags (master objects | scheme | background).
        private static byte[] SlideAtom(uint masterId) =>
            Record(2, 0, RtSlideAtom,
                U32(masterId == 0 ? 0 : 16), new byte[8], U32(masterId), U32(0), U16(7), U16(0));

        // PPDrawing > DgContainer > { Dg, SpgrContainer > { group Sp, shapes... } }.
        private static byte[] Drawing(byte[][] shapes)
        {
            var groupShape = Container(0, OaSpContainer,
                Record(1, 0, OaSpgr, U32(0), U32(0), U32(SlideWidth), U32(SlideHeight)),
                Record(2, 0, OaSp, U32(1024), U32(0x5))); // spid, fGroup | fPatriarch
            var group = Container(0, OaSpgrContainer, new[] { groupShape }.Concat(shapes).ToArray());

            return Container(0, RtPpDrawing,
                Container(0, OaDgContainer,
                    Record(0, 1, OaDg, U32(shapes.Length + 1), U32(1026 + shapes.Length)), // csp, spidCur
                    group));
        }

        private static byte[] TextBox(string text, int spid, int top, int height) =>
            Container(0, OaSpContainer,
                Record(2, TextBoxShapeType, OaSp, U32(spid), U32(0xA00)), // fHaveAnchor | fHaveSpt
                Record(3, 1, OaOpt, U16(0x0080), U32(0)),                 // one property: lTxid = 0 (readers require an Opt)
                Record(0, 0, OaClientAnchor, U16(top), U16(100), U16(SlideWidth - 100), U16(top + height)),
                Container(0, OaClientTextbox,
                    Record(0, 0, RtTextHeaderAtom, U32(TextTypeOther)),
                    Record(0, 0, RtTextCharsAtom, Encoding.Unicode.GetBytes(text))));

        // Eight RGBs (background, text, shadow, title, fill, accent1-3) + reserved byte each.
        private static byte[] ColorScheme() => new byte[]
        {
            0xFF, 0xFF, 0xFF, 0, 0x00, 0x00, 0x00, 0, 0x80, 0x80, 0x80, 0, 0x00, 0x00, 0x00, 0,
            0x99, 0x99, 0x00, 0, 0x00, 0x66, 0x99, 0, 0x99, 0x99, 0x99, 0, 0xCC, 0xCC, 0xCC, 0,
        };

        // ---- record plumbing ----

        // Record header: recVer (4 bits) | recInstance (12 bits), recType, recLen.
        private static byte[] Record(int version, int instance, int type, params byte[][] payload)
        {
            var body = Concat(payload);
            var record = new byte[8 + body.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(record, (ushort)(version | (instance << 4)));
            BinaryPrimitives.WriteUInt16LittleEndian(record.AsSpan(2), (ushort)type);
            BinaryPrimitives.WriteInt32LittleEndian(record.AsSpan(4), body.Length);
            body.CopyTo(record, 8);
            return record;
        }

        private static byte[] Container(int instance, int type, params byte[][] children) =>
            Record(0xF, instance, type, children);

        private static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

        private static byte[] U16(int value) => BitConverter.GetBytes((ushort)value);

        private static byte[] U32(long value) => BitConverter.GetBytes((uint)value);
    }
}
