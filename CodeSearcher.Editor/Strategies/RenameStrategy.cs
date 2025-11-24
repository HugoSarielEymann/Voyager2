using CodeSearcher.Core;
using CodeSearcher.Editor.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;

namespace CodeSearcher.Editor.Strategies
{
    /// <summary>
    /// Stratégie de renommage d'entités C#
    /// </summary>
    public class RenameStrategy : IRenameStrategy
    {
        public EditResult Rename(string code, string oldName, string newName, string entityType)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new EditResult { Success = false, ErrorMessage = "Code cannot be empty" };

            if (string.IsNullOrWhiteSpace(oldName))
                return new EditResult { Success = false, ErrorMessage = "Old name cannot be empty" };

            if (string.IsNullOrWhiteSpace(newName))
                return new EditResult { Success = false, ErrorMessage = "New name cannot be empty" };

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = (CompilationUnitSyntax)tree.GetRoot();
                
                CSharpSyntaxRewriter? rewriter = entityType?.ToLower() switch
                {
                    "method" => new MethodRenameRewriter(oldName, newName),
                    "class" => new ClassRenameRewriter(oldName, newName),
                    "variable" => new VariableRenameRewriter(oldName, newName),
                    "property" => new PropertyRenameRewriter(oldName, newName),
                    _ => throw new ArgumentException($"Unknown entity type: {entityType}")
                };

                var newRoot = rewriter.Visit(root);
                var modifiedCode = newRoot?.ToFullString() ?? code;

                return new EditResult
                {
                    Success = true,
                    ModifiedCode = modifiedCode,
                    Changes = new() { $"Renamed {entityType} '{oldName}' to '{newName}'" }
                };
            }
            catch (Exception ex)
            {
                return new EditResult
                {
                    Success = false,
                    ErrorMessage = $"Error during rename: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Rewriter pour renommer les méthodes
        /// </summary>
        private class MethodRenameRewriter : CSharpSyntaxRewriter
        {
            private readonly string _oldName;
            private readonly string _newName;

            public MethodRenameRewriter(string oldName, string newName)
            {
                _oldName = oldName;
                _newName = newName;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.Text == _oldName)
                {
                    return node.WithIdentifier(SyntaxFactory.Identifier(_newName));
                }
                return base.VisitMethodDeclaration(node);
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                // Rename method calls
                if (node.Expression is IdentifierNameSyntax idn && idn.Identifier.Text == _oldName)
                {
                    return node.WithExpression(SyntaxFactory.IdentifierName(_newName));
                }
                if (node.Expression is MemberAccessExpressionSyntax mae && 
                    mae.Name.Identifier.Text == _oldName)
                {
                    return node.WithExpression(
                        mae.WithName(SyntaxFactory.IdentifierName(_newName))
                    );
                }
                return base.VisitInvocationExpression(node);
            }
        }

        /// <summary>
        /// Rewriter pour renommer les classes
        /// </summary>
        private class ClassRenameRewriter : CSharpSyntaxRewriter
        {
            private readonly string _oldName;
            private readonly string _newName;

            public ClassRenameRewriter(string oldName, string newName)
            {
                _oldName = oldName;
                _newName = newName;
            }

            public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var updated = (ClassDeclarationSyntax?)base.VisitClassDeclaration(node);
                
                if (updated?.Identifier.Text == _oldName)
                {
                    updated = updated.WithIdentifier(SyntaxFactory.Identifier(_newName));
                }
                
                return updated;
            }

            public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                if (node.Type is IdentifierNameSyntax idn && idn.Identifier.Text == _oldName)
                {
                    return node.WithType(SyntaxFactory.IdentifierName(_newName));
                }
                return base.VisitObjectCreationExpression(node);
            }
        }

        /// <summary>
        /// Rewriter pour renommer les variables
        /// </summary>
        private class VariableRenameRewriter : CSharpSyntaxRewriter
        {
            private readonly string _oldName;
            private readonly string _newName;

            public VariableRenameRewriter(string oldName, string newName)
            {
                _oldName = oldName;
                _newName = newName;
            }

            public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                if (node.Identifier.Text == _oldName)
                {
                    return node.WithIdentifier(SyntaxFactory.Identifier(_newName));
                }
                return base.VisitVariableDeclarator(node);
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.Text == _oldName)
                {
                    return SyntaxFactory.IdentifierName(_newName);
                }
                return base.VisitIdentifierName(node);
            }
        }

        /// <summary>
        /// Rewriter pour renommer les propriétés
        /// </summary>
        private class PropertyRenameRewriter : CSharpSyntaxRewriter
        {
            private readonly string _oldName;
            private readonly string _newName;

            public PropertyRenameRewriter(string oldName, string newName)
            {
                _oldName = oldName;
                _newName = newName;
            }

            public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                if (node.Identifier.Text == _oldName)
                {
                    return node.WithIdentifier(SyntaxFactory.Identifier(_newName));
                }
                return base.VisitPropertyDeclaration(node);
            }

            public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.Text == _oldName)
                {
                    return SyntaxFactory.IdentifierName(_newName);
                }
                return base.VisitIdentifierName(node);
            }
        }
    }
}
