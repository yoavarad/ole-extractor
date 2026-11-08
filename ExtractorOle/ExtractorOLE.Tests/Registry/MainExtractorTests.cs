using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    public class MainExtractorTests
    {
        [Fact]
        public void Extract_UnsupportedMime_ThrowsWithCappedEcho()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var extractor = services.BuildServiceProvider().GetRequiredService<MainExtractor>();
            var ex = Assert.Throws<UnsupportedFormatException>(() => extractor.Extract(
                new ExtractionRequest { FileBytes = new byte[] { 1, 2, 3 }, FileName = "a.bin", DetectedMimeType = new string('y', 3000) }));
            Assert.True(ex.DetectedMimeType.Length <= 100);
        }
    }
}
