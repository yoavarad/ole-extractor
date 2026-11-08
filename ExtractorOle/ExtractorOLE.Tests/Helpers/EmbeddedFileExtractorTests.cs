using ExtractorOLE.Helpers;
using System.IO;
using System.Reflection;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    // #277: buffers must grow with bytes actually read, never from a declared (untrusted) length.
    public class EmbeddedFileExtractorTests
    {
        private sealed class LyingLengthStream : MemoryStream
        {
            public LyingLengthStream(byte[] data) : base(data) { }
            public override long Length => int.MaxValue;
        }

        [Fact]
        public void ReadAllBytes_IgnoresDeclaredLength()
        {
            var method = typeof(EmbeddedFileExtractor).GetMethod("ReadAllBytes", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            var data = new byte[] { 1, 2, 3, 4, 5 };

            var result = (byte[]?)method!.Invoke(null, new object[] { new LyingLengthStream(data) });

            Assert.Equal(data, result);
        }
    }
}
