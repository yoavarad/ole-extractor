using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.FileTypeStrategy;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    public class IOpenStrategyTests
    {
        private sealed class BytesOnlyStrategy : IOpenStrategy
        {
            public byte[]? Seen;
            public Exception? Throw;

            public DocumentExtractionResult? Open(byte[] fileBytes)
            {
                Seen = fileBytes;
                if (Throw != null) throw Throw;
                return new DocumentExtractionResult();
            }
        }

        [Fact]
        public void DefaultStreamOpen_PassesTheStreamBytesToByteOpen()
        {
            var strategy = new BytesOnlyStrategy();

            ((IOpenStrategy)strategy).Open(new MemoryStream(new byte[] { 7, 8, 9 }));

            Assert.Equal(new byte[] { 7, 8, 9 }, strategy.Seen);
        }

        [Fact]
        public void DefaultStreamOpen_LetsTheParseExceptionPropagate()
        {
            // #287: a parse failure is thrown, not turned into null, so the guard can carry it as InnerException.
            var parse = new InvalidDataException("bad");
            var strategy = new BytesOnlyStrategy { Throw = parse };

            Assert.Same(parse, Assert.Throws<InvalidDataException>(() => ((IOpenStrategy)strategy).Open(new MemoryStream(new byte[] { 1 }))));
        }
    }
}
