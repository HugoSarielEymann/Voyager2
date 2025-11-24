using CodeSearcher.Core.Abstractions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Core.Queries
{
    /// <summary>
    /// Implémentation de requête pour les méthodes
    /// </summary>
    public class MethodQuery : Internal.BaseCodeQuery<MethodDeclarationSyntax>, IMethodQuery
    {
        private readonly ILogger _logger;

        public MethodQuery(CompilationUnitSyntax root, ILogger logger = null) : base(root)
        {
            _logger = logger ?? new NullLogger();
        }

        public IMethodQuery WithName(string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

            Predicates.Add(m => m.Identifier.Text == methodName);
            _logger.LogDebug($"Filter: WithName('{methodName}')");
            return this;
        }

        public IMethodQuery WithNameContaining(string partialName)
        {
            if (string.IsNullOrWhiteSpace(partialName))
                throw new ArgumentException("Partial name cannot be null or empty", nameof(partialName));

            Predicates.Add(m => m.Identifier.Text.Contains(partialName, StringComparison.OrdinalIgnoreCase));
            _logger.LogDebug($"Filter: WithNameContaining('{partialName}')");
            return this;
        }

        public IMethodQuery ReturningTask()
        {
            Predicates.Add(m => 
                m.ReturnType.ToString().StartsWith("Task", StringComparison.Ordinal) ||
                m.ReturnType is GenericNameSyntax gns && gns.Identifier.Text == "Task"
            );
            _logger.LogDebug("Filter: ReturningTask()");
            return this;
        }

        public IMethodQuery ReturningTask<T>()
        {
            var typeName = typeof(T).Name;
            Predicates.Add(m =>
                (m.ReturnType is GenericNameSyntax gns && 
                 gns.Identifier.Text == "Task" && 
                 gns.TypeArgumentList.Arguments.Any(arg => arg.ToString().Contains(typeName)))
            );
            _logger.LogDebug($"Filter: ReturningTask<{typeName}>()");
            return this;
        }

        public IMethodQuery ReturningType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

            Predicates.Add(m => m.ReturnType.ToString().Contains(typeName));
            _logger.LogDebug($"Filter: ReturningType('{typeName}')");
            return this;
        }

        public IMethodQuery IsAsync()
        {
            Predicates.Add(m => m.Modifiers.Any(mod => mod.Kind() == SyntaxKind.AsyncKeyword));
            _logger.LogDebug("Filter: IsAsync()");
            return this;
        }

        public IMethodQuery IsPublic()
        {
            Predicates.Add(m => m.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PublicKeyword));
            _logger.LogDebug("Filter: IsPublic()");
            return this;
        }

        public IMethodQuery IsPrivate()
        {
            Predicates.Add(m => m.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PrivateKeyword) ||
                               (m.Modifiers.Count == 0 || 
                                !m.Modifiers.Any(mod => 
                                    mod.Kind() == SyntaxKind.PublicKeyword ||
                                    mod.Kind() == SyntaxKind.ProtectedKeyword ||
                                    mod.Kind() == SyntaxKind.InternalKeyword)));
            _logger.LogDebug("Filter: IsPrivate()");
            return this;
        }

        public IMethodQuery IsProtected()
        {
            Predicates.Add(m => m.Modifiers.Any(mod => mod.Kind() == SyntaxKind.ProtectedKeyword));
            _logger.LogDebug("Filter: IsProtected()");
            return this;
        }

        public IMethodQuery HasParameterCount(int count)
        {
            if (count < 0)
                throw new ArgumentException("Parameter count cannot be negative", nameof(count));

            Predicates.Add(m => m.ParameterList.Parameters.Count == count);
            _logger.LogDebug($"Filter: HasParameterCount({count})");
            return this;
        }

        public IMethodQuery HasParameter(Func<ParameterSyntax, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            Predicates.Add(m => m.ParameterList.Parameters.Any(predicate));
            _logger.LogDebug("Filter: HasParameter(predicate)");
            return this;
        }

        public IMethodQuery WithAttribute(string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
                throw new ArgumentException("Attribute name cannot be null or empty", nameof(attributeName));

            Predicates.Add(m => m.AttributeLists.Any(al => 
                al.Attributes.Any(a => 
                    a.Name.ToString().EndsWith(attributeName) ||
                    a.Name.ToString().Contains(attributeName)
                )
            ));
            _logger.LogDebug($"Filter: WithAttribute('{attributeName}')");
            return this;
        }

        public new IEnumerable<MethodDeclarationSyntax> Execute()
        {
            var results = base.Execute().ToList();
            _logger.LogInfo($"FindMethods executed: found {results.Count} method(s)");
            foreach (var result in results)
            {
                _logger.LogSelection($"Method: {result.Identifier.Text}", result.ToString());
            }
            return results;
        }
    }
}
