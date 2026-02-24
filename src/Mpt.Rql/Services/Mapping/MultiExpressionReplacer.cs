using System.Linq.Expressions;

namespace Mpt.Rql.Services.Mapping;

/// <summary>
/// Replaces multiple expressions in a single tree walk using reference equality.
/// Expression does not override Equals/GetHashCode, so Dictionary uses reference equality by default.
/// </summary>
internal class MultiExpressionReplacer : ExpressionVisitor
{
    private readonly Dictionary<Expression, Expression> _replacements;

    public MultiExpressionReplacer(Dictionary<Expression, Expression> replacements)
        => _replacements = replacements;

    public override Expression Visit(Expression? node)
    {
        if (node != null && _replacements.TryGetValue(node, out var replacement))
            return replacement;

        return base.Visit(node)!;
    }
}
