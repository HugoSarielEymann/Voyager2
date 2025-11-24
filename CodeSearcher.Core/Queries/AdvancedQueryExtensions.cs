using CodeSearcher.Core.Abstractions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Core.Queries
{
    /// <summary>
    /// Extensions pour les sélecteurs avancés
    /// Implémente les fonctionnalités manquantes identifiées
    /// </summary>
    public static class AdvancedQueryExtensions
    {
        /// <summary>
        /// Filtre les méthodes avec une complexité cyclomatique > N
        /// </summary>
        public static List<MethodDeclarationSyntax> FilterByCyclomaticComplexity(
            this IEnumerable<MethodDeclarationSyntax> methods, 
            int greaterThan)
        {
            return methods
                .Where(m => CalculateCyclomaticComplexity(m) > greaterThan)
                .ToList();
        }

        /// <summary>
        /// Filtre les méthodes avec plus de N lignes
        /// </summary>
        public static List<MethodDeclarationSyntax> FilterByBodyLines(
            this IEnumerable<MethodDeclarationSyntax> methods,
            int greaterThan)
        {
            return methods
                .Where(m => 
                    m.Body?.Statements.Count > greaterThan || 
                    (m.ExpressionBody != null && 1 > greaterThan)
                )
                .ToList();
        }

        /// <summary>
        /// Filtre les méthodes avec des paramètres inutilisés
        /// </summary>
        public static List<MethodDeclarationSyntax> FilterWithUnusedParameters(
            this IEnumerable<MethodDeclarationSyntax> methods)
        {
            return methods
                .Where(m => HasUnusedParameters(m))
                .ToList();
        }

        /// <summary>
        /// Filtre les classes orphelines (jamais utilisées)
        /// </summary>
        public static List<ClassDeclarationSyntax> FilterOrphanClasses(
            this IEnumerable<ClassDeclarationSyntax> classes)
        {
            return classes
                .Where(c => IsClassOrphan(c))
                .ToList();
        }

        /// <summary>
        /// Filtre les classes avec plus de N lignes
        /// </summary>
        public static List<ClassDeclarationSyntax> FilterByLineCount(
            this IEnumerable<ClassDeclarationSyntax> classes,
            int greaterThan)
        {
            return classes
                .Where(c => c.GetText().Lines.Count > greaterThan)
                .ToList();
        }

        /// <summary>
        /// Filtre les classes avec un héritage profond
        /// </summary>
        public static List<ClassDeclarationSyntax> FilterByInheritanceDepth(
            this IEnumerable<ClassDeclarationSyntax> classes,
            int greaterThan)
        {
            return classes
                .Where(c => CalculateInheritanceDepth(c) > greaterThan)
                .ToList();
        }

        // ===== Méthodes d'aide privées =====

        private static int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
        {
            if (method.Body == null)
                return 1;

            int complexity = 1; // Base complexity

            // Compter les décisions
            var ifStatements = method.Body.DescendantNodes().OfType<IfStatementSyntax>().Count();
            var switchSections = method.Body.DescendantNodes().OfType<SwitchSectionSyntax>().Count();
            var forLoops = method.Body.DescendantNodes().OfType<ForStatementSyntax>().Count();
            var foreachLoops = method.Body.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
            var whileLoops = method.Body.DescendantNodes().OfType<WhileStatementSyntax>().Count();
            var doWhileLoops = method.Body.DescendantNodes().OfType<DoStatementSyntax>().Count();
            var catchClauses = method.Body.DescendantNodes().OfType<CatchClauseSyntax>().Count();

            complexity += ifStatements + switchSections + forLoops + foreachLoops + 
                         whileLoops + doWhileLoops + catchClauses;

            return complexity;
        }

        private static int CalculateInheritanceDepth(ClassDeclarationSyntax classDecl)
        {
            int depth = 0;
            var current = classDecl;

            while (current?.BaseList != null && current.BaseList.Types.Count > 0)
            {
                depth++;
                break;
            }

            return depth;
        }

        private static bool IsClassOrphan(ClassDeclarationSyntax classDecl)
        {
            return classDecl.Modifiers.Any(m => m.Kind() == SyntaxKind.InternalKeyword) &&
                   !IsClassInstantiatedOrInherited(classDecl);
        }

        private static bool IsClassInstantiatedOrInherited(ClassDeclarationSyntax classDecl)
        {
            var className = classDecl.Identifier.Text;
            var parent = classDecl.Parent;

            if (parent is CompilationUnitSyntax compilation)
            {
                return compilation.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Any(id => id.Identifier.Text == className);
            }

            return true;
        }

        private static bool HasUnusedParameters(MethodDeclarationSyntax method)
        {
            if (method.ParameterList.Parameters.Count == 0)
                return false;

            var body = method.Body;
            if (body == null)
                return false;

            var usedIdentifiers = body.DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .ToHashSet();

            return method.ParameterList.Parameters
                .Any(p => !usedIdentifiers.Contains(p.Identifier.Text));
        }
    }
}
