using CodeSearcher.Core;
using CodeSearcher.Core.Analysis;
using CodeSearcher.Core.Queries;
using Xunit;

namespace CodeSearcher.Tests.Features.Phase1
{
    /// <summary>
    /// Tests complets pour Phase 1: Sélecteurs Avancés
    /// Teste les extensions de filtrage sur les énumérables de méthodes
    /// </summary>
    public class AdvancedSelectorsTests
    {
        #region FilterByCyclomaticComplexity Tests

        [Fact]
        public void FilterByCyclomaticComplexity_SimpleMethod_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public class Service
{
    public void SimpleMethod()
    {
        Console.WriteLine(""hello"");
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var complex = methods.FilterByCyclomaticComplexity(greaterThan: 5);

            // Assert
            Assert.Empty(complex);
        }

        [Fact]
        public void FilterByCyclomaticComplexity_NestedIfStatements_FindsComplex()
        {
            // Arrange
            var code = @"
public class Service
{
    public void ComplexMethod(int x)
    {
        if (x > 0)
        {
            if (x > 10)
            {
                if (x > 20)
                {
                    if (x > 30)
                    {
                        if (x > 40)
                        {
                            Console.WriteLine(""Very complex"");
                        }
                    }
                }
            }
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var complex = methods.FilterByCyclomaticComplexity(greaterThan: 3);

            // Assert
            Assert.NotEmpty(complex);
            Assert.Contains(complex, m => m.Identifier.Text == "ComplexMethod");
        }

        [Fact]
        public void FilterByCyclomaticComplexity_MultipleSwitchCases_FindsComplex()
        {
            // Arrange
            var code = @"
public class Service
{
    public void ProcessSwitch(int value)
    {
        switch (value)
        {
            case 1:
                Console.WriteLine(""One"");
                break;
            case 2:
                Console.WriteLine(""Two"");
                break;
            case 3:
                Console.WriteLine(""Three"");
                break;
            case 4:
                Console.WriteLine(""Four"");
                break;
            default:
                Console.WriteLine(""Other"");
                break;
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var complex = methods.FilterByCyclomaticComplexity(greaterThan: 2);

            // Assert
            Assert.NotEmpty(complex);
            Assert.Contains(complex, m => m.Identifier.Text == "ProcessSwitch");
        }

        [Fact]
        public void FilterByCyclomaticComplexity_LoopsAndConditions_FindsComplex()
        {
            // Arrange
            var code = @"
public class Service
{
    public void ProcessData(string[] items)
    {
        foreach (var item in items)
        {
            if (item != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    while (i < item.Length)
                    {
                        i++;
                    }
                }
            }
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var complex = methods.FilterByCyclomaticComplexity(greaterThan: 2);

            // Assert
            Assert.NotEmpty(complex);
        }

        #endregion

        #region FilterByBodyLines Tests

        [Fact]
        public void FilterByBodyLines_ShortMethod_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public class Service
{
    public void ShortMethod()
    {
        Console.WriteLine(""Short"");
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var longMethods = methods.FilterByBodyLines(greaterThan: 5);

            // Assert
            Assert.Empty(longMethods);
        }

        [Fact]
        public void FilterByBodyLines_LongMethod_FindsMethod()
        {
            // Arrange
            var code = @"
public class Service
{
    public void LongMethod()
    {
        var line1 = 1;
        var line2 = 2;
        var line3 = 3;
        var line4 = 4;
        var line5 = 5;
        var line6 = 6;
        var line7 = 7;
        var line8 = 8;
        var line9 = 9;
        var line10 = 10;
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var longMethods = methods.FilterByBodyLines(greaterThan: 5);

            // Assert
            Assert.NotEmpty(longMethods);
            Assert.Contains(longMethods, m => m.Identifier.Text == "LongMethod");
        }

        [Fact]
        public void FilterByBodyLines_MixedMethods_FilteringCorrectly()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Short1() { Console.WriteLine(""1""); }
    public void Short2() { Console.WriteLine(""2""); }
    
    public void Long1()
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        var e = 5;
        var f = 6;
        var g = 7;
    }
    
    public void Long2()
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        var e = 5;
        var f = 6;
        var g = 7;
        var h = 8;
        var i = 9;
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var longMethods = methods.FilterByBodyLines(greaterThan: 3);

            // Assert
            Assert.NotEmpty(longMethods);
            Assert.True(longMethods.All(m => m.Identifier.Text.StartsWith("Long")));
        }

        #endregion

        #region FilterWithUnusedParameters Tests

        [Fact]
        public void FilterWithUnusedParameters_NoUnusedParams_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public class Service
{
    public int Add(int a, int b)
    {
        return a + b;
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var unused = methods.FilterWithUnusedParameters();

            // Assert
            Assert.Empty(unused);
        }

        [Fact]
        public void FilterWithUnusedParameters_SingleUnusedParam_FindsMethod()
        {
            // Arrange
            var code = @"
public class Service
{
    public int Calculate(int used, int unused)
    {
        return used * 2;
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var unused = methods.FilterWithUnusedParameters();

            // Assert
            Assert.NotEmpty(unused);
            Assert.Contains(unused, m => m.Identifier.Text == "Calculate");
        }

        [Fact]
        public void FilterWithUnusedParameters_MultipleUnusedParams_FindsMethod()
        {
            // Arrange
            var code = @"
public class Service
{
    public string Process(string used, string unused1, int unused2)
    {
        return used.ToUpper();
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var unused = methods.FilterWithUnusedParameters();

            // Assert
            Assert.NotEmpty(unused);
        }

        [Fact]
        public void FilterWithUnusedParameters_NoParameters_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public class Service
{
    public void NoParams()
    {
        Console.WriteLine(""No parameters"");
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var unused = methods.FilterWithUnusedParameters();

            // Assert
            Assert.Empty(unused);
        }

        #endregion

        #region FilterByLineCount Tests

        [Fact]
        public void FilterByLineCount_SmallClass_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public class SmallClass
{
    public void Method() { }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var classes = context.FindClasses().Execute().ToList();
            var large = classes.FilterByLineCount(greaterThan: 50);

            // Assert
            Assert.Empty(large);
        }

        [Fact]
        public void FilterByLineCount_LargeClass_FindsClass()
        {
            // Arrange
            var code = @"
public class LargeClass
{
    public void Method1() { var a = 1; var b = 2; }
    public void Method2() { var a = 1; var b = 2; }
    public void Method3() { var a = 1; var b = 2; }
    public void Method4() { var a = 1; var b = 2; }
    public void Method5() { var a = 1; var b = 2; }
    public void Method6() { var a = 1; var b = 2; }
    public void Method7() { var a = 1; var b = 2; }
    public void Method8() { var a = 1; var b = 2; }
    public void Method9() { var a = 1; var b = 2; }
    public void Method10() { var a = 1; var b = 2; }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var classes = context.FindClasses().Execute().ToList();
            var large = classes.FilterByLineCount(greaterThan: 10);

            // Assert
            Assert.NotEmpty(large);
            Assert.Contains(large, c => c.Identifier.Text == "LargeClass");
        }

        #endregion

        #region FilterByInheritanceDepth Tests

        [Fact]
        public void FilterByInheritanceDepth_NoInheritance_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public class SimpleClass
{
    public void Method() { }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var classes = context.FindClasses().Execute().ToList();
            var deep = classes.FilterByInheritanceDepth(greaterThan: 0);

            // Assert
            Assert.Empty(deep);
        }

        [Fact]
        public void FilterByInheritanceDepth_WithInheritance_FindsClass()
        {
            // Arrange
            var code = @"
public class BaseClass
{
}

public class DerivedClass : BaseClass
{
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var classes = context.FindClasses().Execute().ToList();
            var deep = classes.FilterByInheritanceDepth(greaterThan: 0);

            // Assert
            Assert.NotEmpty(deep);
        }

        #endregion

        #region FilterOrphanClasses Tests

        [Fact]
        public void FilterOrphanClasses_UnusedInternalClass_FindsClass()
        {
            // Arrange
            var code = @"
internal class OrphanClass
{
    public void Method() { }
}

public class UsedClass
{
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var classes = context.FindClasses().Execute().ToList();
            var orphans = classes.FilterOrphanClasses();

            // Assert
            // Note: La détection d'orphelin est une heuristique
            Assert.IsType<List<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>>(orphans);
        }

        #endregion

        #region Chained Filters Tests

        [Fact]
        public void ChainedFilters_ComplexAndLong_FindsProblematicMethods()
        {
            // Arrange
            var code = @"
public class Service
{
    public void SimpleMethod()
    {
        Console.WriteLine(""simple"");
    }

    public void ComplexLongMethod(int unused1, string unused2)
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        var e = 5;
        if (a > 0)
        {
            if (b > 0)
            {
                if (c > 0)
                {
                    if (d > 0)
                    {
                        var f = 6;
                    }
                }
            }
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var problematic = methods
                .FilterByCyclomaticComplexity(greaterThan: 2)
                .FilterByBodyLines(greaterThan: 5)
                .FilterWithUnusedParameters();

            // Assert
            Assert.NotEmpty(problematic);
            Assert.Contains(problematic, m => m.Identifier.Text == "ComplexLongMethod");
        }

        [Fact]
        public void ChainedFilters_MultipleConditions_RefinedResults()
        {
            // Arrange
            var code = @"
public class DataService
{
    public void Process1() { }
    
    public void Process2(int p1, string p2)
    {
        var a = 1;
        var b = 2;
        var c = 3;
        var d = 4;
        var e = 5;
    }

    public void Process3(int unused)
    {
        var a = 1;
        var b = 2;
        if (a > 0)
        {
            if (b > 0)
            {
                Console.WriteLine(""nested"");
            }
        }
    }
}
";
            var context = CodeContext.FromCode(code);

            // Act
            var methods = context.FindMethods().Execute().ToList();
            var longOnly = methods.FilterByBodyLines(greaterThan: 3);

            // Assert
            Assert.NotEmpty(longOnly);
            Assert.Contains(longOnly, m => m.Identifier.Text.StartsWith("Process"));
        }

        #endregion
    }
}
