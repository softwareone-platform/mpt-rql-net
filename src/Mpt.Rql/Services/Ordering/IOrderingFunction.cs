using Mpt.Rql.Core;
using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace Mpt.Rql;

/// <summary>
/// Defines a custom ordering function usable in RQL sort expressions.
/// </summary>
/// <remarks>
/// Implement this interface and register it with the DI container as
/// <see cref="IOrderingFunction"/> to add a custom function to the sort parser.
/// <para>
/// Functions are referenced in RQL order expressions as:
/// <c>+functionName(arg1,arg2,...)</c>
/// </para>
/// <para>
/// The built-in <see cref="Mpt.Rql.Services.Ordering.Functions.OrderByOrderingFunction"/> uses this contract:
/// <c>+orderby(collectionProperty,filterPropertyName,filterValue,resultPropertyName)</c>
/// </para>
/// </remarks>
public interface IOrderingFunction
{
    /// <summary>The function name as it appears in the sort expression (case-insensitive).</summary>
    string FunctionName { get; }

    /// <summary>
    /// Builds a LINQ expression that produces the sort key value for a given root entity.
    /// </summary>
    /// <param name="root">Parameter expression representing the root entity.</param>
    /// <param name="arguments">Ordered list of string arguments from the sort expression.</param>
    /// <returns>A <see cref="Result{T}"/> containing the key expression or validation errors.</returns>
    Result<Expression> Build(Expression root, IReadOnlyList<string> arguments);
}
