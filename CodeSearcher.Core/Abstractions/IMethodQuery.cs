using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace CodeSearcher.Core.Abstractions
{
    /// <summary>
    /// Interface pour les requêtes de méthodes
    /// </summary>
    public interface IMethodQuery : ICodeQuery<MethodDeclarationSyntax>
    {
        IMethodQuery WithName(string methodName);
        IMethodQuery WithNameContaining(string partialName);
        IMethodQuery ReturningTask();
        IMethodQuery ReturningTask<T>();
        IMethodQuery ReturningType(string typeName);
        IMethodQuery IsAsync();
        IMethodQuery IsPublic();
        IMethodQuery IsPrivate();
        IMethodQuery IsProtected();
        IMethodQuery HasParameterCount(int count);
        IMethodQuery HasParameter(Func<ParameterSyntax, bool> predicate);
        IMethodQuery WithAttribute(string attributeName);
    }
}
