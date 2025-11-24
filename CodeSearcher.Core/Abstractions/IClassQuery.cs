using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace CodeSearcher.Core.Abstractions
{
    /// <summary>
    /// Interface pour les requêtes de classes
    /// </summary>
    public interface IClassQuery : ICodeQuery<ClassDeclarationSyntax>
    {
        IClassQuery WithName(string className);
        IClassQuery WithNameContaining(string partialName);
        IClassQuery InNamespace(string namespaceName);
        IClassQuery WithAttribute(string attributeName);
        IClassQuery IsAbstract();
        IClassQuery IsSealed();
        IClassQuery IsPublic();
        IClassQuery Inherits(string baseClassName);
        IClassQuery Implements(string interfaceName);
        IClassQuery WithMemberCount(int count, Func<int, bool> predicate);
    }
}
