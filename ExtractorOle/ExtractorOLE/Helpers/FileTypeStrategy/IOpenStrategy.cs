using ExtractorOLE.DTOs;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    public interface IOpenStrategy
    {
        DocumentExtractionResult? Open(byte[] fileBytes);

        /// <summary>
        /// Opens the input from a seekable stream positioned at its start (ADR-008). The caller owns
        /// the stream and keeps it open until the returned ParsedDocument is disposed; the strategy
        /// never disposes it. The default reads the stream into a byte[] and calls
        /// <see cref="Open(byte[])"/>; strategies whose library reads a stream lazily override it
        /// (OOXML in #192, legacy CFB in #194).
        /// </summary>
        DocumentExtractionResult? Open(Stream input) => Open(ExtractionInput.ReadAll(input));
    }
}
