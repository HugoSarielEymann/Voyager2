using CodeSearcher.Editor.Strategies;
using Xunit;

namespace CodeSearcher.Tests.Editor.Strategies
{
    /// <summary>
    /// Tests pour le système avancé de wrapper avec sélecteurs de bloc et primitives
    /// </summary>
    public class AdvancedBlockWrapperTests
    {
        #region 1. Tests de Sélecteur de Bloc - Entre Types

        [Fact]
        public void SelectBetweenVariables_FindsBlockBetweenTypes()
        {
            // Arrange
            var code = @"
public class Service
{
    public void GenerateToto()
    {
        List<int> data = new List<int>();
        Console.WriteLine(""Between"");
        int result = 42;
    }
}
";
            var selector = new CodeBlockSelector(code, "GenerateToto");

            // Act
            var selected = selector.SelectBetweenVariables("List", "int");

            // Assert
            Assert.NotEmpty(selected);
            Assert.Contains(selected, s => s.ToString().Contains("Between"));
        }

        [Fact]
        public void SelectBetweenVariables_FindsCorrectBlockSize()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Process()
    {
        List<string> items = new List<string>();
        var x = items.Count;
        var y = items.First();
        string result = ""done"";
    }
}
";
            var selector = new CodeBlockSelector(code, "Process");

            // Act
            var selected = selector.SelectBetweenVariables("List", "string");

            // Assert
            Assert.Equal(2, selected.Count);  // Les deux statements entre List et string
        }

        #endregion

        #region 2. Tests de Sélecteur de Bloc - Entre Noms

        [Fact]
        public void SelectBetweenVariableNames_FindsBlockBetweenNames()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Execute()
    {
        var firstName = ""John"";
        var middle = 42;
        var middle2 = 43;
        var lastName = ""Doe"";
    }
}
";
            var selector = new CodeBlockSelector(code, "Execute");

            // Act
            var selected = selector.SelectBetweenVariableNames("firstName", "lastName");

            // Assert
            Assert.NotEmpty(selected);
            Assert.True(selected.Count >= 2);  // Au moins middle et middle2
        }

        #endregion

        #region 3. Tests de Sélecteur de Bloc - Entre Indices

        [Fact]
        public void SelectBetweenIndices_SelectsCorrectRange()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Multi()
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        var e = 5;
    }
}
";
            var selector = new CodeBlockSelector(code, "Multi");

            // Act
            var selected = selector.SelectBetweenIndices(1, 3);

            // Assert
            Assert.Equal(3, selected.Count);
        }

        #endregion

        #region 4. Tests de Sélecteur de Bloc - Contenant Identifiant

        [Fact]
        public void SelectStatementsWithIdentifier_FindsStatements()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Work()
    {
        var data = 1;
        Console.WriteLine(data);
        var other = 2;
        data = 3;
        var last = data;
    }
}
";
            var selector = new CodeBlockSelector(code, "Work");

            // Act
            var selected = selector.SelectStatementsContainingIdentifier("data");

            // Assert
            Assert.NotEmpty(selected);
            Assert.True(selected.All(s => s.ToString().Contains("data")));
        }

        #endregion
    }
}
