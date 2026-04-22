using System.Linq.Expressions;

namespace Mpt.Rql.Abstractions;

/// <summary>
/// Resolves property access on types that don't have standard CLR properties.
/// When one or more <see cref="IRqlCustomPropertyResolver"/> implementations are registered,
/// <c>PathInfoBuilder</c> tries each in order as a fallback when CLR reflection fails to find a property.
/// Each resolver decides whether it handles the given parent expression and property name.
/// </summary>
public interface IRqlCustomPropertyResolver
{
    /// <summary>
    /// Attempts to resolve a property name on the given parent expression.
    /// </summary>
    /// <param name="parentExpression">The expression representing the parent (e.g. <c>e.Attributes.Navision</c>).</param>
    /// <param name="propertyName">The property name to resolve (e.g. <c>ipCaseNo</c>).</param>
    /// <param name="resolvedExpression">The resulting expression (e.g. a <c>JSON_VALUE</c> call).</param>
    /// <param name="propertyInfo">A synthetic property descriptor defining allowed operators and actions.</param>
    /// <returns><c>true</c> if the property was resolved; <c>false</c> to try the next resolver or fall through to the default error.</returns>
    bool TryResolve(
        Expression parentExpression,
        string propertyName,
        out Expression resolvedExpression,
        out IRqlPropertyInfo propertyInfo);
}
