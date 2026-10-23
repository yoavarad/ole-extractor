using ExtractorOLE.DTOs;
using System;
using System.IO;

namespace ExtractorOLE.Helpers.MimeDetection
{
    /// <summary>
    /// Resolves a <see cref="MimeDetectionRequest"/> to a seekable stream (ADR-008). Owns only the
    /// stream it creates (MemoryStream view over FileBytes, or the FileStream for FilePath); a
    /// caller's InputStream is never disposed.
    /// </summary>
    internal sealed class MimeDetectionInput : IDisposable
    {
        private readonly bool _ownsStream;

        private MimeDetectionInput(Stream stream, bool ownsStream)
        {
            Stream = stream;
            _ownsStream = ownsStream;
        }

        public Stream Stream { get; }

        public long Length => Stream.Length;

        /// <summary>
        /// Validates the request's sources. Throws <see cref="ArgumentException"/>
        /// for more than one source or a non-seekable/unreadable InputStream.
        /// </summary>
        public static void Validate(MimeDetectionRequest request)
        {
            var sources = (request.FileBytes != null && request.FileBytes.Length > 0 ? 1 : 0)
                          + (request.InputStream != null ? 1 : 0)
                          + (request.FilePath != null ? 1 : 0);
            if (sources > 1)
            {
                throw new ArgumentException("Set only one of FileBytes, InputStream or FilePath.", nameof(request));
            }

            if (request.InputStream != null && (!request.InputStream.CanRead || !request.InputStream.CanSeek))
            {
                throw new ArgumentException(
                    "InputStream must be readable and seekable; spool non-seekable input to a temp file and pass FilePath (ADR-008).",
                    nameof(request));
            }

            if (request.FilePath != null && request.FilePath.Length == 0) throw new ArgumentException("FilePath is empty.", nameof(request));
        }

        /// <summary>Validates, then opens the content.</summary>
        public static MimeDetectionInput From(MimeDetectionRequest request)
        {
            Validate(request);

            if (request.InputStream != null)
            {
                return new MimeDetectionInput(request.InputStream, ownsStream: false);
            }

            if (request.FilePath != null)
            {
                return new MimeDetectionInput(
                    new FileStream(request.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read), ownsStream: true);
            }

            return new MimeDetectionInput(
                new MemoryStream(request.FileBytes ?? Array.Empty<byte>(), writable: false), ownsStream: true);
        }

        public void Dispose()
        {
            if (_ownsStream) Stream.Dispose();
        }
    }
}
