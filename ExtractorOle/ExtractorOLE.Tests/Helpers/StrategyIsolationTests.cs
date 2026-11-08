using ExtractorOLE.DTOs;
using ExtractorOLE.Exceptions;
using ExtractorOLE.Helpers.FileTypeStrategy;
using System;
using System.IO;
using Xunit;

namespace ExtractorOLE.Tests.Helpers
{
    public class StrategyIsolationTests
    {
        [Fact]
        public void FormatMetadataPath_PrefixesLeaf() =>
            Assert.Equal("FormatMetadata.SheetCount", StrategyIsolation.FormatMetadataPath("SheetCount"));

        [Fact]
        public void PopulateLeaf_Failure_RecordsLeafPathOnly()
        {
            var result = new DocumentExtractionResult();

            Assert.False(StrategyIsolation.PopulateLeaf(result, "SheetCount", () => throw new InvalidDataException("bad")));

            var error = Assert.Single(result.FieldErrors);
            Assert.Equal("FormatMetadata.SheetCount", error.FieldPath);
            Assert.Equal(FieldErrorCode.CorruptFile, error.Code);
        }

        [Fact]
        public void PopulateEmbedded_Failure_RecordsBareEmbeddedFiles()
        {
            var result = new DocumentExtractionResult();

            Assert.False(StrategyIsolation.PopulateEmbedded(result, () => throw new InvalidOperationException("boom")));

            var error = Assert.Single(result.FieldErrors);
            Assert.Equal("EmbeddedFiles", error.FieldPath);
            Assert.Equal(FieldErrorCode.UnexpectedFieldFailure, error.Code);
        }

        [Fact]
        public void IsOpenFailure_DeclaredTypes_True()
        {
            Assert.True(StrategyIsolation.IsOpenFailure(new InvalidDataException("x")));
            Assert.True(StrategyIsolation.IsOpenFailure(new IOException("x")));
            Assert.True(StrategyIsolation.IsOpenFailure(new ArgumentException("x")));
            Assert.True(StrategyIsolation.IsOpenFailure(new CorruptFileException("application/msword", "x")));
        }

        [Fact]
        public void IsOpenFailure_UndeclaredTypes_False()
        {
            Assert.False(StrategyIsolation.IsOpenFailure(new InvalidOperationException("x")));
            Assert.False(StrategyIsolation.IsOpenFailure(new DivideByZeroException("x")));
            Assert.False(StrategyIsolation.IsOpenFailure(new NotSupportedException("x")));
        }
    }
}
