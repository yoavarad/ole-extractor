using System;
using System.IO;

namespace ExtractorOLE.Helpers.Input
{
    /// <summary>
    /// Non-closing view of the caller's stream that records the first exception a read throws (#287).
    /// Format libraries throw plain IOException for malformed data too, so the exception type alone
    /// cannot tell a failing stream from a corrupt file; a recorded <see cref="Fault"/> can. Only reads
    /// are recorded: a seek to an offset taken from corrupt data fails because of the data, not the stream.
    /// </summary>
    internal sealed class FaultTrackingStream : Stream
    {
        private readonly Stream _inner;

        public FaultTrackingStream(Stream inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>The first exception thrown by a read of the inner stream, or null.</summary>
        public Exception? Fault { get; private set; }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                return _inner.Read(buffer, offset, count);
            }
            catch (Exception ex)
            {
                Fault ??= ex;
                throw;
            }
        }

        public override int Read(Span<byte> buffer)
        {
            try
            {
                return _inner.Read(buffer);
            }
            catch (Exception ex)
            {
                Fault ??= ex;
                throw;
            }
        }

        public override int ReadByte()
        {
            try
            {
                return _inner.ReadByte();
            }
            catch (Exception ex)
            {
                Fault ??= ex;
                throw;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void Flush()
        {
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        // Dispose/Close not overridden: disposing the wrapper leaves inner open.
    }
}
