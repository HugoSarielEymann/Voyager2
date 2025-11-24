using CodeSearcher.Editor.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Editor.Strategies
{
    /// <summary>
    /// Stratégie pour wrapper les returns d'une méthode dans Task<T>
    /// Modifie la signature de la méthode et enveloppe les returns
    /// </summary>
    public class ReturnTypeWrapperStrategy
    {
        private readonly string _code;

        public ReturnTypeWrapperStrategy(string code)
        {
            _code = code ?? throw new ArgumentNullException(nameof(code));
        }

        /// <summary>
        /// Enveloppe les returns d'une méthode dans Task<ReturnType>
        /// et rend la méthode asynchrone
        /// </summary>
        public EditResult WrapReturnsInTask(string methodName)
        {
            return WrapReturnsInTask(methodName, ReturnWrapStyle.TaskFromResult);
        }

        /// <summary>
        /// Enveloppe les returns dans Task.FromResult ou await
        /// </summary>
        public EditResult WrapReturnsInTask(string methodName, ReturnWrapStyle style)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                return new EditResult
                {
                    Success = false,
                    ErrorMessage = "Method name cannot be null or empty"
                };

            try
            {
                var tree = CSharpSyntaxTree.ParseText(_code);
                var root = (CompilationUnitSyntax)tree.GetRoot();

                var rewriter = new ReturnTypeWrapperRewriter(methodName, style);
                var newRoot = (CompilationUnitSyntax)rewriter.Visit(root);
                var modifiedCode = newRoot?.ToFullString() ?? _code;

                if (!rewriter.MethodFound)
                {
                    return new EditResult
                    {
                        Success = false,
                        ErrorMessage = $"Method '{methodName}' not found"
                    };
                }

                return new EditResult
                {
                    Success = true,
                    ModifiedCode = modifiedCode,
                    Changes = new()
                    {
                        $"Changed method '{methodName}' to async Task<T>",
                        $"Wrapped {rewriter.ReturnStatementsModified} return statement(s) with {style}",
                        $"Original return type: {rewriter.OriginalReturnType}"
                    }
                };
            }
            catch (Exception ex)
            {
                return new EditResult
                {
                    Success = false,
                    ErrorMessage = $"Error wrapping returns in Task: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Style de wrapping pour les returns
    /// </summary>
    public enum ReturnWrapStyle
    {
        /// <summary>Task.FromResult(value)</summary>
        TaskFromResult,

        /// <summary>await Task.FromResult(value)</summary>
        AwaitTaskFromResult,

        /// <summary>Task.CompletedTask pour void, Task.FromResult pour T</summary>
        Auto
    }

    /// <summary>
    /// Rewriter pour modifier la signature et wrapper les returns
    /// </summary>
    internal class ReturnTypeWrapperRewriter : CSharpSyntaxRewriter
    {
        private readonly string _methodName;
        private readonly ReturnWrapStyle _style;
        private bool _methodFound;
        private string _originalReturnType = "";
        private int _returnStatementsModified;
        private bool _processingTargetMethod;

        public bool MethodFound => _methodFound;
        public string OriginalReturnType => _originalReturnType;
        public int ReturnStatementsModified => _returnStatementsModified;

        public ReturnTypeWrapperRewriter(string methodName, ReturnWrapStyle style = ReturnWrapStyle.TaskFromResult)
        {
            _methodName = methodName;
            _style = style;
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.Text == _methodName && !_methodFound)
            {
                _methodFound = true;
                _originalReturnType = node.ReturnType.ToString();
                _processingTargetMethod = true;

                // Ne pas modifier les méthodes déjà asynchrones ou void
                if (node.Modifiers.Any(m => m.Kind() == SyntaxKind.AsyncKeyword))
                {
                    _processingTargetMethod = false;
                    return base.VisitMethodDeclaration(node);
                }

                MethodDeclarationSyntax result;

                if (node.ReturnType.Kind() == SyntaxKind.VoidKeyword)
                {
                    result = ModifyVoidMethod(node);
                }
                else
                {
                    // Changer la signature
                    var newReturnType = SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Task"),
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(node.ReturnType)
                        )
                    );

                    var newModifiers = node.Modifiers.Add(
                        SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                    );

                    // Visiter le body pour modifier les returns
                    var newBody = node.Body != null
                        ? (BlockSyntax?)Visit(node.Body)
                        : node.Body;

                    result = node
                        .WithReturnType(newReturnType)
                        .WithModifiers(newModifiers)
                        .WithBody(newBody);
                }

                _processingTargetMethod = false;
                return result;
            }

            return base.VisitMethodDeclaration(node);
        }

        private MethodDeclarationSyntax ModifyVoidMethod(MethodDeclarationSyntax node)
        {
            // Changer void ? Task
            var newReturnType = SyntaxFactory.IdentifierName("Task");

            var newModifiers = node.Modifiers.Add(
                SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
            );

            var newBody = node.Body != null
                ? (BlockSyntax?)Visit(node.Body)
                : node.Body;

            return node
                .WithReturnType(newReturnType)
                .WithModifiers(newModifiers)
                .WithBody(newBody);
        }

        public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
        {
            if (!_processingTargetMethod || node.Expression == null)
                return base.VisitReturnStatement(node);

            _returnStatementsModified++;

            var wrappedExpression = _style switch
            {
                ReturnWrapStyle.TaskFromResult =>
                    WrapWithTaskFromResult(node.Expression),

                ReturnWrapStyle.AwaitTaskFromResult =>
                    WrapWithAwaitTaskFromResult(node.Expression),

                ReturnWrapStyle.Auto =>
                    WrapWithAuto(node.Expression),

                _ => node.Expression
            };

            return node.WithExpression(wrappedExpression);
        }

        private ExpressionSyntax WrapWithTaskFromResult(ExpressionSyntax expression)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Task"),
                    SyntaxFactory.IdentifierName("FromResult")
                ),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(expression)
                    )
                )
            );
        }

        private ExpressionSyntax WrapWithAwaitTaskFromResult(ExpressionSyntax expression)
        {
            var taskFromResult = WrapWithTaskFromResult(expression);
            return SyntaxFactory.AwaitExpression(taskFromResult);
        }

        private ExpressionSyntax WrapWithAuto(ExpressionSyntax expression)
        {
            var expressionText = expression.ToString();

            if (expressionText.Equals("void", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(expressionText))
            {
                return SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Task"),
                    SyntaxFactory.IdentifierName("CompletedTask")
                );
            }

            return WrapWithTaskFromResult(expression);
        }
    }
}
