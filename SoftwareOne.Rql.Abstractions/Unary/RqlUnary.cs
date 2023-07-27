using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Abstractions.Unary
{
    public abstract class RqlUnary : RqlExpression
    {
        private readonly RqlExpression _expression;

        internal RqlUnary(RqlExpression expression)
        {
            _expression = expression;
        }

        public RqlExpression Nested => _expression;
    }
}
