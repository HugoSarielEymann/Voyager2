using System.Collections.Generic;

namespace CodeSearcher.Core.Abstractions
{
    /// <summary>
    /// Interface générique pour les requêtes de code C#
    /// </summary>
    /// <typeparam name="T">Type d'entité retourné par la requête</typeparam>
    public interface ICodeQuery<out T>
    {
        /// <summary>
        /// Exécute la requête et retourne les résultats
        /// </summary>
        IEnumerable<T> Execute();

        /// <summary>
        /// Retourne le nombre de résultats
        /// </summary>
        int Count();

        /// <summary>
        /// Retourne le premier résultat ou null
        /// </summary>
        T FirstOrDefault();
    }
}
