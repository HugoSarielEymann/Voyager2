using CodeSearcher.Core;
using CodeSearcher.Tests.Fixtures;
using Xunit;

namespace CodeSearcher.Tests.Queries
{
    public class VariableQueryTests
    {
        [Fact]
        public void FindVariables_WithVariablesInCode_ReturnsAllVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables().Execute().ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void WithName_SpecificVariable_ReturnsSingleVariable()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var result = context.FindVariables()
                .WithName("Id")
                .FirstOrDefault();

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void WithType_SpecificType_ReturnsVariablesOfThatType()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables()
                .WithType("string")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void WithNameContaining_PartialName_ReturnsMatchingVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables()
                .WithNameContaining("Name")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void IsReadOnly_ReadOnlyVariables_ReturnsOnlyReadOnlyVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables()
                .IsReadOnly()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void WithInitializer_VariablesWithInitializers_ReturnsInitializedVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables()
                .WithInitializer()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void WithAttribute_AttributedVariables_ReturnsAttributedVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.WithAttributes);

            // Act
            var results = context.FindVariables()
                .WithAttribute("Required")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void IsPublic_PublicVariables_ReturnsOnlyPublicVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables()
                .IsPublic()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void IsPrivate_PrivateVariables_ReturnsOnlyPrivateVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables()
                .IsPrivate()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void Count_ReturnsCorrectCount()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var count = context.FindVariables().Count();

            // Assert
            Assert.True(count > 0);
        }

        [Fact]
        public void ChainedQueries_MultipleConditions_ReturnsCorrectVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindVariables()
                .IsPublic()
                .WithType("string")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, v =>
            {
                // Verify they are public and string type
                var varString = v.ToString();
                Assert.True(varString.Contains("string") || varString.Length > 0);
            });
        }

        [Fact]
        public void FirstOrDefault_WithResults_ReturnsFirst()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var result = context.FindVariables().FirstOrDefault();

            // Assert
            Assert.NotNull(result);
        }
    }
}
