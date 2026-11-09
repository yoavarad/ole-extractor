using ExtractorOLE;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ExtractorOLE.Tests
{
    public class ServiceRegistrationTests
    {
        [Fact]
        public void Register_WithLimits_RegistersThatInstance()
        {
            var limits = new MimeDetectionLimits { MaxFileSizeBytes = 42 };
            var services = new ServiceCollection();
            ServiceRegistration.Register(services, limits);
            using var provider = services.BuildServiceProvider();

            Assert.Same(limits, provider.GetRequiredService<MimeDetectionLimits>());
        }

        [Fact]
        public void Register_WithoutLimits_RegistersDefaults()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();

            Assert.Equal(104_857_600, provider.GetRequiredService<MimeDetectionLimits>().MaxFileSizeBytes);
        }

        [Fact]
        public void Register_WithHostLogger_GuardrailRejectionsLogThroughIt()
        {
            var logger = new CapturingLogger<ExtractionGuardrails>();
            var services = new ServiceCollection();
            services.AddSingleton<ILogger<ExtractionGuardrails>>(logger);
            ServiceRegistration.Register(services, new MimeDetectionLimits { MaxFileSizeBytes = 10 });
            using var provider = services.BuildServiceProvider();

            Assert.Throws<FileTooLargeException>(() => provider.GetRequiredService<MainExtractor>().Extract(new ExtractionRequest
            {
                FileName = "x.docx",
                FileBytes = new byte[11],
                DetectedMimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            }));

            Assert.Contains("file-too-large", Assert.Single(logger.Messages), System.StringComparison.Ordinal);
        }

        [Fact]
        public void Register_WithoutLogging_ResolvesGuardrails()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();

            Assert.NotNull(provider.GetRequiredService<ExtractionGuardrails>());
        }

        [Fact]
        public void Register_DispatchDictionaries_AreNotResolvableAsMutable()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            using var provider = services.BuildServiceProvider();

            Assert.Null(provider.GetService<System.Collections.Generic.IDictionary<ExtractorOLE.DTOs.OfficeMimeType, ExtractorOLE.Helpers.FileTypeStrategy.IOpenStrategy>>());
            Assert.Null(provider.GetService<System.Collections.Generic.IDictionary<ExtractorOLE.DTOs.OfficeMimeType, ExtractorOLE.Abstractions.ITextExtractor>>());
            Assert.IsNotType<System.Collections.Generic.Dictionary<ExtractorOLE.DTOs.OfficeMimeType, ExtractorOLE.Helpers.FileTypeStrategy.IOpenStrategy>>(
                provider.GetRequiredService<System.Collections.Generic.IReadOnlyDictionary<ExtractorOLE.DTOs.OfficeMimeType, ExtractorOLE.Helpers.FileTypeStrategy.IOpenStrategy>>());
        }
    }
}
