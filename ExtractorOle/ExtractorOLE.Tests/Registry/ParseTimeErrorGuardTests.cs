using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// #287: only a format-library parse failure is reported as corrupt-file (with the library
    /// exception as InnerException); a failing stream, out-of-memory or a missing strategy is not.
    /// </summary>
    public class ParseTimeErrorGuardTests
    {
        private const string Mime = "application/test";

        public static TheoryData<string> Strategies() => new()
        {
            "docx", "xlsx", "pptx", "doc", "xls", "ppt", "zip-fallback",
        };

        private static IOpenStrategy Strategy(string name)
        {
            var helper = new ExtractionHelper();
            return name switch
            {
                "docx" => new WordOpenStrategy(helper),
                "xlsx" => new ExcelOpenStrategy(helper),
                "pptx" => new PowerPointOpenStrategy(helper),
                "doc" => new DocOpenStrategy(helper),
                "xls" => new XlsOpenStrategy(helper),
                "ppt" => new PptOpenStrategy(helper),
                _ => new ZipFallbackOpenStrategy(new WordOpenStrategy(helper), new ExcelOpenStrategy(helper), new PowerPointOpenStrategy(helper)),
            };
        }

        // Seekable stream whose every read throws the same exception instance.
        private sealed class FaultingStream : Stream
        {
            private readonly Func<Exception> _fault;
            public FaultingStream(Func<Exception> fault) => _fault = fault;
            public Exception? Thrown { get; private set; }
            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => 4096;
            public override long Position { get; set; }
            public override int Read(byte[] buffer, int offset, int count) => throw (Thrown ??= _fault());
            public override int Read(Span<byte> buffer) => throw (Thrown ??= _fault());
            public override long Seek(long offset, SeekOrigin origin) => Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                _ => Length + offset,
            };
            public override void Flush() { }
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        private sealed class ThrowingStrategy : IOpenStrategy
        {
            private readonly Exception _ex;
            public ThrowingStrategy(Exception ex) => _ex = ex;
            public DocumentExtractionResult? Open(byte[] fileBytes) => throw _ex;
        }

        private static Exception OpenFaulting(string strategy, Func<Exception> fault, out FaultingStream stream)
        {
            stream = new FaultingStream(fault);
            using var input = ExtractionInput.From(new ExtractionRequest { InputStream = stream, DetectedMimeType = Mime });
            var s = Strategy(strategy);
            return Assert.ThrowsAny<Exception>(() => ParseTimeErrorGuard.OpenOrThrow(s, input, Mime));
        }

        private static bool ChainContains(Exception ex, Exception target)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (ReferenceEquals(e, target)) return true;
            }
            return false;
        }

        [Theory]
        [MemberData(nameof(Strategies))]
        public void StreamIOException_IsNotReportedAsCorruptFile(string strategy)
        {
            var ex = OpenFaulting(strategy, () => new IOException("disk gone"), out var stream);

            Assert.IsNotType<CorruptFileException>(ex);
            Assert.NotNull(stream.Thrown);
            Assert.True(ChainContains(ex, stream.Thrown!), $"Expected the stream's IOException, got {ex}");
        }

        [Theory]
        [MemberData(nameof(Strategies))]
        public void StreamObjectDisposedException_IsNotReportedAsCorruptFile(string strategy)
        {
            var ex = OpenFaulting(strategy, () => new ObjectDisposedException("input"), out var stream);

            Assert.IsNotType<CorruptFileException>(ex);
            Assert.NotNull(stream.Thrown);
            Assert.True(ChainContains(ex, stream.Thrown!), $"Expected the stream's ObjectDisposedException, got {ex}");
        }

        [Theory]
        [MemberData(nameof(Strategies))]
        public void GarbageBytes_AreCorruptFile_WithTheParseExceptionAsInner(string strategy)
        {
            var ex = Assert.Throws<CorruptFileException>(() =>
                ParseTimeErrorGuard.OpenOrThrow(Strategy(strategy), new byte[] { 0x00, 0x01, 0x02, 0x03 }, Mime));

            Assert.Equal(Mime, ex.DetectedMimeType);
            Assert.NotNull(ex.InnerException);
        }

        [Fact]
        public void OutOfMemory_IsRethrownAsIs()
        {
            var oom = new InsufficientMemoryException(); // an OutOfMemoryException (CA2201 reserves the base type)

            var ex = Assert.Throws<InsufficientMemoryException>(() =>
                ParseTimeErrorGuard.OpenOrThrow(new ThrowingStrategy(oom), new byte[] { 1 }, Mime));

            Assert.Same(oom, ex);
        }

        [Fact]
        public void LibraryIOException_FromByteInput_IsCorruptFile()
        {
            // Format libraries throw plain IOException for malformed data; with no stream fault it is a parse failure.
            var io = new IOException("Invalid header signature");

            var ex = Assert.Throws<CorruptFileException>(() =>
                ParseTimeErrorGuard.OpenOrThrow(new ThrowingStrategy(io), new byte[] { 1 }, Mime));

            Assert.Same(io, ex.InnerException);
        }

        [Fact]
        public void MissingStrategy_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => ParseTimeErrorGuard.OpenOrThrow(null, new byte[] { 1 }, Mime));
        }

        [Fact]
        public void ZipFallback_RethrowsNonParseFailureWithoutTryingOtherFlavors()
        {
            var oom = new InsufficientMemoryException(); // an OutOfMemoryException (CA2201 reserves the base type)
            var calls = 0;
            var helper = new ExtractionHelper();
            var fallback = new ZipFallbackOpenStrategy(new CountingWord(helper, () => { calls++; throw oom; }), new ExcelOpenStrategy(helper), new PowerPointOpenStrategy(helper));

            Assert.Same(oom, Assert.Throws<InsufficientMemoryException>(() => fallback.Open(new byte[] { 1, 2, 3 })));
            Assert.Equal(1, calls);
        }

        [Fact]
        public void ZipFallback_RethrowsEncryptedDocumentWithoutTryingOtherFlavors()
        {
            var encrypted = new NPOI.EncryptedDocumentException("encrypted");
            var calls = 0;
            var helper = new ExtractionHelper();
            var fallback = new ZipFallbackOpenStrategy(new CountingWord(helper, () => { calls++; throw encrypted; }), new ExcelOpenStrategy(helper), new PowerPointOpenStrategy(helper));

            Assert.Same(encrypted, Assert.Throws<NPOI.EncryptedDocumentException>(() => fallback.Open(new byte[] { 1, 2, 3 })));
            Assert.Equal(1, calls);
        }

        // Re-implements IOpenStrategy so ZipFallback's interface call reaches this Open(byte[]).
        private sealed class CountingWord : WordOpenStrategy, IOpenStrategy
        {
            private readonly Action _onOpen;
            public CountingWord(IExtractionHelper helper, Action onOpen) : base(helper) => _onOpen = onOpen;
            public new DocumentExtractionResult? Open(byte[] fileBytes) { _onOpen(); return null; }
        }
    }
}
