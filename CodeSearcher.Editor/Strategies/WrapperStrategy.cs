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
    /// Stratégie pour wrapper du code (try-catch, logging, etc.)
    /// </summary>
    public class WrapperStrategy : IWrapperStrategy
    {
        public EditResult Wrap(string code, string methodName, string wrapperType, string wrapperCode)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new EditResult { Success = false, ErrorMessage = "Code cannot be empty" };

            if (string.IsNullOrWhiteSpace(methodName))
                return new EditResult { Success = false, ErrorMessage = "Method name cannot be empty" };

            if (string.IsNullOrWhiteSpace(wrapperType))
                return new EditResult { Success = false, ErrorMessage = "Wrapper type cannot be empty" };

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = (CompilationUnitSyntax)tree.GetRoot();

                CSharpSyntaxRewriter? wrapper = wrapperType.ToLower() switch
                {
                    "trycatch" => new TryCatchWrapper(methodName, wrapperCode),
                    "logging" => new LoggingWrapper(methodName, wrapperCode),
                    "validation" => new ValidationWrapper(methodName, wrapperCode),
                    _ => throw new ArgumentException($"Unknown wrapper type: {wrapperType}")
                };

                var newRoot = wrapper.Visit(root);
                var modifiedCode = newRoot?.ToFullString() ?? code;

                return new EditResult
                {
                    Success = true,
                    ModifiedCode = modifiedCode,
                    Changes = new() { $"Wrapped method '{methodName}' with {wrapperType}" }
                };
            }
            catch (Exception ex)
            {
                return new EditResult
                {
                    Success = false,
                    ErrorMessage = $"Error during wrap: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Wrapper try-catch
        /// </summary>
        private class TryCatchWrapper : CSharpSyntaxRewriter
        {
            private readonly string _methodName;
            private readonly string? _exceptionHandling;

            public TryCatchWrapper(string methodName, string? exceptionHandling)
            {
                _methodName = methodName;
                _exceptionHandling = exceptionHandling;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.Text == _methodName && node.Body != null)
                {
                    // Créer la clause catch
                    var catchClause = SyntaxFactory.CatchClause()
                        .WithDeclaration(
                            SyntaxFactory.CatchDeclaration(
                                SyntaxFactory.IdentifierName("Exception"),
                                SyntaxFactory.Identifier("ex")
                            )
                        )
                        .WithBlock(
                            SyntaxFactory.Block(
                                SyntaxFactory.ParseStatement(_exceptionHandling ?? "throw;")
                            )
                        );

                    // Créer le statement try-catch
                    var tryStatement = SyntaxFactory.TryStatement()
                        .WithBlock(node.Body)
                        .WithCatches(new Microsoft.CodeAnalysis.SyntaxList<CatchClauseSyntax>(catchClause));

                    // Envelopper le try-catch dans un bloc
                    var newBody = SyntaxFactory.Block(tryStatement);
                    
                    return node.WithBody(newBody);
                }

                return base.VisitMethodDeclaration(node);
            }
        }

        /// <summary>
        /// Wrapper logging
        /// </summary>
        private class LoggingWrapper : CSharpSyntaxRewriter
        {
            private readonly string _methodName;
            private readonly string? _loggingCode;

            public LoggingWrapper(string methodName, string? loggingCode)
            {
                _methodName = methodName;
                _loggingCode = loggingCode;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.Text == _methodName && node.Body != null)
                {
                    var statements = node.Body.Statements.Insert(
                        0,
                        SyntaxFactory.ParseStatement(_loggingCode ?? "")
                    );

                    var newBody = SyntaxFactory.Block(statements);
                    return node.WithBody(newBody);
                }

                return base.VisitMethodDeclaration(node);
            }
        }

        /// <summary>
        /// Wrapper validation
        /// </summary>
        private class ValidationWrapper : CSharpSyntaxRewriter
        {
            private readonly string _methodName;
            private readonly string? _validationCode;

            public ValidationWrapper(string methodName, string? validationCode)
            {
                _methodName = methodName;
                _validationCode = validationCode;
            }

            public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (node.Identifier.Text == _methodName && node.Body != null)
                {
                    var statements = node.Body.Statements.Insert(
                        0,
                        SyntaxFactory.ParseStatement(_validationCode ?? "")
                    );

                    var newBody = SyntaxFactory.Block(statements);
                    return node.WithBody(newBody);
                }

                return base.VisitMethodDeclaration(node);
            }
        }
    }
}
