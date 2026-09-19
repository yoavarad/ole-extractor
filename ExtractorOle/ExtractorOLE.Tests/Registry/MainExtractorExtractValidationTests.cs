using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// Covers T-a2b91dc6: Extract(ExtractionRequest) rejects an unsupported DetectedMimeType
    /// ([ydk:error:extraction/unsupported-format]) and validates null arguments before any
    /// detection/extraction logic runs.
    /// </summary>
    public class MainExtractorExtractValidationTests
    {
        // Counts every call made through the proxied interface, delegating to the real instance.
        public class CallCountingProxy : DispatchProxy
        {
            public object Inner { get; set; } = null!;
            public int Calls { get; private set; }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                Calls++;
                return targetMethod!.Invoke(Inner, args);
            }
        }

        private sealed class Spied
        {
            public MainExtractor Extractor { get; init; } = null!;
            public CallCountingProxy Helper { get; init; } = null!;
            public CallCountingProxy Registry { get; init; } = null!;
            public int TotalCalls => Helper.Calls + Registry.Calls;
        }

        private static Spied BuildSpied()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();

            var helper = DispatchProxy.Create<IExtractionHelper, CallCountingProxy>();
            var helperProxy = (CallCountingProxy)(object)helper;
            helperProxy.Inner = provider.GetRequiredService<IExtractionHelper>();

            var registry = DispatchProxy.Create<IFormatDispatchRegistry, CallCountingProxy>();
            var registryProxy = (CallCountingProxy)(object)registry;
            registryProxy.Inner = provider.GetRequiredService<IFormatDispatchRegistry>();

            return new Spied
            {
                Extractor = new MainExtractor(helper, registry),
                Helper = helperProxy,
                Registry = registryProxy,
            };
        }

        private static ExtractionRequest ValidRequest(string mime) =>
            new() { FileBytes = new byte[] { 1, 2, 3 }, FileName = "f", DetectedMimeType = mime };

        [Theory]
        [InlineData("application/octet-stream")]
        [InlineData("text/plain")]
        [InlineData("application/vnd.openxmlformats-package")]
        [InlineData("application/pdf")]
        [InlineData("")]
        public void UnsupportedMimeType_ThrowsUnsupportedFormat(string mime)
        {
            var spied = BuildSpied();

            var ex = Assert.Throws<UnsupportedFormatException>(() => spied.Extractor.Extract(ValidRequest(mime)));

            Assert.Equal("UNSUPPORTED_FORMAT", ex.Code);
            Assert.Equal(415, ex.StatusCode);
            Assert.Equal(mime, ex.DetectedMimeType);
            Assert.Contains($"'{mime}'", ex.Detail);
            Assert.Equal(0, spied.Registry.Calls); // rejected before any open/extract dispatch
        }

        [Fact]
        public void NullRequest_ThrowsBeforeAnyLogic()
        {
            var spied = BuildSpied();

            var ex = Assert.Throws<ArgumentNullException>(() => spied.Extractor.Extract((ExtractionRequest)null!));

            Assert.Equal("request", ex.ParamName);
            Assert.Equal(0, spied.TotalCalls);
        }

        [Theory]
        [InlineData("FileBytes")]
        [InlineData("FileName")]
        [InlineData("DetectedMimeType")]
        public void NullRequiredField_ThrowsBeforeAnyLogic(string field)
        {
            var spied = BuildSpied();
            var request = ValidRequest("application/octet-stream");
            switch (field)
            {
                case "FileBytes": request.FileBytes = null!; break;
                case "FileName": request.FileName = null!; break;
                case "DetectedMimeType": request.DetectedMimeType = null!; break;
            }

            var ex = Assert.Throws<ArgumentNullException>(() => spied.Extractor.Extract(request));

            Assert.Equal(field, ex.ParamName);
            Assert.Equal(0, spied.TotalCalls);
        }
    }
}
