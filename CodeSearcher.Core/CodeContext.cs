using CodeSearcher.Core.Abstractions;
using CodeSearcher.Core.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeSearcher.Core
{
    /// <summary>
    /// Contexte principal pour l'accès aux requêtes de code C#
    /// Supporte l'injection de dépendance pour le logging et l'analyse
    /// </summary>
    public class CodeContext : ICodeContext
    {
        private readonly CompilationUnitSyntax _root;
        private readonly ILogger _logger;
        private readonly IConditionalAnalyzer _conditionalAnalyzer;

        private CodeContext(CompilationUnitSyntax root, ILogger logger = null, IConditionalAnalyzer conditionalAnalyzer = null)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _logger = logger ?? new NullLogger();
            _conditionalAnalyzer = conditionalAnalyzer ?? new ConditionalAnalyzer();
        }

        /// <summary>
        /// Crée un CodeContext à partir d'une chaîne de code C#
        /// </summary>
        public static CodeContext FromCode(string code, ILogger logger = null, IConditionalAnalyzer conditionalAnalyzer = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code cannot be null or empty", nameof(code));

            var tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            return new CodeContext(root, logger, conditionalAnalyzer);
        }

        /// <summary>
        /// Crée un CodeContext à partir d'un fichier C#
        /// </summary>
        public static CodeContext FromFile(string filePath, ILogger logger = null, IConditionalAnalyzer conditionalAnalyzer = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException($"File not found: {filePath}");

            var code = System.IO.File.ReadAllText(filePath);
            return FromCode(code, logger, conditionalAnalyzer);
        }

        /// <summary>
        /// Crée un CodeContext de manière asynchrone à partir d'un fichier C#
        /// </summary>
        public static async Task<CodeContext> FromFileAsync(string filePath, ILogger logger = null, IConditionalAnalyzer conditionalAnalyzer = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException($"File not found: {filePath}");

            var code = await System.IO.File.ReadAllTextAsync(filePath);
            return FromCode(code, logger, conditionalAnalyzer);
        }

        public IMethodQuery FindMethods()
        {
            _logger.LogDebug("FindMethods() called");
            return new Queries.MethodQuery(_root, _logger);
        }

        public IClassQuery FindClasses()
        {
            _logger.LogDebug("FindClasses() called");
            return new Queries.ClassQuery(_root, _logger);
        }

        public Queries.VariableQuery FindVariables()
        {
            _logger.LogDebug("FindVariables() called");
            return new Queries.VariableQuery(_root, _logger);
        }

        public IReturnQuery FindReturns()
        {
            _logger.LogDebug("FindReturns() called");
            return new Queries.ReturnQuery(_root, _logger);
        }

        public IEnumerable<SyntaxNode> FindByPredicate(Func<SyntaxNode, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            _logger.LogDebug("FindByPredicate() called");
            return _root.DescendantNodes().Where(predicate);
        }

        /// <summary>
        /// Trouve les conditions menant à une instruction donnée
        /// </summary>
        public IEnumerable<ConditionPath> FindConditionsLeadingTo(StatementSyntax statement)
        {
            if (statement == null)
                throw new ArgumentNullException(nameof(statement));

            var conditions = _conditionalAnalyzer.GetConditionsLeadingTo(statement);
            _logger.LogDebug($"Found {conditions.Count()} conditions leading to statement");
            return conditions;
        }

        /// <summary>
        /// Trouve tous les chemins conditionnels dans une méthode
        /// </summary>
        public IEnumerable<(StatementSyntax statement, List<ConditionPath> conditions)> FindAllConditionalPaths(MethodDeclarationSyntax method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            var paths = _conditionalAnalyzer.GetAllConditionalPaths(method);
            _logger.LogDebug($"Found {paths.Count()} conditional paths in method '{method.Identifier.Text}'");
            return paths;
        }

        /// <summary>
        /// Détermine si une instruction est atteignable
        /// </summary>
        public bool IsStatementReachable(StatementSyntax statement)
        {
            var isReachable = _conditionalAnalyzer.IsStatementReachable(statement);
            _logger.LogDebug($"Statement reachability: {isReachable}");
            return isReachable;
        }

        /// <summary>
        /// Détermine si une instruction est toujours exécutée
        /// </summary>
        public bool IsStatementUnconditionallyReachable(StatementSyntax statement)
        {
            var isUnconditional = _conditionalAnalyzer.IsStatementUnconditionallyReachable(statement);
            _logger.LogDebug($"Statement unconditional reachability: {isUnconditional}");
            return isUnconditional;
        }
    }
}
