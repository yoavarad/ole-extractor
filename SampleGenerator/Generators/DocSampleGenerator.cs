using System.Buffers.Binary;
using System.Text;
using NPOI.POIFS.FileSystem;
using SampleGenerator.Abstractions;

namespace SampleGenerator.Generators
{
    /// <summary>
    /// Authors a legacy Word 97-2003 .doc (OLE-CFB) from scratch (docs/adrs/005-legacy-doc-ppt-authoring.md):
    /// no OSS library in this repo can write .doc body content (b2xtranslator only converts to
    /// OOXML; NPOI's HWPF is scratchpad-only and not compiled here), so the WordDocument and
    /// 1Table streams are written directly per [MS-DOC] on top of NPOI's POIFS container writer.
    ///
    /// The document is one Unicode piece (UTF-16LE, no code-page compression, so multilingual and
    /// emoji text is stored verbatim) with one paragraph per body line, the default "Normal" style,
    /// one section and default character formatting. <see cref="SampleSpec.Metadata"/> goes to
    /// SummaryInformation, embeddings to opaque ObjectPool storages, and a
    /// <see cref="SampleSpec.VbaProject"/> to a "Macros" storage - the locations
    /// ExtractorOLE's DocOpenStrategy reads.
    /// </summary>
    public sealed class DocSampleGenerator : ISampleGenerator
    {
        private const int FibSize = 900;              // FibBase + FibRgW97 + FibRgLw97 + FibRgFcLcb97 (93 pairs) + cswNew
        private const int FcLcbStart = 154;           // offset of the first (fc, lcb) pair in the FIB
        private const int TextFc = 0x400;             // where the text starts in the WordDocument stream
        private const int FkpSize = 512;
        private const int ParagraphsPerPapxFkp = 24;  // 4 + 17 * crun must fit ahead of the shared PAPX
        private const int PapxOffsetInFkp = 502;      // even; [0][cw][istd][grpprl] = 8 bytes, crun at 511
        private const int DopSize = 500;
        private const int MinStreamSize = 4096;

        // Indices into FibRgFcLcb97.
        private const int Stshf = 1, Plcfsed = 6, PlcfbteChpx = 12, PlcfbtePapx = 13, SttbfFfn = 15, Dop = 31, Clx = 33;

        public SampleFormat Format => SampleFormat.Doc;

        public GeneratedSample Generate(SampleSpec spec)
        {
            ArgumentNullException.ThrowIfNull(spec);

            var paragraphs = DocxSampleGenerator.SplitLines(spec.BodyText).ToList();
            var (wordDocument, table) = BuildStreams(paragraphs);

            var fs = new NPOIFSFileSystem();
            try
            {
                fs.Root.CreateDocument("WordDocument", new MemoryStream(wordDocument));
                fs.Root.CreateDocument("1Table", new MemoryStream(table));
                LegacyCfbHelper.WriteSummaryInformation(fs, spec.Metadata);

                if (spec.Embeddings.Count > 0)
                {
                    LegacyCfbHelper.AddEmbeddedObjects(fs.Root.CreateDirectory("ObjectPool"), spec.Embeddings, "_");
                }
                if (spec.VbaProject is { } vbaProject)
                {
                    LegacyCfbHelper.AddVbaProject(fs.Root, vbaProject, LegacyCfbHelper.WordPptMacrosStorage);
                }

                using var output = new MemoryStream();
                fs.WriteFileSystem(output);
                return new GeneratedSample { FileName = "sample.doc", Content = output.ToArray() };
            }
            finally
            {
                fs.Close();
            }
        }

        private static (byte[] WordDocument, byte[] Table) BuildStreams(IReadOnlyList<string> paragraphs)
        {
            // Every paragraph, including the last, ends with a paragraph mark (0x0D).
            var text = string.Concat(paragraphs.Select(p => p.Replace('\r', ' ') + "\r"));
            var textBytes = Encoding.Unicode.GetBytes(text);
            var ccpText = text.Length;
            var textEndFc = TextFc + textBytes.Length;

            // ---- WordDocument stream: FIB | text | CHPX FKP | PAPX FKPs | SEPX ----
            var chpxPn = (textEndFc + FkpSize - 1) / FkpSize;
            var papxFirstPn = chpxPn + 1;
            var papxPages = (paragraphs.Count + ParagraphsPerPapxFkp - 1) / ParagraphsPerPapxFkp;
            var sepxFc = (papxFirstPn + papxPages) * FkpSize;
            // At least 4096 bytes so the stream lives in regular sectors, as in every Word-written file.
            var wordDocument = new byte[Math.Max(sepxFc + 8, MinStreamSize)];
            textBytes.CopyTo(wordDocument, TextFc);

            // CHPX FKP: one run over all the text with no character formatting (rgb = 0).
            var chpx = wordDocument.AsSpan(chpxPn * FkpSize, FkpSize);
            WriteInt32(chpx, 0, TextFc);
            WriteInt32(chpx, 4, textEndFc);
            chpx[FkpSize - 1] = 1;

            // PAPX FKPs: one run per paragraph, all sharing one PAPX (istd 0 + sprmPDxaLeft 0).
            var paragraphEndFcs = new List<int>();
            var cp = 0;
            foreach (var p in paragraphs)
            {
                cp += p.Replace('\r', ' ').Length + 1;
                paragraphEndFcs.Add(TextFc + 2 * cp);
            }
            var papxPageStartFcs = new List<int>();
            for (var page = 0; page < papxPages; page++)
            {
                var first = page * ParagraphsPerPapxFkp;
                var count = Math.Min(ParagraphsPerPapxFkp, paragraphs.Count - first);
                var startFc = first == 0 ? TextFc : paragraphEndFcs[first - 1];
                papxPageStartFcs.Add(startFc);

                var fkp = wordDocument.AsSpan((papxFirstPn + page) * FkpSize, FkpSize);
                WriteInt32(fkp, 0, startFc);
                for (var i = 0; i < count; i++)
                {
                    WriteInt32(fkp, 4 * (i + 1), paragraphEndFcs[first + i]);
                    fkp[4 * (count + 1) + 13 * i] = PapxOffsetInFkp / 2; // bOffset; the 12-byte PHE stays zero
                }
                fkp[PapxOffsetInFkp] = 0;                                  // pad byte: real word count follows
                fkp[PapxOffsetInFkp + 1] = 3;                              // 3 words = istd (2) + grpprl (4)
                BinaryPrimitives.WriteUInt16LittleEndian(fkp[(PapxOffsetInFkp + 2)..], 0); // istd = Normal
                BinaryPrimitives.WriteUInt16LittleEndian(fkp[(PapxOffsetInFkp + 4)..], 0x845E); // sprmPDxaLeft
                BinaryPrimitives.WriteInt16LittleEndian(fkp[(PapxOffsetInFkp + 6)..], 0);
                fkp[FkpSize - 1] = (byte)count;
            }

            // SEPX: cb = 6, then sprmSBkc (new page) + sprmSFTitlePage (off).
            BinaryPrimitives.WriteUInt16LittleEndian(wordDocument.AsSpan(sepxFc), 6);
            new byte[] { 0x09, 0x30, 0x02, 0x0A, 0x30, 0x00 }.CopyTo(wordDocument, sepxFc + 2);

            // ---- 1Table stream ----
            var tableParts = new List<(int Index, byte[] Bytes)>
            {
                (Stshf, BuildStylesheet()),
                (SttbfFfn, BuildFontTable()),
                (Plcfsed, BuildPlcfsed(ccpText, sepxFc)),
                (PlcfbteChpx, BuildBinTable(new[] { TextFc, textEndFc }, new[] { chpxPn })),
                (PlcfbtePapx, BuildBinTable(papxPageStartFcs.Append(textEndFc).ToArray(),
                    Enumerable.Range(papxFirstPn, papxPages).ToArray())),
                (Dop, new byte[DopSize]),
                (Clx, BuildClx(ccpText)),
            };

            using var table = new MemoryStream();
            var fib = new byte[FibSize];
            foreach (var (index, bytes) in tableParts)
            {
                WriteInt32(fib, FcLcbStart + 8 * index, (int)table.Length);
                WriteInt32(fib, FcLcbStart + 8 * index + 4, bytes.Length);
                table.Write(bytes);
            }
            WriteFibHeader(fib, wordDocument.Length, ccpText, textEndFc);
            fib.CopyTo(wordDocument, 0);

            return (wordDocument, table.ToArray());
        }

        // FibBase (32) | csw + FibRgW97 (28) | cslw + FibRgLw97 (88) | cbRgFcLcb + pairs | cswNew.
        private static void WriteFibHeader(byte[] fib, int cbMac, int ccpText, int textEndFc)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(0), 0xA5EC);   // wIdent
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(2), 0x00C1);   // nFib: Word 97
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(6), 0x0409);   // lid
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(10), 0x1200);  // fWhichTblStm (1Table) | fExtChar
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(12), 0x00BF);  // nFibBack
            fib[19] = 0x10;                                                    // fWord97Saved
            WriteInt32(fib, 24, TextFc);                                       // fcMin (Word 97 still writes these two
            WriteInt32(fib, 28, textEndFc);                                    // fcMac  legacy fields; readers use fcMac)

            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(32), 14);      // csw
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(60), 0x0409);  // lidFE
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(62), 22);      // cslw
            WriteInt32(fib, 64, cbMac);                                        // cbMac
            WriteInt32(fib, 64 + 4 * 3, ccpText);                              // ccpText
            BinaryPrimitives.WriteUInt16LittleEndian(fib.AsSpan(152), 93);     // cbRgFcLcb
            // cswNew (at 898) stays 0 for nFib 0x00C1.
        }

        // Stshf: cbStshi | STSHI (18 bytes) | 15 LPStd entries. Only "Normal" (istd 0) is defined.
        private static byte[] BuildStylesheet()
        {
            using var ms = new MemoryStream();
            var w = new BinaryWriter(ms);

            w.Write((ushort)18);       // cbStshi
            w.Write((ushort)15);       // cstd
            w.Write((ushort)10);       // cbSTDBaseInFile
            w.Write((ushort)1);        // fStdStylenamesWritten
            w.Write((ushort)15);       // stiMaxWhenSaved
            w.Write((ushort)15);       // istdMaxFixedWhenSaved
            w.Write((ushort)0);        // nVerBuiltInNamesWhenSaved
            w.Write((ushort)0); w.Write((ushort)0); w.Write((ushort)0); // rgftcStandardChpStsh

            var std = new MemoryStream();
            var s = new BinaryWriter(std);
            s.Write((ushort)0);        // sti = 0 (Normal)
            s.Write((ushort)0xFFF1);   // stk = 1 (paragraph) | istdBase = 0xFFF (none)
            s.Write((ushort)0x0002);   // cupx = 2 | istdNext = 0
            s.Write((ushort)0);        // bchUpe (patched below)
            s.Write((ushort)0);        // grfstd
            s.Write((ushort)6);        // xstzName: cch
            s.Write(Encoding.Unicode.GetBytes("Normal\0"));
            s.Write((ushort)2); s.Write((ushort)0);   // upxPapx: cb = 2, istd = 0
            s.Write((ushort)0);                       // upxChpx: cb = 0
            var stdBytes = std.ToArray();
            BinaryPrimitives.WriteUInt16LittleEndian(stdBytes.AsSpan(6), (ushort)stdBytes.Length);

            w.Write((ushort)stdBytes.Length);
            w.Write(stdBytes);
            for (var i = 1; i < 15; i++) w.Write((ushort)0);   // null styles
            return ms.ToArray();
        }

        // SttbfFfn: cData | cbExtra | one FFN ("Times New Roman"), 1-byte length prefix.
        private static byte[] BuildFontTable()
        {
            using var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            var name = Encoding.Unicode.GetBytes("Times New Roman\0");

            w.Write((ushort)1);
            w.Write((ushort)0);
            w.Write((byte)(1 + 2 + 1 + 1 + 10 + 24 + name.Length)); // cbFfnM1: FFN size without this byte
            w.Write((byte)0x12);       // prq = 2 (variable pitch) | fTrueType | ff = 1 (roman)
            w.Write((ushort)400);      // wWeight
            w.Write((byte)0);          // chs (ANSI)
            w.Write((byte)0);          // ixchSzAlt
            w.Write(new byte[10]);     // panose
            w.Write(new byte[24]);     // font signature
            w.Write(name);
            return ms.ToArray();
        }

        // One section covering the whole main text; its SED points at an empty SEPX in the WordDocument stream.
        private static byte[] BuildPlcfsed(int ccpText, int sepxFc)
        {
            var bytes = new byte[2 * 4 + 12];
            WriteInt32(bytes, 4, ccpText);
            WriteInt32(bytes, 8 + 2, sepxFc);   // SED: fn (2) = 0, fcSepx (4), fnMpr (2), fcMpr (4)
            return bytes;
        }

        // Bin table (PlcfBte): n + 1 fc boundaries followed by n FKP page numbers.
        private static byte[] BuildBinTable(int[] fcs, int[] pageNumbers)
        {
            var bytes = new byte[fcs.Length * 4 + pageNumbers.Length * 4];
            for (var i = 0; i < fcs.Length; i++) WriteInt32(bytes, 4 * i, fcs[i]);
            for (var i = 0; i < pageNumbers.Length; i++) WriteInt32(bytes, fcs.Length * 4 + 4 * i, pageNumbers[i]);
            return bytes;
        }

        // Clx: Pcdt (0x02, lcb) holding a PlcPcd with a single uncompressed (UTF-16LE) piece.
        private static byte[] BuildClx(int ccpText)
        {
            var bytes = new byte[1 + 4 + 2 * 4 + 8];
            bytes[0] = 0x02;
            WriteInt32(bytes, 1, 2 * 4 + 8);
            WriteInt32(bytes, 5 + 4, ccpText);           // CP[1]; CP[0] = 0
            WriteInt32(bytes, 5 + 8 + 2, TextFc);        // PCD: flags (2) = 0, fc (4): fCompressed clear, prm (2) = 0
            return bytes;
        }

        private static void WriteInt32(Span<byte> buffer, int offset, int value) =>
            BinaryPrimitives.WriteInt32LittleEndian(buffer[offset..], value);
    }
}
