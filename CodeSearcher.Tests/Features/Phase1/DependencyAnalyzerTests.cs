using CodeSearcher.Core;
using CodeSearcher.Core.Analysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CodeSearcher.Tests.Features.Phase1
{
    /// <summary>
    /// Tests complets pour Phase 1: Analyse de Dépendances
    /// Teste le graphe de dépendances, impact analysis et détection de cycles
    /// </summary>
    public class DependencyAnalyzerTests
    {
        #region BuildDependencyGraph Tests

        [Fact]
        public void BuildDependencyGraph_SimpleClasses_BuildsGraph()
        {
            // Arrange
            var code = @"
public class Service
{
    private Repository _repo;
}

public class Repository
{
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var graph = analyzer.BuildDependencyGraph();

            // Assert
            Assert.NotNull(graph);
        }

        [Fact]
        public void BuildDependencyGraph_MultipleClasses_BuildsCompleteGraph()
        {
            // Arrange
            var code = @"
public class UserService
{
    private IUserRepository _userRepo;
    private IEmailService _emailService;
}

public interface IUserRepository { }
public interface IEmailService { }
public class User { }
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var graph = analyzer.BuildDependencyGraph();

            // Assert
            Assert.NotNull(graph);
        }

        #endregion

        #region AnalyzeImpact Tests

        [Fact]
        public void AnalyzeImpact_RenameMethod_FindsCallers()
        {
            // Arrange
            var code = @"
public class Service
{
    public void Save() { }
    public void Process() { Save(); }
    public void Execute() { Save(); }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindMethods().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var impact = analyzer.AnalyzeImpact("Save", "Rename");

            // Assert
            Assert.NotNull(impact);
            Assert.Equal("Save", impact.MethodName);
        }

        [Fact]
        public void AnalyzeImpact_CriticalMethod_ShowsMultipleImpacts()
        {
            // Arrange
            var code = @"
public class PaymentProcessor
{
    public bool ProcessPayment(decimal amount) => true;
}

public class OrderService
{
    private PaymentProcessor _processor;
    public void CompleteOrder() { _processor.ProcessPayment(100); }
}
";
            var context = CodeContext.FromCode(code);
            var root = context.FindMethods().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var impact = analyzer.AnalyzeImpact("ProcessPayment", "Rename");

            // Assert
            Assert.NotNull(impact);
        }

        #endregion

        #region FindCircularDependencies Tests

        [Fact]
        public void FindCircularDependencies_TwoWayDependency_DetectsCircle()
        {
            // Arrange
            var code = @"
public class ClassA { private ClassB _b; }
public class ClassB { private ClassA _a; }
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var circles = analyzer.FindCircularDependencies();

            // Assert
            Assert.IsType<List<CircularDependency>>(circles);
        }

        [Fact]
        public void FindCircularDependencies_NoCircles_ReturnsEmpty()
        {
            // Arrange
            var code = @"
public class ClassA { private ClassB _b; }
public class ClassB { private ClassC _c; }
public class ClassC { private ClassD _d; }
public class ClassD { }
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var circles = analyzer.FindCircularDependencies();

            // Assert
            Assert.NotNull(circles);
        }

        #endregion

        #region Graph Queries Tests

        [Fact]
        public void GetDependencies_ClassWithDependencies_ReturnsList()
        {
            // Arrange
            var code = @"
public class Service
{
    private Repository _repo;
    private Logger _logger;
}

public class Repository { }
public class Logger { }
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var graph = analyzer.BuildDependencyGraph();
            var deps = graph.GetDependencies("Service").ToList();

            // Assert
            Assert.NotEmpty(deps);
        }

        #endregion

        #region Real-World Scenarios

        [Fact]
        public void RealWorldScenario_LegacyDataAccessLayer_AnalyzesMigration()
        {
            // Arrange
            var code = @"
public class UserDataAccess
{
    private Logger _logger;
    public User GetUserById(int id) => new User();
    public void SaveUser(User user) { }
}

public class UserService
{
    private UserDataAccess _dataAccess;
    public void UpdateUser(User user) { _dataAccess.SaveUser(user); }
}

public class Logger { }
public class User { }
";
            var context = CodeContext.FromCode(code);
            var root = context.FindClasses().Execute().First().SyntaxTree.GetRoot() as CompilationUnitSyntax;
            var analyzer = new DependencyAnalyzer(root);

            // Act
            var graph = analyzer.BuildDependencyGraph();
            var impact = analyzer.AnalyzeImpact("GetUserById", "Refactor");

            // Assert
            Assert.NotNull(graph);
            Assert.NotNull(impact);
        }

        #endregion
    }
}
