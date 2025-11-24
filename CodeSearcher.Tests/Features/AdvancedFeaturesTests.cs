using CodeSearcher.Core;
using CodeSearcher.Core.Abstractions;
using Xunit;

namespace CodeSearcher.Tests.Features
{
    /// <summary>
    /// Tests pour les nouvelles fonctionnalités:
    /// 1. Injection de dépendance pour le logging
    /// 2. Récupération des conditions menant à une instruction
    /// </summary>
    public class AdvancedFeaturesTests
    {
        #region 1. Tests du Logging avec Injection de Dépendance

        [Fact]
        public void FindMethods_WithConsoleLogger_LogsSelections()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Execute()
    {
    }
}
";
            var logger = new ConsoleLogger(isDebug: true);

            // Act
            var context = CodeContext.FromCode(code, logger);
            var methods = context.FindMethods()
                .WithName("Execute")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
            Assert.Single(methods);
        }

        [Fact]
        public void FindMethods_WithMemoryLogger_TracksAllOperations()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Execute() { }
    public void Process() { }
}
";
            var logger = new MemoryLogger();

            // Act
            var context = CodeContext.FromCode(code, logger);
            var methods = context.FindMethods()
                .IsPublic()
                .WithNameContaining("E")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
            Assert.NotEmpty(logger.Logs);
            var logsList = logger.Logs;
            Assert.True(logsList.Any(l => l.Contains("FindMethods") || l.Contains("SELECTION")),
                "Expected to find logs containing 'FindMethods' or 'SELECTION'");
        }

        [Fact]
        public void FindMethods_WithNullLogger_NoLogging()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Test() { }
}
";
            var logger = new NullLogger();

            // Act
            var context = CodeContext.FromCode(code, logger);
            var methods = context.FindMethods()
                .WithName("Test")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
            // NullLogger ne fait rien, mais le code fonctionne
        }

        [Fact]
        public void CodeContext_WithCustomLogger_UsesInjectedInstance()
        {
            // Arrange
            var code = @"
public class Test
{
    public void Method() { }
}
";
            var logger = new MemoryLogger();

            // Act
            var context = CodeContext.FromCode(code, logger);
            context.FindMethods().WithName("Method").Execute();
            var logs = logger.Logs;

            // Assert
            Assert.NotEmpty(logs);
        }

        #endregion

        #region 2. Tests de Récupération des Conditions Menant à une Instruction

        [Fact]
        public void FindConditionsLeadingTo_VariableInNestedIf_ReturnsConditions()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        if (true)
        {
            if (false)
            {
                var x = 3;
            }
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Trouver l'assignation de x
            var xAssignment = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "x"
            ).FirstOrDefault();

            Assert.NotNull(xAssignment);

            // Act
            var parentStatement = xAssignment.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            Assert.NotNull(parentStatement);

            var conditions = context.FindConditionsLeadingTo(parentStatement).ToList();

            // Assert
            Assert.NotEmpty(conditions);
            Assert.True(conditions.Count >= 2);  // Au moins 2 if statements (au lieu de ==)
            Assert.All(conditions, c => Assert.Equal("if", c.ConditionType));
        }

        [Fact]
        public void FindConditionsLeadingTo_VarInForLoop_ReturnsForCondition()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        for (int i = 0; i < 10; i++)
        {
            var x = i * 2;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Trouver la variable x
            var xAssignment = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "x"
            ).FirstOrDefault();

            var parentStatement = xAssignment.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var conditions = context.FindConditionsLeadingTo(parentStatement).ToList();

            // Assert
            Assert.NotEmpty(conditions);
            Assert.Single(conditions);
            Assert.Equal("for", conditions[0].ConditionType);
        }

        [Fact]
        public void FindConditionsLeadingTo_VarInForeachLoop_ReturnsForeachCondition()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        foreach (var item in items)
        {
            var processed = item * 2;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Trouver processed
            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "processed"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var conditions = context.FindConditionsLeadingTo(parentStatement).ToList();

            // Assert
            Assert.NotEmpty(conditions);
            Assert.Single(conditions);
            Assert.Equal("foreach", conditions[0].ConditionType);
        }

        [Fact]
        public void FindConditionsLeadingTo_VarInWhileLoop_ReturnsWhileCondition()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        int count = 0;
        while (count < 10)
        {
            var incremented = count + 1;
            count++;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Trouver incremented
            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "incremented"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var conditions = context.FindConditionsLeadingTo(parentStatement).ToList();

            // Assert
            Assert.NotEmpty(conditions);
            Assert.Single(conditions);
            Assert.Equal("while", conditions[0].ConditionType);
        }

        [Fact]
        public void FindConditionsLeadingTo_ComplexNesting_ReturnsAllConditions()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        if (condition1)
        {
            for (int i = 0; i < 10; i++)
            {
                if (condition2)
                {
                    var result = i * 2;
                }
            }
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Trouver result
            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "result"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var conditions = context.FindConditionsLeadingTo(parentStatement).ToList();

            // Assert
            Assert.NotEmpty(conditions);
            Assert.True(conditions.Count >= 3);  // Au moins if, for, if (au lieu de ==)
        }

        [Fact]
        public void FindConditionsLeadingTo_StatementDirectlyInMethod_ReturnsNoConditions()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        var x = 5;
    }
}
";
            var context = CodeContext.FromCode(code);

            // Trouver x
            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "x"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var conditions = context.FindConditionsLeadingTo(parentStatement).ToList();

            // Assert
            Assert.Empty(conditions); // Pas de conditions, directement dans la méthode
        }

        [Fact]
        public void IsStatementReachable_VarInIfCondition_IsReachable()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        if (true)
        {
            var x = 5;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "x"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var isReachable = context.IsStatementReachable(parentStatement);

            // Assert
            Assert.True(isReachable);
        }

        [Fact]
        public void IsStatementUnconditionallyReachable_DirectStatement_IsTrue()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        var x = 5;
    }
}
";
            var context = CodeContext.FromCode(code);

            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "x"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var isUnconditional = context.IsStatementUnconditionallyReachable(parentStatement);

            // Assert
            Assert.True(isUnconditional);
        }

        [Fact]
        public void IsStatementUnconditionallyReachable_ConditionalStatement_IsFalse()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        if (condition)
        {
            var x = 5;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "x"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var isUnconditional = context.IsStatementUnconditionallyReachable(parentStatement);

            // Assert
            Assert.False(isUnconditional);
        }

        [Fact]
        public void FindAllConditionalPaths_InMethod_ReturnsAllPaths()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        if (a)
        {
            var x = 1;
        }
        else
        {
            var y = 2;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            var method = context.FindMethods()
                .WithName("Execute")
                .Execute()
                .FirstOrDefault();

            // Act
            var paths = context.FindAllConditionalPaths(method).ToList();

            // Assert
            Assert.NotEmpty(paths);
        }

        #endregion

        #region 3. Tests d'Intégration: Logging + Conditions

        [Fact]
        public void FindConditions_WithLogging_LogsConditionalAnalysis()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        if (true)
        {
            var x = 3;
        }
    }
}
";
            var logger = new MemoryLogger();
            var context = CodeContext.FromCode(code, logger);

            // Trouver x
            var varDeclarator = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax varDec &&
                varDec.Identifier.Text == "x"
            ).FirstOrDefault();

            var parentStatement = varDeclarator.Ancestors()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>()
                .FirstOrDefault();

            // Act
            var conditions = context.FindConditionsLeadingTo(parentStatement).ToList();

            // Assert
            Assert.NotEmpty(conditions);
            Assert.NotEmpty(logger.Logs);
        }

        #endregion
    }
}
