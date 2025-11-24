using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace CodeSearcher.Core.Abstractions
{
    /// <summary>
    /// Représente une condition menant à une instruction
    /// </summary>
    public class ConditionPath
    {
        public string ConditionType { get; set; } // "if", "for", "foreach", "while", "switch"
        public string ConditionExpression { get; set; }
        public int NestingLevel { get; set; }
        public bool IsNegated { get; set; } // Pour les else
        public string Description => $"{ConditionType}({ConditionExpression})";
    }

    /// <summary>
    /// Interface pour trouver les conditions menant à une instruction
    /// </summary>
    public interface IConditionalAnalyzer
    {
        /// <summary>
        /// Trouve tous les chemins conditionnels (conditions) menant à une instruction donnée
        /// Par exemple, pour var x = 3 imbriqué dans if/for/if, retourne les 3 conditions
        /// </summary>
        IEnumerable<ConditionPath> GetConditionsLeadingTo(StatementSyntax statement);

        /// <summary>
        /// Trouve tous les chemins conditionnels dans une méthode donnée
        /// </summary>
        IEnumerable<(StatementSyntax statement, List<ConditionPath> conditions)> GetAllConditionalPaths(MethodDeclarationSyntax method);

        /// <summary>
        /// Détermine si une instruction est atteignable (reachable)
        /// </summary>
        bool IsStatementReachable(StatementSyntax statement);

        /// <summary>
        /// Détermine si une instruction est toujours exécutée (unconditionally reachable)
        /// </summary>
        bool IsStatementUnconditionallyReachable(StatementSyntax statement);
    }
}
