using CodeSearcher.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Core.Analysis
{
    /// <summary>
    /// Détecteur de patterns obsolètes et anti-patterns
    /// Implémentation de la Catégorie 3: Détection de Patterns Obsolètes
    /// </summary>
    public class ObsoletePatternDetector
    {
        private readonly CompilationUnitSyntax _root;
        private readonly ILogger _logger;

        public ObsoletePatternDetector(CompilationUnitSyntax root, ILogger logger = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _logger = logger ?? new NullLogger();
        }

        /// <summary>
        /// Détecte les anti-patterns dans le code
        /// </summary>
        public AntiPatternReport FindAntiPatterns()
        {
            var report = new AntiPatternReport();

            _logger.LogInfo("Analyzing anti-patterns...");

            report.ServiceLocators = FindServiceLocatorPattern().ToList();
            report.Singletons = FindSingletonPattern().ToList();
            report.GodObjects = FindGodObjects().ToList();
            report.DataAccessInUI = FindDataAccessInUI().ToList();

            _logger.LogInfo($"Found {report.ServiceLocators.Count} service locators");
            _logger.LogInfo($"Found {report.Singletons.Count} singletons");
            _logger.LogInfo($"Found {report.GodObjects.Count} god objects");
            _logger.LogInfo($"Found {report.DataAccessInUI.Count} data access in UI");

            return report;
        }

        /// <summary>
        /// Détecte les Code Smells
        /// </summary>
        public CodeSmellReport FindCodeSmells()
        {
            var report = new CodeSmellReport();

            _logger.LogInfo("Analyzing code smells...");

            report.DuplicateCode = FindDuplicateCode().ToList();
            report.LongMethods = FindLongMethods().ToList();
            report.DeadCode = FindDeadCode().ToList();
            report.TightCoupling = FindTightCoupling().ToList();

            return report;
        }

        /// <summary>
        /// Détecte les violations SOLID
        /// </summary>
        public SolidViolationReport FindSolidViolations()
        {
            var report = new SolidViolationReport();

            _logger.LogInfo("Analyzing SOLID violations...");

            report.SingleResponsibilityViolations = FindSRPViolations().ToList();
            report.DependencyInversionIssues = FindDIPIssues().ToList();

            return report;
        }

        // ===== Détecteurs spécialisés =====

        private IEnumerable<ClassDeclarationSyntax> FindServiceLocatorPattern()
        {
            return _root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => 
                {
                    var className = c.Identifier.Text;
                    var hasStaticGetMethod = c.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Any(m => m.Modifiers.Any(mod => mod.Kind() == SyntaxKind.StaticKeyword) &&
                                  (m.Identifier.Text.Contains("Get") || m.Identifier.Text.Contains("Resolve")));
                    
                    return hasStaticGetMethod && (className.Contains("ServiceLocator") || 
                                                   className.Contains("Locator") ||
                                                   className.Contains("Container"));
                });
        }

        private IEnumerable<ClassDeclarationSyntax> FindSingletonPattern()
        {
            return _root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c =>
                {
                    var hasInstanceProperty = c.Members
                        .OfType<PropertyDeclarationSyntax>()
                        .Any(p => p.Identifier.Text == "Instance" || 
                                  p.Identifier.Text == "Singleton");

                    var hasPrivateConstructor = c.Members
                        .OfType<ConstructorDeclarationSyntax>()
                        .Any(ctor => ctor.Modifiers.Any(m => m.Kind() == SyntaxKind.PrivateKeyword));

                    return hasInstanceProperty && hasPrivateConstructor;
                });
        }

        private IEnumerable<ClassDeclarationSyntax> FindGodObjects()
        {
            return _root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Members.OfType<MethodDeclarationSyntax>().Count() > 100 ||
                           c.GetText().Lines.Count > 1000);
        }

        private IEnumerable<MethodDeclarationSyntax> FindDataAccessInUI()
        {
            return _root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m =>
                {
                    var classParent = m.Parent as ClassDeclarationSyntax;
                    var isUIClass = classParent?.Identifier.Text.Contains("Form") == true ||
                                   classParent?.Identifier.Text.Contains("Control") == true ||
                                   classParent?.Identifier.Text.Contains("View") == true;

                    if (!isUIClass)
                        return false;

                    // Chercher les références de base de données
                    var hasDatabaseAccess = m.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Any(id => id.Identifier.Text.Contains("SqlCommand") ||
                                  id.Identifier.Text.Contains("Connection") ||
                                  id.Identifier.Text.Contains("Query"));

                    return hasDatabaseAccess;
                });
        }

        private IEnumerable<MethodDeclarationSyntax> FindLongMethods()
        {
            return _root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Body?.Statements.Count > 30 || m.GetText().Lines.Count > 50);
        }

        private IEnumerable<SyntaxNode> FindDuplicateCode()
        {
            // Simplification: chercher les blocs similaires
            return _root.DescendantNodes()
                .OfType<BlockSyntax>()
                .GroupBy(b => b.ToString())
                .Where(g => g.Count() > 1)
                .SelectMany(g => g);
        }

        private IEnumerable<MethodDeclarationSyntax> FindDeadCode()
        {
            // Chercher les méthodes privées jamais appelées
            return _root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PrivateKeyword) &&
                           !IsMethodCalled(m));
        }

        private IEnumerable<ClassDeclarationSyntax> FindTightCoupling()
        {
            return _root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c =>
                {
                    // Compter les dépendances
                    var fieldDependencies = c.Members
                        .OfType<FieldDeclarationSyntax>()
                        .SelectMany(f => f.Declaration.Variables)
                        .Count();

                    return fieldDependencies > 10; // Limite arbitraire
                });
        }

        private IEnumerable<ClassDeclarationSyntax> FindSRPViolations()
        {
            return _root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c => c.Members.OfType<MethodDeclarationSyntax>().Count() > 20);
        }

        private IEnumerable<ClassDeclarationSyntax> FindDIPIssues()
        {
            return _root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c =>
                {
                    // Chercher les instantiations directes de concrétions
                    var hasDirectInstantiation = c.DescendantNodes()
                        .OfType<ObjectCreationExpressionSyntax>()
                        .Any();

                    return hasDirectInstantiation;
                });
        }

        private bool IsMethodCalled(MethodDeclarationSyntax method)
        {
            var methodName = method.Identifier.Text;
            var classParent = method.Parent as ClassDeclarationSyntax;

            if (classParent == null)
                return true;

            var otherMembers = classParent.Members
                .OfType<MethodDeclarationSyntax>()
                .Where(m => !m.Identifier.Text.Equals(methodName));

            return otherMembers.Any(m =>
                m.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Any(inv => inv.Expression.ToString().Contains(methodName))
            );
        }
    }

    /// <summary>
    /// Rapport d'anti-patterns détectés
    /// </summary>
    public class AntiPatternReport
    {
        public List<ClassDeclarationSyntax> ServiceLocators { get; set; } = new();
        public List<ClassDeclarationSyntax> Singletons { get; set; } = new();
        public List<ClassDeclarationSyntax> GodObjects { get; set; } = new();
        public List<MethodDeclarationSyntax> DataAccessInUI { get; set; } = new();
    }

    /// <summary>
    /// Rapport de Code Smells détectés
    /// </summary>
    public class CodeSmellReport
    {
        public List<SyntaxNode> DuplicateCode { get; set; } = new();
        public List<MethodDeclarationSyntax> LongMethods { get; set; } = new();
        public List<MethodDeclarationSyntax> DeadCode { get; set; } = new();
        public List<ClassDeclarationSyntax> TightCoupling { get; set; } = new();
    }

    /// <summary>
    /// Rapport de violations SOLID détectées
    /// </summary>
    public class SolidViolationReport
    {
        public List<ClassDeclarationSyntax> SingleResponsibilityViolations { get; set; } = new();
        public List<ClassDeclarationSyntax> DependencyInversionIssues { get; set; } = new();
    }
}
