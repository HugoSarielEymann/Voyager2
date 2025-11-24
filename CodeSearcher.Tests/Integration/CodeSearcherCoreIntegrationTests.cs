using CodeSearcher.Core;
using CodeSearcher.Tests.Fixtures;
using Xunit;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeSearcher.Tests.Integration
{
    /// <summary>
    /// Tests d'intégration pour démontrer les capacités fluentes de CodeSearcher.Core
    /// Valide que CodeSearcher.Core peut réaliser son objectif principal :
    /// requêter du code en mode fluent pour obtenir des syntaxes via CodeAnalysis
    /// </summary>
    public class CodeSearcherCoreIntegrationTests
    {
        #region Test 1: Rechercher toutes les méthodes d'un certain nom retournant un certain type

        [Fact]
        public void FindMethodsByNameAndReturnType_FindsAsyncUserMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var methods = context.FindMethods()
                .WithName("GetUserByIdAsync")
                .ReturningType("User")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
            Assert.Single(methods);
            Assert.Equal("GetUserByIdAsync", methods[0].Identifier.Text);
            Assert.Contains("User", methods[0].ReturnType.ToString());
        }

        [Fact]
        public void FindMethodsByNamePartialAndReturnType_FindsMultipleMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act - Chercher toutes les méthodes contenant "User" dans le nom ET retournant Task
            var methods = context.FindMethods()
                .WithNameContaining("User")
                .ReturningTask()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
            Assert.Contains(methods, m => m.Identifier.Text == "GetUserByIdAsync");
            Assert.Contains(methods, m => m.Identifier.Text == "ValidateUserAsync");
        }

        [Fact]
        public void FindAsyncPublicMethods_ReturningSpecificType()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act - Chercher méthodes publiques, asynchrones retournant Task
            var methods = context.FindMethods()
                .IsPublic()
                .IsAsync()
                .ReturningTask()
                .Execute()
                .ToList();

            // Assert - GetUserByIdAsync est async et public
            Assert.NotEmpty(methods);
            Assert.Contains(methods, m => m.Identifier.Text == "GetUserByIdAsync");
        }

        #endregion

        #region Test 2: Chercher tous les return statements d'une classe

        [Fact]
        public void FindAllReturnsInMethod_FindsAllReturnStatements()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act - Chercher tous les returns dans la méthode ProcessData
            var returns = context.FindReturns()
                .InMethod("ProcessData")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(returns);
            Assert.True(returns.Count >= 2); // return null; et return input.ToUpper();
        }

        [Fact]
        public void FindNullReturnsInClass_FindsAllNullReturns()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act - Chercher tous les returns de null
            var nullReturns = context.FindReturns()
                .ReturningNull()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(nullReturns);
            // Vérifier que tous les returns trouvés sont valides (un null return a Expression == null)
            Assert.All(nullReturns, r => 
                Assert.True(r.Expression == null || 
                           r.Expression.ToString() == "null"));
        }

        [Fact]
        public void FindReturnsOfSpecificType_FindsTaskReturns()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act - Chercher tous les returns de Task
            var taskReturns = context.FindReturns()
                .ReturningType("Task")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(taskReturns);
        }

        [Fact]
        public void FindReturnsWithCustomExpression_FindsSpecificReturns()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ComplexReturnStatements);

            // Act - Chercher les returns avec une expression contenant "Length"
            var returns = context.FindReturns()
                .WithExpression(expr => expr.ToString().Contains("Length"))
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(returns);
        }

        #endregion

        #region Test 3: Chercher les conditions qui mènent à une certaine ligne de code

        [Fact]
        public void FindConditionalsBeforeReturn_FindsIfStatements()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act - Chercher tous les if statements (conditions) dans le code
            var conditions = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax
            ).ToList();

            // Assert
            Assert.NotEmpty(conditions);
            Assert.True(conditions.All(c => c is Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax));
        }

        [Fact]
        public void FindAllControlFlowStructures_FindsIfAndWhile()
        {
            // Arrange
            var code = @"
public class Logic
{
    public void Execute()
    {
        if (true)
        {
            Console.WriteLine(""If"");
        }
        
        while (true)
        {
            break;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act - Chercher tous les if et while statements
            var controlFlow = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax ||
                n is Microsoft.CodeAnalysis.CSharp.Syntax.WhileStatementSyntax
            ).ToList();

            // Assert
            Assert.NotEmpty(controlFlow);
            Assert.Contains(controlFlow, c => c is Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax);
            Assert.Contains(controlFlow, c => c is Microsoft.CodeAnalysis.CSharp.Syntax.WhileStatementSyntax);
        }

        [Fact]
        public void FindComplexConditions_WithMultiplePredicates()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act - Chercher les if statements contenant "null"
            var nullCheckConditions = context.FindByPredicate(n =>
                n is Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax ifStmt &&
                ifStmt.Condition.ToString().Contains("null")
            ).ToList();

            // Assert
            Assert.NotEmpty(nullCheckConditions);
        }

        #endregion

        #region Test 4: Recherches avancées sur les classes

        [Fact]
        public void FindClassesByNameAndNamespace_FindsUserServiceClass()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var classes = context.FindClasses()
                .WithName("UserService")
                .InNamespace("MyApp.Services")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(classes);
            Assert.Single(classes);
            Assert.Equal("UserService", classes[0].Identifier.Text);
        }

        [Fact]
        public void FindAbstractClassesInNamespace_FindsBaseEntity()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ClassWithInheritance);

            // Act
            var abstractClasses = context.FindClasses()
                .IsAbstract()
                .InNamespace("MyApp.Models")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(abstractClasses);
            Assert.Contains(abstractClasses, c => c.Identifier.Text == "BaseEntity");
        }

        [Fact]
        public void FindSealedClasses_FindsConcreteEntity()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.ClassWithInheritance);

            // Act
            var sealedClasses = context.FindClasses()
                .IsSealed()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(sealedClasses);
            Assert.Contains(sealedClasses, c => c.Identifier.Text == "ConcreteEntity");
        }

        [Fact]
        public void FindClassesWithAttribute_FindsSerializableClass()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.WithAttributes);

            // Act
            var attrClasses = context.FindClasses()
                .WithAttribute("Serializable")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(attrClasses);
            Assert.Contains(attrClasses, c => c.Identifier.Text == "PersonDto");
        }

        #endregion

        #region Test 5: Recherches sur les variables, champs et propriétés

        [Fact]
        public void FindPropertyByName_FindsNameProperty()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var props = context.FindVariables()
                .WithName("Name")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(props);
        }

        [Fact]
        public void FindPublicPropertiesWithAttribute_FindsRequiredProperties()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.WithAttributes);

            // Act
            var requiredProps = context.FindVariables()
                .IsPublic()
                .WithAttribute("Required")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(requiredProps);
        }

        [Fact]
        public void FindReadOnlyFields_FindsReadOnlyProperty()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var readOnlyFields = context.FindVariables()
                .IsReadOnly()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(readOnlyFields);
            Assert.Contains(readOnlyFields, f => 
                f.ToString().Contains("ReadOnlyField"));
        }

        [Fact]
        public void FindPropertiesWithInitializer_FindsInitializedProperties()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var initializedProps = context.FindVariables()
                .WithInitializer()
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(initializedProps);
        }

        [Fact]
        public void FindVariablesByType_FindsStringVariables()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.WithAttributes);

            // Act
            var stringVars = context.FindVariables()
                .WithType("string")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(stringVars);
        }

        #endregion

        #region Test 6: Recherches fluentes complexes (chaînage de prédicats)

        [Fact]
        public void FluentChainedSearch_ComplexPublicAsyncMethods()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act - Chaîner plusieurs prédicats : public + async + retournant Task
            var methods = context.FindMethods()
                .IsPublic()
                .IsAsync()
                .ReturningTask()
                .WithNameContaining("User")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
            // Validation that the query returns async public Task methods containing "User"
            Assert.True(methods.All(m => m.Identifier.Text.Contains("User")));
        }

        [Fact]
        public void FluentChainedSearch_PrivateMethodsWithParameters()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act - Chercher les méthodes privées avec paramètres
            var methods = context.FindMethods()
                .IsPrivate()
                .HasParameterCount(1)
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
        }

        #endregion

        #region Test 7: Validation que les résultats sont cohérents

        [Fact]
        public void ExecuteMultipleTimes_ReturnsSameResults()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);
            var query = context.FindMethods()
                .WithNameContaining("User");

            // Act
            var results1 = query.Execute().Count();
            var results2 = query.Execute().Count();

            // Assert
            Assert.Equal(results1, results2);
        }

        [Fact]
        public void CountAndFirstOrDefault_ConsistentResults()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.MultipleMethodsClass);

            // Act
            var query = context.FindMethods().WithName("ProcessUser");
            var count = query.Count();
            var first = query.FirstOrDefault();

            // Assert
            Assert.Equal(1, count);
            Assert.NotNull(first);
            Assert.Equal("ProcessUser", first.Identifier.Text);
        }

        #endregion

        #region Test 8: Cas d'erreur et validation

        [Fact]
        public void FindMethodsWithNullName_ThrowsArgumentException()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.SimpleClass);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                context.FindMethods().WithName(null)
            );
        }

        [Fact]
        public void FindVariablesWithNullType_ThrowsArgumentException()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.SimpleClass);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                context.FindVariables().WithType(null)
            );
        }

        [Fact]
        public void FindReturnsWithNullExpression_ThrowsArgumentNullException()
        {
            // Arrange
            var context = CodeContext.FromCode(CodeSamples.SimpleClass);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                context.FindReturns().WithExpression(null)
            );
        }

        #endregion

        #region Test 9: Recherches sur du code réel complexe

        [Fact]
        public void RealWorldScenario_FindingDataAccessPattern()
        {
            // Arrange - Code simulant un pattern de data access
            var code = @"
public class UserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        if (id <= 0)
            return null;
        
        return await _db.Users.FindAsync(id);
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _db.Users.ToListAsync();
    }

    private User GetSingle(int id)
    {
        return null;
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act - Chercher tous les méthodes publiques async retournant Task avec paramètres
            var methods = context.FindMethods()
                .IsPublic()
                .IsAsync()
                .ReturningTask()
                .HasParameterCount(1)
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(methods);
            Assert.Contains(methods, m => m.Identifier.Text == "GetByIdAsync");
            Assert.DoesNotContain(methods, m => m.Identifier.Text == "GetSingle");
            Assert.DoesNotContain(methods, m => m.Identifier.Text == "GetAllAsync");
        }

        [Fact]
        public void RealWorldScenario_FindingNullChecksBeforeUse()
        {
            // Arrange - Code avec plusieurs null checks
            var code = @"
public class UserService
{
    public void ProcessUser(User user)
    {
        if (user == null)
            return;

        if (user.Name == null)
            return;

        Console.WriteLine(user.Name);
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act - Chercher tous les null returns
            var nullReturns = context.FindReturns()
                .InMethod("ProcessUser")
                .Execute()
                .ToList();

            // Assert
            Assert.NotEmpty(nullReturns);
        }

        #endregion
    }
}
