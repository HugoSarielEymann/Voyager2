using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace CodeSearcher.Core.Abstractions
{
    /// <summary>
    /// Interface pour les requêtes de return statements
    /// </summary>
    public interface IReturnQuery : ICodeQuery<ReturnStatementSyntax>
    {
        IReturnQuery InMethod(string methodName);
        IReturnQuery ReturningType(string typeName);
        IReturnQuery ReturningNull();
        IReturnQuery WithExpression(Func<ExpressionSyntax, bool> predicate);
    }
}
