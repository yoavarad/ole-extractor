using b2xtranslator.StructuredStorage.Common;
using ExtractorOLE.Abstractions;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Handlers;
using ExtractorOLE.Helpers;
using ExtractorOLE.Helpers.FileTypeStrategy;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using ExtractorOLE.Registry;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System;
using Xunit;

namespace ExtractorOLE.Tests.Encryption.Legacy
{
    // Defence in depth ([ydk:error:extraction/password-protected]): with the legacy-encryption guard
    // bypassed, b2xtranslator's typed EncryptedDocumentException is a password outcome, never a FieldError.
    [Collection("ConsoleRedirect")]
    public class B2xEncryptedDocumentTests
    {
        private const string PptMime = "application/vnd.ms-powerpoint";

        [Theory]
        [InlineData(LegacyFixtures.CryptoApiProc2356Ppt)]
        [InlineData(LegacyFixtures.PngEncryptedPpt)]
        public void EncryptedPpt_GuardBypassed_OpenOrThrowReportsPasswordProtected(string fixture)
        {
            var strategy = new PptOpenStrategy(new ExtractionHelper());

            Assert.Throws<PasswordProtectedException>(() =>
                ParseTimeErrorGuard.OpenOrThrow(strategy, LegacyFixtures.Bytes(fixture), PptMime));
        }

        [Fact]
        public void EncryptedPpt_GuardBypassed_WithPassword_ReportsInvalidPassword()
        {
            var strategy = new PptOpenStrategy(new ExtractionHelper());
            using var input = ExtractionInput.FromPlaintext(LegacyFixtures.Bytes(LegacyFixtures.CryptoApiProc2356Ppt));

            Assert.Throws<InvalidPasswordException>(() =>
                ParseTimeErrorGuard.OpenOrThrow(strategy, input, PptMime, "secret"));
        }

        [Theory]
        [InlineData(LegacyFixtures.CryptoApiProc2356Ppt)]
        [InlineData(LegacyFixtures.PngEncryptedPpt)]
        public void EncryptedPpt_PptToPptxConverter_ThrowsB2xEncryptedDocument(string fixture)
        {
            using var stream = new System.IO.MemoryStream(LegacyFixtures.Bytes(fixture));

            Assert.Throws<EncryptedDocumentException>(() => PptToPptxConverter.Convert(stream));
        }

        [Theory]
        [InlineData(LegacyFixtures.PasswordCryptoApiDoc)]
        [InlineData(LegacyFixtures.Rc4CryptoApiDoc)]
        [InlineData(LegacyFixtures.PasswordProtectedDoc)]
        public void EncryptedDoc_DocTextExtractor_ThrowsB2xEncryptedDocument(string fixture)
        {
            Assert.Throws<EncryptedDocumentException>(() =>
                new DocTextExtractor().ExtractText(LegacyFixtures.Bytes(fixture)));
        }

        [Fact]
        public void B2xEncryptedDocument_IsNotIsolatedIntoFieldError()
        {
            var result = new DocumentExtractionResult();

            Assert.False(FieldIsolation.IsIsolatable(new EncryptedDocumentException("x")));
            Assert.Throws<EncryptedDocumentException>(() =>
                FieldIsolation.TryPopulate(result, "ExtractedText", () => throw new EncryptedDocumentException("x")));
            Assert.Empty(result.FieldErrors);
            Assert.False(StrategyIsolation.IsOpenFailure(new EncryptedDocumentException("x")));
        }

        private sealed class EncryptedThrowingExtractor : ITextExtractor
        {
            public string ExtractText(byte[] fileBytes) => throw new EncryptedDocumentException("x");
        }

        private sealed class InjectingRegistry : IFormatDispatchRegistry
        {
            private readonly IFormatDispatchRegistry _real;
            public InjectingRegistry(IFormatDispatchRegistry real) => _real = real;
            public IOpenStrategy? GetOpenStrategy(OfficeMimeType format) => _real.GetOpenStrategy(format);
            public ITextExtractor? GetTextExtractor(OfficeMimeType format) => new EncryptedThrowingExtractor();
        }

        [Fact]
        public void B2xEncryptedDocumentFromTextExtraction_ExtractThrowsPasswordProtected_NotFieldError()
        {
            var services = new ServiceCollection();
            ServiceRegistration.Register(services);
            var provider = services.BuildServiceProvider();
            var extractor = new MainExtractor(provider.GetRequiredService<IExtractionHelper>(),
                new InjectingRegistry(provider.GetRequiredService<IFormatDispatchRegistry>()));
            var plainDoc = File.ReadAllBytes(EncryptedSamples.FindRepoRootFile(LegacyFixtures.PlainTwinDocPath));

            Assert.Throws<PasswordProtectedException>(() => extractor.Extract(
                new ExtractionRequest { FileBytes = plainDoc, FileName = "fixture", DetectedMimeType = "application/msword" }));
        }
    }
}
