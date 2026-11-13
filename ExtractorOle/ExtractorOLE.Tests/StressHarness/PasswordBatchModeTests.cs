using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.StressHarness;
using Xunit;

namespace ExtractorOLE.Tests.StressHarness
{
    public class PasswordBatchModeTests
    {
        private static PasswordBatchMode.PasswordCase Ok(string text) => new("ok", [], null, text, null);

        private static PasswordBatchMode.PasswordCase Err(string error) => new("err", [], null, null, error);

        private static BatchExtractionResult Success(string text) =>
            new() { Result = new DocumentExtractionResult { ExtractedText = text } };

        private static BatchExtractionResult Failure(Exception ex) => new() { Error = ex };

        [Fact]
        public void Matches_SuccessWithExpectedText_True() => Assert.True(PasswordBatchMode.Matches(Ok("t"), Success("t")));

        [Fact]
        public void Matches_SuccessWithOtherText_False() => Assert.False(PasswordBatchMode.Matches(Ok("t"), Success("x")));

        [Fact]
        public void Matches_SuccessWhereErrorExpected_False() =>
            Assert.False(PasswordBatchMode.Matches(Err(nameof(InvalidPasswordException)), Success("t")));

        [Fact]
        public void Matches_ExpectedErrorType_True() =>
            Assert.True(PasswordBatchMode.Matches(Err(nameof(InvalidPasswordException)), Failure(new InvalidPasswordException())));

        [Fact]
        public void Matches_WrongErrorType_False() =>
            Assert.False(PasswordBatchMode.Matches(Err(nameof(InvalidPasswordException)), Failure(new PasswordProtectedException())));

        [Fact]
        public void Matches_FailureWhereTextExpected_False() =>
            Assert.False(PasswordBatchMode.Matches(Ok("t"), Failure(new PasswordProtectedException())));

        [Fact]
        public void BuildCases_RepoSamples_CoversCorrectWrongAndNoPasswordPerProtectedSample()
        {
            var dir = Program.ResolveSamplesDirectory(null)!;

            var cases = PasswordBatchMode.BuildCases(dir, _ => "twin", crossWire: false);

            Assert.Contains(cases, c => c.Label.EndsWith("+ correct password", StringComparison.Ordinal) && c.ExpectedText == "twin");
            Assert.Contains(cases, c => c.ExpectedError == nameof(InvalidPasswordException));
            Assert.Contains(cases, c => c.ExpectedError == nameof(PasswordProtectedException));
            Assert.Contains(cases, c => c.Label.Contains("(default key)", StringComparison.Ordinal));
        }

        [Fact]
        public void BuildCases_CrossWire_SwapsCorrectPasswordOnly()
        {
            var dir = Program.ResolveSamplesDirectory(null)!;

            var normal = PasswordBatchMode.BuildCases(dir, _ => "twin", crossWire: false).Where(c => c.Label.EndsWith("+ correct password", StringComparison.Ordinal)).ToList();
            var wired = PasswordBatchMode.BuildCases(dir, _ => "twin", crossWire: true).Where(c => c.Label.EndsWith("+ correct password", StringComparison.Ordinal)).ToList();

            Assert.Equal(normal.Count, wired.Count);
            Assert.All(normal.Zip(wired), p => Assert.NotEqual(p.First.Password, p.Second.Password));
        }
    }
}
