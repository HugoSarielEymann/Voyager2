using CodeSearcher.Core;
using CodeSearcher.Tests.Fixtures;
using Xunit;

namespace CodeSearcher.Tests.Integration
{
    public class CodeContextIntegrationTests
    {
        [Fact]
        public void FindMethods_Complex_AllMethodsFound()
        {
            // Arrange - Large piece of code with various methods
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act & Assert - Test various complex queries

            // Find all public async methods returning Task
            var publicAsyncTaskMethods = context.FindMethods()
                .IsPublic()
                .IsAsync()
                .ReturningTask()
                .Execute()
                .ToList();
            Assert.NotEmpty(publicAsyncTaskMethods);

            // Find all methods with specific parameter count
            var methodsWithOneParam = context.FindMethods()
                .HasParameterCount(1)
                .Execute()
                .ToList();
            Assert.NotEmpty(methodsWithOneParam);

            // Find methods containing "User" in return type
            var userRelatedMethods = context.FindMethods()
                .ReturningType("User")
                .Execute()
                .ToList();
            Assert.NotEmpty(userRelatedMethods);
        }

        [Fact]
        public void CompleteScenario_FindAndAnalyzeClass()
        {
            // Scenario: Find all public classes, then find their public async methods

            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var publicClasses = context.FindClasses()
                .IsPublic()
                .Execute()
                .ToList();

            var publicAsyncMethods = context.FindMethods()
                .IsPublic()
                .IsAsync()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(publicClasses);
            Assert.NotEmpty(publicAsyncMethods);
        }

        [Fact]
        public void FindByPredicate_CustomLogic_FindsCorrectNodes()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var results = context.FindByPredicate(node =>
                node.ToString().Contains("GetUserById") ||
                node.ToString().Contains("GetUserByIdAsync")
            ).ToList();

            // Assert
            Assert.NotEmpty(results);
        }

        [Fact]
        public void MultipleQueryTypes_CombinedAnalysis()
        {
            // Scenario: Find classes with properties, find methods returning Task, and find return statements

            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var classes = context.FindClasses().Execute().ToList();
            var taskMethods = context.FindMethods().ReturningTask().Execute().ToList();
            var returnStatements = context.FindReturns().Execute().ToList();
            var properties = context.FindVariables().Execute().ToList();

            // Assert
            Assert.NotEmpty(classes);
            Assert.NotEmpty(taskMethods);
            Assert.NotEmpty(returnStatements);
            Assert.NotEmpty(properties);
        }

        [Fact]
        public void QueryChaining_DeepFiltering()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act - Complex chained query
            var results = context.FindMethods()
                .IsPublic()                          // Only public methods
                .ReturningTask()                     // That return Task
                .HasParameterCount(1)                // With exactly 1 parameter
                .WithNameContaining("User")          // And "User" in name
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(results);
            Assert.All(results, m =>
            {
                Assert.True(m.Modifiers.Any(mod => mod.Text == "public"));
                Assert.True(m.ReturnType.ToString().Contains("Task"));
                Assert.Single(m.ParameterList.Parameters);
                Assert.Contains("User", m.Identifier.Text);
            });
        }

        [Fact]
        public void CountAndFirstOrDefault_ConvenienceMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var methodCount = context.FindMethods().Count();
            var firstMethod = context.FindMethods().FirstOrDefault();
            var firstAsyncMethod = context.FindMethods().IsAsync().FirstOrDefault();

            // Assert
            Assert.True(methodCount > 0);
            Assert.NotNull(firstMethod);
            Assert.NotNull(firstAsyncMethod);
        }
    }
}
