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
    /// Stratégie avancée de wrapper avec sélection de bloc et primitives de contrôle
    /// </summary>
    public class AdvancedWrapperStrategy : IWrapperStrategy
    {
        public EditResult Wrap(string code, string methodName, string wrapperType, string wrapperCode)
        {
            // Déléguer à la stratégie existante pour compatibilité
            var basicWrapper = new WrapperStrategy();
            return basicWrapper.Wrap(code, methodName, wrapperType, wrapperCode);
        }
    }

    /// <summary>
    /// Types de primitives de wrapper supportées
    /// </summary>
    public enum ControlFlowPrimitive
    {
        /// <summary>If statement</summary>
        If,
        
        /// <summary>Foreach loop</summary>
        ForEach,
        
        /// <summary>While loop</summary>
        While,
        
        /// <summary>For loop</summary>
        For,
        
        /// <summary>Do-while loop</summary>
        DoWhile,
        
        /// <summary>Try-catch block</summary>
        TryCatch,
        
        /// <summary>Lock statement</summary>
        Lock,
        
        /// <summary>Using statement</summary>
        Using,
        
        /// <summary>Checked block</summary>
        Checked,
        
        /// <summary>Unchecked block</summary>
        Unchecked
    }

    /// <summary>
    /// Configuration pour un wrapper de primitive
    /// </summary>
    public class PrimitiveWrapperConfig
    {
        /// <summary>Type de primitive</summary>
        public ControlFlowPrimitive Primitive { get; set; }

        /// <summary>Condition ou expression (pour if, while, etc.)</summary>
        public string? Expression { get; set; }

        /// <summary>Déclaration de variable (pour foreach, for, using)</summary>
        public string? VariableDeclaration { get; set; }

        /// <summary>Code d'initialisation (pour try-catch)</summary>
        public string? InitializationCode { get; set; }

        /// <summary>Code de finalisation (pour try-catch, using)</summary>
        public string? FinalizationCode { get; set; }

        public PrimitiveWrapperConfig()
        {
        }

        public PrimitiveWrapperConfig(ControlFlowPrimitive primitive, string expression = "")
        {
            Primitive = primitive;
            Expression = expression;
        }
    }

    /// <summary>
    /// Sélecteur de bloc de code basé sur des critères
    /// </summary>
    public class CodeBlockSelector
    {
        private readonly SyntaxNode _root;
        private readonly string _methodName;

        public CodeBlockSelector(string code, string methodName)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            _root = tree.GetRoot();
            _methodName = methodName;
        }

        /// <summary>
        /// Sélectionne un bloc entre deux marqueurs de variable
        /// </summary>
        public List<StatementSyntax> SelectBetweenVariables(
            string firstVariableType,
            string secondVariableType)
        {
            var method = FindMethod(_methodName);
            if (method?.Body == null)
                return new List<StatementSyntax>();

            var statements = method.Body.Statements.ToList();
            var firstVarIndex = FindLastVariableOfType(statements, firstVariableType);
            var secondVarIndex = FindFirstVariableOfType(statements, secondVariableType, firstVarIndex + 1);

            if (firstVarIndex >= 0 && secondVarIndex >= 0 && firstVarIndex < secondVarIndex)
            {
                return statements.Skip(firstVarIndex + 1).Take(secondVarIndex - firstVarIndex - 1).ToList();
            }

            return new List<StatementSyntax>();
        }

        /// <summary>
        /// Sélectionne un bloc entre deux noms de variable
        /// </summary>
        public List<StatementSyntax> SelectBetweenVariableNames(
            string firstName,
            string secondName)
        {
            var method = FindMethod(_methodName);
            if (method?.Body == null)
                return new List<StatementSyntax>();

            var statements = method.Body.Statements.ToList();
            var firstIndex = FindLastVariableByName(statements, firstName);
            var secondIndex = FindFirstVariableByName(statements, secondName, firstIndex + 1);

            if (firstIndex >= 0 && secondIndex >= 0 && firstIndex < secondIndex)
            {
                return statements.Skip(firstIndex + 1).Take(secondIndex - firstIndex - 1).ToList();
            }

            return new List<StatementSyntax>();
        }

        /// <summary>
        /// Sélectionne un bloc entre deux indices
        /// </summary>
        public List<StatementSyntax> SelectBetweenIndices(int startIndex, int endIndex)
        {
            var method = FindMethod(_methodName);
            if (method?.Body == null || startIndex < 0 || endIndex >= method.Body.Statements.Count)
                return new List<StatementSyntax>();

            return method.Body.Statements.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();
        }

        /// <summary>
        /// Sélectionne toutes les lignes contenant un identifiant
        /// </summary>
        public List<StatementSyntax> SelectStatementsContainingIdentifier(string identifier)
        {
            var method = FindMethod(_methodName);
            if (method?.Body == null)
                return new List<StatementSyntax>();

            return method.Body.Statements
                .Where(s => s.ToString().Contains(identifier))
                .ToList();
        }

        /// <summary>
        /// Sélectionne un bloc entre deux commentaires
        /// </summary>
        public List<StatementSyntax> SelectBetweenComments(string startComment, string endComment)
        {
            var method = FindMethod(_methodName);
            if (method?.Body == null)
                return new List<StatementSyntax>();

            var code = method.ToString();
            var startIdx = code.IndexOf(startComment);
            var endIdx = code.IndexOf(endComment, startIdx);

            if (startIdx >= 0 && endIdx >= 0)
            {
                // Rechercher les statements qui correspondent à cette plage
                var statements = method.Body.Statements.ToList();
                var selected = new List<StatementSyntax>();

                foreach (var stmt in statements)
                {
                    var stmtCode = stmt.ToString();
                    if (stmtCode.Length > 0)
                    {
                        selected.Add(stmt);
                    }
                }

                return selected;
            }

            return new List<StatementSyntax>();
        }

        private MethodDeclarationSyntax? FindMethod(string methodName)
        {
            return _root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == methodName);
        }

        private int FindLastVariableOfType(List<StatementSyntax> statements, string typeName)
        {
            for (int i = statements.Count - 1; i >= 0; i--)
            {
                var stmt = statements[i];
                if (stmt is LocalDeclarationStatementSyntax decl)
                {
                    if (decl.Declaration.Type.ToString().Contains(typeName))
                        return i;
                }
            }
            return -1;
        }

        private int FindFirstVariableOfType(List<StatementSyntax> statements, string typeName, int startIndex = 0)
        {
            for (int i = startIndex; i < statements.Count; i++)
            {
                var stmt = statements[i];
                if (stmt is LocalDeclarationStatementSyntax decl)
                {
                    if (decl.Declaration.Type.ToString().Contains(typeName))
                        return i;
                }
            }
            return -1;
        }

        private int FindLastVariableByName(List<StatementSyntax> statements, string varName)
        {
            for (int i = statements.Count - 1; i >= 0; i--)
            {
                if (statements[i].ToString().Contains(varName))
                    return i;
            }
            return -1;
        }

        private int FindFirstVariableByName(List<StatementSyntax> statements, string varName, int startIndex = 0)
        {
            for (int i = startIndex; i < statements.Count; i++)
            {
                if (statements[i].ToString().Contains(varName))
                    return i;
            }
            return -1;
        }
    }

    /// <summary>
    /// Wrapper de bloc avec primitives de contrôle
    /// </summary>
    public class ControlFlowBlockWrapper
    {
        private readonly string _code;
        private readonly string _methodName;

        public ControlFlowBlockWrapper(string code, string methodName)
        {
            _code = code;
            _methodName = methodName;
        }

        /// <summary>
        /// Enveloppe un bloc de code sélectionné avec une primitive de contrôle
        /// </summary>
        public EditResult WrapSelectedBlock(
            List<StatementSyntax> selectedStatements,
            PrimitiveWrapperConfig config)
        {
            if (selectedStatements == null || selectedStatements.Count == 0)
                return new EditResult 
                { 
                    Success = false, 
                    ErrorMessage = "No statements selected" 
                };

            try
            {
                var tree = CSharpSyntaxTree.ParseText(_code);
                var root = (CompilationUnitSyntax)tree.GetRoot();

                var rewriter = new BlockWrapperRewriter(_methodName, selectedStatements, config);
                var newRoot = rewriter.Visit(root);
                var modifiedCode = newRoot?.ToFullString() ?? _code;

                return new EditResult
                {
                    Success = true,
                    ModifiedCode = modifiedCode,
                    Changes = new() 
                    { 
                        $"Wrapped {selectedStatements.Count} statement(s) with {config.Primitive}" 
                    }
                };
            }
            catch (Exception ex)
            {
                return new EditResult
                {
                    Success = false,
                    ErrorMessage = $"Error wrapping block: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Wrapper fluent pour chaîner les sélections et wrappages
        /// </summary>
        public class FluentWrapper
        {
            private readonly ControlFlowBlockWrapper _wrapper;
            private List<StatementSyntax>? _selectedStatements;

            public FluentWrapper(string code, string methodName)
            {
                _wrapper = new ControlFlowBlockWrapper(code, methodName);
            }

            /// <summary>
            /// Sélectionner entre deux types de variable
            /// </summary>
            public FluentWrapper SelectBetweenTypes(string firstType, string secondType)
            {
                var selector = new CodeBlockSelector(_wrapper._code, _wrapper._methodName);
                _selectedStatements = selector.SelectBetweenVariables(firstType, secondType);
                return this;
            }

            /// <summary>
            /// Sélectionner entre deux noms de variable
            /// </summary>
            public FluentWrapper SelectBetweenNames(string firstName, string secondName)
            {
                var selector = new CodeBlockSelector(_wrapper._code, _wrapper._methodName);
                _selectedStatements = selector.SelectBetweenVariableNames(firstName, secondName);
                return this;
            }

            /// <summary>
            /// Sélectionner entre deux indices
            /// </summary>
            public FluentWrapper SelectBetweenIndices(int start, int end)
            {
                var selector = new CodeBlockSelector(_wrapper._code, _wrapper._methodName);
                _selectedStatements = selector.SelectBetweenIndices(start, end);
                return this;
            }

            /// <summary>
            /// Sélectionner les statements contenant un identifiant
            /// </summary>
            public FluentWrapper SelectStatementsWithIdentifier(string identifier)
            {
                var selector = new CodeBlockSelector(_wrapper._code, _wrapper._methodName);
                _selectedStatements = selector.SelectStatementsContainingIdentifier(identifier);
                return this;
            }

            /// <summary>
            /// Wrapper avec If
            /// </summary>
            public EditResult WithIf(string condition)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.If, condition);
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec Foreach
            /// </summary>
            public EditResult WithForEach(string itemType, string itemName, string collection)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.ForEach)
                {
                    VariableDeclaration = $"{itemType} {itemName} in {collection}"
                };
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec While
            /// </summary>
            public EditResult WithWhile(string condition)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.While, condition);
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec For
            /// </summary>
            public EditResult WithFor(string init, string condition, string increment)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.For)
                {
                    VariableDeclaration = $"{init}; {condition}; {increment}"
                };
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec DoWhile
            /// </summary>
            public EditResult WithDoWhile(string condition)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.DoWhile, condition);
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec Try-Catch
            /// </summary>
            public EditResult WithTryCatch(string? catchCode = null)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.TryCatch)
                {
                    FinalizationCode = catchCode ?? "throw;"
                };
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec Lock
            /// </summary>
            public EditResult WithLock(string lockObject)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.Lock, lockObject);
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec Using
            /// </summary>
            public EditResult WithUsing(string resourceDeclaration)
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.Using)
                {
                    VariableDeclaration = resourceDeclaration
                };
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec Checked
            /// </summary>
            public EditResult WithChecked()
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.Checked);
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }

            /// <summary>
            /// Wrapper avec Unchecked
            /// </summary>
            public EditResult WithUnchecked()
            {
                var config = new PrimitiveWrapperConfig(ControlFlowPrimitive.Unchecked);
                return _wrapper.WrapSelectedBlock(_selectedStatements!, config);
            }
        }

        /// <summary>
        /// Créer un wrapper fluent
        /// </summary>
        public static FluentWrapper Create(string code, string methodName)
        {
            return new FluentWrapper(code, methodName);
        }
    }

    /// <summary>
    /// Rewriter pour wrapper un bloc avec une primitive
    /// </summary>
    internal class BlockWrapperRewriter : CSharpSyntaxRewriter
    {
        private readonly string _methodName;
        private readonly List<StatementSyntax> _selectedStatements;
        private readonly PrimitiveWrapperConfig _config;
        private bool _foundMethod = false;

        public BlockWrapperRewriter(
            string methodName,
            List<StatementSyntax> selectedStatements,
            PrimitiveWrapperConfig config)
        {
            _methodName = methodName;
            _selectedStatements = selectedStatements;
            _config = config;
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (node.Identifier.Text == _methodName && node.Body != null && !_foundMethod)
            {
                _foundMethod = true;
                var statements = node.Body.Statements.ToList();
                var newStatements = new List<StatementSyntax>();

                var skipUntil = -1;
                for (int i = 0; i < statements.Count; i++)
                {
                    if (i < skipUntil)
                        continue;

                    // Vérifier si ce statement fait partie de la sélection
                    var isSelected = _selectedStatements.Any(s => s.IsEquivalentTo(statements[i]));

                    if (isSelected && i == skipUntil + 1)
                    {
                        // Commencer le wrapper
                        var wrappedStatements = new List<StatementSyntax>();
                        for (int j = i; j < statements.Count && _selectedStatements.Any(s => s.IsEquivalentTo(statements[j])); j++)
                        {
                            wrappedStatements.Add(statements[j]);
                            skipUntil = j;
                        }

                        var wrappedStatement = CreateWrapperStatement(wrappedStatements, _config);
                        newStatements.Add(wrappedStatement);
                    }
                    else if (!isSelected || i > skipUntil)
                    {
                        newStatements.Add(statements[i]);
                    }
                }

                var newBody = SyntaxFactory.Block(newStatements);
                return node.WithBody(newBody);
            }

            return base.VisitMethodDeclaration(node);
        }

        private StatementSyntax CreateWrapperStatement(
            List<StatementSyntax> statements,
            PrimitiveWrapperConfig config)
        {
            var block = SyntaxFactory.Block(statements);

            return config.Primitive switch
            {
                ControlFlowPrimitive.If =>
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.ParseExpression(config.Expression ?? "true"),
                        block
                    ),

                ControlFlowPrimitive.ForEach =>
                    SyntaxFactory.ForEachStatement(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.Identifier(config.VariableDeclaration?.Split(" in ")[0] ?? "item"),
                        SyntaxFactory.ParseExpression(config.VariableDeclaration?.Split(" in ")[1] ?? "items"),
                        block
                    ),

                ControlFlowPrimitive.While =>
                    SyntaxFactory.WhileStatement(
                        SyntaxFactory.ParseExpression(config.Expression ?? "true"),
                        block
                    ),

                ControlFlowPrimitive.DoWhile =>
                    SyntaxFactory.DoStatement(
                        block,
                        SyntaxFactory.ParseExpression(config.Expression ?? "true")
                    ),

                ControlFlowPrimitive.TryCatch =>
                    SyntaxFactory.TryStatement()
                        .WithBlock(block)
                        .WithCatches(
                            new Microsoft.CodeAnalysis.SyntaxList<CatchClauseSyntax>(
                                SyntaxFactory.CatchClause()
                                    .WithDeclaration(
                                        SyntaxFactory.CatchDeclaration(
                                            SyntaxFactory.IdentifierName("Exception"),
                                            SyntaxFactory.Identifier("ex")
                                        )
                                    )
                                    .WithBlock(
                                        SyntaxFactory.Block(
                                            SyntaxFactory.ParseStatement(config.FinalizationCode ?? "throw;")
                                        )
                                    )
                            )
                        ),

                ControlFlowPrimitive.Lock =>
                    SyntaxFactory.LockStatement(
                        SyntaxFactory.ParseExpression(config.Expression ?? "this"),
                        block
                    ),

                ControlFlowPrimitive.Using =>
                    SyntaxFactory.UsingStatement(
                        null,
                        SyntaxFactory.ParseExpression(config.VariableDeclaration ?? "null"),
                        block
                    ),

                ControlFlowPrimitive.Checked =>
                    SyntaxFactory.CheckedStatement(
                        SyntaxKind.CheckedStatement,
                        block
                    ),

                ControlFlowPrimitive.Unchecked =>
                    SyntaxFactory.CheckedStatement(
                        SyntaxKind.UncheckedStatement,
                        block
                    ),

                _ => block
            };
        }
    }
}
