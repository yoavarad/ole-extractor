using ExtractorOLE.Helpers.Input;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    public class FaultTrackingStreamTests
    {
        private sealed class FaultingStream : MemoryStream
        {
            public int Reads;
            public FaultingStream() : base(new byte[16]) { }
            public override int Read(byte[] buffer, int offset, int count) => throw new IOException($"read {++Reads}");
            public override int Read(Span<byte> buffer) => throw new IOException($"read {++Reads}");
            public override long Seek(long offset, SeekOrigin origin) => throw new IOException("seek");
        }

        [Fact]
        public void HealthyReads_RecordNoFault_AndPassBytesThrough()
        {
            var inner = new MemoryStream(new byte[] { 1, 2, 3 });
            var stream = new FaultTrackingStream(inner);

            var buffer = new byte[3];
            Assert.Equal(3, stream.Read(buffer, 0, 3));

            Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
            Assert.Null(stream.Fault);
        }

        [Fact]
        public void FirstReadFault_IsRecordedAndRethrown()
        {
            var stream = new FaultTrackingStream(new FaultingStream());

            var first = Assert.Throws<IOException>(() => stream.Read(new byte[4], 0, 4));
            Assert.Throws<IOException>(() => stream.Read(new byte[4].AsSpan()));

            Assert.Same(first, stream.Fault);
        }

        [Fact]
        public void SeekFault_IsNotRecorded()
        {
            var stream = new FaultTrackingStream(new FaultingStream());

            Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Begin));

            Assert.Null(stream.Fault);
        }

        [Fact]
        public void Dispose_LeavesInnerOpen_AndWritesAreRejected()
        {
            var inner = new MemoryStream(new byte[4]);
            var stream = new FaultTrackingStream(inner);

            Assert.False(stream.CanWrite);
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[1], 0, 1));
            stream.Dispose();

            Assert.True(inner.CanRead);
        }
    }
}
