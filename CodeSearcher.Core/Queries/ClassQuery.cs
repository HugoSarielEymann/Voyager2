using CodeSearcher.Core.Abstractions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Core.Queries
{
    /// <summary>
    /// Implémentation de requête pour les classes
    /// </summary>
    public class ClassQuery : Internal.BaseCodeQuery<ClassDeclarationSyntax>, IClassQuery
    {
        private readonly ILogger _logger;

        public ClassQuery(CompilationUnitSyntax root, ILogger logger = null) : base(root)
        {
            _logger = logger ?? new NullLogger();
        }

        public IClassQuery WithName(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Class name cannot be null or empty", nameof(className));

            Predicates.Add(c => c.Identifier.Text == className);
            _logger.LogDebug($"Filter: WithName('{className}')");
            return this;
        }

        public IClassQuery WithNameContaining(string partialName)
        {
            if (string.IsNullOrWhiteSpace(partialName))
                throw new ArgumentException("Partial name cannot be null or empty", nameof(partialName));

            Predicates.Add(c => c.Identifier.Text.Contains(partialName, StringComparison.OrdinalIgnoreCase));
            _logger.LogDebug($"Filter: WithNameContaining('{partialName}')");
            return this;
        }

        public IClassQuery InNamespace(string namespaceName)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                throw new ArgumentException("Namespace name cannot be null or empty", nameof(namespaceName));

            Predicates.Add(c =>
            {
                var parent = c.Parent;
                while (parent != null)
                {
                    if (parent is NamespaceDeclarationSyntax ns)
                    {
                        return ns.Name.ToString() == namespaceName ||
                               ns.Name.ToString().StartsWith(namespaceName);
                    }
                    if (parent is FileScopedNamespaceDeclarationSyntax fsns)
                    {
                        return fsns.Name.ToString() == namespaceName ||
                               fsns.Name.ToString().StartsWith(namespaceName);
                    }
                    parent = parent.Parent;
                }
                return false;
            });
            _logger.LogDebug($"Filter: InNamespace('{namespaceName}')");
            return this;
        }

        public IClassQuery WithAttribute(string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
                throw new ArgumentException("Attribute name cannot be null or empty", nameof(attributeName));

            Predicates.Add(c => c.AttributeLists.Any(al =>
                al.Attributes.Any(a =>
                    a.Name.ToString().EndsWith(attributeName) ||
                    a.Name.ToString().Contains(attributeName)
                )
            ));
            _logger.LogDebug($"Filter: WithAttribute('{attributeName}')");
            return this;
        }

        public IClassQuery IsAbstract()
        {
            Predicates.Add(c => c.Modifiers.Any(mod => mod.Kind() == SyntaxKind.AbstractKeyword));
            _logger.LogDebug("Filter: IsAbstract()");
            return this;
        }

        public IClassQuery IsSealed()
        {
            Predicates.Add(c => c.Modifiers.Any(mod => mod.Kind() == SyntaxKind.SealedKeyword));
            _logger.LogDebug("Filter: IsSealed()");
            return this;
        }

        public IClassQuery IsPublic()
        {
            Predicates.Add(c => c.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PublicKeyword));
            _logger.LogDebug("Filter: IsPublic()");
            return this;
        }

        public IClassQuery Inherits(string baseClassName)
        {
            if (string.IsNullOrWhiteSpace(baseClassName))
                throw new ArgumentException("Base class name cannot be null or empty", nameof(baseClassName));

            Predicates.Add(c => c.BaseList?.Types.Any(bt => 
                bt.Type.ToString().Contains(baseClassName)
            ) ?? false);
            _logger.LogDebug($"Filter: Inherits('{baseClassName}')");
            return this;
        }

        public IClassQuery Implements(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
                throw new ArgumentException("Interface name cannot be null or empty", nameof(interfaceName));

            Predicates.Add(c => c.BaseList?.Types.Any(bt => 
                bt.Type.ToString().Contains(interfaceName)
            ) ?? false);
            _logger.LogDebug($"Filter: Implements('{interfaceName}')");
            return this;
        }

        public IClassQuery WithMemberCount(int count, Func<int, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            Predicates.Add(c => predicate(c.Members.Count));
            _logger.LogDebug($"Filter: WithMemberCount({count})");
            return this;
        }

        public new IEnumerable<ClassDeclarationSyntax> Execute()
        {
            var results = base.Execute().ToList();
            _logger.LogInfo($"FindClasses executed: found {results.Count} class(es)");
            foreach (var result in results)
            {
                _logger.LogSelection($"Class: {result.Identifier.Text}", result.ToString());
            }
            return results;
        }
    }
}
