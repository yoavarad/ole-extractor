using System.Diagnostics;
using System.Xml;
using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    // #303: FieldError DTO, additive FieldErrors, and the leaf-isolation primitive.
    public class FieldIsolationTests
    {
        private const string SecretDetail = "secret-library-detail";
        private static readonly string[] OrderedPaths = { "Metadata.Title", "EmbeddedFiles", "EmbeddedFiles" };

        public static TheoryData<Exception, FieldErrorCode> CausesAndCodes => new()
        {
            { new CorruptFileException("application/msword", SecretDetail), FieldErrorCode.CorruptFile },
            { new InvalidDataException(SecretDetail), FieldErrorCode.CorruptFile },
            { new EndOfStreamException(SecretDetail), FieldErrorCode.CorruptFile },
            { new FormatException(SecretDetail), FieldErrorCode.CorruptFile },
            { new XmlException(SecretDetail), FieldErrorCode.CorruptFile },
            { new NPOI.Util.RecordFormatException(SecretDetail), FieldErrorCode.CorruptFile },
            { new OversizedNestedContentException(SecretDetail), FieldErrorCode.OversizedNestedContent },
            { new InvalidOperationException(SecretDetail), FieldErrorCode.UnexpectedFieldFailure },
            { new NotSupportedException(SecretDetail), FieldErrorCode.UnexpectedFieldFailure },
        };

        [Fact]
        public void TryPopulate_Success_ReturnsTrueAndRecordsNothing()
        {
            var result = new DocumentExtractionResult();

            var ok = FieldIsolation.TryPopulate(result, "Metadata.Title", () => result.Metadata.Title = "t");

            Assert.True(ok);
            Assert.Equal("t", result.Metadata.Title);
            Assert.Empty(result.FieldErrors);
        }

        [Theory]
        [MemberData(nameof(CausesAndCodes))]
        public void TryPopulate_Throw_RecordsCodeByCause(Exception thrown, FieldErrorCode expected)
        {
            var result = new DocumentExtractionResult();

            var ok = FieldIsolation.TryPopulate(result, "Metadata.Created", () => throw thrown);

            Assert.False(ok);
            Assert.Null(result.Metadata.Created);
            var error = Assert.Single(result.FieldErrors);
            Assert.Equal("Metadata.Created", error.FieldPath);
            Assert.Equal(expected, error.Code);
        }

        [Theory]
        [MemberData(nameof(CausesAndCodes))]
        public void TryPopulate_Message_CarriesNoExceptionTypeMessageOrStack(Exception thrown, FieldErrorCode _)
        {
            var result = new DocumentExtractionResult();
            Exception? caught = null;

            FieldIsolation.TryPopulate(result, "ExtractedText", () =>
            {
                try { throw thrown; }
                catch (Exception ex) { caught = ex; throw; }
            });

            var message = Assert.Single(result.FieldErrors).Message;
            Assert.Contains("ExtractedText", message, StringComparison.Ordinal);
            Assert.DoesNotContain(SecretDetail, message, StringComparison.Ordinal);
            Assert.DoesNotContain(thrown.GetType().Name, message, StringComparison.Ordinal);
            Assert.DoesNotContain(" at ", message, StringComparison.Ordinal);
            Assert.NotNull(caught!.StackTrace);
        }

        [Fact]
        public void TryPopulate_SamePathTwice_RecordsOneEntry()
        {
            var result = new DocumentExtractionResult();

            FieldIsolation.TryPopulate(result, "Metadata.Title", () => throw new FormatException());
            FieldIsolation.TryPopulate(result, "Metadata.Title", () => throw new InvalidOperationException());

            // First failure on a path wins; a later one on the same path is not appended.
            var error = Assert.Single(result.FieldErrors);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
        }

        [Fact]
        public void TryPopulate_BareEmbeddedFiles_MayRepeat_InPopulationOrder()
        {
            var result = new DocumentExtractionResult();

            FieldIsolation.TryPopulate(result, "Metadata.Title", () => throw new FormatException());
            FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath(null), () => throw new InvalidDataException());
            FieldIsolation.TryPopulate(result, FieldIsolation.SubfilePath(""), () => throw new InvalidOperationException());

            Assert.Equal(OrderedPaths, result.FieldErrors.Select(e => e.FieldPath));
        }

        [Fact]
        public void SubfilePath_KeysByPackagePath()
        {
            Assert.Equal("EmbeddedFiles[word/embeddings/oleObject1.bin]", FieldIsolation.SubfilePath("word/embeddings/oleObject1.bin"));
            Assert.Equal("EmbeddedFiles", FieldIsolation.SubfilePath(null));
            Assert.Equal("EmbeddedFiles", FieldIsolation.SubfilePath(string.Empty));
        }

        [Fact]
        public void TryPopulate_OutOfMemory_IsNotCaught()
        {
            var result = new DocumentExtractionResult();

            // InsufficientMemoryException derives from OutOfMemoryException (which CA2201 reserves).
            Assert.Throws<InsufficientMemoryException>(() =>
                FieldIsolation.TryPopulate(result, "ExtractedText", () => throw new InsufficientMemoryException()));
            Assert.Empty(result.FieldErrors);
        }

        [Fact]
        public void TryPopulate_Cancellation_IsNotCaught()
        {
            var result = new DocumentExtractionResult();

            Assert.Throws<OperationCanceledException>(() =>
                FieldIsolation.TryPopulate(result, "ExtractedText", () => throw new OperationCanceledException()));
            Assert.Empty(result.FieldErrors);
        }

        [Fact]
        public void TryPopulate_WrappedOutOfMemory_PropagatesAndRecordsNothing()
        {
            var result = new DocumentExtractionResult();

            Assert.ThrowsAny<InvalidOperationException>(() =>
                FieldIsolation.TryPopulate(result, "ExtractedText", () =>
                    throw new InvalidOperationException("x", new InsufficientMemoryException())));
            Assert.Empty(result.FieldErrors);
        }

        [Fact]
        public void TryPopulate_AggregateWithCancellation_PropagatesAndRecordsNothing()
        {
            var result = new DocumentExtractionResult();

            Assert.ThrowsAny<AggregateException>(() =>
                FieldIsolation.TryPopulate(result, "ExtractedText", () =>
                    throw new AggregateException(new InvalidOperationException(), new OperationCanceledException())));
            Assert.Empty(result.FieldErrors);
        }

        [Fact]
        public void IsIsolatable_PlainWrappedChain_IsTrue()
        {
            var wrapped = new InvalidOperationException("outer", new InvalidDataException("inner"));

            Assert.True(FieldIsolation.IsIsolatable(wrapped));
        }

        [Fact]
        public void TryPopulate_SendsExceptionToTelemetry()
        {
            var result = new DocumentExtractionResult();
            var thrown = new XmlException(SecretDetail);
            using var activity = new Activity("field-isolation-test").Start();

            FieldIsolation.TryPopulate(result, "FormatMetadata.PageCount", () => throw thrown);

            var evt = Assert.Single(activity.Events, e => e.Name == "exception");
            var tags = evt.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.Equal(typeof(XmlException).FullName, tags["exception.type"]);
            Assert.Equal("FormatMetadata.PageCount", tags["field.path"]);
            Assert.Equal(nameof(FieldErrorCode.CorruptFile), tags["field.code"]);
        }
    }
}
