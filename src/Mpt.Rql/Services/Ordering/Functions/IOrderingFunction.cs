using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using System.Linq.Expressions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// An ordering function usable in RQL order strings as <c>+name(arg1,arg2,...).path</c>: the call
/// evaluates to an element, the trailing member path selects the sort key from it.
/// Implementations are registered in DI as <see cref="IOrderingFunction"/> and resolved by name
/// through <see cref="OrderingFunctionRegistry"/>.
/// </summary>
/// <remarks>
/// Functions and the registry are <b>singletons</b>: an implementation must be stateless and take
/// everything request-specific from <see cref="OrderingFunctionContext"/>, never from constructor-injected
/// scoped services.
/// </remarks>
internal interface IOrderingFunction
{
    /// <summary>Function name as written in the order string (matched case-insensitively).</summary>
    string Name { get; }

    /// <summary>
    /// Adds to the projection graph every node the key will read, so mapping projects those columns.
    /// Runs before <see cref="Build"/>; malformed input should simply add nothing — <see cref="Build"/>
    /// reports the error.
    /// </summary>
    /// <param name="memberPath">The dotted path after the call (<c>.value</c>), or <c>null</c> when absent.</param>
    void IncludeInGraph(IOrderingFunctionGraph graph, RqlNode target, IReadOnlyList<RqlExpression> arguments, string? memberPath);

    /// <summary>
    /// Builds the sort-key expression for one root entity. Any problem with the arguments must be
    /// reported as validation errors in the result, never thrown.
    /// </summary>
    Result<Expression> Build(OrderingFunctionContext context);
}
