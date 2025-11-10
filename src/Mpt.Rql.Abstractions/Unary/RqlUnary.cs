using Mpt.Rql.Abstractions;

namespace Mpt.Rql.Abstractions.Unary;

public abstract class RqlUnary : RqlExpression
{
    private readonly RqlExpression _expression;

    private protected RqlUnary(RqlExpression expression)
    {
        _expression = expression;
    }

    public RqlExpression Nested => _expression;
}
