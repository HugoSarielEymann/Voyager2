using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace CodeSearcher.Core.Abstractions
{
    /// <summary>
    /// Interface principale pour l'accès au code à requêter
    /// </summary>
    public interface ICodeContext
    {
        /// <summary>
        /// Crée une requête pour chercher des méthodes
        /// </summary>
        IMethodQuery FindMethods();

        /// <summary>
        /// Crée une requête pour chercher des classes
        /// </summary>
        IClassQuery FindClasses();

        /// <summary>
        /// Crée une requête pour chercher des variables/champs/propriétés
        /// </summary>
        Queries.VariableQuery FindVariables();

        /// <summary>
        /// Crée une requête pour chercher des return statements
        /// </summary>
        IReturnQuery FindReturns();

        /// <summary>
        /// Retourne toutes les entités SyntaxNode satisfaisant un prédicat
        /// </summary>
        IEnumerable<SyntaxNode> FindByPredicate(Func<SyntaxNode, bool> predicate);
    }
}
