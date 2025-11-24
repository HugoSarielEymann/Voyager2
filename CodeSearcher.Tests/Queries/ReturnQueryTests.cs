using CodeSearcher.Core;
using CodeSearcher.Tests.Fixtures;
using Xunit;

namespace CodeSearcher.Tests.Queries
{
    public class ReturnQueryTests
    {
        [Fact]
        public void FindReturns_WithReturnsInCode_ReturnsAllReturnStatements()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var results = context.FindReturns().Execute().ToList();

            // Assert
            Assert.NotEmpty(results);
            // ProcessData has 2 returns, CountAsync has 1 return, NoReturn has 0
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void InMethod_SpecificMethod_ReturnsOnlyFromThatMethod()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var results = context.FindReturns()
                .InMethod("ProcessData")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void ReturningNull_NullReturns_ReturnsNullStatements()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var results = context.FindReturns()
                .ReturningNull()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void InMethod_NonExistentMethod_ReturnsEmpty()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var results = context.FindReturns()
                .InMethod("NonExistentMethod")
                .Execute()
                .ToList();

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public void WithExpression_CustomPredicate_ReturnsMatchingReturns()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var results = context.FindReturns()
                .WithExpression(expr => expr.ToString().Contains("ToUpper"))
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void Count_ReturnsCorrectCount()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var count = context.FindReturns().Count();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void ChainedQueries_MultipleConditions_ReturnsCorrectReturns()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var results = context.FindReturns()
                .InMethod("ProcessData")
                .ReturningNull()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void FirstOrDefault_WithResults_ReturnsFirst()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act
            var result = context.FindReturns()
                .InMethod("ProcessData")
                .FirstOrDefault();

            // Assert
            Assert.NotNull(result);
        }
    }
}
