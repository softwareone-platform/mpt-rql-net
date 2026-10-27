using System.Linq.Expressions;
using System.Reflection;
using Mpt.Rql.Settings;

namespace Mpt.Rql.Services.Mapping;

/// <summary>
/// Rejects unsupported mapping expressions that access scoped collections.
/// </summary>
internal static class CollectionScopeMappingValidator
{
    /// <summary>
    /// Validates a source expression before collection restrictions are applied.
    /// </summary>
    public static void Validate(
        Expression expression,
        ParameterExpression sourceParameter,
        IReadOnlyList<RqlCollectionScope> scopes,
        string targetProperty,
        bool allowDirectCollection = false,
        bool allowReferenceTraversal = false)
    {
        foreach (var scope in scopes)
        {
            var scopePath = GetPath(
                scope.CollectionSelector.Body,
                scope.CollectionSelector.Parameters[0]);

            if (scopePath is null)
            {
                throw new NotSupportedException(
                    "The collection scope must select a member path.");
            }

            var expressionPath = GetPath(expression, sourceParameter);

            if (expressionPath is not null)
            {
                if (allowDirectCollection &&
                    expressionPath.Length == scopePath.Length &&
                    IsPrefix(expressionPath, scopePath))
                {
                    continue;
                }

                if (allowReferenceTraversal &&
                    expressionPath.Length < scopePath.Length &&
                    IsPrefix(expressionPath, scopePath))
                {
                    continue;
                }
            }

            var visitor = new ScopedAccessVisitor(sourceParameter, scopePath);
            visitor.Visit(expression);

            if (visitor.HasScopedAccess)
            {
                throw new NotSupportedException(
                    $"Mapping '{targetProperty}' uses scoped collection " +
                    $"'{scope.CollectionSelector.Body}' in an unsupported expression. " +
                    "Map the collection directly. Computed values, collection " +
                    "operators, and static mappings of its containing object " +
                    "are not supported.");
            }
        }
    }

    private static MemberInfo[]? GetPath(
        Expression expression,
        ParameterExpression root)
    {
        var members = new List<MemberInfo>();
        var current = expression;

        while (current is MemberExpression member)
        {
            if (member.Expression is null)
                return null;

            members.Add(member.Member);
            current = member.Expression;
        }

        if (current != root)
            return null;

        members.Reverse();
        return members.ToArray();
    }

    private static bool IsPrefix(
        IReadOnlyList<MemberInfo> prefix,
        IReadOnlyList<MemberInfo> path)
    {
        if (prefix.Count > path.Count)
            return false;

        for (var index = 0; index < prefix.Count; index++)
        {
            if (!prefix[index].Equals(path[index]))
                return false;
        }

        return true;
    }

    private sealed class ScopedAccessVisitor(
        ParameterExpression sourceParameter,
        MemberInfo[] scopePath) : ExpressionVisitor
    {
        public bool HasScopedAccess { get; private set; }

        public override Expression? Visit(Expression? node)
        {
            return HasScopedAccess ? node : base.Visit(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var path = GetPath(node, sourceParameter);

            if (path is null)
                return base.VisitMember(node);

            HasScopedAccess =
                IsPrefix(scopePath, path) ||
                IsPrefix(path, scopePath);

            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == sourceParameter)
                HasScopedAccess = true;

            return node;
        }
    }
}