using System.Linq.Expressions;
using static Mpt.Rql.Core.CollectionQueryHelper;

namespace Mpt.Rql.Services.Filtering;

/// <summary>
/// Detects Enumerable.Any and Enumerable.All calls within an expression tree.
/// </summary>
internal sealed class CollectionQuantifierDetector : ExpressionVisitor
{
    private bool _containsQuantifier;

    public static bool ContainsQuantifier(Expression expression)
    {
        var detector = new CollectionQuantifierDetector();
        detector.Visit(expression);

        return detector._containsQuantifier;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsQuantifier(node))
            _containsQuantifier = true;

        return base.VisitMethodCall(node);
    }
}