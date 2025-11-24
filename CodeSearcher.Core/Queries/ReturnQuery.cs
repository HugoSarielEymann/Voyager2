using CodeSearcher.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Core.Queries
{
    /// <summary>
    /// Implémentation de requête pour les return statements
    /// </summary>
    public class ReturnQuery : Internal.BaseCodeQuery<ReturnStatementSyntax>, IReturnQuery
    {
        private readonly ILogger _logger;

        public ReturnQuery(CompilationUnitSyntax root, ILogger logger = null) : base(root)
        {
            _logger = logger ?? new NullLogger();
        }

        public IReturnQuery InMethod(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

            Predicates.Add(r =>
            {
                var methodParent = r.Parent;
                while (methodParent != null)
                {
                    if (methodParent is MethodDeclarationSyntax method &&
                        method.Identifier.Text == methodName)
                    {
                        return true;
                    }
                    methodParent = methodParent.Parent;
                }
                return false;
            });
            _logger.LogDebug($"Filter: InMethod('{methodName}')");
            return this;
        }

        public IReturnQuery ReturningType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

            Predicates.Add(r =>
            {
                if (r.Expression == null)
                    return false;

                var expressionType = r.Expression.ToString();
                return expressionType.Contains(typeName);
            });
            _logger.LogDebug($"Filter: ReturningType('{typeName}')");
            return this;
        }

        public IReturnQuery ReturningNull()
        {
            Predicates.Add(r =>
                r.Expression != null &&
                r.Expression.IsKind(SyntaxKind.NullLiteralExpression)
            );
            _logger.LogDebug("Filter: ReturningNull()");
            return this;
        }

        public IReturnQuery WithExpression(Func<ExpressionSyntax, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            Predicates.Add(r => r.Expression != null && predicate(r.Expression));
            _logger.LogDebug("Filter: WithExpression(predicate)");
            return this;
        }

        public new IEnumerable<ReturnStatementSyntax> Execute()
        {
            var results = base.Execute().ToList();
            _logger.LogInfo($"FindReturns executed: found {results.Count} return(s)");
            foreach (var result in results)
            {
                _logger.LogSelection($"Return: {result.Expression?.ToString() ?? "void"}", result.ToString());
            }
            return results;
        }
    }
}
