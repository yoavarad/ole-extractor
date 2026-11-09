using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.Guardrails;
using ExtractorOLE.Helpers.Input;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    public class ParseFailureTests
    {
        public static TheoryData<Exception> NotParseFailures() => new()
        {
            new InsufficientMemoryException(),
            new ObjectDisposedException("s"),
            new OperationCanceledException(),
            new InsufficientExecutionStackException(),
            new PasswordProtectedException(),
            new InvalidOperationException("wrapped", new ObjectDisposedException("s")),
        };

        public static TheoryData<Exception> ParseFailures() => new()
        {
            new IOException("Invalid header signature"),
            new InvalidDataException("bad zip"),
            new FormatException("bad"),
            new ArgumentOutOfRangeException("offset"),
        };

        [Theory]
        [MemberData(nameof(NotParseFailures))]
        public void NonParseExceptions_AnywhereInTheChain_AreNotParseFailures(Exception ex)
        {
            Assert.False(ParseFailure.IsParseFailure(ex));
        }

        [Theory]
        [MemberData(nameof(ParseFailures))]
        public void LibraryExceptions_WithoutStreamFault_AreParseFailures(Exception ex)
        {
            Assert.True(ParseFailure.IsParseFailure(ex));
        }

        [Fact]
        public void AnyException_AfterASourceReadFault_IsNotAParseFailure()
        {
            var source = new FaultTrackingStream(new ThrowingReadStream());
            Assert.Throws<IOException>(() => source.ReadByte());

            Assert.False(ParseFailure.IsParseFailure(new FormatException("replaced by the library"), source));
        }

        private sealed class ThrowingReadStream : MemoryStream
        {
            public override int ReadByte() => throw new IOException("disk gone");
        }
    }
}
