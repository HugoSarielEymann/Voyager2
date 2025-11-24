using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace CodeSearcher.Editor.Abstractions
{
    /// <summary>
    /// Représente le résultat d'une opération d'édition
    /// </summary>
    public class EditResult
    {
        public bool Success { get; set; }
        public string? ModifiedCode { get; set; }
        public List<string> Changes { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Interface pour les opérations d'édition de code
    /// </summary>
    public interface IEditOperation
    {
        /// <summary>
        /// Exécute l'opération d'édition
        /// </summary>
        EditResult Execute(string code);

        /// <summary>
        /// Description de l'opération
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Interface pour les modificateurs de code
    /// </summary>
    public interface ICodeModifier
    {
        /// <summary>
        /// Ajoute une opération d'édition
        /// </summary>
        ICodeModifier AddOperation(IEditOperation operation);

        /// <summary>
        /// Exécute toutes les opérations
        /// </summary>
        EditResult Apply(string? code);

        /// <summary>
        /// Réinitialise les opérations
        /// </summary>
        ICodeModifier Clear();
    }

    /// <summary>
    /// Interface pour les stratégies de renommage
    /// </summary>
    public interface IRenameStrategy
    {
        /// <summary>
        /// Renomme une entité dans le code
        /// </summary>
        EditResult Rename(string code, string oldName, string newName, string entityType);
    }

    /// <summary>
    /// Interface pour les stratégies de wrapper
    /// </summary>
    public interface IWrapperStrategy
    {
        /// <summary>
        /// Wrapper du code avec une logique supplémentaire
        /// </summary>
        EditResult Wrap(string code, string methodName, string wrapperType, string wrapperCode);
    }
}
