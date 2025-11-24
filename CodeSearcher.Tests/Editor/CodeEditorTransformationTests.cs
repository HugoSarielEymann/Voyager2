using CodeSearcher.Core;
using CodeSearcher.Editor;
using CodeSearcher.Tests.Fixtures;
using Xunit;

namespace CodeSearcher.Tests.Editor
{
    /// <summary>
    /// Tests pour démontrer que CodeSearcher.Editor peut transformer les syntaxes
    /// extraites par CodeSearcher.Core et effectuer des modifications avancées.
    /// 
    /// Valide les capacités:
    /// - Renommer des méthodes/classes/variables/propriétés
    /// - Wrapper des méthodes (try-catch, logging, validation)
    /// - Remplacer du code
    /// - Transformations fluentes et chaînées
    /// </summary>
    public class CodeEditorTransformationTests
    {
        #region 1. Tests de Renommage - Méthodes

        [Fact]
        public void RenameMethod_SuccessfullyRenamesMethodDeclaration()
        {
            // Arrange
            var code = @"
public class UserService
{
    public User GetUser(int id)
    {
        return new User { Id = id };
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor.RenameMethod("GetUser", "FetchUser").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.ModifiedCode);
            Assert.Contains("FetchUser", result.ModifiedCode);
            Assert.DoesNotContain("GetUser(", result.ModifiedCode);
        }

        [Fact]
        public void RenameMethod_WithMultipleCalls_RenamesOccurrences()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Calculate()
    {
    }

    public void Process()
    {
        Calculate();
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor.RenameMethod("Calculate", "Compute").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Compute", result.ModifiedCode);
        }

        #endregion

        #region 2. Tests de Renommage - Classes

        [Fact]
        public void RenameClass_RenamesDeclaration()
        {
            // Arrange
            var code = @"
public class User
{
    public string Name { get; set; }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor.RenameClass("User", "Person").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Person", result.ModifiedCode);
            Assert.DoesNotContain("class User", result.ModifiedCode);
        }

        [Fact]
        public void RenameClass_RenamesInstanciations()
        {
            // Arrange
            var code = @"
public class User { }

public class Service
{
    public void Create()
    {
        var u = new User();
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor.RenameClass("User", "Person").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("new Person", result.ModifiedCode);
        }

        #endregion

        #region 3. Tests de Renommage - Variables

        [Fact]
        public void RenameVariable_RenamesDeclaration()
        {
            // Arrange
            var code = @"
public class Calc
{
    public void Sum()
    {
        int tempValue = 10;
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor.RenameVariable("tempValue", "baseValue").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("baseValue", result.ModifiedCode);
            Assert.DoesNotContain("tempValue", result.ModifiedCode);
        }

        #endregion

        #region 4. Tests de Renommage - Propriétés

        [Fact]
        public void RenameProperty_RenamesDeclaration()
        {
            // Arrange
            var code = @"
public class Product
{
    public string Name { get; set; }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor.RenameProperty("Name", "Title").Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Title", result.ModifiedCode);
            Assert.DoesNotContain("Name", result.ModifiedCode);
        }

        #endregion

        #region 5. Tests de Wrapper - Try-Catch

        [Fact]
        public void WrapWithTryCatch_AddsTryCatchBlock()
        {
            // Arrange
            var code = @"
public class Handler
{
    public void Process()
    {
        Console.WriteLine(""test"");
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor
                .WrapWithTryCatch("Process", "throw;")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("try", result.ModifiedCode);
        }

        #endregion

        #region 6. Tests de Wrapper - Logging

        [Fact]
        public void WrapWithLogging_InsertsLoggingStatement()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Execute()
    {
        Console.WriteLine(""work"");
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor
                .WrapWithLogging("Execute", "Console.WriteLine(\"Starting\");")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Starting", result.ModifiedCode);
        }

        #endregion

        #region 7. Tests de Wrapper - Validation

        [Fact]
        public void WrapWithValidation_InsertsValidationStatement()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Process(string name)
    {
        Console.WriteLine(name);
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor
                .WrapWithValidation("Process", "if (string.IsNullOrEmpty(name)) throw new ArgumentException();")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("ArgumentException", result.ModifiedCode);
        }

        #endregion

        #region 8. Tests de Remplacement

        [Fact]
        public void Replace_ReplacesCodeSnippet()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Execute()
    {
        int.Parse(""123"");
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor
                .Replace("int.Parse(\"123\")", "Convert.ToInt32(\"123\")")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Convert.ToInt32", result.ModifiedCode);
        }

        [Fact]
        public void Replace_FailsWhenNotFound()
        {
            // Arrange
            var code = @"public class Test { }";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor.Replace("NotFound", "Replacement").Apply();

            // Assert
            Assert.False(result.Success);
        }

        #endregion

        #region 9. Tests Chaînés

        [Fact]
        public void ChainedOperations_AppliesMultipleTransformations()
        {
            // Arrange
            var code = @"
public class DataService
{
    public void GetData()
    {
        int temp = 10;
        Console.WriteLine(temp);
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor
                .RenameMethod("GetData", "FetchData")
                .RenameVariable("temp", "data")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("FetchData", result.ModifiedCode);
            Assert.Contains("data", result.ModifiedCode);
        }

        [Fact]
        public void ChainedOperations_MixedTransformations()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Process()
    {
        int value = 5;
        Console.WriteLine(value);
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act
            var result = editor
                .RenameMethod("Process", "Execute")
                .Replace("int value = 5", "int value = 10")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Execute", result.ModifiedCode);
            Assert.Contains("value = 10", result.ModifiedCode);
        }

        #endregion

        #region 10. Tests d'Intégration Core + Editor

        [Fact]
        public void FindMethodsThenTransform_SearchAndModifyTogether()
        {
            // Arrange
            var code = @"
public class Repository
{
    public User GetById(int id)
    {
        return null;
    }

    public List<User> GetAll()
    {
        return null;
    }
}

public class User { }
";
            var context = CodeContext.FromCode(code);

            // Act 1 - Trouver les méthodes Get*
            var methods = context.FindMethods()
                .WithNameContaining("Get")
                .IsPublic()
                .Execute()
                .ToList();

            Assert.Equal(2, methods.Count);

            // Act 2 - Les renommer
            var editor = CodeEditor.FromCode(code);
            var methodCount = 0;
            foreach (var method in methods)
            {
                editor.RenameMethod(method.Identifier.Text, "Fetch" + method.Identifier.Text.Substring(3));
                methodCount++;
            }

            var result = editor.Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Equal(2, methodCount);
            Assert.Contains("Fetch", result.ModifiedCode);
        }

        [Fact]
        public void FindClassesThenRename_LocateAndTransform()
        {
            // Arrange
            var code = @"
public class User
{
    public string Name { get; set; }
}

public class UserService
{
    public User CreateUser()
    {
        return new User();
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act 1 - Trouver la classe User
            var classes = context.FindClasses()
                .WithName("User")
                .IsPublic()
                .Execute()
                .ToList();

            Assert.Single(classes);

            // Act 2 - La renommer
            var editor = CodeEditor.FromCode(code);
            editor.RenameClass(classes[0].Identifier.Text, "Person");

            var result = editor.Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("Person", result.ModifiedCode);
        }

        [Fact]
        public void FindReturnsThenWrap_LocateReturnsAndAddErrorHandling()
        {
            // Arrange
            var code = @"
public class Parser
{
    public int Parse(string value)
    {
        return int.Parse(value);
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act 1 - Trouver les returns dans la méthode Parse
            var returns = context.FindReturns()
                .InMethod("Parse")
                .Execute()
                .ToList();

            Assert.NotEmpty(returns);

            // Act 2 - Wrapper la méthode
            var editor = CodeEditor.FromCode(code);
            editor.WrapWithTryCatch("Parse", "return 0;");

            var result = editor.Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("try", result.ModifiedCode);
        }

        #endregion

        #region 11. Tests d'État

        [Fact]
        public void Reset_RestoresOriginalCode()
        {
            // Arrange
            var original = @"public class Test { }";
            var editor = CodeEditor.FromCode(original);

            // Act
            editor.RenameClass("Test", "Updated");
            editor.Reset();

            // Assert
            Assert.Equal(original, editor.GetCode());
        }

        [Fact]
        public void Clear_ClearsOperations()
        {
            // Arrange
            var code = @"public class Test { }";
            var editor = CodeEditor.FromCode(code);

            // Act
            editor.RenameClass("Test", "Updated");
            editor.Clear();
            var result = editor.Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("class Test", result.ModifiedCode);
        }

        [Fact]
        public void GetChangeLog_TracksTransformations()
        {
            // Arrange
            var code = @"public class Test { }";
            var editor = CodeEditor.FromCode(code);

            // Act
            editor.RenameClass("Test", "Updated");
            var result = editor.Apply();
            var log = editor.GetChangeLog();

            // Assert
            Assert.NotEmpty(log);
            Assert.True(log.Any(l => l.Contains("Rename")));
        }

        #endregion

        #region 12. Cas Réels de Refactoring

        [Fact]
        public void RealWorld_RefactoringLegacyCode()
        {
            // Arrange - Code legacy
            var code = @"
public class UserRep
{
    public User GetUser(int id)
    {
        return new User();
    }
}

public class User
{
    public string Name { get; set; }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act - Refactoring: meilleur nommage
            var result = editor
                .RenameClass("UserRep", "UserRepository")
                .RenameMethod("GetUser", "FetchById")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("UserRepository", result.ModifiedCode);
            Assert.Contains("FetchById", result.ModifiedCode);
        }

        [Fact]
        public void RealWorld_AddErrorHandlingToDataAccess()
        {
            // Arrange
            var code = @"
public class Database
{
    public int ExecuteQuery(string sql)
    {
        return 0;
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act - Ajouter try-catch
            var result = editor
                .WrapWithTryCatch("ExecuteQuery", "return -1;")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("try", result.ModifiedCode);
        }

        [Fact]
        public void RealWorld_ModifyParserLogic()
        {
            // Arrange
            var code = @"
public class Parser
{
    public int Convert(string value)
    {
        return int.Parse(value);
    }
}
";
            var editor = CodeEditor.FromCode(code);

            // Act - Améliorer la logique de parsing
            var result = editor
                .Replace("int.Parse(value)", "int.TryParse(value, out int result) ? result : 0")
                .Apply();

            // Assert
            Assert.True(result.Success);
            Assert.Contains("TryParse", result.ModifiedCode);
        }

        #endregion
    }
}
