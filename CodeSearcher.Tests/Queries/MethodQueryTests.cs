using CodeSearcher.Core;
using CodeSearcher.Tests.Fixtures;
using Xunit;

namespace CodeSearcher.Tests.Queries
{
    public class MethodQueryTests
    {
        [Fact]
        public void FindMethods_SimpleClass_ReturnsAllMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.SimpleClass);

            // Act
            var results = context.FindMethods().Execute().ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal(3, results.Count);
        }

        [Fact]
        public void WithName_SpecificMethod_ReturnsSingleMethod()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.SimpleClass);

            // Act
            var result = context.FindMethods()
                .WithName("GetName")
                .FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("GetName", result.Identifier.Text);
        }

        [Fact]
        public void ReturningTask_AsyncMethods_ReturnsOnlyTaskMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .ReturningTask()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, m => Assert.True(m.ReturnType.ToString().Contains("Task")));
        }

        [Fact]
        public void IsAsync_AsyncMethods_ReturnsOnlyAsyncMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .IsAsync()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.True(results.Count >= 1);
            Assert.All(results, m => Assert.True(m.Modifiers.Any(mod => mod.Text == "async")));
        }

        [Fact]
        public void IsPublic_PublicMethods_ReturnsOnlyPublicMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .IsPublic()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, m => Assert.True(m.Modifiers.Any(mod => mod.Text == "public")));
        }

        [Fact]
        public void IsPrivate_PrivateMethods_ReturnsOnlyPrivateMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .IsPrivate()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void WithNameContaining_PartialName_ReturnsMatchingMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .WithNameContaining("Async")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, m => Assert.Contains("Async", m.Identifier.Text));
        }

        [Fact]
        public void HasParameterCount_MethodsWithParams_ReturnsCorrectMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .HasParameterCount(1)
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, m => Assert.Single(m.ParameterList.Parameters));
        }

        [Fact]
        public void WithAttribute_MethodsWithObsolete_ReturnsAttributedMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .WithAttribute("Obsolete")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void ChainedQueries_MultipleConditions_ReturnsCorrectMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .IsPublic()
                .ReturningTask()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, m =>
            {
                Assert.True(m.Modifiers.Any(mod => mod.Text == "public"));
                Assert.True(m.ReturnType.ToString().Contains("Task"));
            });
        }

        [Fact]
        public void Count_ReturnsCorrectCount()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.SimpleClass);

            // Act
            var count = context.FindMethods().Count();

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void ReturningType_SpecificReturnType_ReturnsMatchingMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindMethods()
                .ReturningType("User")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }
    }
}
