using Mpt.Rql.Abstractions;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Graph operations an <see cref="IOrderingFunction"/> uses to declare the columns its key reads,
/// so the projection materializes them when mapping is enabled. Implemented by the ordering graph builder.
/// </summary>
internal interface IOrderingFunctionGraph
{
    /// <summary>Includes a (possibly dotted) collection path as hierarchy and returns the collection node, or <c>null</c> if it does not resolve.</summary>
    RqlNode? IncludeHierarchy(RqlNode target, RqlExpression path);

    /// <summary>Traverses a filter predicate under <paramref name="target"/> exactly as <c>any()</c> does (Filter reason, Filter permission).</summary>
    void TraversePredicate(RqlNode target, RqlExpression predicate);

    /// <summary>Includes a (possibly dotted) path under <paramref name="target"/> with the Order reason.</summary>
    void IncludeOrderPath(RqlNode target, RqlExpression path);
}
