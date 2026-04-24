using System.Linq.Expressions;

namespace Mpt.Rql.Abstractions;

/// <summary>
/// Translates RQL sub-paths that have no CLR counterpart (e.g. keys of a dynamic JSON bag)
/// into query expressions. Wired per-property via <c>[RqlProperty(CustomResolver = ...)]</c>.
/// </summary>
public interface IRqlCustomPropertyResolver
{
    /// <summary>
    /// Translates <paramref name="propertyPath"/> (single key or dotted for nested access)
    /// anchored at <paramref name="parentExpression"/> into a leaf expression. Returning
    /// <c>false</c> surfaces the standard "invalid property path" error.
    /// </summary>
    bool TryResolve(
        Expression parentExpression,
        string propertyPath,
        out Expression resolvedExpression,
        out IRqlPropertyInfo propertyInfo);
}
