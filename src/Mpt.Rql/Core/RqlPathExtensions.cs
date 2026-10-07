using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;

namespace Mpt.Rql.Core;

internal static class RqlPathExtensions
{
    /// <summary>
    /// The property path an argument names: a constant, or a constant within <c>self()</c>, which names the same
    /// property. <c>null</c> for anything else.
    /// </summary>
    public static RqlConstant? AsPath(this RqlExpression expression) => expression switch
    {
        RqlConstant constant => constant,
        RqlSelf { Inner: { } inner } => inner.AsPath(),
        _ => null,
    };
}
