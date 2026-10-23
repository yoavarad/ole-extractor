using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using NPOI.HSSF.UserModel;
using SampleGenerator.Abstractions;
using SampleGenerator.Generators;
using System.Buffers.Binary;
using System.IO.Compression;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Task #193: guardrails and preflight read stream/path input in place (headers and tail
    /// only) instead of buffering the whole file, with the same exceptions and values as
    /// byte[] input, and without disposing the caller's stream.
    /// </summary>
    public class StreamPreflightGuardrailTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string DocMime = "application/msword";

        private static byte[] Sample(string fileName)
        {
            var relative = Path.Combine("samples", "adversarial", fileName);
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
                dir = dir.Parent;
            }
            throw new FileNotFoundException($"Could not locate '{relative}' walking up from {AppContext.BaseDirectory}");
        }

        private static byte[] BuildXls(int rows = 1)
        {
            var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Sheet1");
            for (var i = 0; i < rows; i++) sheet.CreateRow(i).CreateCell(0).SetCellValue("row " + i);
            using var ms = new MemoryStream();
            workbook.Write(ms);
            return ms.ToArray();
        }

        private static byte[] Fixture(string name)
        {
            var spec = new SampleSpec { BodyText = "preflight parity" };
            switch (name)
            {
                case "empty": return Array.Empty<byte>();
                case "non-container": return new byte[] { 1, 2, 3, 4, 5 };
                case "zip-signature-only": return new byte[] { 0x50, 0x4B, 0x03, 0x04 };
                case "cfb-header-cut": return BuildXls()[..100];
                case "cfb-cut": { var full = BuildXls(); return full[..(full.Length - 600)]; }
                case "valid-docx": return new DocxSampleGenerator().Generate(spec).Content;
                case "valid-xlsx": return new XlsxSampleGenerator().Generate(spec).Content;
                case "valid-pptx": return new PptxSampleGenerator().Generate(spec).Content;
                case "valid-doc": return new DocSampleGenerator().Generate(spec).Content;
                case "valid-xls": return new XlsSampleGenerator().Generate(spec).Content;
                case "valid-ppt": return new PptSampleGenerator().Generate(spec).Content;
                case "zip-comment": return WithZipComment(Fixture("valid-docx"), new byte[100]);
                case "zip-fake-eocd-in-comment": return WithZipComment(Fixture("valid-docx"), FakeEocdComment());
                case "zip-max-comment": return WithZipComment(Fixture("valid-docx"), new byte[ushort.MaxValue]);
                case "zip-max-comment-truncated": return WithZipComment(Sample("truncated-container.docx"), new byte[ushort.MaxValue]);
                case "zip-large-truncated":
                    {
                        var full = BuildLargeStoredZip(200_000);
                        return full[..1000].Concat(full[2000..]).ToArray(); // drop 1000 payload bytes
                    }
                case "cfb-fat-past-eof":
                    {
                        var xls = BuildXls();
                        BinaryPrimitives.WriteUInt32LittleEndian(xls.AsSpan(0x4C, 4), 100_000);
                        return xls;
                    }
                case "cfb-v4": return BuildCfbV4();
                default: return Sample(name);
            }
        }

        // Appends a trailing comment to a ZIP whose EOCD is the last 22 bytes (no comment yet).
        private static byte[] WithZipComment(byte[] zip, byte[] comment)
        {
            var eocd = zip.Length - 22;
            Assert.Equal(0x06054B50u, BinaryPrimitives.ReadUInt32LittleEndian(zip.AsSpan(eocd, 4)));
            Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(zip.AsSpan(eocd + 20, 2)));
            var result = zip.Concat(comment).ToArray();
            BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(eocd + 20, 2), (ushort)comment.Length);
            return result;
        }

        // A fake EOCD inside the comment whose own comment length runs past EOF: the scan must
        // skip it (continue) and find the real EOCD before it.
        private static byte[] FakeEocdComment()
        {
            var comment = new byte[40];
            BinaryPrimitives.WriteUInt32LittleEndian(comment.AsSpan(5, 4), 0x06054B50);
            BinaryPrimitives.WriteUInt32LittleEndian(comment.AsSpan(5 + 16, 4), 0x7FFFFFFF); // would read as truncated if trusted
            BinaryPrimitives.WriteUInt16LittleEndian(comment.AsSpan(5 + 20, 2), ushort.MaxValue);
            return comment;
        }

        private static byte[] BuildCfbV4()
        {
            using var ms = new MemoryStream();
            using (var root = OpenMcdf.RootStorage.Create(ms, OpenMcdf.Version.V4, OpenMcdf.StorageModeFlags.LeaveOpen))
            {
                using var entry = root.CreateStream("WordDocument");
                entry.Write(new byte[10_000]);
            }
            var bytes = ms.ToArray();
            Assert.Equal(12, BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(0x1E, 2)));
            return bytes;
        }

        // ---- Preflight(Stream) parity with Preflight(byte[]) ----

        [Theory]
        [InlineData("empty")]
        [InlineData("zero-byte.docx")]
        [InlineData("non-container")]
        [InlineData("zip-signature-only")]
        [InlineData("truncated-container.docx")]
        [InlineData("corrupt-container.docx")]
        [InlineData("password-protected.docx")]
        [InlineData("cfb-header-cut")]
        [InlineData("cfb-cut")]
        [InlineData("valid-docx")]
        [InlineData("valid-xlsx")]
        [InlineData("valid-pptx")]
        [InlineData("valid-doc")]
        [InlineData("valid-xls")]
        [InlineData("valid-ppt")]
        [InlineData("zip-comment")]
        [InlineData("zip-fake-eocd-in-comment")]
        [InlineData("zip-max-comment")]
        [InlineData("zip-max-comment-truncated")]
        [InlineData("zip-large-truncated")]
        [InlineData("cfb-fat-past-eof")]
        [InlineData("cfb-v4")]
        public void PreflightStream_MatchesPreflightBytes(string fixture)
        {
            var bytes = Fixture(fixture);

            foreach (var isOoxml in new[] { true, false })
            {
                var bytesError = Record.Exception(() => ParseTimeErrorGuard.Preflight(bytes, isOoxml));
                using var stream = new MemoryStream(bytes, writable: false);
                var streamError = Record.Exception(() => ParseTimeErrorGuard.Preflight(stream, isOoxml));

                Assert.Equal(bytesError?.GetType(), streamError?.GetType());
                if (bytesError is TruncatedContainerException expected)
                {
                    var actual = (TruncatedContainerException)streamError!;
                    Assert.Equal(expected.ExpectedBytes, actual.ExpectedBytes);
                    Assert.Equal(expected.ActualBytes, actual.ActualBytes);
                }
                Assert.True(stream.CanRead); // not disposed
            }
        }

        [Theory]
        [InlineData("zip-comment", false)]
        [InlineData("zip-fake-eocd-in-comment", false)]
        [InlineData("zip-max-comment", false)]
        [InlineData("zip-max-comment-truncated", true)]
        [InlineData("zip-large-truncated", true)]
        [InlineData("cfb-fat-past-eof", true)]
        [InlineData("cfb-v4", false)]
        public void PreflightStream_EdgeFixtures_TruncationVerdict(string fixture, bool truncated)
        {
            var bytes = Fixture(fixture);
            using var stream = new MemoryStream(bytes, writable: false);

            var error = Record.Exception(() => ParseTimeErrorGuard.Preflight(stream, isOoxmlFormat: true));

            if (truncated)
            {
                var ex = Assert.IsType<TruncatedContainerException>(error);
                Assert.Equal(bytes.Length, ex.ActualBytes);
                Assert.True(ex.ExpectedBytes > bytes.Length);
            }
            else
            {
                Assert.Null(error);
            }
        }

        [Fact]
        public void PreflightStream_KnownScenarios_ThrowTheExpectedErrors()
        {
            using var truncated = new MemoryStream(Sample("truncated-container.docx"));
            var ex = Assert.Throws<TruncatedContainerException>(() => ParseTimeErrorGuard.Preflight(truncated, true));
            Assert.Equal(3714, ex.ExpectedBytes);
            Assert.Equal(truncated.Length, ex.ActualBytes);

            using var encrypted = new MemoryStream(Sample("password-protected.docx"));
            Assert.Throws<PasswordProtectedException>(() => ParseTimeErrorGuard.Preflight(encrypted, true));
            Assert.True(encrypted.CanRead);

            var empty = Assert.Throws<TruncatedContainerException>(() => ParseTimeErrorGuard.Preflight(new MemoryStream(), false));
            Assert.Equal(512, empty.ExpectedBytes);
            Assert.Equal(0, empty.ActualBytes);
        }

        // ---- guardrails: byte[] / InputStream / FilePath give the same rejection ----

        private static byte[] BuildZipWithDeclaredSizes(params uint[] declaredUncompressedSizes)
        {
            using var ms = new MemoryStream();
            foreach (var size in declaredUncompressedSizes)
            {
                var header = new byte[30 + 1];
                header[0] = 0x50; header[1] = 0x4B; header[2] = 0x03; header[3] = 0x04;
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(18, 4), 0);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(22, 4), size);
                BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(26, 2), 1);
                header[30] = (byte)'a';
                ms.Write(header);
            }
            return ms.ToArray();
        }

        private static byte[] BuildCfbWithFatSectorCount(uint fatSectorCount)
        {
            var bytes = new byte[512];
            new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }.CopyTo(bytes, 0);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x1E, 2), 9);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x2C, 4), fatSectorCount);
            return bytes;
        }

        private static (MainExtractor extractor, List<string> log) Build()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();
            var log = new List<string>();
            var limits = new MimeDetectionLimits { MaxDeclaredEntryBytes = 1000, MaxDeclaredNestedContentBytes = 1500 };
            var extractor = new MainExtractor(
                provider.GetRequiredService<IExtractionHelper>(),
                provider.GetRequiredService<IFormatDispatchRegistry>(),
                new ExtractionGuardrails(limits, log.Add));
            return (extractor, log);
        }

        public static IEnumerable<object[]> OversizedContainers() => new[]
        {
            new object[] { "zip-entry", BuildZipWithDeclaredSizes(1001) },
            new object[] { "zip-total", BuildZipWithDeclaredSizes(800, 800) },
            new object[] { "zip64-sentinel", BuildZipWithDeclaredSizes(10, uint.MaxValue) },
            new object[] { "cfb-fat", BuildCfbWithFatSectorCount(1) },
        };

        [Theory]
        [MemberData(nameof(OversizedContainers))]
        public void Guardrails_ByteStreamAndPathInput_RejectIdentically(string name, byte[] bytes)
        {
            var mime = name.StartsWith("cfb") ? DocMime : DocxMime;

            var (byBytes, bytesLog) = Build();
            var bytesEx = Assert.Throws<OversizedNestedContentException>(() => byBytes.Extract(
                new ExtractionRequest { FileBytes = bytes, FileName = "f", DetectedMimeType = mime }));

            var (byStream, streamLog) = Build();
            using var stream = new MemoryStream(bytes, writable: false);
            var streamEx = Assert.Throws<OversizedNestedContentException>(() => byStream.Extract(
                new ExtractionRequest { InputStream = stream, FileName = "f", DetectedMimeType = mime }));
            Assert.True(stream.CanRead);

            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                File.WriteAllBytes(path, bytes);
                var (byPath, pathLog) = Build();
                var pathEx = Assert.Throws<OversizedNestedContentException>(() => byPath.Extract(
                    new ExtractionRequest { FilePath = path, FileName = "f", DetectedMimeType = mime }));

                Assert.Equal(bytesEx.Message, streamEx.Message);
                Assert.Equal(bytesEx.Message, pathEx.Message);
                Assert.Equal(bytesLog, streamLog);
                Assert.Equal(bytesLog, pathLog);
            }
            finally
            {
                File.Delete(path); // throws if Extract left the file open
            }
        }

        // ---- caller stream stays usable ----

        private static MainExtractor DefaultExtractor()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            return services.BuildServiceProvider().GetRequiredService<MainExtractor>();
        }

        private static void AssertStreamIntact(Stream stream, byte[] bytes)
        {
            Assert.True(stream.CanRead);
            stream.Position = 0;
            var copy = new byte[bytes.Length];
            stream.ReadExactly(copy);
            Assert.Equal(bytes, copy);
            Assert.Equal(bytes.Length, stream.Length);
        }

        [Theory]
        [InlineData("valid-docx", DocxMime)]
        [InlineData("valid-doc", DocMime)]
        public void Extract_ValidStreamInput_ExtractsText_AndLeavesCallerStreamOpenAndUnchanged(string fixture, string mime)
        {
            var bytes = Fixture(fixture);
            // Writable on purpose: nothing in the pipeline may write to the caller's stream.
            using var stream = new MemoryStream();
            stream.Write(bytes);

            var result = DefaultExtractor().Extract(
                new ExtractionRequest { InputStream = stream, FileName = "f", DetectedMimeType = mime });

            Assert.Contains("preflight parity", result.ExtractedText);
            AssertStreamIntact(stream, bytes);
        }

        [Fact]
        public void Extract_EncryptedStreamInput_ThrowsPasswordProtected_AndLeavesCallerStreamOpenAndUnchanged()
        {
            var bytes = Sample("password-protected.docx");
            using var stream = new MemoryStream();
            stream.Write(bytes);

            Assert.Throws<PasswordProtectedException>(() => DefaultExtractor().Extract(
                new ExtractionRequest { InputStream = stream, FileName = "f", DetectedMimeType = DocxMime }));

            AssertStreamIntact(stream, bytes);
        }

        [Theory]
        [InlineData("valid-docx", DocxMime)]
        [InlineData("valid-xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        [InlineData("valid-doc", DocMime)]
        [InlineData("valid-xls", "application/vnd.ms-excel")]
        [InlineData("valid-ppt", "application/vnd.ms-powerpoint")]
        public void Extract_StreamWithNonZeroPosition_MatchesByteArrayInput(string fixture, string mime)
        {
            var bytes = Fixture(fixture);
            var extractor = DefaultExtractor();
            var fromBytes = extractor.Extract(new ExtractionRequest { FileBytes = bytes, FileName = "f", DetectedMimeType = mime });

            using var stream = new MemoryStream(bytes, writable: false);
            stream.Position = bytes.Length / 2;
            var fromStream = extractor.Extract(new ExtractionRequest { InputStream = stream, FileName = "f", DetectedMimeType = mime });

            Assert.Equal(fromBytes.ExtractedText, fromStream.ExtractedText);
            Assert.Equal(fromBytes.EmbeddedFiles.Count, fromStream.EmbeddedFiles.Count);
        }

        [Fact]
        public void Extract_FilePathOnPasswordProtectedCfb_ThrowsPasswordProtected()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".docx");
            try
            {
                File.WriteAllBytes(path, Sample("password-protected.docx"));

                Assert.Throws<PasswordProtectedException>(() => DefaultExtractor().Extract(
                    new ExtractionRequest { FilePath = path, FileName = "f", DetectedMimeType = DocxMime }));
            }
            finally
            {
                File.Delete(path); // throws if Extract left the file open
            }
        }

        // OpenMcdf is opened on the caller's stream for the encryption check; it must not write
        // to a writable stream (no Commit/Flush on dispose).
        [Theory]
        [InlineData("valid-doc")]
        [InlineData("valid-xls")]
        [InlineData("password-protected.docx")]
        public void PreflightStream_NeverWritesToAWritableCallerStream(string fixture)
        {
            var bytes = Fixture(fixture);
            using var stream = new WriteTrackingStream(bytes);
            Assert.Equal(0, stream.Writes);

            Record.Exception(() => ParseTimeErrorGuard.Preflight(stream, isOoxmlFormat: false));

            Assert.Equal(0, stream.Writes);
            AssertStreamIntact(stream, bytes);
        }

        // ---- no full-file buffer ----

        private static byte[] BuildLargeStoredZip(int payloadBytes)
        {
            var payload = new byte[payloadBytes];
            new Random(193).NextBytes(payload);
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                using (var w = new StreamWriter(zip.CreateEntry("[Content_Types].xml").Open())) w.Write("<Types/>");
                using var e = zip.CreateEntry("big.bin", CompressionLevel.NoCompression).Open();
                e.Write(payload);
            }
            return ms.ToArray();
        }

        [Fact]
        public void GuardrailsAndPreflight_OnStreamInput_ReadOnlyHeadersAndTail()
        {
            var bytes = BuildLargeStoredZip(4 * 1024 * 1024);
            using var counting = new ReadCountingStream(new MemoryStream(bytes, writable: false));
            var request = new ExtractionRequest { InputStream = counting, FileName = "f", DetectedMimeType = DocxMime };

            using (var input = ExtractionInput.From(request))
            {
                new ExtractionGuardrails(new MimeDetectionLimits(), _ => { }).Enforce(request, input);
                ParseTimeErrorGuard.Preflight(input.Stream!, isOoxmlFormat: true);
            }

            Assert.True(counting.BytesRead < 100_000, $"read {counting.BytesRead} of {bytes.Length} bytes");
            Assert.True(counting.CanRead);
        }

        [Fact]
        public void Preflight_OnCfbStream_ReadsFarLessThanTheFile()
        {
            var bytes = BuildXls(rows: 40_000);
            using var counting = new ReadCountingStream(new MemoryStream(bytes, writable: false));

            ParseTimeErrorGuard.Preflight(counting, isOoxmlFormat: false);

            Assert.True(counting.BytesRead < bytes.Length / 4, $"read {counting.BytesRead} of {bytes.Length} bytes");
        }

        private sealed class WriteTrackingStream : MemoryStream
        {
            public WriteTrackingStream(byte[] bytes) { base.Write(bytes, 0, bytes.Length); Position = 0; }
            public int Writes { get; private set; }
            public override void Write(byte[] buffer, int offset, int count) { Writes++; base.Write(buffer, offset, count); }
            public override void Write(ReadOnlySpan<byte> buffer) { Writes++; base.Write(buffer); }
            public override void WriteByte(byte value) { Writes++; base.WriteByte(value); }
            public override void SetLength(long value) { Writes++; base.SetLength(value); }
        }

        private sealed class ReadCountingStream : Stream
        {
            private readonly Stream _inner;
            private bool _disposed;
            public long BytesRead { get; private set; }
            public ReadCountingStream(Stream inner) => _inner = inner;
            public override bool CanRead => !_disposed;
            public override bool CanSeek => !_disposed;
            public override bool CanWrite => false;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
            public override int Read(Span<byte> buffer)
            {
                var n = _inner.Read(buffer);
                BytesRead += n;
                return n;
            }
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void Flush() { }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            protected override void Dispose(bool disposing) { _disposed = true; base.Dispose(disposing); }
        }
    }
}
