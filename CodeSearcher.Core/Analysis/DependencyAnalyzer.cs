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
    /// Analyseur de dépendances pour construire un graphe et analyser les impacts
    /// Implémentation de la Catégorie 2: Analyses de Dépendances
    /// </summary>
    public class DependencyAnalyzer
    {
        private readonly CompilationUnitSyntax _root;
        private readonly ILogger _logger;
        private Dictionary<string, ClassNode> _graph;

        public DependencyAnalyzer(CompilationUnitSyntax root, ILogger logger = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _logger = logger ?? new NullLogger();
            _graph = new Dictionary<string, ClassNode>();
        }

        /// <summary>
        /// Construit le graphe de dépendances complet
        /// </summary>
        public DependencyGraph BuildDependencyGraph()
        {
            _logger.LogInfo("Building dependency graph...");

            var classes = _root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .ToList();

            // Créer les nœuds
            foreach (var classDecl in classes)
            {
                var className = classDecl.Identifier.Text;
                _graph[className] = new ClassNode { Name = className, Syntax = classDecl };
            }

            // Construire les connexions
            foreach (var classDecl in classes)
            {
                var className = classDecl.Identifier.Text;
                var node = _graph[className];

                // Trouver les dépendances
                var dependencies = FindDependencies(classDecl);
                foreach (var dep in dependencies)
                {
                    if (_graph.ContainsKey(dep))
                    {
                        node.Dependencies.Add(dep);
                        _graph[dep].DependentOn.Add(className);
                    }
                }
            }

            _logger.LogInfo($"Built graph with {_graph.Count} classes");

            return new DependencyGraph(_graph);
        }

        /// <summary>
        /// Analyse l'impact d'une transformation
        /// </summary>
        public ImpactAnalysisResult AnalyzeImpact(string methodName, string action)
        {
            _logger.LogInfo($"Analyzing impact of {action} on {methodName}...");

            var result = new ImpactAnalysisResult
            {
                MethodName = methodName,
                Action = action
            };

            // Trouver la méthode
            var method = _root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == methodName);

            if (method == null)
            {
                result.BreakingChanges.Add($"Method {methodName} not found");
                return result;
            }

            // Trouver tous les appels
            var callers = FindMethodCallers(methodName);
            result.Callers = callers.ToList();

            // Identifier les tests affectés
            var affectedTests = FindAffectedTests(methodName);
            result.AffectedTests = affectedTests.ToList();

            _logger.LogInfo($"Found {callers.Count()} callers and {affectedTests.Count()} affected tests");

            return result;
        }

        /// <summary>
        /// Détecte les dépendances circulaires
        /// </summary>
        public List<CircularDependency> FindCircularDependencies()
        {
            _logger.LogInfo("Detecting circular dependencies...");

            var graph = BuildDependencyGraph();
            var circles = new List<CircularDependency>();

            foreach (var node in _graph.Values)
            {
                var visited = new HashSet<string>();
                var path = new List<string>();

                if (HasCircularDependency(node, visited, path))
                {
                    circles.Add(new CircularDependency { Path = path.ToList() });
                }
            }

            _logger.LogInfo($"Found {circles.Count} circular dependencies");

            return circles;
        }

        // ===== Méthodes d'aide privées =====

        private List<string> FindDependencies(ClassDeclarationSyntax classDecl)
        {
            var dependencies = new List<string>();

            // Dépendances des champs
            var fieldTypes = classDecl.Members
                .OfType<FieldDeclarationSyntax>()
                .Select(f => f.Declaration.Type.ToString());

            // Dépendances des paramètres de constructeur
            var constructorParams = classDecl.Members
                .OfType<ConstructorDeclarationSyntax>()
                .SelectMany(c => c.ParameterList.Parameters)
                .Select(p => p.Type.ToString());

            // Dépendances des types de retour de méthodes
            var methodReturnTypes = classDecl.Members
                .OfType<MethodDeclarationSyntax>()
                .Select(m => m.ReturnType.ToString());

            dependencies.AddRange(fieldTypes);
            dependencies.AddRange(constructorParams);
            dependencies.AddRange(methodReturnTypes);

            return dependencies.Distinct().ToList();
        }

        private IEnumerable<string> FindMethodCallers(string methodName)
        {
            return _root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => inv.Expression.ToString().EndsWith(methodName))
                .Select(inv => GetCallerContext(inv))
                .Distinct();
        }

        private string GetCallerContext(InvocationExpressionSyntax invocation)
        {
            var method = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            var classDecl = invocation.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();

            return $"{classDecl?.Identifier.Text}.{method?.Identifier.Text}";
        }

        private IEnumerable<string> FindAffectedTests(string methodName)
        {
            return _root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Identifier.Text.Contains("Test") || m.Identifier.Text.Contains("Should"))
                .Where(m => m.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Any(inv => inv.Expression.ToString().Contains(methodName)))
                .Select(m => m.Identifier.Text);
        }

        private bool HasCircularDependency(ClassNode node, HashSet<string> visited, List<string> path)
        {
            if (path.Contains(node.Name))
            {
                return true;
            }

            if (visited.Contains(node.Name))
            {
                return false;
            }

            visited.Add(node.Name);
            path.Add(node.Name);

            foreach (var dep in node.Dependencies)
            {
                if (_graph.ContainsKey(dep))
                {
                    if (HasCircularDependency(_graph[dep], visited, path))
                    {
                        return true;
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }
    }

    /// <summary>
    /// Représentation du graphe de dépendances
    /// </summary>
    public class DependencyGraph
    {
        private Dictionary<string, ClassNode> _nodes;

        public DependencyGraph(Dictionary<string, ClassNode> nodes)
        {
            _nodes = nodes;
        }

        public IEnumerable<CircularDependency> FindCircularDependencies()
        {
            // Implémentation du détecteur de cycles
            var circles = new List<CircularDependency>();
            // ... logique ...
            return circles;
        }

        public IEnumerable<string> GetDependencies(string className)
        {
            return _nodes.ContainsKey(className) ? _nodes[className].Dependencies : Enumerable.Empty<string>();
        }

        public IEnumerable<string> GetDependents(string className)
        {
            return _nodes.ContainsKey(className) ? _nodes[className].DependentOn : Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Nœud du graphe de dépendances
    /// </summary>
    public class ClassNode
    {
        public string Name { get; set; }
        public ClassDeclarationSyntax Syntax { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public List<string> DependentOn { get; set; } = new();
    }

    /// <summary>
    /// Résultat de l'analyse d'impact
    /// </summary>
    public class ImpactAnalysisResult
    {
        public string MethodName { get; set; }
        public string Action { get; set; }
        public List<string> Callers { get; set; } = new();
        public List<string> AffectedTests { get; set; } = new();
        public List<string> BreakingChanges { get; set; } = new();
    }

    /// <summary>
    /// Représente une dépendance circulaire
    /// </summary>
    public class CircularDependency
    {
        public List<string> Path { get; set; } = new();

        public override string ToString() => string.Join(" -> ", Path) + " -> " + Path[0];
    }
}
