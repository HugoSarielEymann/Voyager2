using CodeSearcher.Core;
using CodeSearcher.Tests.Fixtures;
using Xunit;

namespace CodeSearcher.Tests.Queries
{
    public class ClassQueryTests
    {
        [Fact]
        public void FindClasses_SimpleClass_ReturnsAllClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindClasses().Execute().ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void WithName_SpecificClass_ReturnsSingleClass()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var result = context.FindClasses()
                .WithName("UserService")
                .FirstOrDefault();

            // Assert
            Assert.NotNull(result);
            Assert.Equal("UserService", result.Identifier.Text);
        }

        [Fact]
        public void InNamespace_SpecificNamespace_ReturnsClassesInNamespace()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindClasses()
                .InNamespace("MyApp.Services")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void IsPublic_PublicClasses_ReturnsOnlyPublicClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindClasses()
                .IsPublic()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, c => Assert.True(c.Modifiers.Any(m => m.Text == "public")));
        }

        [Fact]
        public void WithNameContaining_PartialName_ReturnsMatchingClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindClasses()
                .WithNameContaining("Service")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, c => Assert.Contains("Service", c.Identifier.Text));
        }

        [Fact]
        public void IsAbstract_AbstractClasses_ReturnsOnlyAbstractClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ClassWithInheritance);

            // Act
            var results = context.FindClasses()
                .IsAbstract()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, c => Assert.True(c.Modifiers.Any(m => m.Text == "abstract")));
        }

        [Fact]
        public void IsSealed_SealedClasses_ReturnsOnlySealedClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ClassWithInheritance);

            // Act
            var results = context.FindClasses()
                .IsSealed()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, c => Assert.True(c.Modifiers.Any(m => m.Text == "sealed")));
        }

        [Fact]
        public void Inherits_BaseClass_ReturnsInheritingClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ClassWithInheritance);

            // Act
            var results = context.FindClasses()
                .Inherits("BaseEntity")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void Implements_Interface_ReturnsImplementingClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ClassWithInheritance);

            // Act
            var results = context.FindClasses()
                .Implements("ICloneable")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void WithAttribute_AttributedClasses_ReturnsAttributedClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.WithAttributes);

            // Act
            var results = context.FindClasses()
                .WithAttribute("Serializable")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void WithMemberCount_ClassesWithSpecificMemberCount_ReturnsCorrectClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindClasses()
                .WithMemberCount(4, count => count >= 3)
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void ChainedQueries_MultipleConditions_ReturnsCorrectClasses()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ClassWithInheritance);

            // Act
            var results = context.FindClasses()
                .IsPublic()
                .IsSealed()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, c =>
            {
                Assert.True(c.Modifiers.Any(m => m.Text == "public"));
                Assert.True(c.Modifiers.Any(m => m.Text == "sealed"));
            });
        }

        [Fact]
        public void Count_ReturnsCorrectCount()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var count = context.FindClasses().Count();

            // Assert
            Assert.Equal(2, count);
        }
    }
}
