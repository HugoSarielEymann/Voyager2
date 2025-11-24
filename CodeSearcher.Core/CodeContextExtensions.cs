using CodeSearcher.Core;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeSearcher.Core
{
    /// <summary>
    /// Extensions pour CodeContext
    /// </summary>
    public static class CodeContextExtensions
    {
        /// <summary>
        /// Récupère la racine du CompilationUnitSyntax
        /// </summary>
        public static CompilationUnitSyntax GetRoot(this CodeContext context)
        {
            // Utiliser la réflexion pour accéder à la propriété privée _root
            var rootProperty = context.GetType()
                .GetProperty("_root", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
            
            return rootProperty?.GetValue(context) as CompilationUnitSyntax;
        }
    }
}
