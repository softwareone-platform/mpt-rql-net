using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Abstractions.Unary
{
    [Obsolete]
    public class RqlIsNull : RqlUnary
    {
        internal RqlIsNull(RqlExpression expression) : base(expression)
        {
        }
    }
}
