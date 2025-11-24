using CodeSearcher.Core;
using CodeSearcher.Core.Analysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CodeSearcher.Tests.Features.Phase1
{
    /// <summary>
    /// Tests complets pour Phase 1: Détection de Patterns Obsolètes
    /// Teste les détecteurs d'anti-patterns, code smells et violations SOLID
    /// </summary>
    public class ObsoletePatternDetectorTests
    {
        #region Anti-Pattern Detection Tests

        [Fact]
        public void FindAntiPatterns_ServiceLocatorPattern_DetectsServiceLocator()
        {
            // Arrange
            var code = @"
public class ServiceLocator
{
    public static T GetService<T>() where T : class
    {
        return null;
    }

    public static IService GetService()
    {
        return null;
    }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var detector = new ObsoletePatternDetector(root);

            // Act
            var report = detector.FindAntiPatterns();

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.ServiceLocators);
            Assert.Contains(report.ServiceLocators, c => c.Identifier.Text == "ServiceLocator");
        }

        [Fact]
        public void FindAntiPatterns_SingletonPattern_DetectsSingleton()
        {
            // Arrange
            var code = @"
public class ConfigurationSingleton
{
    public static ConfigurationSingleton Instance { get; private set; }

    private ConfigurationSingleton() { }

    static ConfigurationSingleton()
    {
        Instance = new ConfigurationSingleton();
    }

    public string GetValue(string key) => key;
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var detector = new ObsoletePatternDetector(root);

            // Act
            var report = detector.FindAntiPatterns();

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.Singletons);
            Assert.Contains(report.Singletons, c => c.Identifier.Text == "ConfigurationSingleton");
        }

        [Fact]
        public void FindAntiPatterns_NoAntiPatterns_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public interface IUserRepository
{
    User GetUser(int id);
}

public class UserRepository : IUserRepository
{
    private readonly IDatabase _db;

    public UserRepository(IDatabase db)
    {
        _db = db;
    }

    public User GetUser(int id)
    {
        return _db.GetUser(id);
    }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var detector = new ObsoletePatternDetector(root);

            // Act
            var report = detector.FindAntiPatterns();

            // Assert
            Assert.NotNull(report);
            Assert.Empty(report.ServiceLocators);
            Assert.Empty(report.Singletons);
        }

        #endregion

        #region Code Smell Detection Tests

        [Fact]
        public void FindCodeSmells_LongMethodDetection_FindsLongMethods()
        {
            // Arrange
            var code = @"
public class DataProcessor
{
    public void ProcessData(string data)
    {
        var line1 = data;
        var line2 = data;
        var line3 = data;
        var line4 = data;
        var line5 = data;
        var line6 = data;
        var line7 = data;
        var line8 = data;
        var line9 = data;
        var line10 = data;
        var line11 = data;
        var line12 = data;
        var line13 = data;
        var line14 = data;
        var line15 = data;
        var line16 = data;
        var line17 = data;
        var line18 = data;
        var line19 = data;
        var line20 = data;
        var line21 = data;
        var line22 = data;
        var line23 = data;
        var line24 = data;
        var line25 = data;
        var line26 = data;
        var line27 = data;
        var line28 = data;
        var line29 = data;
        var line30 = data;
        Console.WriteLine(line30);
    }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindMethods().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var detector = new ObsoletePatternDetector(root);

            // Act
            var report = detector.FindCodeSmells();

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.LongMethods);
            Assert.Contains(report.LongMethods, m => m.Identifier.Text == "ProcessData");
        }

        [Fact]
        public void FindCodeSmells_DeadCodeDetection_FindsDeadMethods()
        {
            // Arrange
            var code = @"
public class Service
{
    public void PublicMethod()
    {
        Console.WriteLine(""Public"");
    }

    private void UnusedPrivateMethod()
    {
        Console.WriteLine(""Never called"");
    }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindMethods().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var detector = new ObsoletePatternDetector(root);

            // Act
            var report = detector.FindCodeSmells();

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.DeadCode);
        }

        [Fact]
        public void FindSolidViolations_DirectInstantiation_FindsDIPViolation()
        {
            // Arrange
            var code = @"
public class EmailSender
{
    public void Send(string message) { }
}

public class OrderProcessor
{
    private EmailSender _sender;

    public OrderProcessor()
    {
        _sender = new EmailSender();
    }

    public void Process()
    {
        _sender.Send(""Order processed"");
    }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var detector = new ObsoletePatternDetector(root);

            // Act
            var report = detector.FindSolidViolations();

            // Assert
            Assert.NotNull(report);
            Assert.NotEmpty(report.DependencyInversionIssues);
        }

        #endregion

        #region Combined Analysis Tests

        [Fact]
        public void AnalyzeAllPatterns_LegacyCode_FindsMultipleIssues()
        {
            // Arrange - Code legacy typique
            var code = @"
public class GlobalServiceLocator
{
    public static T GetService<T>() => null;
}

public class LegacyDataAccess
{
    public static LegacyDataAccess Instance { get; } = new();

    private LegacyDataAccess() { }

    public void ProcessUserData(string unused1, int unused2)
    {
        var a = 1; var b = 2; var c = 3; var d = 4; var e = 5;
        var f = 6; var g = 7; var h = 8; var i = 9; var j = 10;
        var k = 11; var l = 12; var m = 13; var n = 14; var o = 15;
        var p = 16; var q = 17; var r = 18; var s = 19; var t = 20;
        var u = 21; var v = 22; var w = 23; var x = 24; var y = 25;
        var z = 26;
        if (a > 0) if (b > 0) if (c > 0) if (d > 0) { }
    }

    private void UnusedMethod() { }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var detector = new ObsoletePatternDetector(root);

            // Act
            var antiPatterns = detector.FindAntiPatterns();
            var smells = detector.FindCodeSmells();
            var violations = detector.FindSolidViolations();

            // Assert
            Assert.NotNull(antiPatterns);
            Assert.NotNull(smells);
            Assert.NotNull(violations);
        }

        #endregion
    }
}
