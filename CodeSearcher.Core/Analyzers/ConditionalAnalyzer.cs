using CodeSearcher.Core.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Core.Analyzers
{
    /// <summary>
    /// Analyseur pour trouver les conditions menant à une instruction
    /// </summary>
    public class ConditionalAnalyzer : IConditionalAnalyzer
    {
        /// <summary>
        /// Trouve tous les chemins conditionnels menant à une instruction
        /// </summary>
        public IEnumerable<ConditionPath> GetConditionsLeadingTo(StatementSyntax statement)
        {
            if (statement == null)
                throw new ArgumentNullException(nameof(statement));

            var conditions = new List<ConditionPath>();
            var currentNode = statement.Parent;
            var nestingLevel = 0;

            while (currentNode != null)
            {
                nestingLevel++;

                // Chercher les conditions if/else
                if (currentNode is IfStatementSyntax ifStmt)
                {
                    var isInElse = IsNodeInElseBranch(statement, ifStmt);
                    conditions.Insert(0, new ConditionPath
                    {
                        ConditionType = "if",
                        ConditionExpression = ifStmt.Condition.ToString(),
                        NestingLevel = nestingLevel,
                        IsNegated = isInElse
                    });
                }

                // Chercher les boucles for
                if (currentNode is ForStatementSyntax forStmt)
                {
                    var condition = forStmt.Condition?.ToString() ?? "no condition";
                    conditions.Insert(0, new ConditionPath
                    {
                        ConditionType = "for",
                        ConditionExpression = condition,
                        NestingLevel = nestingLevel,
                        IsNegated = false
                    });
                }

                // Chercher les boucles foreach
                if (currentNode is ForEachStatementSyntax foreachStmt)
                {
                    conditions.Insert(0, new ConditionPath
                    {
                        ConditionType = "foreach",
                        ConditionExpression = $"{foreachStmt.Type} in {foreachStmt.Expression}",
                        NestingLevel = nestingLevel,
                        IsNegated = false
                    });
                }

                // Chercher les boucles while
                if (currentNode is WhileStatementSyntax whileStmt)
                {
                    conditions.Insert(0, new ConditionPath
                    {
                        ConditionType = "while",
                        ConditionExpression = whileStmt.Condition.ToString(),
                        NestingLevel = nestingLevel,
                        IsNegated = false
                    });
                }

                // Chercher les do-while
                if (currentNode is DoStatementSyntax doStmt)
                {
                    conditions.Insert(0, new ConditionPath
                    {
                        ConditionType = "do-while",
                        ConditionExpression = doStmt.Condition.ToString(),
                        NestingLevel = nestingLevel,
                        IsNegated = false
                    });
                }

                // Chercher les switch
                if (currentNode is SwitchStatementSyntax switchStmt)
                {
                    conditions.Insert(0, new ConditionPath
                    {
                        ConditionType = "switch",
                        ConditionExpression = switchStmt.Expression.ToString(),
                        NestingLevel = nestingLevel,
                        IsNegated = false
                    });
                }

                currentNode = currentNode.Parent;
            }

            return conditions;
        }

        /// <summary>
        /// Trouve tous les chemins conditionnels dans une méthode
        /// </summary>
        public IEnumerable<(StatementSyntax statement, List<ConditionPath> conditions)> GetAllConditionalPaths(MethodDeclarationSyntax method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (method.Body == null)
                return Enumerable.Empty<(StatementSyntax, List<ConditionPath>)>();

            var results = new List<(StatementSyntax, List<ConditionPath>)>();
            var allStatements = method.Body.DescendantNodes().OfType<StatementSyntax>();

            foreach (var statement in allStatements)
            {
                // Filtrer les statements qui sont des blocs ou des déclarations simples
                if (statement is BlockSyntax || statement is MethodDeclarationSyntax)
                    continue;

                var conditions = GetConditionsLeadingTo(statement).ToList();
                results.Add((statement, conditions));
            }

            return results;
        }

        /// <summary>
        /// Détermine si une instruction est atteignable
        /// </summary>
        public bool IsStatementReachable(StatementSyntax statement)
        {
            if (statement == null)
                throw new ArgumentNullException(nameof(statement));

            var conditions = GetConditionsLeadingTo(statement).ToList();

            // Si pas de conditions, c'est directement atteignable
            if (conditions.Count == 0)
                return true;

            // Vérifier si toutes les conditions sont possibles
            return !HasImpossibleCondition(conditions);
        }

        /// <summary>
        /// Détermine si une instruction est toujours exécutée
        /// </summary>
        public bool IsStatementUnconditionallyReachable(StatementSyntax statement)
        {
            if (statement == null)
                throw new ArgumentNullException(nameof(statement));

            var conditions = GetConditionsLeadingTo(statement).ToList();

            // Si pas de conditions, c'est toujours exécuté
            if (conditions.Count == 0)
                return true;

            // L'instruction est inconditionnellement atteignable si pas de conditions parents
            return false;
        }

        /// <summary>
        /// Vérifie si le noeud est dans la branche else d'un if
        /// </summary>
        private bool IsNodeInElseBranch(SyntaxNode node, IfStatementSyntax ifStmt)
        {
            // Vérifier si le noeud est dans la déclaration then
            if (ifStmt.Statement.Contains(node))
                return false;

            // Vérifier si le noeud est dans la déclaration else
            if (ifStmt.Else != null)
            {
                var elseStatement = ifStmt.Else.Statement;
                if (elseStatement.Contains(node))
                    return true;

                // Si c'est un else if, vérifier récursivement
                if (elseStatement is IfStatementSyntax elseIfStmt)
                {
                    return IsNodeInElseBranch(node, elseIfStmt);
                }
            }

            return false;
        }

        /// <summary>
        /// Vérifie s'il existe une condition impossible
        /// (Actuellement simplifié - une vérification réelle nécessiterait une analyse de flux de contrôle complète)
        /// </summary>
        private bool HasImpossibleCondition(List<ConditionPath> conditions)
        {
            // Détection simple de contradictions
            var ifConditions = conditions.Where(c => c.ConditionType == "if").ToList();

            // Si on a if(x) et if(!x), c'est impossible
            for (int i = 0; i < ifConditions.Count - 1; i++)
            {
                for (int j = i + 1; j < ifConditions.Count; j++)
                {
                    var cond1 = ifConditions[i];
                    var cond2 = ifConditions[j];

                    // Vérification basique de contradiction
                    if (cond1.ConditionExpression == cond2.ConditionExpression &&
                        cond1.IsNegated != cond2.IsNegated)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
