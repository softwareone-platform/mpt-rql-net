using System.Linq.Expressions;
using Mpt.Rql.Settings;
using static Mpt.Rql.Core.CollectionQueryHelper;

namespace Mpt.Rql.Services.Filtering;

/// <summary>
/// Builds restricted collection sources from per-transformation rules.
/// </summary>
internal static class CollectionScopeExpressionBuilder
{
    /// <summary>
    /// Applies all restrictions matching a collection rooted in the source parameter.
    /// </summary>
    public static Expression Apply(
        Expression collection,
        ParameterExpression sourceParameter,
        IReadOnlyList<RqlCollectionScope> scopes)
    {
        var restrictedCollection = collection;

        foreach (var scope in scopes)
        {
            var selector = scope.CollectionSelector;

            if (!MatchesPath(
                    collection,
                    selector.Body,
                    sourceParameter,
                    selector.Parameters[0]))
            {
                continue;
            }

            var elementType = scope.Predicate.Parameters[0].Type;

            restrictedCollection = Expression.Call(
                EnumerableWhere.MakeGenericMethod(elementType),
                restrictedCollection,
                scope.Predicate);
        }

        return restrictedCollection;
    }

    private static bool MatchesPath(
        Expression actual,
        Expression expected,
        ParameterExpression actualRoot,
        ParameterExpression expectedRoot)
    {
        while (actual is MemberExpression actualMember &&
               expected is MemberExpression expectedMember)
        {
            if (!actualMember.Member.Equals(expectedMember.Member))
                return false;

            if (actualMember.Expression is null ||
                expectedMember.Expression is null)
            {
                return false;
            }

            actual = actualMember.Expression;
            expected = expectedMember.Expression;
        }

        return actual == actualRoot && expected == expectedRoot;
    }
}