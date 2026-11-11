using System.Reflection;
using ExtractorOLE.Exceptions;
using Xunit;

namespace ExtractorOLE.Tests.Exceptions
{
    public class InvalidPasswordExceptionTests
    {
        [Fact]
        public void Exception_HasCodeStatusAndFixedDetail()
        {
            var ex = new InvalidPasswordException();

            Assert.IsAssignableFrom<ExtractorOleException>(ex);
            Assert.Equal("INVALID_PASSWORD", ex.Code);
            Assert.Equal(422, ex.StatusCode);
            Assert.Equal("The supplied password does not unlock this encrypted file.", ex.Detail);
            Assert.Equal(ex.Detail, ex.Message);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public void Exception_HasOnlyParameterlessConstructor()
        {
            var ctors = typeof(InvalidPasswordException).GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var ctor = Assert.Single(ctors);
            Assert.Empty(ctor.GetParameters());
        }
    }
}
