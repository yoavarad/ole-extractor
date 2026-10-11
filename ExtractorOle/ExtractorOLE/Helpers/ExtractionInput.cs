using ExtractorOLE.DTOs;
using System;
using System.IO;

namespace ExtractorOLE.Helpers
{
    /// <summary>
    /// The content of one <see cref="ExtractionRequest"/>, from whichever source it carries
    /// (FileBytes, InputStream or FilePath; ADR-008). Owns only the FileStream it opens for
    /// FilePath; a caller's InputStream is never disposed here. Dispose after the open strategy's
    /// ParsedDocument has been released, since an OOXML package reads the stream lazily.
    /// </summary>
    internal sealed class ExtractionInput : IDisposable
    {
        private readonly byte[]? _bytes;
        private readonly Stream? _stream;
        private readonly bool _ownsStream;
        private byte[]? _bridgeBytes;

        private ExtractionInput(byte[]? bytes, Stream? stream, bool ownsStream)
        {
            _bytes = bytes;
            _stream = stream;
            _ownsStream = ownsStream;
        }

        /// <summary>
        /// Validates the request's input source and opens it. Throws <see cref="ArgumentException"/>
        /// for more than one source or a non-seekable/unreadable InputStream.
        /// </summary>
        public static ExtractionInput From(ExtractionRequest request)
        {
            var sources = (request.FileBytes.Length > 0 ? 1 : 0)
                          + (request.InputStream != null ? 1 : 0)
                          + (request.FilePath != null ? 1 : 0);
            if (sources > 1)
            {
                throw new ArgumentException(
                    "Set only one of FileBytes, InputStream or FilePath.", nameof(request));
            }

            if (request.InputStream != null)
            {
                if (!request.InputStream.CanRead || !request.InputStream.CanSeek)
                {
                    throw new ArgumentException(
                        "InputStream must be readable and seekable; spool non-seekable input to a temp file and pass FilePath (ADR-008).",
                        nameof(request));
                }
                return new ExtractionInput(null, request.InputStream, ownsStream: false);
            }

            if (request.FilePath != null)
            {
                if (request.FilePath.Length == 0) throw new ArgumentException("FilePath is empty.", nameof(request));
                var file = new FileStream(request.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new ExtractionInput(null, file, ownsStream: true);
            }

            return new ExtractionInput(request.FileBytes, null, ownsStream: false);
        }

        public long Length => _bytes?.LongLength ?? _stream!.Length;

        /// <summary>The caller's byte[] when the request carried one; null for stream/path input.</summary>
        public byte[]? OriginalBytes => _bytes;

        /// <summary>The input stream rewound to position 0; null for byte[] input.</summary>
        public Stream? Stream
        {
            get
            {
                if (_stream == null) return null;
                _stream.Position = 0;
                return _stream;
            }
        }

        /// <summary>
        /// The whole content as a byte[], for consumers that still take byte[]: guardrails and
        /// preflight (#193), legacy CFB open strategies (#194) and ITextExtractor.ExtractText(byte[]).
        /// For stream input this reads the stream into a buffer that is kept until
        /// <see cref="ReleaseBytes"/>, so call that before the format library opens the stream.
        /// </summary>
        public byte[] Bytes => _bytes ?? (_bridgeBytes ??= ReadAll(Stream!));

        /// <summary>Drops the buffer <see cref="Bytes"/> made for stream input.</summary>
        public void ReleaseBytes() => _bridgeBytes = null;

        public void Dispose()
        {
            _bridgeBytes = null;
            if (_ownsStream) _stream!.Dispose();
        }

        internal static byte[] ReadAll(Stream stream)
        {
            if (stream.CanSeek)
            {
                var remaining = stream.Length - stream.Position;
                if (remaining > Array.MaxLength) throw new IOException("Input is too large to buffer as a byte array.");
                var buffer = new byte[remaining];
                stream.ReadExactly(buffer);
                return buffer;
            }

            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            return copy.ToArray();
        }
    }
}
