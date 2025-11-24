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
    /// Implémentation de requête pour les variables, champs et propriétés
    /// </summary>
    public class VariableQuery : Internal.BaseCodeQuery<SyntaxNode>
    {
        private List<Func<SyntaxNode, bool>> _variablePredicates;
        private readonly ILogger _logger;

        public VariableQuery(CompilationUnitSyntax root, ILogger logger = null) : base(root)
        {
            _variablePredicates = new List<Func<SyntaxNode, bool>>();
            _logger = logger ?? new NullLogger();
        }

        public VariableQuery WithName(string variableName)
        {
            if (string.IsNullOrWhiteSpace(variableName))
                throw new ArgumentException("Variable name cannot be null or empty", nameof(variableName));

            _variablePredicates.Add(v => GetVariableName(v) == variableName);
            _logger.LogDebug($"Filter: WithName('{variableName}')");
            return this;
        }

        public VariableQuery WithNameContaining(string partialName)
        {
            if (string.IsNullOrWhiteSpace(partialName))
                throw new ArgumentException("Partial name cannot be null or empty", nameof(partialName));

            _variablePredicates.Add(v => 
                GetVariableName(v)?.Contains(partialName, StringComparison.OrdinalIgnoreCase) ?? false);
            _logger.LogDebug($"Filter: WithNameContaining('{partialName}')");
            return this;
        }

        public VariableQuery WithType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

            _variablePredicates.Add(v => GetVariableType(v)?.Contains(typeName) ?? false);
            _logger.LogDebug($"Filter: WithType('{typeName}')");
            return this;
        }

        public VariableQuery WithAttribute(string attributeName)
        {
            if (string.IsNullOrWhiteSpace(attributeName))
                throw new ArgumentException("Attribute name cannot be null or empty", nameof(attributeName));

            _variablePredicates.Add(v => HasAttribute(v, attributeName));
            _logger.LogDebug($"Filter: WithAttribute('{attributeName}')");
            return this;
        }

        public VariableQuery IsPublic()
        {
            _variablePredicates.Add(v => HasModifier(v, Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword));
            _logger.LogDebug("Filter: IsPublic()");
            return this;
        }

        public VariableQuery IsPrivate()
        {
            _variablePredicates.Add(v => HasModifier(v, Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword) ||
                                         (GetModifiers(v).Count == 0));
            _logger.LogDebug("Filter: IsPrivate()");
            return this;
        }

        public VariableQuery IsProtected()
        {
            _variablePredicates.Add(v => HasModifier(v, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ProtectedKeyword));
            _logger.LogDebug("Filter: IsProtected()");
            return this;
        }

        public VariableQuery IsReadOnly()
        {
            _variablePredicates.Add(v => HasModifier(v, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ReadOnlyKeyword));
            _logger.LogDebug("Filter: IsReadOnly()");
            return this;
        }

        public VariableQuery WithInitializer()
        {
            _variablePredicates.Add(v =>
            {
                if (v is PropertyDeclarationSyntax prop)
                    return prop.Initializer != null;
                
                if (v is VariableDeclaratorSyntax varDec)
                {
                    if (varDec.Initializer != null)
                        return true;
                    
                    var parent = varDec.Parent as VariableDeclarationSyntax;
                    if (parent?.Parent is PropertyDeclarationSyntax)
                        return false;
                    
                    return false;
                }
                
                if (v is FieldDeclarationSyntax field)
                    return field.Declaration.Variables.Any(var => var.Initializer != null);
                
                return false;
            });
            _logger.LogDebug("Filter: WithInitializer()");
            return this;
        }

        protected override IEnumerable<SyntaxNode> GetMatches()
        {
            var allVariables = Root.DescendantNodes()
                .Where(n => n is FieldDeclarationSyntax ||
                            n is PropertyDeclarationSyntax ||
                            n is VariableDeclaratorSyntax)
                .SelectMany(ExpandVariables);

            foreach (var predicate in _variablePredicates)
            {
                allVariables = allVariables.Where(predicate);
            }

            return allVariables;
        }

        /// <summary>
        /// Expand compound declarations into individual variables
        /// </summary>
        private IEnumerable<SyntaxNode> ExpandVariables(SyntaxNode node)
        {
            if (node is FieldDeclarationSyntax field)
            {
                return field.Declaration.Variables.Cast<SyntaxNode>();
            }
            else if (node is PropertyDeclarationSyntax prop)
            {
                return new[] { prop };
            }
            else if (node is VariableDeclaratorSyntax varDec)
            {
                return new[] { varDec };
            }
            return new[] { node };
        }

        private string GetVariableName(SyntaxNode node)
        {
            return node switch
            {
                VariableDeclaratorSyntax varDec => varDec.Identifier.Text,
                PropertyDeclarationSyntax prop => prop.Identifier.Text,
                FieldDeclarationSyntax field => field.Declaration.Variables.FirstOrDefault()?.Identifier.Text,
                _ => null
            };
        }

        private string GetVariableType(SyntaxNode node)
        {
            return node switch
            {
                VariableDeclaratorSyntax varDec => 
                    (varDec.Parent as VariableDeclarationSyntax)?.Type.ToString(),
                PropertyDeclarationSyntax prop => prop.Type.ToString(),
                FieldDeclarationSyntax field => field.Declaration.Type.ToString(),
                _ => null
            };
        }

        private bool HasAttribute(SyntaxNode node, string attributeName)
        {
            var attributes = node switch
            {
                PropertyDeclarationSyntax prop => prop.AttributeLists,
                FieldDeclarationSyntax field => field.AttributeLists,
                _ => default
            };

            if (attributes == null)
                return false;

            return attributes.Any(al =>
                al.Attributes.Any(a =>
                    a.Name.ToString().EndsWith(attributeName) ||
                    a.Name.ToString().Contains(attributeName)
                )
            );
        }

        private bool HasModifier(SyntaxNode node, Microsoft.CodeAnalysis.CSharp.SyntaxKind kind)
        {
            var modifiers = GetModifiers(node);
            return modifiers.Any(m => m.IsKind(kind));
        }

        private SyntaxTokenList GetModifiers(SyntaxNode node)
        {
            return node switch
            {
                PropertyDeclarationSyntax prop => prop.Modifiers,
                FieldDeclarationSyntax field => field.Modifiers,
                VariableDeclaratorSyntax varDec => 
                    (varDec.Parent as VariableDeclarationSyntax)?.Parent is FieldDeclarationSyntax fd 
                        ? fd.Modifiers 
                        : default,
                _ => default
            };
        }

        public new IEnumerable<SyntaxNode> Execute()
        {
            var results = GetMatches().ToList();
            _logger.LogInfo($"FindVariables executed: found {results.Count} variable(s)");
            foreach (var result in results)
            {
                _logger.LogSelection($"Variable: {GetVariableName(result)}", result.ToString());
            }
            return results;
        }
    }
}
