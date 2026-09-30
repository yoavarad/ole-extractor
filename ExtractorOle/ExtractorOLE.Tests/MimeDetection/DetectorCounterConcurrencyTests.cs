using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using ExtractorOLE.DTOs;
using ExtractorOLE.Helpers.MimeDetection;
using Xunit;

namespace ExtractorOLE.Tests.MimeDetection
{
    public class DetectorCounterConcurrencyTests
    {
        private static byte[] MinimalZip()
        {
            using var stream = new MemoryStream();
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry("x.txt");
                using var s = entry.Open();
                s.WriteByte(1);
            }
            return stream.ToArray();
        }

        private static int RunDetections(int n)
        {
            var detector = new OoxmlMimeDetector();
            var request = new MimeDetectionRequest { FileBytes = MinimalZip(), FileName = "a.docx" };
            OoxmlMimeDetector.PackageOpenAttemptCount = 0;
            Parallel.For(0, n, _ => detector.Detect(request));
            return OoxmlMimeDetector.PackageOpenAttemptCount;
        }

        [Fact]
        public void ParallelDetect_CounterIsExact()
        {
            Assert.Equal(2000, RunDetections(2000));
        }

        [Fact]
        public async Task ConcurrentScopes_DoNotSeeEachOthersCounts()
        {
            var a = Task.Run(() => RunDetections(500));
            var b = Task.Run(() => RunDetections(700));
            Assert.Equal(500, await a);
            Assert.Equal(700, await b);
        }
    }
}
