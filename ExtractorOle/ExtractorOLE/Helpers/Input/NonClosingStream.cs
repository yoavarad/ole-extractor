using System;
using System.IO;

namespace ExtractorOLE.Helpers.Input
{
    /// <summary>
    /// NPOIFSFileSystem(Stream) and b2xtranslator's StructuredStorageReader close the stream they are given;
    /// this wrapper lets them read the caller's stream (ADR-008, #194) without closing it.
    /// </summary>
    internal sealed class NonClosingStream : Stream
    {
        private readonly Stream _inner;

        public NonClosingStream(Stream inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => _inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override int Read(Span<byte> buffer) => _inner.Read(buffer);

        public override int ReadByte() => _inner.ReadByte();

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void Flush()
        {
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        // Dispose/Close not overridden: disposing the wrapper leaves inner open.
    }
}
