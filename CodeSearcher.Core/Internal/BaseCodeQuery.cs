using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeSearcher.Core.Internal
{
    /// <summary>
    /// Classe de base abstraite pour toutes les requêtes de code
    /// </summary>
    /// <typeparam name="T">Type d'entité retourné par la requête</typeparam>
    public abstract class BaseCodeQuery<T> where T : SyntaxNode
    {
        protected CompilationUnitSyntax Root { get; }
        protected List<Func<T, bool>> Predicates { get; }

        protected BaseCodeQuery(CompilationUnitSyntax root)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Predicates = new List<Func<T, bool>>();
        }

        /// <summary>
        /// Retourne tous les nœuds du type T satisfaisant les prédicats
        /// </summary>
        protected virtual IEnumerable<T> GetMatches()
        {
            var allNodes = Root.DescendantNodes().OfType<T>();
            
            foreach (var predicate in Predicates)
            {
                allNodes = allNodes.Where(predicate);
            }

            return allNodes;
        }

        public virtual IEnumerable<T> Execute() => GetMatches();

        public virtual int Count() => GetMatches().Count();

        public virtual T FirstOrDefault() => GetMatches().FirstOrDefault();
    }
}
