using NPOI.POIFS.FileSystem;

namespace ExtractorOLE.Tests.Encryption.Legacy
{
    // Mutates a root-level stream of a compound file for NPOI-read mutation tests.
    // Not suitable for b2xtranslator round trips: the rewritten file is NPOI's layout.
    internal static class CfbMutator
    {
        public static byte[] ReplaceStream(byte[] cfb, string streamName, Func<byte[], byte[]> edit)
        {
            using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
            var entry = (DocumentEntry)fs.Root.GetEntry(streamName);
            var current = new byte[entry.Size];
            using (var input = fs.Root.CreateDocumentInputStream(entry))
            {
                input.ReadFully(current);
            }

            fs.Root.CreateOrUpdateDocument(streamName, new MemoryStream(edit(current)));
            return Write(fs);
        }

        public static byte[] RemoveEntry(byte[] cfb, string name)
        {
            using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
            if (!fs.Root.GetEntry(name).Delete())
            {
                throw new InvalidOperationException($"Could not delete entry '{name}'.");
            }

            return Write(fs);
        }

        public static byte[] AddStream(byte[] cfb, string name, byte[] data)
        {
            using var fs = new NPOIFSFileSystem(new MemoryStream(cfb));
            fs.Root.CreateDocument(name, new MemoryStream(data));
            return Write(fs);
        }

        private static byte[] Write(NPOIFSFileSystem fs)
        {
            using var output = new MemoryStream();
            fs.WriteFileSystem(output);
            return output.ToArray();
        }
    }
}
