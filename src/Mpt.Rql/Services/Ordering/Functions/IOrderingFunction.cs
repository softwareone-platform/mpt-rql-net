using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// An ordering function usable in RQL order strings as <c>+name(arg1,arg2,...)</c>.
/// Implementations are registered in DI as <see cref="IOrderingFunction"/> and resolved by name
/// through <see cref="OrderingFunctionRegistry"/>.
/// </summary>
internal interface IOrderingFunction
{
    /// <summary>Function name as written in the order string (matched case-insensitively).</summary>
    string Name { get; }

    /// <summary>
    /// Builds the sort-key expression for one root entity. Any problem with the arguments must be
    /// reported as validation errors in the result, never thrown.
    /// </summary>
    Result<Expression> Build(OrderingFunctionContext context);
}
