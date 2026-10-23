using ExtractorOLE.Helpers.Input;
using NPOI.POIFS.FileSystem;
using NPOI.POIFS.NIO;
using System.IO;

namespace ExtractorOLE.Helpers.FileTypeStrategy
{
    // Opens the NPOI compound file for the legacy (xls/doc/ppt) strategies.
    internal static class CfbFileSystems
    {
        // Stream/path input reaches the strategies wrapped in NonClosingStream (#194). NPOI reads it
        // block by block on demand and never closes it, so the input is not buffered (#236).
        // Byte[] input arrives as a MemoryStream, which NPOI buffers as before.
        internal static NPOIFSFileSystem Open(Stream source) =>
            source is NonClosingStream
                ? new NPOIFSFileSystem(new StreamBackedDataSource(source))
                : new NPOIFSFileSystem(source);
    }
}
