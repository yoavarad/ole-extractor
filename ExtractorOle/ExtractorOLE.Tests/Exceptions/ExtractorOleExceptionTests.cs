using ExtractorOLE.Exceptions;
using Xunit;

namespace ExtractorOLE.Tests.Exceptions
{
    public class ExtractorOleExceptionTests
    {
        private sealed class TestException : ExtractorOleException
        {
            public TestException(string message, Exception? inner) : base(message, inner) { }
            public override string Code => "TEST";
            public override int StatusCode => 500;
        }

        [Fact]
        public void MessageAndInnerConstructor_SetsBoth_AndDetailIsMessage()
        {
            var cause = new IOException("cause");

            var ex = new TestException("failed", cause);

            Assert.Same(cause, ex.InnerException);
            Assert.Equal("failed", ex.Message);
            Assert.Equal("failed", ex.Detail);
        }
    }
}
