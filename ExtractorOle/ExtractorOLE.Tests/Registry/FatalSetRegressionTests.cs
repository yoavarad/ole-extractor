using ExtractorOLE.Abstractions;
using ExtractorOLE.Configuration;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExtractorOLE.Tests.Registry
{
    /// <summary>
    /// #311 regression guards for the fatal set ([ydk:req:extraction/per-field-error-isolation]):
    /// per-field isolation must not swallow a fatal condition, and a field-errors-only item
    /// is a batch success (ADR-007).
    /// </summary>
    public class FatalSetRegressionTests
    {
        private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string DocMime = "application/msword";

        // A fault that is not a parse failure (ParseFailure.IsParseFailure is false) must surface
        // as-is, never as an empty result or a field error.
        private sealed class ThrowingOpenStrategy : IOpenStrategy
        {
            public DocumentExtractionResult? Open(byte[] fileBytes) => throw new ObjectDisposedException("injected");
        }

        private sealed class InjectingRegistry : IFormatDispatchRegistry
        {
            public IOpenStrategy? GetOpenStrategy(OfficeMimeType format) => new ThrowingOpenStrategy();

            public ITextExtractor? GetTextExtractor(OfficeMimeType format) => null;
        }

        private static byte[] Sample(string fileName)
        {
            var relative = Path.Combine("samples", "adversarial", fileName);
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(candidate)) return File.ReadAllBytes(candidate);
                dir = dir.Parent;
            }
            throw new FileNotFoundException(relative);
        }

        private static MainExtractor Real(MimeDetectionLimits? limits = null)
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();
            var real = provider.GetRequiredService<MainExtractor>();
            if (limits == null) return real;
            return new MainExtractor(provider.GetRequiredService<IExtractionHelper>(),
                provider.GetRequiredService<IFormatDispatchRegistry>(), limits);
        }

        private static MainExtractor WithInjectedStrategy()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();
            return new MainExtractor(provider.GetRequiredService<IExtractionHelper>(), new InjectingRegistry());
        }

        private static ExtractionRequest Request(byte[] bytes, string mime, string name = "fixture") =>
            new() { FileBytes = bytes, FileName = name, DetectedMimeType = mime };

        // name, extractor factory, request factory, exact expected exception type
        public static TheoryData<string, Func<MainExtractor>, Func<ExtractionRequest?>, Type> FatalCases() => new()
        {
            { "file-too-large", () => Real(new MimeDetectionLimits { MaxFileSizeBytes = 10 }), () => Request(new byte[11], DocxMime), typeof(FileTooLargeException) },
            { "password-protected", () => Real(), () => Request(EncryptedSamples.ReadBytes("password-protected.docx"), DocxMime), typeof(PasswordProtectedException) },
            { "unsupported-format", () => Real(), () => Request(new byte[] { 1, 2, 3 }, "text/plain"), typeof(UnsupportedFormatException) },
            { "truncated-container", () => Real(), () => Request(Sample("truncated-container.docx"), DocxMime), typeof(TruncatedContainerException) },
            { "zero-byte", () => Real(), () => Request(Sample("zero-byte.docx"), DocxMime), typeof(TruncatedContainerException) },
            { "corrupt-container", () => Real(), () => Request(Sample("corrupt-container.docx"), DocxMime), typeof(CorruptFileException) },
            { "declared-size-bomb", () => Real(), () => Request(Sample("oversized-declared-size-bomb.doc"), DocMime), typeof(OversizedNestedContentException) },
            { "null-request", () => Real(), () => null, typeof(ArgumentNullException) },
            { "null-field", () => Real(), () => new ExtractionRequest { FileBytes = null!, FileName = "x", DetectedMimeType = DocxMime }, typeof(ArgumentNullException) },
            { "undeclared-open-exception", () => WithInjectedStrategy(), () => Request(Sample("corrupt-embedded-object.docx"), DocxMime), typeof(ObjectDisposedException) },
        };

        [Theory]
        [MemberData(nameof(FatalCases))]
        public void FatalCase_ThrowsDeclaredException_AndYieldsNoResult(
            string name, Func<MainExtractor> extractor, Func<ExtractionRequest?> request, Type expected)
        {
            // Assert.Throws fails on any return, so a thrown exception means no result.
            var ex = Assert.ThrowsAny<Exception>(() => extractor().Extract(request()!));

            Assert.True(expected == ex.GetType(), $"{name}: expected {expected.Name}, got {ex.GetType().Name}");
        }

        [Fact]
        public async Task ExtractMany_CorruptEmbeddedObject_IsSucceededWithFieldErrorsAndNullError()
        {
            var items = await Real().ExtractManyAsync(new[] { Request(Sample("corrupt-embedded-object.docx"), DocxMime, "corrupt-embedded-object.docx") });

            var item = Assert.Single(items);
            Assert.True(item.Succeeded);
            Assert.Null(item.Error);
            Assert.NotEmpty(item.Result!.FieldErrors);
        }
    }
}
